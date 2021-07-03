﻿using DirBackupper.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DirBackupper.Models
{
	public class Backup
	{
		public enum BackupDoneStatus
		{
			None, Completed, Failed, Cancelled
		}

		public struct ProgressInfo
		{
			public float Ratio { get; }
			public string Message { get; }

			public ProgressInfo(float ratio, string message)
			{
				Ratio = ratio < 0f ? ratio : 1f < ratio ? 1f : ratio;
				Message = message;
			}

			public const float Minimum = 0f;
			public const float Maximum = 1f;
		}

		private CancellationTokenSource _cancellation = null;
		private bool PendingFlag { get; set; } = false;

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

		public async Task<BackupDoneStatus> Execute(IProgress<ProgressInfo> progress, string srcdir, string dstdir)
		{
			var message = new Func<string, string>( msg =>
			 {
				 return string.Join( Tools.NewLine, new[]
				 {
					 msg,
					 "Source: " + Path.GetFullPath( srcdir ),
					 "Dest  : " + Path.GetFullPath( dstdir )
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
							 Directory.GetDirectories( srcdir, "*", SearchOption.AllDirectories ).Select( p => "D::" + p ),
							 Directory.GetFiles( srcdir, "*", SearchOption.AllDirectories ).Select(p => "F::" + p )
						 }.SelectMany( x => x ).Distinct();
						 var isDirectory = new Func<string, bool>(p => p.Substring(0, 3) == "D::");
						 allFiles = srcpathes.Count();

						 // Create destination directory
						 if ( !Directory.Exists( dstdir ) )
							 Directory.CreateDirectory( dstdir );

						 // Copy files
						 foreach ( var src in srcpathes )
						 {
							 var isDir = isDirectory( src );
							 var realsrc = src.Substring( 3 );
							 var dst = Path.Combine( Path.GetDirectoryName( dstdir ), realsrc.Substring( realsrc.IndexOf( srcdir ) + srcdir.Length ) ) + ( isDir ? "\\" : "" );

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
					return BackupDoneStatus.Cancelled;
				}
				catch ( Exception unknownex )
				{
					logging( "Backup operation failed.", unknownex.ToString(), ratio, Logger.LogStates.Error );
					return BackupDoneStatus.Failed;
				}
			}

			logging( "Backup operation has completed successfully!", string.Empty, ProgressInfo.Maximum, Logger.LogStates.Info );
			return BackupDoneStatus.Completed;
		}

		public void TaskCancel()
		{
			_cancellation?.Cancel();
			PendingFlag = false;
		}

		public void SwitchPending() => PendingFlag = !PendingFlag;
	}
}