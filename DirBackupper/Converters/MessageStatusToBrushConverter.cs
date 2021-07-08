using DirBackupper.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DirBackupper.Converters
{
	[ValueConversion( typeof( MessageStatus ), typeof( SolidColorBrush ) )]
	public class MessageStatusToBrushConverter : IValueConverter
	{
		public SolidColorBrush GetStatusColor(MessageStatus status)
		{
			switch ( status )
			{
				case MessageStatus.Info:
					return new SolidColorBrush( Colors.CornflowerBlue );
				case MessageStatus.Working:
					return new SolidColorBrush( Colors.LightGreen );
				case MessageStatus.Warning:
					return new SolidColorBrush( Colors.Yellow );
			}
			return new SolidColorBrush( Colors.Tomato );
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> GetStatusColor( (MessageStatus)value );

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
