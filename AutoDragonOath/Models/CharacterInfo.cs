using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        private string _currentTitle = "";
        private int _titleCount;

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

        public string CurrentTitle
        {
            get => _currentTitle;
            set
            {
                _currentTitle = value;
                OnPropertyChanged();
            }
        }

        public int TitleCount
        {
            get => _titleCount;
            set
            {
                _titleCount = value;
                OnPropertyChanged();
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a skill that the character has
    /// </summary>
    public class SkillInfo
    {
        public string SkillName { get; set; } = string.Empty;
        public string Hotkey { get; set; } = string.Empty;
        public int SkillId { get; set; }
        public bool IsAvailable { get; set; }

        public SkillInfo(string name, string hotkey, int id = 0)
        {
            SkillName = name;
            Hotkey = hotkey;
            SkillId = id;
            IsAvailable = false;
        }
    }
}
