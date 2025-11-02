# Complete Class Reference - MicroAuto 6.9

**Project**: MicroAuto 6.9 (Dragon Oath Game Automation Tool)
**Framework**: .NET Framework 2.0
**Language**: C# (Decompiled/Obfuscated)

## Overview

This document provides a complete reference of all classes in the MicroAuto 6.9 project, their responsibilities, and relationships. Class names are obfuscated (ClassX, GClassX) but functionality is documented.

---

## Entry Point

### Class1.cs
**Type**: Static class with Main() entry point
**Responsibility**: Application bootstrap

```csharp
internal static class Class1
```

#### Key Methods

| Method | Purpose |
|--------|---------|
| `Main()` | Entry point: Initializes WinForms, launches FormMain |

**Initialization Sequence**:
1. `Application.EnableVisualStyles()`
2. `Application.SetCompatibleTextRenderingDefault(false)`
3. `Application.Run(new FormMain())`

**Related**: FormMain.cs

---

## Process Management Layer

### Class0.cs
**Type**: Game Process Manager (Singleton-like behavior)
**Responsibility**: Discovers and manages game process instances

```csharp
internal class Class0
```

#### Properties

| Property | Type | Purpose |
|----------|------|---------|
| `dictionary_0` | `Dictionary<int, GClass0>` | Map of ProcessID → Automation Controller |
| `timer_0` | `Timer` | Process scanner (20-second interval) |
| `gdelegate0_0` | `GDelegate0` | Event: New process discovered |

#### Key Methods

| Method | Signature | Purpose |
|--------|-----------|---------|
| Constructor | `Class0()` | Scans for "game" processes, creates GClass0 instances |
| `method_0()` | `void method_0(GDelegate0)` | Subscribe to process discovery event |
| `method_1()` | `void method_1(GDelegate0)` | Unsubscribe from event |
| `method_2()` | `void method_2(int processId)` | Handle process removal |
| `method_3()` | `void method_3()` | Initialize scanner timer |
| `timer_0_Tick()` | `void timer_0_Tick(object, EventArgs)` | Scan for new processes every 20 seconds |

#### Workflow

```
Application Start
    ↓
Class0 Constructor
    ↓
GetProcessesByName("game")
    ↓
For each process:
    → Create GClass0(processId)
    → Add to dictionary_0
    ↓
Start timer_0 (20 seconds)
    ↓
Every 20 seconds:
    → Scan for new "game" processes
    → Create GClass0 for new processes
    → Fire gdelegate0_0 event
```

**Important**: Only detects processes named "game.exe"

**Related**: GClass0.cs, FormMain.cs

---

## Automation Controller Layer

### GClass0.cs
**Type**: Per-Process Automation Controller
**Responsibility**: Manages all automation for a single game instance

```csharp
public class GClass0
```

#### Properties (Timers)

| Property | Type | Purpose | Default Interval |
|----------|------|---------|------------------|
| `timer_0` - `timer_11` | `Timer` | F1-F12 skill timers | Configurable (3-360 sec) |
| `timer_12` | `Timer` | Main update loop | ~100ms |
| `timer_13` | `Timer` | Pet buff check | Configurable |
| `timer_14` | `Timer` | HP buff check | Configurable |
| `timer_15` | `Timer` | MP buff check | Configurable |

#### Properties (State)

| Property | Type | Purpose |
|----------|------|---------|
| `int_12` | `int` | Process ID |
| `intptr_0` | `IntPtr` | Window handle |
| `class7_0` | `Class7` | Memory reader instance |
| `class2_0` | `Class2` | Settings manager |
| `bool_7` | `bool` | Automation enabled/disabled |
| `ulong_0` | `ulong` | Experience value |
| `int_0` | `int` | Pet buff threshold (%) |
| `int_1` | `int` | HP buff threshold (%) |
| `int_2` | `int` | MP buff threshold (%) |
| `int_3` | `int` | Radius value (game units) |
| `int_4`, `int_5` | `int` | Saved coordinates (X, Y) |
| `string_0` | `string` | Base skill hotkey |
| `string_1` | `string` | Pet buff hotkey |
| `string_2` | `string` | Target selection hotkey |
| `string_3` | `string` | HP buff hotkey |
| `string_4` | `string` | MP buff hotkey |
| `string_5` | `string` | Captcha detection flag |
| `bool_10` | `bool` | Use radius mode (vs coordinates) |

#### Key Methods (Character Stats)

