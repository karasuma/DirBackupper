using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DirBackupperUnitTest.Utils
{
	public static class Tools
	{
		public static IEnumerable<string> SubFileDirs(string basedir)
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

		public static void CreateDir(string dir)
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

		public static void RemoveDir(string dir)
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
	}
}
