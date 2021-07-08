using DirBackupper.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DirBackupper.Models.Modules
{
	public class Backup : IBackupTask
	{
		private CancellationTokenSource _cancellation = null;

		public bool AllowOverwrite { get; set; } = false;

		public Backup(bool overwrite)
		{
			AllowOverwrite = overwrite;
		}

		private KeyValuePair<int, int> Progress(int current, int ceiling) => new KeyValuePair<int, int>( current, ceiling );

		private void ReportInfo(IProgress<ProgressInfo> progress, KeyValuePair<int, int> status, string message)
			=> progress.Report( new ProgressInfo( status, message ) );

		public async Task<TaskDoneStatus> Execute(IProgress<ProgressInfo> progress, string sourceDir, string destDir)
		{
			var message = new Func<string, string>( msg =>
			 {
				 return string.Join( Tools.NewLine, new[]
				 {
					 msg,
					 "Source: " + Path.GetFullPath( sourceDir ),
					 "Dest  : " + Path.GetFullPath( destDir )
				 } );
			 } );
			var logging = new Action<string, string, KeyValuePair<int, int>, Logger.LogStates>( (caption, msg, value, state) =>
			{
				ReportInfo( progress, value, caption );
				Logger.Log( message( string.IsNullOrEmpty( msg ) ? caption : string.Join( Tools.NewLine, new[] { caption, msg } ) ), state );
			} );

			using ( _cancellation = new CancellationTokenSource() )
			{
				var proceededFileCount = 0;
				var allFiles = int.MaxValue;
				var currentRatio = new Func<KeyValuePair<int, int>>( () => Progress( proceededFileCount, allFiles ) );
				logging( "Copy operation start.", string.Empty, currentRatio(), Logger.LogStates.Info );

				try
				{
					await Task.Run( () =>
					 {
						 // Create destination directory
						 if ( !Directory.Exists( destDir ) )
							 Directory.CreateDirectory( destDir );

						 // Create directories if necessary
						 foreach ( var src in Directory.GetDirectories( sourceDir, "*", SearchOption.AllDirectories ).Select( p => p + "\\" ) )
						 {
							 var dest = Path.Combine( Path.GetDirectoryName( destDir ), src.Substring( src.IndexOf( sourceDir ) + sourceDir.Length ) );

							 if ( !Directory.Exists( Path.GetDirectoryName( dest ) ) )
								 Tools.CreateDirectoryRecursive( dest );
						 }

						 // Copy files
						 var parallelOptions = new ParallelOptions()
						 {
							 CancellationToken = _cancellation.Token,
							 MaxDegreeOfParallelism = Environment.ProcessorCount
						 };
						 Parallel.ForEach( Directory.GetFiles( sourceDir, "*", SearchOption.AllDirectories ), parallelOptions, async src =>
						 {
							 var dest = Path.Combine( Path.GetDirectoryName( destDir ), src.Substring( src.IndexOf( sourceDir ) + sourceDir.Length ) );

							 var moved = AllowOverwrite || !File.Exists( src );
							 if ( moved )
								 await Tools.CopyFileStrictlyAsync( src, dest );

							 proceededFileCount++;
							 ReportInfo( progress, currentRatio(), moved ? $"Copied: {src}" : $"Hold: {dest}" );
						 } );
					 }, _cancellation.Token ).ConfigureAwait( false );
				}
				catch ( OperationCanceledException ocex )
				{
					logging( "Copy operation cancelled.", ocex.ToString(), currentRatio(), Logger.LogStates.Warn );
					return TaskDoneStatus.Cancelled;
				}
				catch ( Exception unknownex )
				{
					logging( "Copy operation failed.", unknownex.ToString(), currentRatio(), Logger.LogStates.Error );
					return TaskDoneStatus.Failed;
				}
			}

			logging( "Copy operation has completed successfully!", string.Empty, Progress( 1, 1 ), Logger.LogStates.Info );
			return TaskDoneStatus.Completed;
		}

		public void CancelExecute() => _cancellation?.Cancel();
	}
}
