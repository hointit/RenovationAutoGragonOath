# CLAUDE.md
This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.
## Project Overview

**AutoDragonOath** is a modern .NET 6 WPF application that monitors Dragon Oath (Thiên Long Bát Bộ) game characters in real-time by reading game process memory. Version 1.0 focuses on **monitoring only** - no automation capabilities are implemented yet.

This is a clean rewrite of the legacy MicroAuto 6.9 application, using MVVM architecture with well-documented code instead of obfuscated class names.

## Build Commands

```bash
# Build the project (Debug)
dotnet build --configuration Debug

# Build the project (Release)
dotnet build --configuration Release

# Run the application
dotnet run

# Clean build artifacts
dotnet clean
```

**Output Location**: `bin/Debug/net6.0-windows/AutoDragonOath.exe` or `bin/Release/net6.0-windows/AutoDragonOath.exe`

**Requirements**:
- .NET 6 SDK or later
- Windows OS (WPF is Windows-only)
- Administrator privileges may be required for memory reading

## Architecture Overview

### MVVM Pattern

Clean separation using the MVVM pattern:

**Models/** (`CharacterInfo.cs`, `SkillInfo`)
- Data models with `INotifyPropertyChanged` for automatic UI updates
- Represents character data: name, level, HP%, MP%, coordinates, etc.

**ViewModels/** (`MainViewModel.cs`)
- Business logic for the main window
- Manages `ObservableCollection<CharacterInfo>`
- Handles auto-refresh timer (2-second interval)
- Exposes `ICommand` properties for UI actions

**Views/** (`MainWindow.xaml`)
- WPF UI with data binding to ViewModel
- Dark theme interface
- Color-coded HP status (Green >70%, Orange 30-70%, Red <30%)

**Services/**
- `GameProcessMonitor.cs` - Scans for "Game.exe" processes and reads character data
- `MemoryReader.cs` - Low-level memory reading via Win32 `ReadProcessMemory` API
- `AddressFinder.cs` - Diagnostic tool to find correct memory addresses when game updates break hardcoded offsets

**Helpers/**
- `RelayCommand.cs` - Standard MVVM command implementation

### Key Components

#### GameProcessMonitor
Combines functionality from legacy `Class0.cs` and `GClass0.cs`:
- Scans for processes named "Game"
- Creates `MemoryReader` instance for each process
- Follows pointer chains to read character stats
- Returns list of `CharacterInfo` objects

#### MemoryReader
Ported from legacy `Class7.cs` with clean code:
- `OpenProcess()` - Opens process handle with `PROCESS_VM_READ` permissions
- `ReadInt32()` - Read 32-bit integers from memory
- `ReadFloat()` - Read floating-point coordinates
- `ReadString()` - Read ASCII strings with Vietnamese encoding conversion
- `FollowPointerChain()` - Navigate multi-level pointer chains (e.g., `[base, +12, +344, +4]`)

#### AddressFinder
Diagnostic utility for when game updates break memory addresses:
- `ScanForCObjectManagerPointer()` - Brute-force scan for new base addresses
- `TestAddressChain()` - Validate if a pointer chain works
- `ValidateStatsBase()` - Check if an address contains valid character data
- `GenerateDiagnosticReport()` - Output current address status

## Memory Address System

All character data is read via **pointer chains** from the game's base address.

### Base Pointer Chains

```csharp
// Entity base (for coordinates)
int[] ENTITY_BASE_POINTER = { 2381824, 12 };

// Stats base (for HP, MP, level, name)
int[] STATS_BASE_POINTER = { 2381824, 12, 340, 4 };

// Map base (for map ID)
int[] MAP_BASE_POINTER = { 2381824, 13692 };

// Pet base (for pet HP)
int[] PET_BASE_POINTER = { 7319540, 299356 };
```

### Character Stats Offsets
From stats base address:
- **Character Name**: +48 (30-byte ASCII string)
- **Level**: +92 (int32)
- **Current HP**: +1752 (int32)
- **Max HP**: +1856 (int32)
- **Current MP**: +1756 (int32)
- **Max MP**: +1860 (int32)
- **Experience**: +2408 (int32)
- **Pet ID**: +2356 (int32)

### Character Position Offsets
From entity base address:
- **X Coordinate**: +92 (float)
- **Y Coordinate**: +100 (float)

### Map Offsets
From map base address:
- **Map ID**: +96 (int32)

### Pet Offsets
From pet array base, each pet entry is 92 bytes:
- **Pet Current HP**: +(index × 92) + 40
- **Pet Max HP**: +(index × 92) + 44
- **Pet ID Check**: +(index × 92) + 36

**IMPORTANT**: These addresses are hardcoded and **will break when the game updates**. Use `AddressFinder` to locate new base addresses.

## Data Flow

1. **Process Scanning** (every 2 seconds when auto-refresh enabled)
   - `MainViewModel` timer triggers `RefreshCharacters()`
   - Calls `GameProcessMonitor.ScanForGameProcesses()`

2. **Memory Reading** (per process)
   - `GameProcessMonitor` creates `MemoryReader(processId)`
   - Calls `FollowPointerChain()` to get base addresses
   - Reads stats at base + offsets
   - Packages data into `CharacterInfo` objects

3. **UI Update** (automatic via MVVM)
   - `ObservableCollection<CharacterInfo>` updates
   - WPF data binding refreshes UI automatically
   - No manual UI manipulation needed

## Pointer Chain Explanation

Example: `[2381824, 12, 340, 4]`

1. Read value at address `(Game.exe base + 2381824)` → get pointer A
2. Read value at address `(A + 12)` → get pointer B
3. Read value at address `(B + 340)` → get pointer C
4. Read value at address `(C + 4)` → **final stats base address**

Then read character data: `statsBase + 48` = character name, `statsBase + 92` = level, etc.

## Vietnamese Encoding

The game uses proprietary Vietnamese character encoding. `MemoryReader.ConvertVietnameseEncoding()` converts common characters:
- `¸` → `é`
- `µ` → `õ`
- `Ó` → `ó`
- etc.

More mappings can be added as needed based on the original `Class4.smethod_2()`.

## Skills System

**Current Implementation**: F1-F12 placeholders only.

The original MicroAuto 6.9 code **does not read skill names from memory** - it only automates pressing F1-F12 keys. To read actual skill names would require additional reverse engineering of the game's skill data structures.

## Troubleshooting Memory Addresses

If character data shows as 0 or invalid:

1. **Run Diagnostic Report**:
   ```csharp
   AddressFinder.GenerateDiagnosticReport(processId);
   ```
   Check Debug output to see which pointer chains are failing.

2. **Scan for New Base Address**:
   ```csharp
   var candidates = AddressFinder.ScanForCObjectManagerPointer(processId);
   ```
   This will brute-force scan memory for the new base pointer.

3. **Update Constants in GameProcessMonitor.cs**:
   ```csharp
   private static readonly int[] STATS_BASE_POINTER = { NEW_BASE, 12, 340, 4 };
   ```

## Code Differences from Legacy

**Legacy MicroAuto 6.9** (obfuscated):
```csharp
public int method_40() {
    return this.class7_0.method_0(this.method_29() + 92);
}
```

**AutoDragonOath** (clean):
```csharp
/// <summary>
/// Read character level from memory
/// </summary>
characterInfo.Level = memoryReader.ReadInt32(statsBase + OFFSET_LEVEL);
```

## Technical Notes

- **Thread Safety**: All UI updates happen on UI thread via `DispatcherTimer`
- **Error Handling**: Silent failures for inaccessible processes, defensive null checks throughout
- **Performance**: Minimal CPU usage (~1-2%), approximately 25 memory reads per character per update
- **Administrator Rights**: May be required for `OpenProcess()` with `PROCESS_VM_READ` permissions

## Version 1.0 Feature Set

**Implemented**:
- Multi-instance process detection
- Real-time character monitoring (auto-refresh every 2 seconds)
- Character stats display: Name, Level, HP%, MP%, Pet HP%, Map, Coordinates, Experience
- Color-coded HP status badges
- Dark theme UI

**Not Implemented** (planned for future versions):
- Skill automation (F1-F12 key pressing)
- HP/MP buff triggers
- Settings persistence
- Global hotkeys
- Alarm system
