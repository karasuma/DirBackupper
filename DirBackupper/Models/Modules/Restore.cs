using DirBackupper.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DirBackupper.Models.Modules
{
	public class Restore
	{
		public Restore(bool overwrite, string tempPath = "")
		{
			AllowOverwrite = overwrite;
			TemporaryPath = string.IsNullOrEmpty( tempPath ) ? Path.GetFullPath( ".\\Stockpile\\" ) : tempPath;
			_backup = new Backup( overwrite );
		}

		private Backup _backup = null;

		public bool AllowOverwrite { get; set; } = false;

		public string TemporaryPath { get; set; } = string.Empty;

		public bool IsRestoring { get; private set; } = false;

		public uint BufferLength { get; set; } = 0;

		public void CancelExecute()
		{
			if ( IsRestoring )
				throw new InvalidOperationException( "Restore operation can not cancel!" );
		}

		public async Task<TaskDoneStatus> BackupToTemporary(string sourceDir)
		{
			try
			{
				Tools.DestructDirectory( TemporaryPath );
				_backup.AllowOverwrite = AllowOverwrite;
				return await _backup.Execute( null, sourceDir, TemporaryPath );
			}
			catch ( TaskCanceledException )
			{
				return TaskDoneStatus.Cancelled;
			}
			catch ( Exception unknownex )
			{
				Logger.Log( $"Temporary files creation failed.{Tools.NewLine}{unknownex}", Logger.LogStates.Error );
				return TaskDoneStatus.Failed;
			}
		}

		public async Task<TaskDoneStatus> Recovery(string restoreDir)
		{
			if ( !Directory.Exists( TemporaryPath ) )
			{
				Logger.Log( $"Recovery failed. (Temporary path: {TemporaryPath})", Logger.LogStates.Error );
				return TaskDoneStatus.Failed;
			}

			_backup.AllowOverwrite = AllowOverwrite;
			return await _backup.Execute( null, TemporaryPath, restoreDir );
		}
	}
}
