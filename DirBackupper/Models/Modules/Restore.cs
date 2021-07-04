using DirBackupper.Utils;
using System;
using System.Collections.Generic;
using System.IO;
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

		private CancellationTokenSource _cancellation = null;

		public bool AllowOverwrite { get; set; } = false;

		public string TemporaryPath { get; set; } = string.Empty;

		public bool IsRestoring { get; private set; } = false;

		public void CancelExecute()
		{
			if ( IsRestoring )
				throw new InvalidOperationException( "Restore operation can not cancel!" );
			_cancellation?.Cancel();
		}

		public async Task<TaskDoneStatus> BackupToTemporary(string sourceDir)
		{
			try
			{
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
			using ( _cancellation = new CancellationTokenSource() )
			{
				try
				{
					await Task.Run( () =>
					{
						// Create destination directory
						if ( !Directory.Exists( restoreDir ) )
							Directory.CreateDirectory( restoreDir );

						// Create directories if necessary
						foreach ( var src in Directory.GetDirectories( tempDir, "*", SearchOption.AllDirectories ).Select( p => p + "\\" ) )
						{
							var dest = Path.Combine( Path.GetDirectoryName( restoreDir ), src.Substring( src.IndexOf( tempDir ) + tempDir.Length ) );

							if ( !Directory.Exists( Path.GetDirectoryName( dest ) ) )
								Tools.CreateDirectoryRecursive( dest );
						}

						// Copy files
						var parallelOptions = new ParallelOptions()
						{
							CancellationToken = _cancellation.Token,
							MaxDegreeOfParallelism = Environment.ProcessorCount
						};
						Parallel.ForEach( Directory.GetFiles( tempDir, "*", SearchOption.AllDirectories ), parallelOptions, async src =>
							{
								var dest = Path.Combine( Path.GetDirectoryName( restoreDir ), src.Substring( src.IndexOf( tempDir ) + tempDir.Length ) );

								if ( AllowOverwrite || !File.Exists( src ) )
									await Tools.CopyFileStrictlyAsync( src, dest );
							} );
					}, _cancellation.Token ).ConfigureAwait( false );
				}
				catch ( OperationCanceledException )
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

			Logger.Log( $"Restore operation completed successfully!", Logger.LogStates.Info );
			return TaskDoneStatus.Completed;
		}
	}
}
