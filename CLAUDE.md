# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## WARNING

This codebase is obfuscated source code for a game automation tool. Claude Code will NOT:
- Improve, enhance, or augment this code
- Add new automation features
- Fix bugs that would make the automation more effective
- Help distribute or deploy this software

Claude Code CAN:
- Analyze the code to understand what it does
- Document the architecture
- Answer questions about code behavior
- Generate reports about functionality

## Project Overview

This repository contains **two projects**:

1. **MicroAuto 6.9** (Legacy) - A Windows Forms (.NET Framework 2.0) application with obfuscated code that automates gameplay for a Vietnamese MMORPG (Dragon Oath / Thiên Long Bát Bộ). The code has been deliberately obfuscated with generic class names.

2. **RenovationAutoDragonOath** (Modern) - A .NET 6 WPF rewrite using MVVM architecture located in `RenovationAutoDragonOath/` subdirectory. Version 1.0 focuses on character monitoring only (no automation yet).

## Build Commands

### Legacy Application (MicroAuto 6.9)

```bash
# Build the solution (Debug configuration)
msbuild "MicroAuto 6.0.sln" /p:Configuration=Debug /p:Platform=AnyCPU

# Build for x86 specifically
msbuild "MicroAuto 6.0.sln" /p:Configuration=Debug /p:Platform=x86

# Build Release version
msbuild "MicroAuto 6.0.sln" /p:Configuration=Release /p:Platform=AnyCPU
```

Output location: `bin/Debug/MicroAuto 6.0.exe` or `bin/x86/Debug/MicroAuto 6.0.exe`

### Renovation Project (Modern WPF)

```bash
cd RenovationAutoDragonOath
dotnet build RenovationAutoDragonOath.sln --configuration Release
```

Output location: `RenovationAutoDragonOath/bin/Release/net6.0-windows/RenovationAutoDragonOath.exe`

## Architecture Overview

### Legacy Application (MicroAuto 6.9)

#### Core Components

**FormMain.cs** - Main UI form that provides:
- Multi-account game instance management via ListView
- Per-account automation configuration (skills F1-F12, buffs, HP/MP monitoring)
- Global hotkeys (Pause, PageUp, PageDown, Insert)
- System tray integration

**Class0.cs** - Game Process Manager:
- Discovers running "game.exe" processes every 20 seconds
- Creates GClass0 instances for each game window
- Manages lifecycle via delegates (GDelegate0, GDelegate1)

**GClass0.cs** - Account Automation Controller:
- One instance per game process
- Manages 12 skill timers (F1-F12)
- Handles buff automation (Pet, HP, MP)
- Memory reading for character stats (HP%, MP%, coordinates, experience)
- Executes keyboard commands via Win32 SendMessage

**Class2.cs** - Settings Persistence:
- INI file reader/writer (via GClass1)
- Stores per-account configuration in `Settings/` directory
- 89 methods for reading/writing automation settings

#### Memory Interaction

**Class5.cs** - Win32 API wrappers for:
- Window handle enumeration (EnumWindows, GetForegroundWindow)
- Memory reading (ReadProcessMemory, OpenProcess)
- Input simulation (SendMessage, PostMessage)

**Class4.cs** - Screen and window utilities
**Class7.cs** - Memory address calculations
**Class8.cs** - Application settings (window position, music path)

#### UI Components

**FormAlarm.cs** - Alert dialog for automation events
**Class6.cs** - Global hotkey handler registration

#### Data Structures

**GClass1.cs** - INI file handler
**GClass2-4.cs** - Supporting utilities (music playback, etc.)

### Renovation Project (Modern WPF)

#### Architecture Pattern
MVVM (Model-View-ViewModel) with clean separation of concerns:

