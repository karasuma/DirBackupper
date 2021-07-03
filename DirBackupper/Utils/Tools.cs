using System;
using System.Collections;
using System.Collections.Generic;
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
			if( additionalSeparators != null )
				separators.AddRange( additionalSeparators );

			return self.Split( separators.ToArray(), StringSplitOptions.None ).FirstOrDefault();
		}
	}
}
