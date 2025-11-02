# AutoDragonOath

**Modern WPF Application for Monitoring Dragon Oath Game Characters**

A clean, modern WPF application using MVVM pattern to monitor Dragon Oath (Thiên Long Bát Bộ) game characters in real-time, based on the MicroAuto 6.9 legacy codebase.

## Features

### ✅ Implemented (Version 1.0)

- **Multi-Instance Monitoring**: Automatically detects and monitors all running Dragon Oath game processes
- **Real-time Character Information**:
  - **Character Name** - Read from game memory
  - **Level** (⭐ Important) - Displayed prominently with special highlighting
  - **HP %** - Color-coded status (Green >70%, Orange 30-70%, Red <30%)
  - **MP %** - Mana percentage with blue indicator
  - **Pet HP %** - Pet health monitoring
  - **Map Location** - Current zone/map name
  - **Coordinates** - Character position (X, Y)
  - **Experience** - Current experience points
  - **Status** - Not Logged In / Idle / Running
- **Skills List** - F1-F12 skill slot display (placeholder for future enhancement)
- **Auto-refresh** - Updates every 2 seconds when enabled
- **Dark Theme UI** - Modern, professional dark interface
- **Selected Character Details** - Detailed view of selected character with skills

## Project Structure

```
AutoDragonOath/
├── AutoDragonOath.csproj      # .NET 6 WPF project file
├── App.xaml                    # Application entry point
├── App.xaml.cs
├── MainWindow.xaml             # Main UI window
├── MainWindow.xaml.cs
├── Models/
│   └── CharacterInfo.cs        # Character data model with INotifyPropertyChanged
├── ViewModels/
│   └── MainViewModel.cs        # Main window ViewModel (MVVM pattern)
├── Services/
│   ├── MemoryReader.cs         # Game memory reading (ported from Class7)
│   └── GameProcessMonitor.cs   # Process scanning & character info reading
└── Helpers/
    └── RelayCommand.cs         # MVVM command implementation
```

## Architecture

### MVVM Pattern

**Clean separation of concerns:**

- **Model**: `CharacterInfo.cs` - Represents character data with property change notifications
- **ViewModel**: `MainViewModel.cs` - Business logic, manages character collection, commands
- **View**: `MainWindow.xaml` - UI presentation with data binding

### Services

#### MemoryReader
Ported from original `Class7.cs` with clean, documented code:
- `ReadInt32()` - Read 32-bit integers from memory
- `ReadFloat()` - Read floating-point numbers
- `ReadString()` - Read ASCII strings with Vietnamese encoding conversion
- `FollowPointerChain()` - Navigate through multi-level pointers

#### GameProcessMonitor
Combines functionality from original `Class0.cs` and `GClass0.cs`:
- Scans for "game" processes
- Reads character statistics from memory via pointer chains
- Converts map IDs to readable names
- Calculates HP/MP percentages
- Reads pet information
- Provides skill placeholders (F1-F12)

## Memory Addresses

Based on reverse engineering of MicroAuto 6.9:

### Character Stats Base
- **Pointer Chain**: `[7319476, 12, 344, 4]` → Stats Base Address
- **Character Name**: Base + 48 (30-byte string)
- **Level**: Base + 92 ⭐ (int32)
- **Current HP**: Base + 2292 (int32)
- **Max HP**: Base + 2400 (int32)
- **Current MP**: Base + 2296 (int32)
- **Max MP**: Base + 2404 (int32)
- **Experience**: Base + 2300 (int32)
- **Pet ID**: Base + 2356 (int32)

### Character Position
- **Pointer Chain**: `[7319476, 12]` → Entity Base Address
- **X Coordinate**: EntityBase + 92 (float)
- **Y Coordinate**: EntityBase + 100 (float)

### Map Information
- **Pointer Chain**: `[6870940, 14232]` → Map Base Address
- **Map ID**: MapBase + 96 (int32)

### Pet Information
- **Pointer Chain**: `[7319540, 299356]` → Pet Array Base
- **Pet Entry Size**: 92 bytes per pet
- **Pet Current HP**: PetBase + (index × 92) + 40
- **Pet Max HP**: PetBase + (index × 92) + 44
- **Pet ID Check**: PetBase + (index × 92) + 36