| Method | Return Type | Purpose |
|--------|-------------|---------|
| `method_33()` | `int` | Get HP percentage (0-100) |
| `method_36()` | `int` | Get MP percentage (0-100) |
| `method_38()` | `string` | Get character name |
| `method_40()` | `string` | Get process ID as string |
| `method_41()` | `uint` | Get experience value |
| `method_43()` | `int` | Get pet HP percentage |
| `method_45()` | `int` | Get map ID |

#### Key Methods (Coordinates)

| Method | Return Type | Purpose |
|--------|-------------|---------|
| `method_11()` | `int` | Get entity base address |
| `method_12()` | `int` | Get X coordinate address |
| `method_13()` | `int` | Get X coordinate value |
| `method_16()` | `int` | Get Y coordinate address |
| `method_17()` | `int` | Get Y coordinate value |
| `method_20()` | `float` | Get target X coordinate (monster) |
| `method_21()` | `float` | Get target Y coordinate (monster) |

#### Key Methods (Distance Calculations)

| Method | Return Type | Purpose |
|--------|-------------|---------|
| `method_2()` | `int` | Distance from saved coordinates |
| `method_3()` | `int` | Distance from saved coordinates (float-based) |

#### Key Methods (Automation Control)

| Method | Signature | Purpose |
|--------|-----------|---------|
| `method_6()` | `bool method_6()` | Get automation state |
| `method_7()` | `void method_7(bool)` | Set automation state (enable/disable) |
| `method_55()` | `void method_55()` | Enable all automation timers |
| `method_56()` | `void method_56()` | Disable all automation timers |
| `method_57()` | `void method_57()` | Press F-key skills (F1-F12) |
| `method_58()` | `void method_58()` | Check and execute buffs (Pet/HP/MP) |
| `method_59()` | `void method_59()` | Execute radius/coordinate checking |
| `method_60()` | `void method_60()` | Main automation loop tick |
| `method_61()` | `void method_61()` | Auto-pickup items |
| `method_62()` | `void method_62()` | Update window title with character name |

#### Key Methods (Teleportation)

| Method | Signature | Purpose |
|--------|-----------|---------|
| `method_9()` | `void method_9(int x, int y)` | Initiate teleport to coordinates |
| `method_10()` | `void method_10()` | Execute teleportation (thread) |

#### Key Methods (Experience Tracking)

| Method | Return Type | Purpose |
|--------|-------------|---------|
| `method_0()` | `ulong` | Get saved experience value |
| `method_1()` | `void method_1(ulong)` | Set saved experience value |
| `method_5()` | `uint` | Calculate XP/hour |
| `method_8()` | `int` | Get captcha alert level (1-3) |

#### Automation Flow

```
GClass0 Constructor(processId)
    ↓
Create Class7(processId) - Memory reader
Create Class2(processId) - Settings manager
    ↓
Load all settings from INI file
    ↓
Initialize 16 timers (F1-F12, main loop, buffs)
    ↓
Start timer_12 (main automation loop)
    ↓
Every ~100ms:
    → method_60() (main loop tick)
        → method_57() (press F-key skills if timers elapsed)
        → method_58() (check HP/MP/Pet buffs)
        → method_59() (radius/coordinate checks)
        → Update UI via FormMain
```

**Important**: One GClass0 instance per game process

**Related**: Class0.cs, Class7.cs, Class2.cs, FormMain.cs

---

## Memory Management Layer

### Class7.cs
**Type**: Memory Reader/Writer
**Responsibility**: Read/write game process memory

```csharp
internal class Class7
```

#### Properties

| Property | Type | Purpose |
|----------|------|---------|
| `intptr_0` | `IntPtr` | Process handle |

#### Key Methods

| Method | Signature | Purpose |
|--------|-----------|---------|
| Constructor | `Class7(int processId)` | Open process handle with VM_READ/WRITE |
| `method_0()` | `int method_0(int address)` | Read Int32 from memory |
| `method_1()` | `int method_1(int[] pointerChain)` | Follow pointer chain, return final address |
| `method_2()` | `float method_2(int[] pointerChain)` | Follow pointer chain, read float |
| `method_3()` | `float method_3(int address)` | Read float from memory |
| `method_4()` | `int method_4(int address, float value)` | Write float to memory |
| `method_5()` | `string method_5(int address)` | Read string (30 bytes) from memory |
| `method_6()` | `void method_6()` | Close process handle |

#### Win32 API

| API | Purpose |
|-----|---------|
| `OpenProcess` | Open handle to game process |
| `ReadProcessMemory` | Read memory from process |
| `WriteProcessMemory` | Write memory to process |
| `CloseHandle` | Release process handle |

