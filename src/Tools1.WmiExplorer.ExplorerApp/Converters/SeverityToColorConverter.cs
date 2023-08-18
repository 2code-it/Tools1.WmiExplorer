using System;
using System.Globalization;
using System.Windows.Data;

namespace Tools1.WmiExplorer.ExplorerApp.Converters
{
	public class SeverityToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string? severity = ((string)value)?.ToLower();
			return severity switch
			{
				"error" => "#FF0000",
				"warning" => "#FFCC00",
				_ => "#000000"
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