## Requirements

- **Operating System**: Windows 10 or later (WPF is Windows-only)
- **.NET Runtime**: .NET 6.0 Windows Desktop Runtime
- **Game**: Dragon Oath (Thiên Long Bát Bộ) game client must be running
- **Permissions**: Administrator rights may be required for memory reading

## Building the Project

### Prerequisites

1. Install .NET 6 SDK from: https://dotnet.microsoft.com/download/dotnet/6.0

2. Or install Visual Studio 2022 with:
   - .NET Desktop Development workload
   - WPF support

### Build Instructions

#### Using .NET CLI
```bash
cd AutoDragonOath
dotnet build --configuration Release
```

#### Using Visual Studio
1. Open `AutoDragonOath.csproj` in Visual Studio 2022
2. Set build configuration to `Debug` or `Release`
3. Press `F6` or click `Build > Build Solution`

### Running the Application
```bash
cd bin/Release/net6.0-windows
AutoDragonOath.exe
```

Or press `F5` in Visual Studio to run with debugging.

## Usage

### Initial Setup
1. Launch Dragon Oath game client(s)
2. Run `AutoDragonOath.exe`
3. Characters will be automatically detected and displayed

### Monitoring Characters
1. Click **"Start Auto"** to enable automatic refresh every 2 seconds
2. View character information in the main DataGrid:
   - Character Name
   - Level (highlighted in yellow/gold)
   - HP % (color-coded badge)
   - MP % (blue badge)
   - Pet HP %
   - Map name
   - Coordinates
   - Experience
   - Status
3. Click on a character to view detailed information in the bottom panel
4. Click **"Stop"** to pause auto-refresh
5. Click **"Refresh"** to manually update the list

### Understanding the Display

**HP % Color Coding**:
- **Green**: HP > 70% (Healthy)
- **Orange**: HP 30-70% (Warning)
- **Red**: HP < 30% (Critical)

**Level Display**:
- Displayed in **bold yellow/gold** color for emphasis
- This is a key stat for character progression

**Character Status**:
- **"Not Logged In"** - Character at login screen
- **"Idle"** - Character logged in, automation not running
- **"Running"** - Automation active (future feature)

**Skills List**:
- Shows F1-F12 skill slots
- Currently placeholder names ("Skill 1" - "Skill 12")
- Can be customized in future versions

## Key Differences from MicroAuto 6.9

| Feature | MicroAuto 6.9 (Legacy) | AutoDragonOath (Modern) |
|---------|------------------------|-------------------------|
| **Framework** | .NET Framework 2.0 | .NET 6 |
| **UI** | Windows Forms | WPF |
| **Architecture** | Procedural, obfuscated | MVVM, clean code |
| **Code Quality** | Decompiled, no comments | Fully documented |
| **Data Binding** | Manual UI updates | MVVM automatic binding |
| **Level Display** | Not prominently shown | **Highlighted prominently** ⭐ |
| **Automation** | Full (skills, buffs, farming) | None (v1.0 - monitoring only) |
| **Extensibility** | Difficult | Easy to extend |

## Technical Notes

### Memory Reading
- Uses Windows `ReadProcessMemory` API via P/Invoke
- Requires process handle with `PROCESS_VM_READ` permission (2035711 access rights)
- All memory addresses are hardcoded (may break on game updates)

### Thread Safety
- UI updates via `DispatcherTimer` on UI thread (2-second interval)
- No background threading for memory reads
- Memory operations are synchronous

### Error Handling
- Graceful handling of process termination
- Silent failure for inaccessible processes
- Defensive null checks throughout

### Performance
- Scan interval: 2 seconds (configurable)
- Memory reads: ~25 per character per update
- Minimal CPU usage (~1-2%)

## Skills System

### Current Implementation (v1.0)

The skills list currently shows **F1-F12 placeholders** because:

1. **The original MicroAuto 6.9 code doesn't read skill names from memory**
2. It only automates pressing F1-F12 keys at configured intervals
3. Skill names/IDs are not stored in easily accessible memory locations

