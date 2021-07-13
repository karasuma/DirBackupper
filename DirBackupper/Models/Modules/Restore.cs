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
	public class Restore : IBackupTask
	{
		public Restore(bool overwrite, string tempPath = "")
		{
			AllowOverwrite = overwrite;
			TemporaryPath = string.IsNullOrEmpty( tempPath ) ? Path.GetFullPath( ".\\Stockpile\\" ) : tempPath;
		}

		private CancellationTokenSource _executeCancellation = null;
		private CancellationTokenSource _backupCancellation = null;

		public bool AllowOverwrite { get; set; } = false;

		public string TemporaryPath { get; set; } = string.Empty;

		public bool IsRestoring { get; private set; } = false;

		public uint BufferLength { get; set; } = 0;

		public void CancelExecute()
		{
			if ( IsRestoring )
				throw new InvalidOperationException( "Restore operation can not cancel!" );
			_executeCancellation?.Cancel();
			_backupCancellation?.Cancel();
		}

		public async Task<TaskDoneStatus> BackupToTemporary(string sourceDir)
		{
			try
			{
				Tools.DestructDirectory( TemporaryPath );
				return await Execute( null, sourceDir, TemporaryPath );
			}
			catch ( OperationCanceledException )
			{
				return TaskDoneStatus.Cancelled;
			}
			catch ( Exception unknownex )
			{
				Logger.Log( $"Temporary files creation failed.{Tools.NewLine}{unknownex}", Logger.LogStates.Error );
				return TaskDoneStatus.Failed;
			}
		}

		public async Task<TaskDoneStatus> RestoreExecute(string restoreDir) => await Execute( null, TemporaryPath, restoreDir );

		public async Task<TaskDoneStatus> Execute(IProgress<ProgressInfo> none, string tempDir, string restoreDir)
		{
			IsRestoring = true;
			using ( _executeCancellation = new CancellationTokenSource() )
			{
				try
				{
					await Task.Run( async () =>
					{
						// Create destination directory
						if ( !Directory.Exists( restoreDir ) )
							Directory.CreateDirectory( restoreDir );

						// Create directories if necessary
						foreach ( var src in Directory.GetDirectories( tempDir, "*", SearchOption.AllDirectories ).Select( p => p + "\\" ) )
						{
							if ( _executeCancellation.IsCancellationRequested ) return;
							var dest = Path.Combine( Path.GetDirectoryName( restoreDir ), src.Substring( src.IndexOf( tempDir ) + tempDir.Length ) );

							if ( !Directory.Exists( Path.GetDirectoryName( dest ) ) )
								Tools.CreateDirectoryRecursive( dest );
						}

						// Copy files
						var parallelOptions = new ParallelOptions()
						{
							CancellationToken = _executeCancellation.Token,
							MaxDegreeOfParallelism = Environment.ProcessorCount
						};
						foreach ( var src in Directory.GetFiles( tempDir, "*", SearchOption.AllDirectories ) )
						{
							if ( _executeCancellation.IsCancellationRequested ) return;
							var dest = Path.Combine( Path.GetDirectoryName( restoreDir ), src.Substring( src.IndexOf( tempDir ) + tempDir.Length ) );

							if ( AllowOverwrite || !File.Exists( src ) )
								await Tools.CopyFileStrictlyAsync( src, dest, BufferLength, _executeCancellation.Token );
						}
					}, _executeCancellation.Token );
				}
				catch ( TaskCanceledException )
				{
					throw;
				}
				catch ( Exception unknownex )
				{
					Logger.Log( $"Restore operation failed.{Tools.NewLine}{unknownex}", Logger.LogStates.Error );
					return TaskDoneStatus.Failed;
				}
				finally
				{
					IsRestoring = false;
				}
			}

			return TaskDoneStatus.Completed;
		}
	}
}
