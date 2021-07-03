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

		private async Task CopyFileStrictlyAsync(string source, string destination)
		{
			using ( var src = new FileStream( source, FileMode.Open, FileAccess.Read ) )
			using ( var dst = new FileStream( destination, FileMode.OpenOrCreate, FileAccess.Write ) )
			{
				var orig = new byte[src.Length];
				var bytesFrom = 0;
				var bytesTo = (int)src.Length;
				while ( bytesTo > 0 )
				{
					var count = await src.ReadAsync( orig, bytesFrom, bytesTo );
					if ( count == 0 )
						break;
					bytesFrom += count;
					bytesTo -= count;
				}
				await dst.WriteAsync( orig, 0, orig.Length );
			}
		}

		private void CreateDirectoryRecursive(string path)
		{
			var dir = Path.GetDirectoryName( path );
			if ( !Directory.Exists( dir ) )
				CreateDirectoryRecursive( dir.Substring( 0, dir.LastIndexOf( @"\" ) ) );

			Directory.CreateDirectory( dir );
		}

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
					await Task.Run( async () =>
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

						 // Copy files
						 foreach ( var src in srcpathes )
						 {
							 var isDir = isDirectory( src );
							 var realsrc = src.Substring( 3 );
							 var dst = Path.Combine( Path.GetDirectoryName( destDir ), realsrc.Substring( realsrc.IndexOf( sourceDir ) + sourceDir.Length ) ) + ( isDir ? "\\" : "" );

							 proceededFileCount++;

							 // Check and create directory
							 if ( isDir )
							 {
								 if ( !Directory.Exists( Path.GetDirectoryName( dst ) ) )
								 {
									 CreateDirectoryRecursive( dst );
								 }
								 continue;
							 }

							 // Copy current file
							 if ( AllowOverwrite || !File.Exists( realsrc ) )
							 {
								 await CopyFileStrictlyAsync( realsrc, dst );
								 ReportInfo( progress, currentRatio(), "Copied: " + realsrc );
							 }
							 else
							 {
								 ReportInfo( progress, currentRatio(), "Hold: " + dst );
							 }
						 }
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
