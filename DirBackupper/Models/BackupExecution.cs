using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DirBackupper.Utils;
using System.Linq;
using System.Threading;
using System.IO.Compression;

namespace DirBackupper.Models
{
	public class BackupExecution : BindableBase
	{
		private string _sourceDirectoryPath = string.Empty;
		public string SourceDirectoryPath
		{
			get => _sourceDirectoryPath;
			set => SetProperty( ref _sourceDirectoryPath, value );
		}

		private string _destinationFilePath = string.Empty;
		public string DestinationFilePath
		{
			get => _destinationFilePath;
			set => SetProperty( ref _destinationFilePath, value );
		}

		private string _temporaryFilePath = Path.GetFullPath( @".\\Stockpile\\" );
		public string TemporaryFilePath
		{
			get => _temporaryFilePath;
			set => SetProperty( ref _temporaryFilePath, value );
		}

		private string _progressMessage = "...";
		public string ProgressMessage
		{
			get => _progressMessage;
			private set => SetProperty( ref _progressMessage, value );
		}

		private float _progressRatio = 0f;
		public string ProgressRatio
		{
			get => $"{Math.Floor( _progressRatio ):#}%";
			private set => SetProperty( ref _progressRatio, float.Parse( value ) );
		}

		private bool _allowOverwrite = true;
		public bool AllowOverwrite
		{
			get => _allowOverwrite;
			set
			{
				SetProperty( ref _allowOverwrite, value );
				_backup.AllowOverwrite = value;
				_restore.AllowOverwrite = value;
			}
		}

		private uint _bufferLengthMByte = 256;
		public uint BufferLengthMByte
		{
			get => _bufferLengthMByte;
			set => SetProperty( ref _bufferLengthMByte, value );
		}

		public uint RealBufferLengthByte { get => _bufferLengthMByte * 1024 * 1024; }

		private bool _isCompress = true;
		public bool IsCompress
		{
			get => _isCompress;
			set => SetProperty( ref _isCompress, value );
		}

		private Modules.Backup _backup = new Modules.Backup( true );
		private Modules.Restore _restore = new Modules.Restore( true );
		private CancellationTokenSource _backupCancellation;
		private CancellationTokenSource _restoreCancellation;
		private CancellationTokenSource _dummyCancellation;

		public void Abort()
		{
			try
			{
				_backup.CancelExecute();
				_restore.CancelExecute();
				_backupCancellation?.Cancel();
				_restoreCancellation?.Cancel();
				_dummyCancellation?.Cancel();
			}
			catch { } // DO NOTHING WHEN TASK CANCELLED
		}

		public async Task<Modules.TaskDoneStatus> ExecuteBackup(IProgress<Modules.ProgressInfo> progress)
		{
			_backup.BufferLength = RealBufferLengthByte;

			var sourcePath = SourceDirectoryPath.AddDirectoryIdentify();
			var backupPath = DestinationFilePath.AddDirectoryIdentify();

			using ( _backupCancellation = new CancellationTokenSource() )
			{
				var result = await _backup.Execute( progress, sourcePath, backupPath );
				if ( result == Modules.TaskDoneStatus.Completed && IsCompress )
				{
					progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 1, 1 ), "Compressing backup directory..." ) );
					var backupZipPath = ( backupPath.Last() == '\\' ? backupPath.Substring( 0, backupPath.Length - 1 ) : backupPath ) + ".zip";
					if ( File.Exists( backupZipPath ) ) File.Delete( backupZipPath );
					await Task.Run( () => ZipFile.CreateFromDirectory( backupPath, backupZipPath ) );
				}
				await Task.Run( () => Tools.DestructDirectory( backupPath ) );

				progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 0, 1 ), string.Empty ) );
				return result;
			}
		}

		public async Task<Modules.TaskDoneStatus> ExecuteRestore(IProgress<Modules.ProgressInfo> progress)
		{
			_restore.TemporaryPath = TemporaryFilePath;
			_restore.BufferLength = RealBufferLengthByte;

			var restorePath = SourceDirectoryPath.AddDirectoryIdentify();
			var backupPath = DestinationFilePath.AddDirectoryIdentify();

			if ( IsCompress )
			{
				var backupZipPath = (backupPath.Last() == '\\' ? backupPath.Substring(0, backupPath.Length - 1) : backupPath) + ".zip";
				if ( !File.Exists( backupZipPath ) )
				{
					progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 0, 1 ), "Backup data not found..." ) );
					return Modules.TaskDoneStatus.Failed;
				}
				progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 1, 1 ), "Decompressing backup directory..." ) );
				await Task.Run( () => ZipFile.ExtractToDirectory( backupZipPath, backupPath ) );
			}

			using ( _restoreCancellation = new CancellationTokenSource() )
			{
				var backupResult = await _restore.BackupToTemporary( restorePath );
				if ( backupResult != Modules.TaskDoneStatus.Completed )
				{
					progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 0, 1 ), "Failed to create temporary directory." ) );
					return backupResult;
				}

				var result = await _backup.Execute( progress, backupPath, restorePath );
				if ( result != Modules.TaskDoneStatus.Completed )
				{
					progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 0, 1 ), "Failed to restore." ) );
				}
				else
				{
					progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 0, 1 ), string.Empty ) );
				}
				return result;
			}
		}

		public async Task DummyExecute(IProgress<Modules.ProgressInfo> progress)
		{
			using ( _dummyCancellation = new CancellationTokenSource() )
			{
				var max = 50;
				await Task.Run( async () =>
				{
					foreach ( var count in Enumerable.Range( 0, max + 1 ) )
					{
						if ( _dummyCancellation.IsCancellationRequested ) return;
						var info = new Modules.ProgressInfo( new KeyValuePair<int, int>( count, max ), "Processing" + new string( '.', ( count % 3 ) + 1 ) );
						progress.Report( info );
						await Task.Delay( 100 );
					}
					progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( max, max ), "Completed!" ) );
				}, _dummyCancellation.Token );
			}
		}
	}
}
