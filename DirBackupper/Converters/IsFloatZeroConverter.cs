using System;
using System.Globalization;
using System.Windows.Data;

namespace DirBackupper.Converters
{
	[ValueConversion( typeof( float ), typeof( bool ) )]
	public class IsFloatZeroConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (float)value < 0.001f;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
