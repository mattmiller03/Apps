using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VCenterMigrationTool_WPF_UI.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public static readonly BoolToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool inverse = parameter != null && parameter.ToString().ToLower() == "inverse";

            if (value is bool boolValue)
            {
                return inverse
                    ? (boolValue ? Visibility.Collapsed : Visibility.Visible)
                    : (boolValue ? Visibility.Visible : Visibility.Collapsed);
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool inverse = parameter != null && parameter.ToString().ToLower() == "inverse";

            if (value is Visibility visibilityValue)
            {
                return inverse
                    ? (visibilityValue == Visibility.Collapsed)
                    : (visibilityValue == Visibility.Visible);
            }

            return false;
        }
    }
}
