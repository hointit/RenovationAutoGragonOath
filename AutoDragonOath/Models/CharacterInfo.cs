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
        private int _mapId;
        private int _experience;
        private List<SkillInfo> _skills = new List<SkillInfo>();
        private bool _isAutomationEnabled;
        private int _watchedMapId;
        private string _consoleLog = string.Empty;
        private System.Collections.Generic.Queue<string> _logQueue = new System.Collections.Generic.Queue<string>();

        // Auto-refresh properties
        private bool _isAutoRefreshEnabled;
        private int _refreshIntervalSeconds = 60;
        private int _maxLevel = 130;
        private int _maxLogDisplay = 50;
        private DispatcherTimer? _refreshTimer;
        private GameProcessMonitor? _gameProcessMonitor;

        private readonly Dispatcher _dispatcher;

        public CharacterInfo()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _gameProcessMonitor = new GameProcessMonitor();
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
                OnPropertyChanged(nameof(CharacterHeader));
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
                OnPropertyChanged(nameof(CharacterHeader));
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

        public int MapId
        {
            get => _mapId;
            set
            {
                _mapId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WatchedMapDisplay));
            }
        }

        public int WatchedMapId
        {
            get => _watchedMapId;
            set
            {
                _watchedMapId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WatchedMapDisplay));
            }
        }

        public string ConsoleLog
        {
            get => _consoleLog;
            set
            {
                _consoleLog = value;
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

        /// <summary>
        /// Maximum level for automation (default: 120)
        /// </summary>
        public int MaxLevel
        {
            get => _maxLevel;
            set
            {
                if (value < 1)
                    value = 1; // Minimum level 1

                if (_maxLevel != value)
                {
                    _maxLevel = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Maximum number of log entries to display (default: 10)
        /// </summary>
        public int MaxLogDisplay
        {
            get => _maxLogDisplay;
            set
            {
                if (value < 1)
                    value = 1; // Minimum 1 log entry

                if (_maxLogDisplay != value)
                {
                    _maxLogDisplay = value;
                    OnPropertyChanged();

                    // Rebuild console log with new limit
                    UpdateConsoleLog();
                }
            }
        }

        // Computed properties for UI display
        public string Position => $"({XCoordinate}, {YCoordinate})";

        public string WatchedMapDisplay => $"Watched Map - {WatchedMapId}";

        public string CharacterHeader => $"{CharacterName} - {Level}";

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
        /// Set the watched map to the current map
        /// </summary>
        public void SetWatchedMap()
        {
            WatchedMapId = MapId;
        }

        /// <summary>
        /// Add a log entry to the console (keeps only last N entries based on MaxLogDisplay)
        /// </summary>
        public void AddConsoleLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}]: {message}";

            _logQueue.Enqueue(logEntry);

            // Keep only the last N log entries based on MaxLogDisplay
            while (_logQueue.Count > _maxLogDisplay)
            {
                _logQueue.Dequeue();
            }
            ConsoleLog = string.Join(Environment.NewLine, _logQueue);
        }

        /// <summary>
        /// Update console log display when MaxLogDisplay changes
        /// </summary>
        private void UpdateConsoleLog()
        {
            // Remove oldest entries if we exceeded the new limit
            while (_logQueue.Count > _maxLogDisplay)
            {
                _logQueue.Dequeue();
            }
            ConsoleLog = string.Join(Environment.NewLine, _logQueue);
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
                CharacterName = updatedChar.CharacterName;
                HpPercent = updatedChar.HpPercent;
                MpPercent = updatedChar.MpPercent;
                Level = updatedChar.Level;
                PetHpPercent = updatedChar.PetHpPercent;
                XCoordinate = updatedChar.XCoordinate;
                YCoordinate = updatedChar.YCoordinate;
                MapName = updatedChar.MapName;
                Experience = updatedChar.Experience;
                AddConsoleLog($"Refresh Information of {CharacterName}.");
                if (_level >= _maxLevel)
                {
                    var skillExecutor = new SkillExecutor(ProcessId);
                    Skills.FindAll(skill => skill.IsEnabled).ForEach(skill =>
                    {
                        skillExecutor.ExecuteSkill(skill.SkillId);
                        Thread.Sleep(skill.Delay * 200);
                        skillExecutor.ExecuteSkill(skill.SkillId);
                        Thread.Sleep(skill.Delay * 200);
                        skillExecutor.ExecuteSkill(skill.SkillId);
                        Thread.Sleep(skill.Delay * 200);
                        AddConsoleLog($"Reset Level of  {CharacterName}.");
                    });
                }
            }
            else
            {
                AddConsoleLog($"Window of game {CharacterName} has been closed of crashed.");
            }
        }

        /// <summary>
        /// Cleanup resources when character is removed
        /// </summary>
        public void Dispose()
        {
            StopAutoRefresh();
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
