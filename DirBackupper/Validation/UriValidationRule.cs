using System.Globalization;
using System.IO;
using System.Windows.Controls;

namespace DirBackupper.Validation
{
	public class UriValidationRule : ValidationRule
	{
		public bool CheckDirExists { get; set; } = false;
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var uri = value.ToString();
			var valid = true;

			valid &= uri.IndexOfAny( Path.GetInvalidPathChars() ) == -1;
			if ( CheckDirExists )
				valid &= Directory.Exists( uri );

			return valid ? new ValidationResult( true, "" ) : new ValidationResult( false, "Invalid Path." );
		}
	}
}
