using AutoDragonOath.ViewModels;
using System.Windows;

namespace AutoDragonOath.Views
{
    /// <summary>
    /// Interaction logic for MapScannerWindow.xaml
    /// Map scanner window to find Map Object Pointer
    /// </summary>
    public partial class MapScannerWindow : Window
    {
        public MapScannerWindow(int processId)
        {
            InitializeComponent();
            DataContext = new MapScannerViewModel(processId);
        }
    }
}
