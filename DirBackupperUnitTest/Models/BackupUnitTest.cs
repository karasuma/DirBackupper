using DirBackupper.Models.Modules;
using DirBackupperUnitTest.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DirBackupperUnitTest
{
	[TestClass]
	public class BackupUnitTest
	{
		private string SourceDir { get => @".\Source\"; }
		private string DestDir { get => @".\Destination\"; }

		private async Task<TaskDoneStatus> MainExecuteProcess()
		{
			var proc = new Backup( true );
			var result = TaskDoneStatus.None;
			using ( var listener = new ConsoleTraceListener() )
			{
				Trace.Listeners.Add( listener );
				var progress = new Progress<ProgressInfo>();
				result = await proc.Execute( progress, SourceDir, DestDir );
				Console.WriteLine( $"Executed: {result}" );
				Trace.Listeners.Remove( listener );
			}
			return result;
		}

		[TestMethod]
		public async Task ExecuteTestAsync()
		{
			// Arrange
			Tools.RemoveDir( DestDir );
			Tools.RemoveDir( SourceDir );
			Tools.CreateDir( SourceDir );

			// Act
			var result = await MainExecuteProcess();

			// Assert
			Assert.AreEqual( TaskDoneStatus.Completed, result );
		}

		[TestMethod]
		public async Task ExecuteAndOverwriteTestAsync()
		{
			// Arrange
			Tools.RemoveDir( DestDir );
			Tools.RemoveDir( SourceDir );
			Tools.CreateDir( SourceDir );

			// Act
			var result1 = await MainExecuteProcess();
			var result2 = await MainExecuteProcess();

			// Arrange
			Assert.AreEqual( TaskDoneStatus.Completed, result1, "First check (create)" );
			Assert.AreEqual( TaskDoneStatus.Completed, result2, "Second check (overwrite)" );
		}
	}
}
