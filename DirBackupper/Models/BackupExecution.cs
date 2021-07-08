using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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

		private Modules.Backup _backup = new Modules.Backup( true );
		private Modules.Restore _restore = new Modules.Restore( true );

		public void Abort() => _backup.CancelExecute();

		public async Task<Modules.TaskDoneStatus> ExecuteBackup(IProgress<Modules.ProgressInfo> progress)
		{
			await _restore.BackupToTemporary( SourceDirectoryPath );
			var backupResult = await _backup.Execute( progress, SourceDirectoryPath, DestinationFilePath );
			if ( backupResult == Modules.TaskDoneStatus.Cancelled )
			{
				progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 0, 1 ), "Restoring..." ) );
				backupResult = await _restore.RestoreExecute( SourceDirectoryPath );
			}
			return backupResult;
		}

		public async Task<Modules.TaskDoneStatus> ExecuteRestore(IProgress<Modules.ProgressInfo> progress)
		{
			var backupResult = await _backup.Execute( progress, DestinationFilePath, SourceDirectoryPath );
			if ( backupResult != Modules.TaskDoneStatus.Completed )
			{
				progress.Report( new Modules.ProgressInfo( new KeyValuePair<int, int>( 0, 1 ), "Failed to restore." ) );
			}
			return backupResult;
		}
	}
}
