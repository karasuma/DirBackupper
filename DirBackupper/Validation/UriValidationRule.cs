using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.IO;

namespace DirBackupper.Validation
{
	public class UriValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
			=> value.ToString().IndexOfAny( Path.GetInvalidPathChars() ) == -1 ? ValidationResult.ValidResult : new ValidationResult( false, "Invalid path." );
	}
}
