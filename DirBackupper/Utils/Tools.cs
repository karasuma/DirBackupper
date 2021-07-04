using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirBackupper.Utils
{
	public static class Tools
	{
		public static string NewLine { get => Environment.NewLine; }

		public static string GetFirstLine(this string self, IEnumerable<string> additionalSeparators = null)
		{
			var separators = new List<string>();
			separators.Add( NewLine );
			if ( additionalSeparators != null )
				separators.AddRange( additionalSeparators );

			return self.Split( separators.ToArray(), StringSplitOptions.None ).FirstOrDefault();
		}

		public static async Task CopyFileStrictlyAsync(string source, string destination)
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

		public static void CreateDirectoryRecursive(string path)
		{
			var dir = Path.GetDirectoryName( path );
			if ( !Directory.Exists( dir ) )
				CreateDirectoryRecursive( dir.Substring( 0, dir.LastIndexOf( @"\" ) ) );

			Directory.CreateDirectory( dir );
		}
	}
}
