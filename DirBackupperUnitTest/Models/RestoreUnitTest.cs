using DirBackupper.Models.Modules;
using DirBackupperUnitTest.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DirBackupperUnitTest.Models
{
	[TestClass]
	public class RestoreUnitTest
	{
		private string SourceDir { get => @".\Source\"; }

		[TestMethod]
		public async Task RestoreTestAsync()
		{
			// Arrange
			Tools.CreateDir( SourceDir );

			// Act & Assert
			var restore = new Restore( true );
			using ( var listener = new ConsoleTraceListener() )
			{
				Trace.Listeners.Add( listener );

				Console.WriteLine( "Temporary backup testing..." );
				var backupResult = await restore.BackupToTemporary( SourceDir );
				if ( backupResult != TaskDoneStatus.Completed )
					Assert.Fail( $"Temporary backup operation failed. ({backupResult})" );

				Console.WriteLine( "Restore testing...(Create)" );
				Tools.RemoveDir( SourceDir );
				var restoreResult = await restore.Recovery( SourceDir );
				if ( restoreResult != TaskDoneStatus.Completed )
					Assert.Fail( $"Restore operation failed. ({restoreResult})" );

				Console.WriteLine( "Restore testing...(Overwrite)" );
				restoreResult = await restore.Recovery( SourceDir );
				if ( restoreResult != TaskDoneStatus.Completed )
					Assert.Fail( $"Restore operation failed. ({restoreResult})" );

				Trace.Listeners.Remove( listener );
			}
		}

		[TestMethod]
		public void RemoveTempDirTestAsync()
		{
			// Arrange
			var restore = new Restore( true );
			Tools.CreateDir( restore.TemporaryPath );

			// Act & Assert
			DirBackupper.Utils.Tools.DestructDirectory( restore.TemporaryPath );
		}
	}
}
