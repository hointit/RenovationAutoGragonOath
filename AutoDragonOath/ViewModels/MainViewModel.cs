using System;
using System.Collections.Generic;
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
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_ALL_ACCESS = PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION | PROCESS_QUERY_INFORMATION;



        private readonly GameProcessMonitor _gameProcessMonitor;
        private CharacterInfo? _selectedCharacter;
        private bool _isKeepOnTop = true;

        public bool IsKeepOnTop
        {
            get => _isKeepOnTop;
            set
            {
                _isKeepOnTop = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<CharacterInfo> Characters { get; }

        public CharacterInfo? SelectedCharacter
        {
            get => _selectedCharacter;
            set
            {
                _selectedCharacter = value;
                OnPropertyChanged();
                // Notify that OpenScannerCommand can execute state may have changed
                ((RelayCommand)OpenScannerCommand).RaiseCanExecuteChanged();

                // Bring the selected character's game window to front
                if (_selectedCharacter != null)
                {
                    WindowManager.BringWindowToFront(_selectedCharacter.ProcessId);
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand StartAutoCommand { get; }
        public ICommand StopAutoCommand { get; }
        public ICommand OpenScannerCommand { get; }
        public ICommand TestSkillCommand { get; }
        public ICommand SetWatchedMapCommand { get; }

        public MainViewModel()
        {
            _gameProcessMonitor = new GameProcessMonitor();
            Characters = new ObservableCollection<CharacterInfo>();

            // Initialize commands
            RefreshCommand = new RelayCommand(_ => RefreshCharacters());
            StartAutoCommand = new RelayCommand(_ => { /* Reserved for future use */ });
            StopAutoCommand = new RelayCommand(_ => { /* Reserved for future use */ });
            OpenScannerCommand = new RelayCommand(_ => OpenMemoryScanner(), _ => SelectedCharacter != null);
            TestSkillCommand = new RelayCommand(_ => TestSkillFunction(), _ => SelectedCharacter != null);
            SetWatchedMapCommand = new RelayCommand(_ => SetWatchedMap(), _ => SelectedCharacter != null);

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
                // Cleanup resources before removing
                character.Dispose();
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

        /// <summary>
        /// Test executing a skill using keyboard simulation (RECOMMENDED APPROACH)
        /// This simulates pressing F1 to execute the first skill slot
        /// </summary>
        private void TestSkillFunction()
        {
            
            var a = LuaPlusRemoteCaller.InjectAndCall(SelectedCharacter.ProcessId);
            var b = a;
        }
        

        /// <summary>
        /// Set the watched map to the current map
        /// </summary>
        private void SetWatchedMap()
        {
            if (SelectedCharacter == null)
                return;

            SelectedCharacter.SetWatchedMap();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
