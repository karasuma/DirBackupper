using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DirBackupper.Utils;
using System.Linq;
using System.Threading;
using System.IO.Compression;
using System.Collections.ObjectModel;

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
		private bool _compressInDest = false;
		public bool CompressInDest
		{
			get => _compressInDest;
			set => SetProperty( ref _compressInDest, value );
		}

		public ObservableCollection<string> IgnoreList { get; } = new ObservableCollection<string>();

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

			// Backup
			Toast.Pop( "Backup started.", $"Backup operation execute ({DateTime.Now:yyyy/MM/dd HH:mm})", $"from: {sourcePath}", $"dest: {backupPath}" );
			var result = await _backup.Execute( progress, sourcePath, backupPath, IgnoreList );

			// Compress
			if ( result == Modules.TaskDoneStatus.Completed && IsCompress )
			{
				progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 1, 1 ), "Compressing backup directory..." ) );
				var backupZipPath = ( backupPath.Last() == '\\' ? backupPath.Substring( 0, backupPath.Length - 1 ) : backupPath ) + ".zip";
				if ( CompressInDest )
				{
					var zipname = backupZipPath.Substring(backupZipPath.LastIndexOf('\\') + 1, backupZipPath.Length - backupZipPath.LastIndexOf('\\') - 1);
					backupZipPath = backupZipPath.Replace( zipname, zipname.Replace( ".zip", string.Empty ) + "\\" + zipname );
				}

				if ( File.Exists( backupZipPath ) ) File.Delete( backupZipPath );
				try
				{
					using ( _backupCancellation = new CancellationTokenSource() )
					{
						await Task.Run( async () =>
						 {
							 if ( CompressInDest )
							 {
								 if ( !Directory.Exists( TemporaryFilePath ) ) Directory.CreateDirectory( TemporaryFilePath );
								 var zipname = backupZipPath.Substring(backupZipPath.LastIndexOf('\\') + 1, backupZipPath.Length - backupZipPath.LastIndexOf('\\') - 1);
								 if ( File.Exists( TemporaryFilePath.AddDirectoryIdentify() + zipname ) )
									 File.Delete( TemporaryFilePath.AddDirectoryIdentify() + zipname );
								 ZipFile.CreateFromDirectory( backupPath, TemporaryFilePath.AddDirectoryIdentify() + zipname );
								 Tools.DestructDirectory( backupPath, dontRemoveRoot: true );
								 await Tools.CopyFileStrictlyAsync( TemporaryFilePath.AddDirectoryIdentify() + zipname, backupZipPath, RealBufferLengthByte, _backupCancellation.Token );
								 Tools.DestructDirectory( TemporaryFilePath );
							 }
							 else
							 {
								 ZipFile.CreateFromDirectory( backupPath, backupZipPath );
								 Tools.DestructDirectory( backupPath );
							 }
						 }, _backupCancellation.Token );
					}
				}
				catch ( TaskCanceledException ) { } // DO NOTHING WHEN TASK CANCELLED
				catch ( Exception ex )
				{
					Logger.Log( $"Compression failed.{Tools.NewLine}{ex}", Logger.LogStates.Error );
					result = Modules.TaskDoneStatus.Failed;
				}
			}

			if ( result != Modules.TaskDoneStatus.Completed )
			{
				Toast.Pop( "Backup failed...", $"Backup operation {result} ({DateTime.Now:yyyy/MM/dd HH:mm})" );
				return result;
			}

			Toast.Pop( "Backup done!", $"Backup operation {result} ({DateTime.Now:yyyy/MM/dd HH:mm})" );

			progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 0, 0 ), string.Empty ) );
			return result;
		}

		public async Task<Modules.TaskDoneStatus> ExecuteRestore(IProgress<Modules.ProgressInfo> progress)
		{
			_restore.TemporaryPath = TemporaryFilePath;
			_restore.BufferLength = RealBufferLengthByte;

			var restorePath = SourceDirectoryPath.AddDirectoryIdentify();
			var backupPath = DestinationFilePath.AddDirectoryIdentify();

			Toast.Pop( "Processing...", $"Restore operation execute ({DateTime.Now:yyyy/MM/dd HH:mm})" );

			// Decompress
			if ( IsCompress )
			{
				progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 1, 1 ), "Decompressing backup directory..." ) );
				var backupZipPath = (backupPath.Last() == '\\' ? backupPath.Substring(0, backupPath.Length - 1) : backupPath) + ".zip";
				if ( CompressInDest )
				{
					var zipname = backupZipPath.Substring(backupZipPath.LastIndexOf('\\') + 1, backupZipPath.Length - backupZipPath.LastIndexOf('\\') - 1);
					backupZipPath = backupZipPath.Replace( zipname, zipname.Replace( ".zip", string.Empty ) + "\\" + zipname );
				}
				if ( !File.Exists( backupZipPath ) )
				{
					progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 0, 1 ), "Backup data not found..." ) );
					return Modules.TaskDoneStatus.Failed;
				}
				using ( _restoreCancellation = new CancellationTokenSource() )
				{
					try
					{
						await Task.Run( () =>
						{
							if ( CompressInDest )
							{
								backupPath = backupPath.AddDirectoryIdentify() + $"Stockpile_{Guid.NewGuid().ToString( "N" ).Substring( 0, 8 )}\\";
								Directory.CreateDirectory( backupPath );
							}
							else
							{
								Tools.DestructDirectory( backupPath, dontRemoveRoot: false );
							}
							Tools.ExtractToDirectoryEx( backupZipPath, backupPath, false );

						}, _restoreCancellation.Token );
					}
					catch ( TaskCanceledException )
					{
						Tools.DestructDirectory( backupPath );
						Toast.Pop( "Restore cancelled", $"Restore operation Cancelled ({DateTime.Now:yyyy/MM/dd HH:mm})" );
						return Modules.TaskDoneStatus.Cancelled;
					}
					catch ( Exception ex )
					{
						Logger.Log( $"Decompression failed.{Tools.NewLine}{ex}", Logger.LogStates.Error );
						return Modules.TaskDoneStatus.Failed;
					}
				}
			}

			// Restore
			var restoreResult = await _restore.BackupToTemporary( restorePath );
			if ( restoreResult != Modules.TaskDoneStatus.Completed )
			{
				progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 0, 1 ), "Failed to create temporary directory." ) );
				return restoreResult;
			}

			var result = await _backup.Execute( progress, backupPath, restorePath, IgnoreList );
			if ( result != Modules.TaskDoneStatus.Completed )
			{
				// Recovery from temporary to restore target
				var recoveryResult = await _restore.Recovery( restorePath );
				progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 0, 1 ), "Failed to restore." ) );
				if ( recoveryResult != Modules.TaskDoneStatus.Completed )
				{
					Toast.Pop( "Restore failed", "Sorry, recovery operation from temporary to restore target failed.", "Some your important files may broke..." );
					return recoveryResult;
				}
				Toast.Pop( "Restore failed", $"Restore operation {result} ({DateTime.Now:yyyy/MM/dd HH:mm})" );
				return recoveryResult;
			}

			if ( IsCompress )
				await Task.Run( () => Tools.DestructDirectory( backupPath ) );
			progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 0, 1 ), string.Empty ) );
			Toast.Pop( "Done!", $"Restore operation {result} ({DateTime.Now:yyyy/MM/dd HH:mm})" );
			return result;
		}

		public async Task DummyExecute(IProgress<Modules.ProgressInfo> progress)
		{
			using ( _dummyCancellation = new CancellationTokenSource() )
			{
				var max = 50;
				try
				{
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
				catch ( TaskCanceledException ) { } // DO NOTHING WHEN TASK CANCELLED
			}
		}

		public bool CheckSourceDirectoryExists()
			=> Directory.Exists( SourceDirectoryPath ) && SourceDirectoryPath.IndexOfAny( Path.GetInvalidPathChars() ) == -1;
	}
}
