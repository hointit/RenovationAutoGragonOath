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

**MicroAuto 6.9** - A Windows Forms (.NET Framework 2.0) application written in C# that automates gameplay for a Vietnamese MMORPG. The code has been deliberately obfuscated with generic class names.

## Build Commands

```bash
# Build the solution (Debug configuration)
msbuild "MicroAuto 6.0.sln" /p:Configuration=Debug /p:Platform=AnyCPU

# Build for x86 specifically
msbuild "MicroAuto 6.0.sln" /p:Configuration=Debug /p:Platform=x86

# Build Release version
msbuild "MicroAuto 6.0.sln" /p:Configuration=Release /p:Platform=AnyCPU
```

Output location: `bin/Debug/MicroAuto 6.0.exe`

## Architecture Overview

### Core Components

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

### Memory Interaction

**Class5.cs** - Win32 API wrappers for:
- Window handle enumeration (EnumWindows, GetForegroundWindow)
- Memory reading (ReadProcessMemory, OpenProcess)
- Input simulation (SendMessage, PostMessage)

**Class4.cs** - Screen and window utilities
**Class7.cs** - Memory address calculations
**Class8.cs** - Application settings (window position, music path)

### UI Components

**FormAlarm.cs** - Alert dialog for automation events
**Class6.cs** - Global hotkey handler registration

### Data Structures

**GClass1.cs** - INI file handler
**GClass2-4.cs** - Supporting utilities (music playback, etc.)

## Key Memory Addresses & Offsets

The application reads game memory to extract:
- Character HP/MP percentages
- Pet HP percentage
- Player coordinates (X, Y)
- Experience points
- Character name
- Login state

These are accessed via pointer chains starting from base addresses in the game process.

## Automation Features

1. **Skill Rotation**: F1-F12 keys pressed at configurable intervals
2. **Buff Management**: Auto-use items when Pet/HP/MP fall below thresholds
3. **Radius Detection**: Only attack monsters within range of saved coordinates
4. **HP-Based Targeting**: Only attack monsters with HP in specified range
5. **Auto-Exit**: Leave map when player HP is critically low
6. **Captcha Detection**: Monitors for anti-bot prompts
7. **Multi-Instance**: Manage multiple game windows simultaneously

## Global Hotkeys

- **Pause**: Toggle all automation on/off
- **PageUp**: Show/hide main window
- **PageDown**: Auto-pickup items in focused window
- **Insert**: Toggle automation for focused window

## Settings Storage

- Location: `bin/Debug/Settings/`
- Format: INI files named by character/process ID
- Contains: Skill timers, buff thresholds, coordinates, key bindings

## Development Notes

- This is decompiled/obfuscated code - class names are not semantic
- Targets .NET Framework 2.0 (ancient, from 2005)
- Uses legacy WinForms with manual memory management
- References Vietnamese game forum: vipautopro.com
- Contains hardcoded memory offsets that will break if game updates
