using AutoDragonOath.Helpers;
using AutoDragonOath.Models;
using AutoDragonOath.Services;
using AutoDragonOath.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace AutoDragonOath.ViewModels
{
    /// <summary>
    /// Main view model for the application
    /// Manages the collection of characters and auto-refresh functionality
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly GameProcessMonitor _gameProcessMonitor;
        private readonly DispatcherTimer _autoRefreshTimer;
        private bool _isAutoRefreshEnabled;
        private CharacterInfo? _selectedCharacter;

        public ObservableCollection<CharacterInfo> Characters { get; }

        public bool IsAutoRefreshEnabled
        {
            get => _isAutoRefreshEnabled;
            set
            {
                _isAutoRefreshEnabled = value;
                OnPropertyChanged();

                if (_isAutoRefreshEnabled)
                {
                    _autoRefreshTimer.Start();
                }
                else
                {
                    _autoRefreshTimer.Stop();
                }
            }
        }

        public CharacterInfo? SelectedCharacter
        {
            get => _selectedCharacter;
            set
            {
                _selectedCharacter = value;
                OnPropertyChanged();
                // Notify that OpenScannerCommand can execute state may have changed
                ((RelayCommand)OpenScannerCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand StartAutoCommand { get; }
        public ICommand StopAutoCommand { get; }
        public ICommand OpenScannerCommand { get; }

        public MainViewModel()
        {
            _gameProcessMonitor = new GameProcessMonitor();
            Characters = new ObservableCollection<CharacterInfo>();

            // Initialize auto-refresh timer (2 seconds interval, like the original)
            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(2)
            };
            _autoRefreshTimer.Tick += (s, e) => RefreshCharacters();

            // Initialize commands
            RefreshCommand = new RelayCommand(_ => RefreshCharacters());
            StartAutoCommand = new RelayCommand(_ => IsAutoRefreshEnabled = true);
            StopAutoCommand = new RelayCommand(_ => IsAutoRefreshEnabled = false);
            OpenScannerCommand = new RelayCommand(_ => OpenMemoryScanner(), _ => SelectedCharacter != null);

            // Initial scan
            RefreshCharacters();
        }

        /// <summary>
        /// Scan for game processes and update the character list
        /// </summary>
        private void RefreshCharacters()
        {
            var scannedCharacters = _gameProcessMonitor.ScanForGameProcesses();

            // Remove characters whose processes no longer exist
            var processIds = scannedCharacters.Select(c => c.ProcessId).ToHashSet();
            var toRemove = Characters.Where(c => !processIds.Contains(c.ProcessId)).ToList();
            foreach (var character in toRemove)
            {
                Characters.Remove(character);
            }

            // Update existing characters and add new ones
            foreach (var scannedChar in scannedCharacters)
            {
                var existingChar = Characters.FirstOrDefault(c => c.ProcessId == scannedChar.ProcessId);

                if (existingChar != null)
                {
                    // Update existing character's properties
                    UpdateCharacterInfo(existingChar, scannedChar);
                }
                else
                {
                    // Add new character
                    Characters.Add(scannedChar);
                }
            }
        }

        /// <summary>
        /// Update an existing character's information from a scanned character
        /// </summary>
        private void UpdateCharacterInfo(CharacterInfo existing, CharacterInfo scanned)
        {
            existing.CharacterName = scanned.CharacterName;
            existing.HpPercent = scanned.HpPercent;
            existing.MpPercent = scanned.MpPercent;
            existing.Level = scanned.Level;
            existing.PetHpPercent = scanned.PetHpPercent;
            existing.XCoordinate = scanned.XCoordinate;
            existing.YCoordinate = scanned.YCoordinate;
            existing.MapName = scanned.MapName;
            existing.Experience = scanned.Experience;
            existing.Skills = scanned.Skills;
        }

        /// <summary>
        /// Open the Memory Scanner window for the selected character
        /// </summary>
        private void OpenMemoryScanner()
        {
            if (SelectedCharacter == null)
                return;

            var scannerWindow = new MemoryScannerWindow(SelectedCharacter.ProcessId);
            scannerWindow.Show();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
