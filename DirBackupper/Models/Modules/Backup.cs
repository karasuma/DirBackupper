using DirBackupper.Utils;
using System;
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

		private void ReportInfo(IProgress<ProgressInfo> progress, float ratio, string message)
			=> progress.Report( new ProgressInfo( ratio, message ) );

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
			var logging = new Action<string, string, float, Logger.LogStates>( (caption, msg, value, state) =>
			{
				ReportInfo( progress, value, caption );
				Logger.Log( message( string.IsNullOrEmpty( msg ) ? caption : string.Join( Tools.NewLine, new[] { caption, msg } ) ), state );
			} );

			var ratio = ProgressInfo.Minimum;

			using ( _cancellation = new CancellationTokenSource() )
			{
				logging( "Backup operation start.", string.Empty, ratio, Logger.LogStates.Info );

				try
				{
					var proceededFileCount = 0;
					var allFiles = int.MaxValue;
					var currentRatio = new Func<float>( () => allFiles > 0 ? (float)proceededFileCount / (float)allFiles : 0f );
					await Task.Run( () =>
					 {
						 // Count all files
						 var srcpathes = new[]
						 {
							 Directory.GetDirectories( sourceDir, "*", SearchOption.AllDirectories ).Select( p => "D::" + p ),
							 Directory.GetFiles( sourceDir, "*", SearchOption.AllDirectories ).Select(p => "F::" + p )
						 }.SelectMany( x => x ).Distinct();
						 var isDirectory = new Func<string, bool>(p => p.Substring(0, 3) == "D::");
						 allFiles = srcpathes.Count();

						 // Create destination directory
						 if ( !Directory.Exists( destDir ) )
							 Directory.CreateDirectory( destDir );

						 var parallelOptions = new ParallelOptions()
						 {
							 CancellationToken = _cancellation.Token,
							 MaxDegreeOfParallelism = Environment.ProcessorCount
						 };
						 // Copy directories
						 Parallel.ForEach( Directory.GetDirectories( sourceDir, "*", SearchOption.AllDirectories ).Select( p => p + "\\" ), parallelOptions, src =>
						  {
							  var dest = Path.Combine( Path.GetDirectoryName( destDir ), src.Substring( src.IndexOf( sourceDir ) + sourceDir.Length ) );

							  if ( !Directory.Exists( Path.GetDirectoryName( dest ) ) )
								  Tools.CreateDirectoryRecursive( dest );

							  proceededFileCount++;
							  ReportInfo( progress, currentRatio(), "Copied: " + src );
						  } );

						 // Copy files
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
					logging( "Backup operation cancelled.", ocex.ToString(), ratio, Logger.LogStates.Warn );
					return TaskDoneStatus.Cancelled;
				}
				catch ( Exception unknownex )
				{
					logging( "Backup operation failed.", unknownex.ToString(), ratio, Logger.LogStates.Error );
					return TaskDoneStatus.Failed;
				}
			}

			logging( "Backup operation has completed successfully!", string.Empty, ProgressInfo.Maximum, Logger.LogStates.Info );
			return TaskDoneStatus.Completed;
		}

		public void CancelExecute() => _cancellation?.Cancel();
	}
}