**See**: MEMORY_READING_SYSTEM.md for detailed documentation

**Related**: GClass0.cs

---

## Settings Management Layer

### Class2.cs
**Type**: Per-Character Settings Manager
**Responsibility**: Load/save automation configuration

```csharp
internal class Class2
```

#### Properties

| Property | Type | Purpose |
|----------|------|---------|
| `gclass1_0` | `GClass1` | INI file handler |

#### Key Methods (89 getter/setter pairs)

| Methods | Purpose |
|---------|---------|
| `method_0/1` | IsSkill (master toggle) |
| `method_2/3` | RadiusEnable |
| `method_4/5` | IsUseTarget |
| `method_6/7` | IsOnlyAttackFixHP |
| `method_8/9` | BuffPetEnable |
| `method_10/11` | BuffHPEnable |
| `method_12/13` | BuffMPEnable |
| `method_14/15` through `method_38/39` | F1Enable through F12Enable |
| `method_40/41` through `method_64/65` | F1Delay through F12Delay |
| `method_66/67` | BuffPetPercent |
| `method_68/69` | BuffHPPercent |
| `method_70/71` | BuffMPPercent |
| `method_72/73` | BuffPetKey |
| `method_74/75` | BuffHPKey |
| `method_76/77` | BuffMPKey |
| `method_78/79` | RadiusValue |
| `method_80/81` | RadiusX |
| `method_82/83` | RadiusY |
| `method_84/85` | BaseSkill |
| `method_86/87` | TargetKey |
| `method_88` | String to bool converter |

**File**: `Settings/{ProcessID}.ini`

**See**: SETTINGS_SYSTEM.md for detailed documentation

**Related**: GClass1.cs, GClass0.cs

### GClass1.cs
**Type**: INI File Handler
**Responsibility**: Low-level INI file read/write

```csharp
public class GClass1
```

#### Properties

| Property | Type | Purpose |
|----------|------|---------|
| `string_0` | `string` | INI file path |

#### Key Methods

| Method | Signature | Purpose |
|--------|-----------|---------|
| Constructor | `GClass1(string filePath)` | Initialize with INI file path |
| `method_0()` | `void method_0(string section, string key, string value)` | Write to INI |
| `method_1()` | `string method_1(string section, string key)` | Read from INI |

#### Win32 API

| API | Purpose |
|-----|---------|
| `WritePrivateProfileString` | Write INI key-value pair |
| `GetPrivateProfileString` | Read INI key-value pair |

**Related**: Class2.cs

---

## User Interface Layer

### FormMain.cs
**Type**: Main application window
**Responsibility**: UI for managing multiple game instances

```csharp
public partial class FormMain : Form
```

#### Properties

| Property | Type | Purpose |
|----------|------|---------|
| `dictionary_0` | `Dictionary<int, GClass0>` | Reference to Class0's process dictionary |
| `listViewGame` | `ListView` | Display all game instances |
| `tabPageAcc` | `TabPage` | Per-character configuration panel |
| Multiple UI controls | Various | Checkboxes, numeric inputs, combo boxes for settings |

#### Key Methods

| Method | Signature | Purpose |
|--------|-----------|---------|
| `method_0()` | `void method_0(object, KeyEventArgs)` | Handle global hotkeys (Pause, PageUp, PageDown, Insert) |
| `method_1()` | `void method_1(int processId)` | Update UI for specific process |
| `method_2()` | `void method_2()` | Refresh all UI controls from current character settings |
| `method_3()` | `void method_3()` | Rebuild ListView with all game instances |
| `method_4()` | `void method_4()` | Update ListView item values (HP%, MP%, etc.) |
| `method_6()` | `void method_6()` | Show/hide main window |
| `method_7()` | `void method_7()` | Pause/unpause all automation |

#### UI Components

**ListView** columns:
1. Account/Character Name
2. HP %
3. Pet HP %
4. Captcha alert
5. MP %
6. Distance from saved coordinates

**Configuration Panel**:
- Master toggles (IsSkill, Radius, Buffs)
- F1-F12 enable checkboxes + delay inputs
- Buff settings (Pet/HP/MP thresholds + hotkeys)
- Radius configuration (value, coordinates)
- Combat settings (base skill, target key)
- Exit threshold

**Global Hotkeys**:
- **Pause**: Toggle all automation
- **PageUp**: Show/hide window
- **PageDown**: Auto-pickup items (focused window)
- **Insert**: Toggle automation for focused window

**Related**: GClass0.cs, Class0.cs, Class6.cs

