using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DirBackupper.Models.Modules;
using DirBackupper.Utils;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DirBackupperUnitTest
{
	[TestClass]
	public class BackupUnitTest
	{
		private string SourceDir { get => @".\Source\"; }
		private string DestDir { get => @".\Destination\"; }

		private IEnumerable<string> SubFileDirs(string basedir)
		{
			if ( basedir.Last() != '\\' )
				basedir += '\\';
			return new[]
			{
				"test01.file",
				@"dir01\",
				@"dir01\test02.dat",
				@"dir01\test03",
				@"dir01\dir02\",
				@"dir01\dir02\test04",
				@"dir01\dir03\"
			}.Select( after => basedir + after );
		}
		private bool IsDirectory(string path)
			=> path.IndexOfAny( Path.GetInvalidPathChars() ) == -1 ? false : File.GetAttributes( path ).HasFlag( FileAttributes.Directory );

		private void CreateDir(string dir)
		{
			var sources = SubFileDirs( dir );
			foreach ( var d in sources.Where( f => f.Last() == '\\' ) )
			{
				var currdir = Path.GetDirectoryName( d );
				if ( !Directory.Exists( currdir ) )
					Directory.CreateDirectory( currdir );
			}
			foreach ( var f in sources.Where( f => f.Last() != '\\' ) )
			{
				if ( !File.Exists( f ) )
					using ( File.Create( f ) ) { }
			}
		}

		private void RemoveDir(string dir)
		{
			var sources = SubFileDirs( dir );
			foreach ( var f in sources.Where( f => f.Last() != '\\' ) )
			{
				if ( File.Exists( f ) )
					File.Delete( f );
			}
			foreach ( var d in sources.Where( f => f.Last() == '\\' ) )
			{
				var currdir = Path.GetDirectoryName( d );
				if ( Directory.Exists( currdir ) )
					Directory.Delete( currdir, true );
			}
		}

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
			RemoveDir( DestDir );
			RemoveDir( SourceDir );
			CreateDir( SourceDir );

			// Act
			var result = await MainExecuteProcess();

			// Assert
			Assert.AreEqual( TaskDoneStatus.Completed, result );
		}

		[TestMethod]
		public async Task ExecuteAndOverwriteTestAsync()
		{
			// Arrange
			RemoveDir( DestDir );
			RemoveDir( SourceDir );
			CreateDir( SourceDir );

			// Act
			var result1 = await MainExecuteProcess();
			var result2 = await MainExecuteProcess();

			// Arrange
			Assert.AreEqual( TaskDoneStatus.Completed, result1, "First check (create)" );
			Assert.AreEqual( TaskDoneStatus.Completed, result2, "Second check (overwrite)" );
		}
	}
}
