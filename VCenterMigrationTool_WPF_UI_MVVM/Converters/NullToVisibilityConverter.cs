using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VCenterMigrationTool_WPF_UI.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public static readonly NullToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool inverse = parameter != null && parameter.ToString().ToLower() == "inverse";

            return inverse
                ? (value == null ? Visibility.Visible : Visibility.Collapsed)
                : (value == null ? Visibility.Collapsed : Visibility.Visible);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