### FormAlarm.cs
**Type**: Alert dialog
**Responsibility**: Show alerts for automation events

```csharp
public partial class FormAlarm : Form
```

**Purpose**: Display captcha alerts, low HP warnings, etc.

**Related**: GClass0.cs

---

## Window Management Layer

### Class5.cs
**Type**: Window manipulation utilities
**Responsibility**: Win32 window operations

```csharp
internal class Class5
```

#### Properties

| Property | Type | Purpose |
|----------|------|---------|
| `intptr_0` | `IntPtr` | Window handle |
| `struct0_0` | `Struct0` | Window rectangle (position/size) |

#### Key Methods

| Method | Return Type | Purpose |
|--------|-------------|---------|
| Constructor | `Class5(IntPtr)` | Initialize with window handle |
| `method_0()` | `int` | Get window X position |
| `method_3()` | `int` | Get window width |
| `method_4()` | `int` | Get window height |
| `method_5()` | `int` | Get window center X |
| `method_6()` | `int` | Get window center Y |
| `method_7()` | `FormWindowState` | Get window state (normal/minimized/maximized) |
| `method_8()` | `void` | Bring window to foreground |

#### Win32 API

| API | Purpose |
|-----|---------|
| `SetWindowText` | Set window title |
| `GetWindowRect` | Get window position/size |
| `GetForegroundWindow` | Get currently focused window |
| `MoveWindow` | Move/resize window |
| `SetForegroundWindow` | Bring window to front |

**Related**: GClass0.cs, FormMain.cs

### Class6.cs
**Type**: Global hotkey handler
**Responsibility**: Register and handle system-wide hotkeys

```csharp
// Specific implementation details not in provided excerpts
```

**Hotkeys Registered**:
- Pause
- PageUp
- PageDown
- Insert

**Related**: FormMain.cs

### Class9.cs
**Type**: Window show/hide utilities
**Responsibility**: ShowWindow API wrapper

```csharp
// Minimal class with ShowWindow API
```

#### Win32 API

| API | Purpose |
|-----|---------|
| `ShowWindow` | Show, hide, minimize, maximize window |

**Related**: Class5.cs

---

## Utility Layer

### Class4.cs
**Type**: Screen and encoding utilities
**Responsibility**: Vietnamese character conversion, screen calculations

```csharp
// Method names: smethod_0, smethod_1, smethod_2, etc.
```

#### Key Methods

| Method | Signature | Purpose |
|--------|-----------|---------|
| `smethod_2()` | `string smethod_2(string)` | Convert game encoding to Vietnamese Unicode |

**Related**: Class7.cs (used for character name reading)

### Class8.cs
**Type**: Global application settings
**Responsibility**: Application-wide configuration (not per-character)

```csharp
// Stores: Window position, music path, experience tracking
```

**File**: `General.ini`

**Related**: Class2.cs, FormMain.cs

### Class10.cs
**Type**: Miscellaneous utilities
**Responsibility**: Additional helper functions

```csharp
// Implementation details not in provided excerpts
```

---

## Input Simulation Layer

### GClass2.cs
**Type**: Mouse event simulation
**Responsibility**: Send mouse clicks to game window

```csharp
// Uses SendMessage/PostMessage for mouse input
```

**Related**: GClass0.cs

### Class3.cs
**Type**: Resources/Embedded data
**Responsibility**: Embedded resources (icons, images, sounds)

**Files**:
- `Class3.cs` - Resource class
- `Class3.Designer.cs` - Auto-generated resource designer

---

## Delegate Definitions

### GDelegate0.cs
**Type**: Event delegate
**Signature**: `public delegate void GDelegate0();`

**Purpose**: Notify when new game process is discovered

**Usage**:
- Class0 fires this event when new "game" process detected
- FormMain subscribes to refresh ListView

### GDelegate1.cs
**Type**: Event delegate
**Signature**: `public delegate void GDelegate1(int processId);`

**Purpose**: Notify when game process is removed

**Usage**:
- GClass0 fires this event when process terminates
- Class0 subscribes to remove from dictionary

### GDelegate2.cs
**Type**: Event delegate (if exists)
**Signature**: Unknown (not in provided excerpts)

---

## Supporting Classes

### GClass3.cs
**Type**: Unknown utility class
**Purpose**: Not documented in provided excerpts

### GClass4.cs
**Type**: Unknown utility class
**Purpose**: Not documented in provided excerpts

---

## Resources

### Resources.cs
**Type**: Resource accessor
**Responsibility**: Access embedded resources (icons, sounds, images)