**What's Displayed**:
- Hotkey (F1-F12)
- Placeholder name ("Skill 1" - "Skill 12")

### Future Enhancement

To read actual skill names, we would need to:
1. Reverse engineer the game's skill data structure in memory
2. Find pointer chains to skill information
3. Map skill IDs to skill names
4. Implement skill memory reading in `GameProcessMonitor`

This is possible but requires additional reverse engineering work.

## Troubleshooting

### "No characters detected"
- Ensure Dragon Oath game client is running
- Verify game process is named "game.exe"
- Try running as Administrator
- Check if game is responding

### "Access denied" errors
- Run application as Administrator
- Check Windows Defender / Antivirus settings
- Verify process permissions

### HP/MP showing 0% or 100%
- Character may not be logged in yet
- Memory addresses may have changed (game update)
- Check game version compatibility

### Level showing 0
- Character not logged in
- Memory read failed
- Game may have been updated (breaking memory offsets)

### Map showing "Unknown"
- Character in unsupported zone
- Add new map ID mapping in `GameProcessMonitor.ConvertMapIdToName()`

## Future Roadmap

### Version 2.0 (Planned)
- [ ] Skill automation (F1-F12 key pressing)
- [ ] HP/MP buff triggers
- [ ] Pet buff automation
- [ ] Settings persistence (per-character configurations)

### Version 3.0 (Planned)
- [ ] Actual skill name reading from memory (requires reverse engineering)
- [ ] Radius-based farming
- [ ] Target selection automation
- [ ] Configuration profiles

### Version 4.0 (Planned)
- [ ] Multi-character synchronized control
- [ ] Alarm system for captcha detection
- [ ] Statistics and logging
- [ ] Export/import configurations

## Comparison with Legacy Codebase

### Code Quality Improvements

**MicroAuto 6.9** (Obfuscated):
```csharp
public int method_40() {
    return this.class7_0.method_0(this.method_29() + 92);
}
```

**AutoDragonOath** (Clean):
```csharp
/// <summary>
/// Read character level from memory
/// </summary>
public int Level {
    get => memoryReader.ReadInt32(statsBase + OFFSET_LEVEL);
}
```

### Architecture Improvements

**MicroAuto 6.9**:
- Procedural code with manual UI updates
- Obfuscated class names (Class0, GClass0)
- No separation of concerns
- Tightly coupled components

**AutoDragonOath**:
- MVVM pattern with automatic data binding
- Semantic naming (GameProcessMonitor, MemoryReader)
- Clean separation: Models, ViewModels, Services
- Loosely coupled, testable components

## License & Disclaimer

**This tool is for educational purposes only.**

- Game automation may violate Terms of Service
- Use at your own risk
- No warranty provided
- May be detected as cheat software
- Based on reverse engineering of MicroAuto 6.9

### Original Software
- **MicroAuto 6.9** by VipAutoPro.com
- **Game**: Dragon Oath (Thiên Long Bát Bộ)

### This Renovation
- Modern C# WPF implementation
- MVVM architecture pattern
- Clean, documented codebase
- Educational demonstration of:
  - Modern .NET 6 development
  - WPF and MVVM patterns
  - Memory reading techniques
  - Process monitoring
  - Reverse engineering documentation

## Credits

- **Original Software**: MicroAuto 6.9 by VipAutoPro.com
- **Renovation**: Clean modern WPF rewrite with MVVM
- **Documentation**: Based on analysis of decompiled codebase
- **Game**: Dragon Oath (Thiên Long Bát Bộ)

## Contact

For questions about the architecture or implementation:
- Refer to the comprehensive documentation in the parent directory:
  - `MEMORY_READING_SYSTEM.md`
  - `SETTINGS_SYSTEM.md`
  - `COMPLETE_CLASS_REFERENCE.md`
  - `PROJECT_ARCHITECTURE.md`

For the original MicroAuto software:
- Forum: http://forum.vipautopro.com
- Website: http://www.vipautopro.com

---

**Built with .NET 6 WPF and MVVM Pattern**

**⭐ Special Attention to Character Level Display ⭐**
