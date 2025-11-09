# Dragon Oath (TLBB) AutoIt Script Documentation

**File**: `au3\tlbb.au3`
**Language**: AutoIt v3
**Game**: Dragon Oath / Thiên Long Bát Bộ (TLBB) version 3.55.1503
**Purpose**: Game automation script with memory reading capabilities

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Memory Functions](#memory-functions)
4. [Game Memory Addresses](#game-memory-addresses)
5. [GUI Components](#gui-components)
6. [Automation Features](#automation-features)
7. [Function Reference](#function-reference)
8. [Configuration](#configuration)
9. [Technical Details](#technical-details)

---

## Overview

This AutoIt script provides comprehensive automation for the Dragon Oath MMORPG by reading game process memory and simulating keyboard input. It includes:

- **Real-time character monitoring** (HP, MP, Pet HP)
- **Automated skill rotation** (F1-F10 skills with configurable delays)
- **Buff management** (HP/MP/Pet HP recovery)
- **Anti-bot detection** (alerts on captcha prompts)
- **Auto-pickup items** (color-based pixel search)
- **Emergency exit** (closes game when HP is critically low)

**Total Lines**: 6,436
**Main Sections**:
- Lines 1-118: Global constants (ComboBox, GUI, File I/O)
- Lines 119-248: Memory management functions
- Lines 249-655: Security and privilege functions
- Lines 656-5999: Windows API wrapper functions
- Lines 6000-6436: Game-specific automation logic

---

## Architecture

### High-Level Flow

```
┌─────────────────────────────────────────────────┐
│  AutoIt Script Startup                          │
│  - Detect Game Process (Game.exe)               │
│  - Open Process Memory Handle                   │
│  - Create GUI Control Panel                     │
└────────────────┬────────────────────────────────┘
                 │
        ┌────────▼────────┐
        │  Main Loop      │
        │  (While 1)      │
        └────┬─────┬──────┘
             │     │
     ┌───────▼─┐ ┌▼───────────┐
     │ read()  │ │  check()   │
     │ - Read  │ │  - Read    │
     │   GUI   │ │    Memory  │
     │   Inputs│ │  - Monitor │
     └─────────┘ │    Stats   │
                 └──────┬─────┘
                        │
           ┌────────────▼──────────────┐
           │  Automation Logic         │
           │  - Skill Rotation         │
           │  - Buff Management        │
           │  - Anti-bot Detection     │
           │  - Auto-pickup            │
           └───────────────────────────┘
```

### Component Breakdown

1. **Memory Reader**: Uses Win32 `ReadProcessMemory` API to extract character data
2. **Input Simulator**: Uses `PostMessage` to send keyboard commands to game window
3. **GUI Controller**: Manages user settings and displays real-time stats
4. **Automation Engine**: Executes skill rotations and buff logic based on conditions

---

## Memory Functions

### Core Memory API

#### `_memoryopen($pid, $access, $inherit)`
Opens a handle to the game process for memory reading.

**Parameters:**
- `$pid` - Process ID of Game.exe
- `$access` - Access rights (default: `2035711` = PROCESS_VM_READ | PROCESS_QUERY_INFORMATION)
- `$inherit` - Inherit handle (default: `1`)

**Returns:** Array `[$hKernel32, $hProcess]`

**Usage:**
```autoit
$memid = _memoryopen($pid)
```

---

#### `_memoryread($address, $handle, $type)`
Reads data from game process memory.

**Parameters:**
- `$address` - Memory address to read
- `$handle` - Handle array from `_memoryopen()`
- `$type` - Data type (default: `"dword"`)
  - `"dword"` - 32-bit integer
  - `"float"` - Floating-point number
  - `"char[N]"` - N-byte ASCII string

**Returns:** Value read from memory

**Example:**
```autoit
$hp = _memoryread($base_char + 1752, $memid)
$name = _memoryread($base_char + 48, $memid, "char[16]")
```

---

#### `_memorywrite($address, $handle, $data, $type)`
Writes data to game process memory.

**Parameters:**
- `$address` - Memory address to write
- `$handle` - Handle array
- `$data` - Data to write
- `$type` - Data type (default: `"dword"`)

**Returns:** `1` on success, `0` on failure

---

#### `_memoryclose($handle)`
Closes the process handle and releases resources.

**Parameters:**
- `$handle` - Handle array to close

**Returns:** `1` on success

---

## Game Memory Addresses

### Base Address Pointers

The script uses **multi-level pointer chains** to navigate dynamic game memory:

#### Character Stats Base
```autoit
$base_char = 6576180
$base_char = _memoryread($base_char, $memid)
$base_char = _memoryread($base_char + 100, $memid)
$base_char = _memoryread($base_char + 340, $memid)
$base_char = _memoryread($base_char + 4, $memid)
```

**Pointer Chain**: `[6576180] → +100 → +340 → +4`

This is equivalent to the C# pointer chain:
```csharp
int[] STATS_BASE_POINTER = { 2381824, 12, 340, 4 };
```

**Note**: The AutoIt base `6576180` differs from the C# base `2381824` because they may be targeting different game versions or memory layouts.

---

### Character Stat Offsets

From `$base_char`:

| Offset | Data Type | Description | Size |
|--------|-----------|-------------|------|
| +48 | char[16] | Character Name | 16 bytes |
| +1752 | int32 | Current HP | 4 bytes |
| +1756 | int32 | Current MP | 4 bytes |
| +1856 | int32 | Max HP | 4 bytes |
| +1860 | int32 | Max MP | 4 bytes |

**Usage:**
```autoit
$name    = _memoryread($base_char + 48, $memid, "char[16]")
$hp_cur  = _memoryread($base_char + 1752, $memid)
$mp_cur  = _memoryread($base_char + 1756, $memid)
$hp_max  = _memoryread($base_char + 1856, $memid)
$mp_max  = _memoryread($base_char + 1860, $memid)
```

---

### Pet HP Base
```autoit
$base_pet = 6576192
$base_pet = _memoryread($base_pet, $memid)
$base_pet = _memoryread($base_pet + 294556, $memid)
$pet = _memoryread(_memoryread(6577372, $memid) + 92, $memid)
$hppet = _memoryread($base_pet + 60 + $pet * 312, $memid)
$hppet_max = _memoryread($base_pet + 64 + $pet * 312, $memid)
```

**Pet Array Structure**:
- Each pet entry is **312 bytes**
- Current HP offset: `60 + (pet_index × 312)`
- Max HP offset: `64 + (pet_index × 312)`

---

### Combat State Base
```autoit
$base_atk = 6549696
$base_atk = _memoryread($base_atk, $memid)
$base_atk = _memoryread($base_atk + 4, $memid)
$base_atk = _memoryread($base_atk + 12, $memid)
$atk = _memoryread($base_atk + 100, $memid)
```

**Purpose**: Detects if character is currently attacking (`$atk = 1` = in combat)

---

### Monster/Target Base
```autoit
$base_mob = 6549672
$base_mob = _memoryread($base_mob, $memid)
$base_mob = _memoryread($base_mob, $memid)
$base_mob = _memoryread($base_mob + 12, $memid)
$base_mob = _memoryread($base_mob + 96, $memid)
$base_mob = _memoryread($base_mob + 44, $memid)
$base_mob = _memoryread($base_mob + 4, $memid)
$base_mob = _memoryread($base_mob + 44, $memid)
$base_mob = _memoryread($base_mob + 8, $memid)
$base_mob = _memoryread($base_mob + 44, $memid)
$base_mob = _memoryread($base_mob, $memid)
$mob = _memoryread($base_mob + 1888, $memid)
```

**Purpose**: Detects if a valid target exists (`$mob = 1` = target selected)

---

### Anti-Bot Detection Base
```autoit
$base_detect = 6559704
$base_detect = _memoryread($base_detect, $memid)
$base_detect = _memoryread($base_detect + 0, $memid)
$base_detect = _memoryread($base_detect + 12, $memid)
$detect = _memoryread($base_detect + 100, $memid)
```

**Purpose**: Detects captcha/verification prompts (`$detect = 1` = anti-bot popup active)

**Anti-Bot Response**:
```autoit
If $detect = 1 Then
    ; Play alarm sound
    SoundPlay(@WindowsDir & "\media\tada.wav", 1)
    ; Show warning
    TrayTip("", "Canh bao co cau hoi chong auto, chuong trinh se tu thoat sau 40s...", 5)
    ; Wait 40 seconds, then close game if not resolved
    If TimerDiff($out) > 40000 Then
        ProcessClose($pid)
    EndIf
EndIf
```

---

## GUI Components

### Main Window

**Title**: Character name from memory
**Class**: TianLongBaBu WndClass
**Size**: 230×445 pixels

### Control Panel Tabs

#### **Tab 1: Buff Management**

| Control | Type | Description |
|---------|------|-------------|
| HP Buff | Checkbox + ComboBox + Input | Auto-use HP item when HP% < threshold (F1-F10 key) |
| MP Buff | Checkbox + ComboBox + Input | Auto-use MP item when MP% < threshold (F1-F10 key) |
| Pet Buff | Checkbox + ComboBox + Input | Auto-summon pet when Pet HP% < threshold (F1-F10 key) |

**Default Values**:
- HP: F9 key, trigger at <80%
- MP: F8 key, trigger at <50%
- Pet: F10 key, trigger at <50%

---

#### **Tab 2: Skill Rotation (Combo)**

Configurable skill rotation with 7 slots:

| Skill | Default Key | Default Delay |
|-------|-------------|---------------|
| Skill 2 | F2 | 20 seconds |
| Skill 3 | F3 | 25 seconds |
| Skill 4 | F4 | 30 seconds |
| Skill 5 | F5 | 40 seconds |
| Skill 6 | F6 | 50 seconds |
| Skill 7 | F7 | 60 seconds |

**Logic**: Each skill is pressed after its delay has elapsed (+ 3 second base delay).

---

#### **Tab 3: Options**

| Option | Description |
|--------|-------------|
| Auto Pickup Items | Uses pixel search to detect item drops and right-click to pick them up |
| Buff TTPTC-NM | Special buff mode that clicks top-left corner of window |
| Continuous Targeting | Presses F11 (Tab key) every N seconds to switch targets |
| Emergency Exit | Closes game when HP drops below threshold (default: <10%) |
| Fixed Position | *Not implemented* |

---

### Real-Time Status Display

```
┌─────────────────────────┐
│ Character Name          │
│ Hp: [▓▓▓▓▓▓▓░░░] 70%   │  (Orange/Red color)
│ Mp: [▓▓▓▓▓▓▓░░░] 70%   │  (Blue color)
│ Pet:[▓▓▓▓░░░░░░] 40%   │
│                         │
│ [START] Button          │
└─────────────────────────┘
```

---

## Automation Features

### 1. Skill Rotation Engine

**Function**: `ok()` loop (lines 6294-6358)

**Logic**:
1. Check if automation is enabled (`$ok = True`)
2. Read attack/mob state from memory
3. If not in combat or no target:
   - Press F11 (Tab key) to acquire target
   - Optionally run auto-pickup
4. Press F1 (basic attack)
5. For each configured skill (F2-F7):
   - Check if delay timer has expired
   - Send key press via `PostMessage`
   - Reset timer

**Pseudocode**:
```autoit
While 1
    If $ok Then
        If ($atk = 0 OR $mob = 0) Then
            PostMessage($handle, WM_KEYDOWN, VK_F11, 0)  ; Tab target
        EndIf

        PostMessage($handle, WM_KEYUP, VK_F1, 0)  ; Basic attack

        If TimerDiff($timer2) > ($delay2 * 1000 + 3000) Then
            PostMessage($handle, WM_KEYUP, $skill2, 0)
            $timer2 = TimerInit()
        EndIf
        ; ... repeat for skills 3-7
    EndIf
WEnd
```

---

### 2. Buff Management

**Function**: `check()` (lines 6213-6292)

**HP Recovery Logic**:
```autoit
If $check_hp1 = 1 AND $hp_cur * 100 / $hp_max < $hp Then
    If $check_ttptc1 = 1 Then
        ; TTPTC Mode: Click top-left corner and use buff repeatedly until HP restored
        Do
            MouseClick("left", $winpos[0] + 30, $winpos[1] + 60, 10, 10)
            PostMessage($handle, WM_KEYUP, $ttptc_input1, 0)
            Sleep(500)
            $hp_cur = _memoryread($base_char + 1752, $memid)
        Until $hp_cur * 100 / $hp_max > $hp
    Else
        ; Standard Mode: Use HP item once every 10 seconds
        If TimerDiff($buff_hp) > 10000 Then
            PostMessage($handle, WM_KEYUP, $skill_hp1, 0)
            $buff_hp = TimerInit()
        EndIf
    EndIf
EndIf
```

**MP Recovery**: Triggers when MP% < threshold, presses configured key (F8)

**Pet Recovery**: Triggers when Pet HP% < threshold, presses configured key (F10)

---

### 3. Auto-Pickup (Pixel Search)

**Function**: `nhatdo()` (lines 6383-6435)

**Mechanism**:
1. Search screen for specific item colors (11 predefined colors)
2. If color found, move mouse to coordinates
3. Right-click to pick up item

**Searched Colors** (RGB hex):
- `15918219` (0xF2D67B) - Gold/yellow item
- `15721348` (0xEFDE04) - Bright yellow
- `13745532` (0xD19A7C) - Brown/tan
- `13945207` (0xD4D487) - Pale yellow
- `16775059` (0xFFE493) - Light gold
- `12693615` (0xC1B1EF) - Purple
- `13813621` (0xD2D175) - Olive
- `13550451` (0xCED773) - Light green
- `16777128` (0xFFFFA8) - Pale yellow
- `15192964` (0xE7E004) - Yellow-green

**Search Area**: Center 50% of screen (avoids UI elements)

---

### 4. Anti-Bot Alert System

**Trigger**: `$detect = 1` (captcha popup detected)

**Response**:
1. Show game window
2. Play alarm sound (`tada.wav`)
3. Display tray notification
4. Wait 40 seconds
5. If captcha not cleared, close game process

**Purpose**: Prevents ban by alerting user to manual intervention needed

---

### 5. Emergency Exit

**Trigger**: HP% < configured threshold (default: 10%)

**Action**: `ProcessClose($pid)` - immediately terminates game

**Use Case**: Prevent character death during AFK farming

---

## Function Reference

### GUI Functions

#### `savesetting()`
Saves current GUI settings to INI file.

**File Path**: `@ScriptDir\<CharacterName>.ini`

**Sections**:
- `[Pet]` - Pet buff settings
- `[Hp]` - HP buff settings
- `[Mp]` - MP buff settings
- `[Skill2]` through `[Skill7]` - Skill rotation settings
- `[Opt]` - Optional features (pickup, TTPTC, targeting, emergency exit)

---

#### `loadsetting()`
Loads settings from INI file and populates GUI controls.

---

#### `hide()`
Hides the main window (keeps running in system tray).

---

#### `show()`
Shows the main window from system tray.

---

### Automation Functions

#### `ok()`
Toggles automation on/off. Called when START button is clicked.

**Hotkey**: `Ctrl+P`

---

#### `read()`
Reads all GUI control values into global variables.

**Variables Updated**:
- `$hp`, `$mp`, `$hp_pet` - Buff thresholds
- `$skill2` through `$skill7` - Skill virtual key codes
- `$delay2` through `$delay7` - Skill delay timers
- `$check_hppet1`, `$check_hp1`, `$check_mp1` - Buff enable flags

---

#### `check()`
Main monitoring function. Reads character stats from memory and triggers buffs/alerts.

**Responsibilities**:
1. Read HP/MP/Pet HP from memory
2. Update GUI progress bars
3. Check for anti-bot detection
4. Trigger emergency exit if HP critical
5. Trigger buff items if HP/MP/Pet HP below thresholds

---

#### `nhatdo()`
Auto-pickup function using pixel search for item colors.

**Requirements**: Game window must be active (`WinActive("[Class:TianLongBaBu WndClass]")`)

---

### Utility Functions

#### `terminate()`
Clean exit (Exit 0).

---

#### `exitme()`
Immediate exit.

---

#### `showmessage()`
Displays author info and contact details.

---

#### `tray()`
Minimizes to system tray with notification.

---

#### `showtray()`
Restores from system tray.

---

## Configuration

### INI File Format

**File Name**: `<CharacterName>.ini`

**Example**:
```ini
[Pet]
Check=1
%=50
Key=F10

[Hp]
Check=1
%=80
Key=F9

[Mp]
Check=1
%=50
Key=F8

[Skill2]
Check=1
Key=F2
Delay=20

[Skill3]
Check=1
Key=F3
Delay=25

[Skill4]
Check=1
Key=F4
Delay=30

[Skill5]
Check=1
Key=F5
Delay=40

[Skill6]
Check=1
Key=F6
Delay=50

[Skill7]
Check=1
Key=F7
Delay=60

[Opt]
Nhat_do=1
TTPTC=0
Key_TTPTC=F9
Check_train=0
Tg_train=3
Check_thoat=1
%_thoat=10
Co_dinh=0
```

---

## Technical Details

### Process Communication

**Method**: Windows `PostMessage` API

**Message Types**:
- `256` (`WM_KEYDOWN`) - Key press event
- `257` (`WM_KEYUP`) - Key release event

**Example**:
```autoit
_winapi_postmessage($handle, 257, $skill_hp1, 0)
```

This sends a key-up event for the HP skill key to the game window.

---

### Memory Reading Security

**Privilege Escalation**: The script includes privilege elevation functions (`setprivilege()`) to obtain `SeDebugPrivilege`, allowing memory reads from protected processes.

**Required Access Rights**:
- `PROCESS_VM_READ` (0x10)
- `PROCESS_QUERY_INFORMATION` (0x400)

**Combined Default**: `2035711` (0x1F0FFF = PROCESS_ALL_ACCESS)

---

### Game Window Detection

**Window Class**: `TianLongBaBu WndClass`
**Window Title Validation**:
```autoit
If StringLeft(WinGetTitle("[Class:TianLongBaBu WndClass]"), 22) <> "Thien Long Bat Bo 3.55.1503" Then
    ; Exit if wrong game version
EndIf
```

---

### Timer Management

**Global Timers**:
- `$timer` - Main automation timer
- `$timer2` through `$timer7` - Skill cooldown timers
- `$buff_hp` - HP buff cooldown timer
- `$out` - Anti-bot timeout timer

**Timer Functions**:
- `TimerInit()` - Start new timer
- `TimerDiff($timer)` - Get elapsed milliseconds

---

### Vietnamese Text Handling

The script uses Vietnamese labels and messages:
- `"Tu dong nhat do (Beta)"` = "Auto pickup (Beta)"
- `"Buff TTPTC - NM"` = Special buff type
- `"Target lien tuc sau:"` = "Continuous targeting after:"
- `"Thoat game, Hp <"` = "Exit game when HP <"
- `"Co dinh"` = "Fixed position"
- `"Canh bao co cau hoi chong auto"` = "Warning: anti-bot question detected"

---

## Comparison with C# Implementation

### Similarities

| Feature | AutoIt Script | C# AutoDragonOath |
|---------|--------------|-------------------|
| Memory Reading | `_memoryread()` | `MemoryReader.ReadInt32()` |
| Process Handle | `OpenProcess` via DllCall | P/Invoke OpenProcess |
| Character Stats | HP, MP, Pet HP | HP, MP, Pet HP, Level, Exp |
| Pointer Chains | Multi-level reads | `FollowPointerChain()` |

### Differences

| Aspect | AutoIt Script | C# AutoDragonOath |
|--------|--------------|-------------------|
| Architecture | Procedural script | MVVM pattern |
| UI Framework | AutoIt GUI | WPF with data binding |
| Base Addresses | 6576180, 6576192, etc. | 2381824 (different game version?) |
| Automation | Full (skills, buffs, combat) | Monitoring only (v1.0) |
| Anti-Bot | Active detection + alert | Not implemented |
| Auto-Pickup | Pixel search for colors | Not implemented |
| Skill Rotation | F1-F10 with timers | Placeholder only |

### Address Mapping

| Purpose | AutoIt Base | C# Base | Offset Chain |
|---------|-------------|---------|--------------|
| Character Stats | 6576180 | 2381824 | →+100→+340→+4 (AutoIt) vs →+12→+340→+4 (C#) |
| Pet HP | 6576192 | 7319540 | Different base entirely |
| Combat State | 6549696 | N/A | Not read in C# |

**Hypothesis**: The AutoIt script may target a different game version or use different base address calculation methods.

---

## Usage Example

### Startup Sequence

1. Launch Game.exe
2. Run tlbb.au3 script
3. Script detects game window by class name
4. Opens process memory handle
5. Creates GUI with character name in title
6. Loads settings from INI file (if exists)
7. Waits for user to click START button

### Automation Flow

1. User clicks **START** button (or presses `Ctrl+P`)
2. Script begins main loop:
   ```
   Every 0.5 seconds:
   - Read HP/MP/Pet HP from memory
   - Update GUI progress bars
   - Check for anti-bot popup
   - Trigger emergency exit if HP critical
   - Trigger buffs if HP/MP/Pet HP low

   If automation enabled:
   - Check if in combat
   - If not, press Tab to target enemy
   - Press F1 to attack
   - Press configured skills when timers expire
   - Optionally auto-pickup items
   ```

3. User clicks **STOP** to pause automation

### Saving Settings

1. Configure desired settings in GUI
2. Click **Save** button
3. Settings saved to `<CharacterName>.ini`
4. Settings persist between script restarts

---

## Limitations and Risks

### Game Version Dependency
The script is hardcoded for **Dragon Oath version 3.55.1503**. Memory addresses will break if:
- Game is updated to a new patch
- Game executable is modified
- Game uses address space layout randomization (ASLR)

### Detection Risk
The script uses:
- **Memory reading** - Detectable by anti-cheat systems
- **Input simulation** - Can be detected via timing analysis
- **No randomization** - Predictable skill rotation patterns

### Reliability Issues
- **Pixel search auto-pickup** - Fails if screen resolution/UI layout changes
- **Fixed delays** - May not match actual skill cooldowns
- **No error recovery** - Script crashes if game closes unexpectedly

### Security Concerns
The script requires:
- **Administrator privileges** - To read protected process memory
- **SeDebugPrivilege** - Elevated system access
- **Process termination rights** - Can forcibly close the game

---

## Conclusion

This AutoIt script represents a complete game automation solution for Dragon Oath, demonstrating:

1. **Advanced memory manipulation** - Multi-level pointer dereferencing
2. **Real-time monitoring** - Continuous stat tracking via memory reads
3. **Event-driven automation** - Conditional logic based on game state
4. **User-friendly GUI** - Configuration interface with persistence

While the C# **AutoDragonOath** project provides a modern, maintainable architecture with MVVM and clean code, this AutoIt script offers a battle-tested reference implementation with full automation capabilities that can guide future feature development.

**Key Takeaways for C# Project**:
- The AutoIt script's memory addresses differ significantly - requires reverse engineering for current game version
- Anti-bot detection is critical for safe automation
- Skill rotation logic can be ported once memory reading is stable
- Auto-pickup requires additional research (pixel colors or minimap parsing)
- Emergency exit feature should be implemented for safety

---

## Author Information

**Original Author**: HuyKhang
**Website**: http://clbgamesvn.com
**Contact**: 01234774778

**Documentation Created**: 2025
**Documentation Purpose**: Educational analysis for AutoDragonOath project
