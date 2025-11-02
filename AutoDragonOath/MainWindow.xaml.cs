using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AutoDragonOath.Services;

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
        
        /// <summary>
        /// Run diagnostic report to test current memory addresses
        /// </summary>
        private void ButtonDiagnostic_Click(object sender, RoutedEventArgs e)
        {
            var processes = Process.GetProcessesByName("Game");

            if (processes.Length == 0)
            {
                MessageBox.Show("No game process found! Make sure the game is running.",
                    "Process Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Run diagnostic on first process
            AddressFinder.GenerateDiagnosticReport(processes[0].Id);

            MessageBox.Show(
                "Diagnostic report generated!\n\n" +
                "Check the Debug Output window:\n" +
                "  Debug → Windows → Output\n\n" +
                "Look for:\n" +
                "  ✓ Success messages (green checkmarks)\n" +
                "  ❌ Failure messages (red X)\n" +
                "  Character stats (Name, Level, HP)",
                "Diagnostic Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// Open Map Scanner window to find Map Object Pointer
        /// </summary>
        private void ButtonScanMap_Click(object sender, RoutedEventArgs e)
        {
            var processes = Process.GetProcessesByName("Game");

            if (processes.Length == 0)
            {
                MessageBox.Show("No game process found! Make sure the game is running.",
                    "Process Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var response = MessageBox.Show(
                "IMPORTANT: Before scanning, make sure:\n\n" +
                "✓ Character is LOGGED IN\n" +
                "✓ Character is in the game world (not at login screen)\n" +
                "✓ You know which map the character is standing on\n\n" +
                "This will open the Map Scanner window to help you find:\n" +
                "  • Map name strings in memory\n" +
                "  • Pointers to map structures\n" +
                "  • CWorldManager::s_pMe static address\n\n" +
                "Open Map Scanner?",
                "Map Scanner",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (response != MessageBoxResult.Yes)
                return;

            // Open the Map Scanner window
            var mapScannerWindow = new Views.MapScannerWindow(processes[0].Id);
            mapScannerWindow.Show();
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
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Visible;

            return false;
        }
    }
    
    
}
