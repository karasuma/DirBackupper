using System;
using System.IO;
using System.Text;

namespace DirBackupper.Utils
{
	public static class Logger
	{
		public enum LogStates
		{
			Debug, Info, Warn, Error, Fatal
		}

		public static string LogFilePath
		{
			get
			{
				return Path.Combine(
					Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location ),
					"Logs",
					DateTime.Now.ToString( "yyyyMMdd" ) + ".log"
					);
			}
		}

		public static void Log(string message, LogStates state,
			[System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
			[System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
			[System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = -1)
		{
			// ログ文作成
			var newline = Environment.NewLine;
			var logstr =
				$"[{state.ToString().ToUpper(),-5}] {DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss" )}{newline}" +
				$"{memberName} at [{Path.GetFileName(sourceFilePath)}:{sourceLineNumber}]{newline}" +
				$"{message}{newline}" +
				$"{newline}";

			// ログディレクトリの作成
			var dir = Path.GetDirectoryName( LogFilePath );
			if( !Directory.Exists( dir ) )
			{
				Directory.CreateDirectory( dir );
			}

			// ログに書き込み
			using( var writer = new StreamWriter( LogFilePath, true, Encoding.UTF8 ) )
			{
				writer.Write( logstr );
			}
		}
	}
}
