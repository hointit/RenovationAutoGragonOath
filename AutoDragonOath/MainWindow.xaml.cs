using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoDragonOath
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// Converter for HP status to background color
    /// </summary>
    public class HpStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Healthy" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),    // Green
                    "Warning" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),    // Orange
                    "Critical" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),   // Red
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))           // Gray
                };
            }

            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter for boolean/object to Visibility
    /// Supports "Inverse" parameter to invert the visibility logic
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool inverse = parameter?.ToString()?.ToLower() == "inverse";

            if (value == null)
                return inverse ? Visibility.Visible : Visibility.Collapsed;

            if (value is bool boolValue)
            {
                bool result = inverse ? !boolValue : boolValue;
                return result ? Visibility.Visible : Visibility.Collapsed;
            }

            return inverse ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Visible;

            return false;
        }
    }
}
