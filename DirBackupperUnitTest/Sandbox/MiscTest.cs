using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DirBackupper.Utils;

namespace DirBackupperUnitTest.Sandbox
{
	[TestClass]
	public class MiscTest
	{
		[TestMethod]
		public void Sandbox()
		{
			// Arrange
			var rootDir = Path.GetFullPath(@".\").AddDirectoryIdentify();
			var baseDir = rootDir + @"Root\";
			var targetDir = baseDir + @"Child\Destination\";
			Tools.DestructDirectory( baseDir );
			Directory.CreateDirectory( baseDir );

			// Act
			Tools.CreateDirectoryRecursive( targetDir );

			// Assert
			Assert.IsTrue( Directory.Exists( targetDir ) );
		}
	}
}
