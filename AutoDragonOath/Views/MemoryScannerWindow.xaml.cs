using AutoDragonOath.ViewModels;
using System.Windows;

namespace AutoDragonOath.Views
{
    /// <summary>
    /// Interaction logic for MemoryScannerWindow.xaml
    /// </summary>
    public partial class MemoryScannerWindow : Window
    {
        public MemoryScannerWindow(int processId)
        {
            InitializeComponent();
            DataContext = new MemoryScannerViewModel(processId);
        }
    }
}