**Related**: Class3.cs

### Properties/AssemblyInfo.cs
**Type**: Assembly metadata
**Content**: Version, copyright, assembly attributes

---

## Class Dependency Graph

```
Class1 (Entry Point)
    ↓
FormMain (UI)
    ↓
Class0 (Process Manager)
    ↓
GClass0 (Automation Controller)
    ├→ Class7 (Memory Reader)
    ├→ Class2 (Settings Manager)
    │    └→ GClass1 (INI Handler)
    ├→ Class5 (Window Manager)
    ├→ GClass2 (Mouse Input)
    └→ Class4 (Utilities)
```

---

## Key Design Patterns

### Pattern 1: One Controller Per Process

```
Each game process → One GClass0 instance → Independent automation
```

### Pattern 2: Settings Persistence

```
GClass0 ↔ Class2 ↔ GClass1 ↔ kernel32.dll ↔ INI File
```

### Pattern 3: Timer-Based Automation

```
16 Timer objects in GClass0:
- 12 for F1-F12 skills
- 1 for main loop
- 3 for buffs (Pet/HP/MP)
```

### Pattern 4: Memory Reading Pipeline

```
GClass0 → Class7 → ReadProcessMemory → Game Memory
```

### Pattern 5: Event-Driven Process Discovery

```
Class0 (Scanner) → GDelegate0 event → FormMain (Update UI)
```

---

## Summary Statistics

| Category | Count |
|----------|-------|
| Total Classes | 27 |
| Main Classes | 10 (Class0-9, Class10) |
| G-Classes | 5 (GClass0-4) |
| Delegates | 3 (GDelegate0-2) |
| Form Classes | 2 (FormMain, FormAlarm) |
| Designer Files | 2 |
| Resource Files | 2 |
| Timer Objects (per GClass0) | 16 |
| Settings Methods (Class2) | 89 |
| Win32 API Calls | 15+ |

---

## Obfuscation Patterns

### Class Naming

- **Class0-10**: Main logic classes (numbered sequentially)
- **GClass0-4**: Supporting classes (G prefix)
- **FormX**: UI classes (not obfuscated)

### Method Naming

- **method_0, method_1, method_2...**: Sequential numbering
- **smethod_X**: Static methods (s prefix)
- Event handlers: Sometimes preserved (e.g., `timer_0_Tick`)

### Variable Naming

- **int_0, int_1**: Integer fields (numbered)
- **bool_0, bool_1**: Boolean fields
- **string_0, string_1**: String fields
- **timer_0, timer_1**: Timer objects
- **intptr_0**: IntPtr fields

---

## Project Structure

```
MicroAuto 6.9/
├── Class0.cs - Class10.cs       # Core logic (obfuscated)
├── GClass0.cs - GClass4.cs      # Supporting classes
├── GDelegate0.cs - GDelegate2.cs # Event delegates
├── FormMain.cs / .Designer.cs   # Main UI
├── FormAlarm.cs / .Designer.cs  # Alert UI
├── Class3.cs / .Designer.cs     # Resources
├── Resources.cs                 # Resource accessor
├── Properties/
│   └── AssemblyInfo.cs          # Assembly metadata
├── bin/
│   └── Debug/
│       ├── MicroAuto 6.0.exe    # Compiled output
│       └── Settings/            # Per-character INI files
└── MicroAuto 6.0.sln            # Visual Studio solution
```

---

## Quick Reference: Most Important Classes

### Top 5 Core Classes

1. **GClass0** - Automation controller (400+ lines, 60+ methods)
2. **Class7** - Memory reader (essential for all game data)
3. **Class2** - Settings manager (89 methods for persistence)
4. **Class0** - Process scanner (creates/manages GClass0 instances)
5. **FormMain** - Main UI (displays and controls automation)

### Top 5 Utility Classes

1. **GClass1** - INI file handler
2. **Class5** - Window management
3. **Class4** - Vietnamese encoding conversion
4. **Class8** - Global application settings
5. **GClass2** - Mouse input simulation

---

## Related Documentation

- **MEMORY_READING_SYSTEM.md** - Detailed Class7 documentation
- **SETTINGS_SYSTEM.md** - Detailed Class2/GClass1 documentation
- **CLAUDE.md** - High-level project overview
- **RenovationAutoDragonOath/README.md** - Modern rewrite documentation

---

**Note**: This documentation is based on decompiled/obfuscated code. Original semantic names are unknown. Method numbers (method_0, method_1, etc.) do not indicate functionality - always refer to purpose descriptions.