**Models/**
- `CharacterInfo.cs` - Character data model with INotifyPropertyChanged

**ViewModels/**
- `MainViewModel.cs` - Main window logic, manages character collection

**Views/**
- `MainWindow.xaml` - Material Design-inspired UI with ListView

**Services/**
- `MemoryReader.cs` - Game memory reading (ported from Class7)
- `GameProcessMonitor.cs` - Process scanning and character info reading (ported from Class0/GClass0)

**Helpers/**
- `RelayCommand.cs` - MVVM command implementation

#### Key Differences from Legacy
- Modern .NET 6 WPF instead of .NET Framework 2.0 WinForms
- Clean, documented code instead of obfuscated names
- MVVM data binding instead of manual UI updates
- Auto-refresh every 2 seconds via DispatcherTimer
- Color-coded HP status (Green >70%, Orange 30-70%, Red <30%)
- Currently monitoring-only (automation features planned for v2.0+)

## Key Memory Addresses & Offsets

Both applications read game memory to extract:
- Character HP/MP percentages
- Pet HP percentage
- Player coordinates (X, Y)
- Experience points
- Character name
- Login state
- Map ID (converted to readable names)

These are accessed via pointer chains starting from base addresses in the game process:

### Character Stats
- **Pointer Chain**: `[7319476] → +12 → +344 → +4`
- **Character Name**: Base + 48
- **Current HP**: Base + 2292
- **Max HP**: Base + 2400
- **Current MP**: Base + 2296
- **Max MP**: Base + 2404

### Character Position
- **Pointer Chain**: `[7319476] → +12`
- **X Coordinate**: EntityBase + 92
- **Y Coordinate**: EntityBase + 100

### Map Information
- **Pointer Chain**: `[6870940] → +14232`
- **Map ID**: MapBase + 96

**Note**: These hardcoded addresses will break if the game updates.

## Automation Features

### Legacy Application (Fully Implemented)

1. **Skill Rotation**: F1-F12 keys pressed at configurable intervals
2. **Buff Management**: Auto-use items when Pet/HP/MP fall below thresholds
3. **Radius Detection**: Only attack monsters within range of saved coordinates
4. **HP-Based Targeting**: Only attack monsters with HP in specified range
5. **Auto-Exit**: Leave map when player HP is critically low
6. **Captcha Detection**: Monitors for anti-bot prompts
7. **Multi-Instance**: Manage multiple game windows simultaneously

### Renovation Project (Version 1.0 - Monitoring Only)

**Current Features:**
- Real-time character monitoring (auto-refresh every 2 seconds)
- Multi-instance process detection
- Character name, HP%, MP%, coordinates, map display
- Color-coded HP status badges

**Planned Features** (v2.0+):
- Skill automation
- HP/MP buff triggers
- Combat automation
- Configuration profiles

## Global Hotkeys (Legacy Application Only)

- **Pause**: Toggle all automation on/off
- **PageUp**: Show/hide main window
- **PageDown**: Auto-pickup items in focused window
- **Insert**: Toggle automation for focused window

*Note: Renovation project does not yet implement hotkeys.*

## Settings Storage

### Legacy Application
- Location: `bin/Debug/Settings/`
- Format: INI files named by character/process ID
- Contains: Skill timers, buff thresholds, coordinates, key bindings

### Renovation Project
- Currently no persistent settings (v1.0)
- Planned for future versions

## Development Notes

### Legacy Application
- Decompiled/obfuscated code - class names (Class0-10, GClass0-4) are not semantic
- Targets .NET Framework 2.0 (from 2005)
- Uses legacy WinForms with manual memory management
- References Vietnamese game forum: vipautopro.com
- Contains hardcoded memory offsets that break on game updates

### Renovation Project
- Clean, documented C# code following modern conventions
- Targets .NET 6 with WPF
- MVVM pattern with proper data binding
- Memory addresses ported from legacy but still hardcoded
- Detailed README at `RenovationAutoDragonOath/README.md`

### Code Naming Patterns

**Legacy obfuscation pattern:**
- Classes: `Class0` through `Class10`, `GClass0` through `GClass4`
- Methods: `method_0()`, `method_1()`, etc.
- Delegates: `GDelegate0`, `GDelegate1`

**Renovation clean code pattern:**
- Semantic names: `CharacterInfo`, `MainViewModel`, `MemoryReader`
- Documented with XML comments
- Follows C# naming conventions

### Repository Structure
```
microauto-6.9/
├── MicroAuto 6.0.sln              # Legacy solution
├── Class0.cs - Class10.cs         # Obfuscated classes
├── GClass0.cs - GClass4.cs        # Obfuscated classes
├── FormMain.cs, FormAlarm.cs      # Legacy UI
├── bin/Debug/Settings/            # Legacy config storage
└── RenovationAutoDragonOath/      # Modern rewrite
    ├── RenovationAutoDragonOath.sln
    ├── Models/, ViewModels/, Views/
    ├── Services/MemoryReader.cs
    └── README.md
```
