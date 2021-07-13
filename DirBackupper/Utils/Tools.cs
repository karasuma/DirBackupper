using Prism.Services.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

		/// <summary>
		/// Copy the file
		/// </summary>
		/// <param name="source">Copy target</param>
		/// <param name="destination">Copy destination</param>
		/// <param name="copyBlockBytes">Block length that limit of </param>
		/// <returns></returns>
		public static async Task CopyFileStrictlyAsync(string source, string destination, uint copyBlockBytes, CancellationToken token)
		{
			using ( var src = new FileStream( source, FileMode.Open, FileAccess.Read ) )
			using ( var dst = new FileStream( destination, FileMode.OpenOrCreate, FileAccess.Write ) )
			{
				if ( token.IsCancellationRequested ) return;
				var buffer = new byte[Math.Min( (uint)Math.Min( src.Length, copyBlockBytes > 0L ? copyBlockBytes : src.Length ), uint.MaxValue )];
				var readLength = 0;
				while ( ( readLength = await src.ReadAsync( buffer, 0, buffer.Length, token ) ) > 0 )
				{
					if ( token.IsCancellationRequested ) return;
					await dst.WriteAsync( buffer, 0, readLength, token );
				}
			}
		}

		public static void CreateDirectoryRecursive(string path)
		{
			var dir = Path.GetDirectoryName( path );
			if ( !Directory.Exists( dir ) )
				CreateDirectoryRecursive( dir.Substring( 0, dir.LastIndexOf( @"\" ) ) );

			Directory.CreateDirectory( dir );
		}

		public static string AddDirectoryIdentify(this string path)
			=> ( path.Length > 0 && path.Last() != '\\' ) ? path + "\\" : path;

		public static void DestructDirectory(string path)
		{
			if ( !Directory.Exists( path ) ) return;

			foreach ( var src in Directory.GetFiles( path, "*", SearchOption.AllDirectories ) )
			{
				File.SetAttributes( src, FileAttributes.Normal );
				File.Delete( src );
			}

			foreach ( var src in Directory.GetDirectories( path, "*", SearchOption.AllDirectories ).Select( P => P.AddDirectoryIdentify() ) )
			{
				if ( Directory.Exists( src ) )
					Directory.Delete( src, true );
			}
			Directory.Delete( path );
		}
	}
}
