using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Threading;
using AutoDragonOath.Services;

namespace AutoDragonOath.Models
{
    /// <summary>
    /// Represents character information read from game memory
    /// </summary>
    public class CharacterInfo : INotifyPropertyChanged
    {
        private int _processId;
        private string _characterName = "T";
        private int _hpPercent;
        private int _mpPercent;
        private int _level;
        private int _petHpPercent;
        private int _xCoordinate;
        private int _yCoordinate;
        private string _mapName = "Unknown";
        private int _experience;
        private List<SkillInfo> _skills = new List<SkillInfo>();
        private bool _isAutomationEnabled;
        private bool _isTestRunning;
        private string _elapsedTime = "00:00:00";

        // Auto-refresh properties
        private bool _isAutoRefreshEnabled;
        private int _refreshIntervalSeconds = 2;
        private DispatcherTimer? _refreshTimer;
        private GameProcessMonitor? _gameProcessMonitor;

        // Thread and clock management
        private Thread? _clockThread;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly Dispatcher _dispatcher;

        public CharacterInfo()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _gameProcessMonitor = new GameProcessMonitor();
        }

        /// <summary>
        /// Initialize with GameProcessMonitor dependency (for testing or custom monitor)
        /// </summary>
        public CharacterInfo(GameProcessMonitor gameProcessMonitor)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _gameProcessMonitor = gameProcessMonitor;
        }

        public int ProcessId
        {
            get => _processId;
            set
            {
                _processId = value;
                OnPropertyChanged();
            }
        }

        public string CharacterName
        {
            get => _characterName;
            set
            {
                _characterName = value;
                OnPropertyChanged();
            }
        }
        
        public int HpPercent
        {
            get => _hpPercent;
            set
            {
                _hpPercent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HpStatus));
            }
        }

        public int MpPercent
        {
            get => _mpPercent;
            set
            {
                _mpPercent = value;
                OnPropertyChanged();
            }
        }

        public int Level
        {
            get => _level;
            set
            {
                _level = value;
                OnPropertyChanged();
            }
        }

        public int PetHpPercent
        {
            get => _petHpPercent;
            set
            {
                _petHpPercent = value;
                OnPropertyChanged();
            }
        }

        public int XCoordinate
        {
            get => _xCoordinate;
            set
            {
                _xCoordinate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Position));
            }
        }

        public int YCoordinate
        {
            get => _yCoordinate;
            set
            {
                _yCoordinate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Position));
            }
        }

        public string MapName
        {
            get => _mapName;
            set
            {
                _mapName = value;
                OnPropertyChanged();
            }
        }

        public int Experience
        {
            get => _experience;
            set
            {
                _experience = value;
                OnPropertyChanged();
            }
        }

        public List<SkillInfo> Skills
        {
            get => _skills;
            set
            {
                _skills = value;
                OnPropertyChanged();
            }
        }

        public bool IsAutomationEnabled
        {
            get => _isAutomationEnabled;
            set
            {
                _isAutomationEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Status));
            }
        }

        public bool IsTestRunning
        {
            get => _isTestRunning;
            set
            {
                _isTestRunning = value;
                OnPropertyChanged();
            }
        }

        public string ElapsedTime
        {
            get => _elapsedTime;
            set
            {
                _elapsedTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Enable/disable auto-refresh for this character
        /// </summary>
        public bool IsAutoRefreshEnabled
        {
            get => _isAutoRefreshEnabled;
            set
            {
                _isAutoRefreshEnabled = value;
                OnPropertyChanged();

                if (_isAutoRefreshEnabled)
                {
                    StartAutoRefresh();
                }
                else
                {
                    StopAutoRefresh();
                }
            }
        }

        /// <summary>
        /// Refresh interval in seconds (default: 2 seconds)
        /// </summary>
        public int RefreshIntervalSeconds
        {
            get => _refreshIntervalSeconds;
            set
            {
                if (value < 1)
                    value = 1; // Minimum 1 second

                if (_refreshIntervalSeconds != value)
                {
                    _refreshIntervalSeconds = value;
                    OnPropertyChanged();

                    // Update timer interval if timer is running
                    if (_refreshTimer != null && _refreshTimer.IsEnabled)
                    {
                        _refreshTimer.Interval = TimeSpan.FromSeconds(_refreshIntervalSeconds);
                    }
                }
            }
        }

        // Computed properties for UI display
        public string Position => $"({XCoordinate}, {YCoordinate})";

        public string HpStatus
        {
            get
            {
                if (HpPercent > 70) return "Healthy";
                if (HpPercent > 30) return "Warning";
                return "Critical";
            }
        }

        public string Status
        {
            get
            {
                if (CharacterName == "Đăng nhập") return "Not Logged In";
                if (IsAutomationEnabled) return "Running";
                return "Idle";
            }
        }

        /// <summary>
        /// Start the clock thread
        /// </summary>
        public void StartClock()
        {
            if (IsTestRunning)
                return;

            IsTestRunning = true;
            ElapsedTime = "00:00:00";

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            var skillExecutor = new SkillExecutor(ProcessId);

            _clockThread = new Thread(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    Skills.FindAll(skill => skill.IsEnabled).ForEach(skill =>
                    {
                        skillExecutor.ExecuteSkill(skill.SkillId);
                        Thread.Sleep(skill.Delay * 1000);
                    });
                }
            })
            {
                IsBackground = true
            };

            _clockThread.Start();
        }

        /// <summary>
        /// Stop the clock thread
        /// </summary>
        public void StopClock()
        {
            if (!IsTestRunning)
                return;

            // Cancel the token to signal thread to stop
            _cancellationTokenSource?.Cancel();

            // Wait for thread to finish (with timeout)
            if (_clockThread != null && _clockThread.IsAlive)
            {
                if (!_clockThread.Join(2000)) // Wait max 2 seconds
                {
                    // Force abort if still running
                    _clockThread.Interrupt();
                }
            }

            IsTestRunning = false;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _clockThread = null;
        }

        /// <summary>
        /// Start auto-refresh timer for this character
        /// </summary>
        private void StartAutoRefresh()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer = null;
            }

            _refreshTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(_refreshIntervalSeconds),
                DispatcherPriority.Background,
                async (s, e) => await RefreshCharacterDataAsync(),
                _dispatcher);
            _refreshTimer.Start();
        }

        /// <summary>
        /// Stop auto-refresh timer
        /// </summary>
        private void StopAutoRefresh()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer = null;
            }
        }

        /// <summary>
        /// Refresh this character's data from memory (async, non-blocking)
        /// </summary>
        private async System.Threading.Tasks.Task RefreshCharacterDataAsync()
        {
            if (_gameProcessMonitor == null)
                return;

            // Run memory reading on background thread for this specific process only
            var updatedChar = await System.Threading.Tasks.Task.Run(() =>
                _gameProcessMonitor.ReadCharacterInfo(ProcessId));

            if (updatedChar != null)
            {
                // Update properties on UI thread (we're already on UI thread due to DispatcherTimer)
                CharacterName = updatedChar.CharacterName;
                HpPercent = updatedChar.HpPercent;
                MpPercent = updatedChar.MpPercent;
                Level = updatedChar.Level;
                PetHpPercent = updatedChar.PetHpPercent;
                XCoordinate = updatedChar.XCoordinate;
                YCoordinate = updatedChar.YCoordinate;
                MapName = updatedChar.MapName;
                Experience = updatedChar.Experience;
                // Note: Don't update Skills as they may have user modifications
            }
        }

        /// <summary>
        /// Cleanup resources when character is removed
        /// </summary>
        public void Dispose()
        {
            StopAutoRefresh();
            StopClock();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a skill that the character has
    /// </summary>
    public class SkillInfo : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private int _delay;

        public string SkillName { get; set; } = string.Empty;
        public string Hotkey { get; set; } = string.Empty;
        public int SkillId { get; set; }
        public bool IsAvailable { get; set; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        public int Delay
        {
            get => _delay;
            set
            {
                _delay = value;
                OnPropertyChanged();
            }
        }

        public SkillInfo(string name, string slot, int id = 0, bool isEnabled = false, int delay = 1)
        {
            SkillName = name;
            Hotkey = slot;
            SkillId = id;
            IsAvailable = false;
            IsEnabled = isEnabled;
            Delay = delay;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
