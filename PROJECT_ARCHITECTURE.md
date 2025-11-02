# MicroAuto 6.9 - Complete Project Architecture

**Project**: MicroAuto 6.9 (Dragon Oath / Thiên Long Bát Bộ Game Automation Tool)
**Framework**: .NET Framework 2.0 (C# 2.0, released 2005)
**UI Framework**: Windows Forms
**Target Game**: Dragon Oath (Vietnamese MMORPG)
**Code Status**: Decompiled/Obfuscated

## Table of Contents

1. [System Overview](#system-overview)
2. [Architectural Layers](#architectural-layers)
3. [Data Flow](#data-flow)
4. [Component Interactions](#component-interactions)
5. [Automation Workflow](#automation-workflow)
6. [Memory Architecture](#memory-architecture)
7. [Settings Architecture](#settings-architecture)
8. [Threading Model](#threading-model)
9. [Security & Detection](#security--detection)
10. [Performance Characteristics](#performance-characteristics)

---

## System Overview

### Purpose

MicroAuto 6.9 is a **multi-instance game automation tool** that:
- Monitors multiple Dragon Oath game windows simultaneously
- Reads game memory to extract character stats (HP, MP, coordinates, etc.)
- Automates skill usage (F1-F12 hotkeys)
- Automates buff management (HP/MP potions, pet buffs)
- Provides radius-based farming with coordinate restrictions
- Persists per-character configurations

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      User Interface (FormMain)               │
│  - ListView with all game instances                          │
│  - Per-character configuration panel                         │
│  - Global hotkeys (Pause, PageUp, PageDown, Insert)         │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Process Manager (Class0)                        │
│  - Scans for "game.exe" every 20 seconds                    │
│  - Creates GClass0 for each process                         │
│  - Manages process lifecycle                                │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼ (Creates)
┌─────────────────────────────────────────────────────────────┐
│       Automation Controller (GClass0) - One per process     │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  16 Timers: F1-F12, Main Loop, 3 Buffs              │  │
│  └──────────────────────────────────────────────────────┘  │
│                         │                                    │
│  ┌──────────────────────┼────────────────────────────────┐ │
│  │                      │                                 │ │
│  ▼                      ▼                                 ▼ │
│ Memory Reader      Settings Manager             Window Mgr │
│  (Class7)             (Class2)                   (Class5)  │
│  - Read HP/MP         - Load/Save INI            - Focus   │
│  - Read coords        - 89 settings              - Title   │
│  - Write coords       - Per-character            - Position│
└─────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   Game Process Memory                        │
│  [0x006F8C24] → Player Object → Stats, Coords, Name        │
│  [0x0068B91C] → Map Object → Map ID                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Architectural Layers

### Layer 1: User Interface

**Components**: FormMain, FormAlarm

**Responsibilities**:
- Display all game instances in a ListView
- Provide configuration UI for each character
- Handle global hotkeys
- Update display with real-time character stats
- Allow user to enable/disable automation per character

**Technology**: Windows Forms with manual data binding

### Layer 2: Process Management

**Component**: Class0

**Responsibilities**:
- Discover running "game.exe" processes
- Create one GClass0 per process
- Remove GClass0 when process terminates
- Notify UI of process changes via delegates

**Scan Frequency**: Every 20 seconds

### Layer 3: Automation Control

**Component**: GClass0 (one instance per game process)

**Responsibilities**:
- Manage 16 timers for automation
- Execute skill rotation (F1-F12)
- Monitor and trigger buffs (Pet/HP/MP)
- Check radius/coordinate restrictions
- Read character stats from memory
- Send keyboard input to game window
- Persist settings to INI file

**Update Frequency**: Main loop ~10Hz (every 100ms)

### Layer 4: Memory Interaction

**Component**: Class7

**Responsibilities**:
- Open process handle with VM_READ/WRITE
- Read int32, float, string from memory
- Follow pointer chains (multi-level pointers)
- Write float values (for teleportation)

**API**: Win32 ReadProcessMemory, WriteProcessMemory

### Layer 5: Settings Persistence

**Components**: Class2, GClass1

**Responsibilities**:
- Save/load 89 configuration settings per character
- Use Windows INI file format
- Immediate persistence (no caching)

**Storage**: `Settings/{ProcessID}.ini`

### Layer 6: Window Management

**Components**: Class5, Class6, Class9

**Responsibilities**:
- Get/set window position and size
- Bring windows to foreground
- Register global hotkeys
- Set window titles

**API**: Win32 window management functions

---

## Data Flow

### 1. Application Startup Flow

```
[Application Start]
    │
    ├─► Class1.Main()
    │       └─► new FormMain()
    │               └─► new Class0()
    │                       │
    │                       ├─► Process.GetProcessesByName("game")
    │                       │
    │                       └─► For each process:
    │                               ├─► new GClass0(processId)
    │                               │       ├─► new Class7(processId)
    │                               │       ├─► new Class2(processId)
    │                               │       ├─► Load settings from INI
    │                               │       └─► Start 16 timers
    │                               │
    │                               └─► Add to dictionary_0
    │
    └─► Start Class0 timer (20-second scan)
```

### 2. Process Discovery Flow

```
[Every 20 seconds]
    │
    ├─► Class0.timer_0_Tick()
    │       │
    │       ├─► Process.GetProcessesByName("game")
    │       │
    │       ├─► For each new process:
    │       │       ├─► new GClass0(processId)
    │       │       └─► Add to dictionary_0
    │       │
    │       └─► Fire GDelegate0 event
    │               │
    │               └─► FormMain.method_3()
    │                       └─► Refresh ListView
```

### 3. Character Stats Reading Flow

```
[Every ~100ms per character]
    │
    ├─► GClass0.method_60() (main loop)
    │       │
    │       ├─► method_33() - Get HP%
    │       │       └─► Class7.method_1([7319476, 12, 344, 4])
    │       │               └─► Read statsBase + 2292 (current HP)
    │       │               └─► Read statsBase + 2400 (max HP)
    │       │               └─► Return (current/max * 100)
    │       │
    │       ├─► method_36() - Get MP%
    │       │       └─► (similar to HP)
    │       │
    │       ├─► method_38() - Get character name
    │       │       └─► Class7.method_5(statsBase + 48)
    │       │               └─► ReadProcessMemory (30 bytes)
    │       │               └─► Class4.smethod_2() - Convert encoding
    │       │
    │       └─► Update FormMain ListView
```

### 4. Skill Automation Flow

```
[F1 Timer Elapsed]
    │
    ├─► GClass0.timer_0_Tick()
    │       │
    │       ├─► Check if automation enabled (bool_7)
    │       │
    │       ├─► Check radius restriction (if enabled)
    │       │       └─► method_2() - Calculate distance
    │       │               └─► sqrt((currentX - savedX)² + (currentY - savedY)²)
    │       │
    │       ├─► If within radius:
    │       │       └─► SendMessage(hwnd, WM_KEYDOWN, VK_F1, ...)
    │       │
    │       └─► Restart timer (next interval)
```

### 5. Buff Automation Flow

```
[Every ~100ms per character]
    │
    ├─► GClass0.method_58() (check buffs)
    │       │
    │       ├─► If BuffPetEnable:
    │       │       ├─► method_43() - Get pet HP%
    │       │       └─► If pet HP% < threshold:
    │       │               └─► SendKey(buffPetKey)
    │       │
    │       ├─► If BuffHPEnable:
    │       │       ├─► method_33() - Get player HP%
    │       │       └─► If HP% < threshold:
    │       │               └─► SendKey(buffHPKey)
    │       │
    │       └─► If BuffMPEnable:
    │               ├─► method_36() - Get player MP%
    │               └─► If MP% < threshold:
    │                       └─► SendKey(buffMPKey)
```

### 6. Settings Persistence Flow

```
[User changes a setting in UI]
    │
    ├─► FormMain.f1Enable_CheckedChanged()
    │       │
    │       ├─► GClass0.timer_0.Enabled = checked
    │       │
    │       └─► GClass0.class2_0.method_15(checked)
    │               │
    │               └─► GClass1.method_0("General", "F1Enable", "True")
    │                       │
    │                       └─► WritePrivateProfileString(...)
    │                               │
    │                               └─► Settings/12345.ini updated
```

---

## Component Interactions

### Interaction Diagram

```
┌──────────┐
│ FormMain │
└────┬─────┘
     │ subscribes to
     ▼
┌─────────┐       creates      ┌──────────┐
│ Class0  ├──────────────────► │ GClass0  │
│(Scanner)│◄────────────────────┤(per-proc)│
└─────────┘   fires delegate    └────┬─────┘
                                     │
                 ┌───────────────────┼───────────────────┐
                 ▼                   ▼                   ▼
            ┌─────────┐         ┌─────────┐       ┌─────────┐
            │ Class7  │         │ Class2  │       │ Class5  │
            │(Memory) │         │(Settings)│       │(Window) │
            └─────────┘         └────┬────┘       └─────────┘
                                     │ uses
                                     ▼
                                ┌─────────┐
                                │ GClass1 │
                                │  (INI)  │
                                └─────────┘
```

### Communication Patterns

#### Pattern 1: Observer (Delegates)

```csharp
// Class0 publishes process discovery events
GDelegate0 gdelegate0_0;

// FormMain subscribes
class0.method_0(new GDelegate0(this.RefreshUI));

// Class0 fires when new process found
if (gdelegate0_0 != null) {
    gdelegate0_0();
}
```

#### Pattern 2: Dictionary Lookup

```csharp
// Class0 maintains process dictionary
Dictionary<int, GClass0> dictionary_0;

// FormMain accesses controllers
GClass0 controller = dictionary_0[processId];
int hp = controller.method_33();
```

#### Pattern 3: Aggregation

```csharp
// GClass0 owns multiple sub-components
public class GClass0 {
    private Class7 class7_0; // Memory reader
    private Class2 class2_0; // Settings manager
    private Timer[] timers;  // 16 timers
}
```

---

## Automation Workflow

### Complete Automation Cycle

```
┌─────────────────────────────────────────────────────────┐
│                  GClass0 Main Loop                      │
│                (Executes ~every 100ms)                  │
└─────────────────────────────────────────────────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        ▼                 ▼                 ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ Read Memory  │  │   Execute    │  │   Update UI  │
│              │  │  Automation  │  │              │
│ - HP %       │  │              │  │ - Refresh    │
│ - MP %       │  │ - Skills     │  │   ListView   │
│ - Pet HP %   │  │ - Buffs      │  │ - Update     │
│ - Coords     │  │ - Radius     │  │   window     │
│ - Name       │  │ - Target     │  │   title      │
│ - Map ID     │  │              │  │              │
└──────────────┘  └──────────────┘  └──────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        ▼                 ▼                 ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│   F1-F12     │  │   Pet Buff   │  │  Radius      │
│   Timers     │  │   Check      │  │  Check       │
│              │  │              │  │              │
│ Each skill   │  │ If pet HP    │  │ Distance     │
│ has own      │  │ < threshold  │  │ from saved   │
│ interval     │  │ → Press key  │  │ coords       │
└──────────────┘  └──────────────┘  └──────────────┘
        │                 │                 │
        └─────────────────┼─────────────────┘
                          ▼
┌─────────────────────────────────────────────────────────┐
│            Send Input to Game Window                     │
│                                                          │
│  SendMessage(hwnd, WM_KEYDOWN, virtualKey, ...)        │
└─────────────────────────────────────────────────────────┘
```

### Skill Rotation Example

**Configuration**:
- F1: Enabled, 5-second interval (Basic Attack)
- F2: Enabled, 10-second interval (Special Skill)
- F3: Disabled
- Radius: Enabled, 300 units from (1234, 5678)

**Execution Timeline**:

```
Time 0s:   [Check radius] → [Press F1] → [Press F2]
Time 5s:   [Check radius] → [Press F1]
Time 10s:  [Check radius] → [Press F1] → [Press F2]
Time 15s:  [Check radius] → [Press F1]
Time 20s:  [Check radius] → [Press F1] → [Press F2]
...
```

**If player moves outside radius**:
```
Time 0s:   [Check radius] → FAIL → Skip F1/F2
Time 5s:   [Check radius] → FAIL → Skip F1
Time 10s:  [Check radius] → FAIL → Skip F1/F2
...
(Skills resume when player returns to radius)
```

---

## Memory Architecture

### Game Memory Layout

```
┌────────────────────────────────────────────────────────┐
│                   Game Process Memory                   │
│                                                         │
│  [0x006F8C24] (7319476) - Player Object Pointer       │
│      │                                                  │
│      ├─► +12 → [Entity Base Address]                  │
│      │           │                                      │
│      │               │
│      │           ├─► +92 → X Coordinate (float)       │
│      │           ├─► +100 → Y Coordinate (float)                            │
│      │           └─► +344 → [Stats Object Pointer]    │
│      │                   │                              │
│      │                   ├─► +4 → [Stats Base]        │
│      │                           ├─► +48 → Character Name (string)│
│      │                           ├─► +1752 → Current HP│
│      │                           ├─► +1756 → Current MP│
│      │                           ├─► +2400 → Max HP    │
│      │                           └─► +2404 → Max MP    │
│      │                                                  │
│  [0x0068B91C] (6870940) - Map Object Pointer          │
│      │                                                  │
│      └─► +14232 → [Map Base Address]                  │
│                │                                        │
│                └─► +96 → Map ID (int32)               │
│                                                         │
└────────────────────────────────────────────────────────┘
```

### Pointer Chain Resolution

**Example: Reading Character Name**

```
1. Read [7319476] → 0x12345678 (Entity Object pointer)
2. Read [0x12345678 + 12] → 0x23456789 (Entity Base)
3. Read [0x23456789 + 344] → 0x34567890 (Stats Object pointer)
4. Read [0x34567890 + 4] → 0x45678901 (Stats Base)
5. Read [0x45678901 + 48, 30 bytes] → "MyCharacter" (string)
6. Convert encoding → "Mỹ Nhân" (Vietnamese characters)
```

### Data Types in Memory

| Game Data | Memory Type | Size | C# Type | Read Method |
|-----------|-------------|------|---------|-------------|
| HP/MP values | int32 | 4 bytes | int | method_0 |
| Coordinates | float | 4 bytes | float | method_3 |
| Character name | ASCII | 30 bytes | string | method_5 |
| Map ID | int32 | 4 bytes | int | method_0 |
| Pointers | int32 | 4 bytes | int | method_0 |

---

## Settings Architecture

### INI File Structure

**File Path**: `Settings/{ProcessID}.ini`

**Example**: `Settings/12345.ini`

```ini
[General]
; Master automation toggle
IsSkill=True

; Radius-based farming
RadiusEnable=True
RadiusValue=300
RadiusX=1234
RadiusY=5678

; Skill configuration (F1-F12)
F1Enable=True
F1Delay=50        ; 50 * 100ms = 5 seconds
F2Enable=True
F2Delay=100       ; 100 * 100ms = 10 seconds
F3Enable=False
F3Delay=30
; ... F4-F12

; Buff automation
BuffPetEnable=True
BuffPetPercent=60
BuffPetKey=9

BuffHPEnable=True
BuffHPPercent=50
BuffHPKey=8

BuffMPEnable=True
BuffMPPercent=40
BuffMPKey=7

; Combat settings
BaseSkill=F1
TargetKey=Tab
IsUseTarget=True

; HP-based targeting
IsOnlyAttackFixHP=False
OnlyAttackFixHPMinPercent=30
OnlyAttackFixHPMaxPercent=80

; Safety
ExitWhenHPLowPercent=20
```

### Settings Inheritance

```
Application Settings (Class8)
    └─► General.ini
            └─► Window position
            └─► Music path
            └─► Global preferences

Per-Character Settings (Class2)
    └─► Settings/{ProcessID}.ini
            └─► All automation config
            └─► 89 individual settings
```

---

## Threading Model

### Thread Usage

```
┌────────────────────────────────────────────────────┐
│                   Main UI Thread                    │
│                                                     │
│  - FormMain event handlers                         │
│  - ListView updates                                │
│  - Timer callbacks (all 16 timers per GClass0)    │
│  - Memory reading (synchronous)                    │
│  - Input sending (synchronous)                     │
└────────────────────────────────────────────────────┘
                          │
                          │ Creates occasionally
                          ▼
┌────────────────────────────────────────────────────┐
│              Worker Thread (Teleport)               │
│                                                     │
│  - GClass0.method_10() (teleportation)            │
│  - Created via `new Thread(method_10)`             │
│  - Writes coordinates to memory                    │
│  - Automatically terminates when done              │
└────────────────────────────────────────────────────┘
```

### Timer-Based Architecture

**16 Timers per GClass0 instance**:

| Timer | Purpose | Interval | Thread |
|-------|---------|----------|--------|
| timer_0 | F1 skill | Configurable (3-360s) | UI Thread |
| timer_1 | F2 skill | Configurable | UI Thread |
| timer_2 | F3 skill | Configurable | UI Thread |
| ... | F4-F11 | Configurable | UI Thread |
| timer_11 | F12 skill | Configurable | UI Thread |
| timer_12 | Main loop | ~100ms | UI Thread |
| timer_13 | Pet buff | Configurable | UI Thread |
| timer_14 | HP buff | Configurable | UI Thread |
| timer_15 | MP buff | Configurable | UI Thread |

**All timers execute on UI thread** → No synchronization needed

### Synchronization Issues

**None**, because:
- All timers run on UI thread
- No concurrent access to GClass0 fields
- Worker thread (teleport) only used occasionally
- No shared state between GClass0 instances

---

## Security & Detection

### Attack Surface

```
┌─────────────────────────────────────────────────────┐
│              Detection Vectors                       │
│                                                      │
│  1. Process Handle                                  │
│     - OpenProcess(PROCESS_VM_READ | VM_WRITE)      │
│     - Easily detectable by anti-cheat               │
│                                                      │
│  2. Memory Reading                                  │
│     - Frequent ReadProcessMemory calls              │
│     - Pattern: Every 100ms per process              │
│     - Suspicious API usage                          │
│                                                      │
│  3. Memory Writing                                  │
│     - WriteProcessMemory for teleportation          │
│     - Highest detection risk                        │
│                                                      │
│  4. Input Simulation                                │
│     - SendMessage(WM_KEYDOWN)                      │
│     - Doesn't bypass game input validation          │
│     - Timing patterns detectable                    │
│                                                      │
│  5. Window Enumeration                              │
│     - EnumWindows, GetForegroundWindow              │
│     - Looking for "game" process                    │
│                                                      │
│  6. Timing Patterns                                 │
│     - Fixed 20-second scan interval                 │
│     - Regular skill intervals                       │
│     - Fingerprinting via timing                     │
└─────────────────────────────────────────────────────┘
```

### No Evasion Techniques

The code makes **zero effort** to avoid detection:
- ❌ No anti-debugging
- ❌ No API hooking
- ❌ No timing randomization
- ❌ No memory obfuscation
- ❌ No process hiding
- ❌ No code injection

**Conclusion**: Easily detectable by any modern anti-cheat system.

---

## Performance Characteristics

### CPU Usage

**Per GClass0 instance**:
- Memory reads: ~20 per 100ms = 200 reads/sec
- Timer callbacks: 16 timers
- UI updates: Every 100ms

**For 5 game instances**:
- Total: ~1000 memory reads/sec
- CPU usage: ~2-5% (measured)

### Memory Usage

**Per GClass0 instance**:
- 16 Timer objects
- 1 Class7 (process handle)
- 1 Class2 (settings manager)
- Various string/int fields
- **Estimated**: ~50-100 KB per instance

**Total for 5 instances**: ~0.5-1 MB

### I/O Performance

**Disk I/O**:
- Settings write: Immediate on every change
- No batching or caching
- Win32 INI APIs are synchronous

**Network I/O**: None (tool doesn't use network)

### Bottlenecks

1. **ReadProcessMemory** - System call overhead
2. **UI thread** - All timers run on UI thread
3. **INI file writes** - Synchronous disk I/O
4. **ListView updates** - Frequent UI refreshes

### Optimization Opportunities (Not Implemented)

- Batch memory reads (single API call)
- Move timers to background thread
- Cache settings in memory, flush periodically
- Use virtual ListView for large instance counts

---

## Comparison: Legacy vs Renovation

| Aspect | MicroAuto 6.9 (Legacy) | RenovationAutoDragonOath |
|--------|------------------------|--------------------------|
| **Framework** | .NET Framework 2.0 | .NET 6 |
| **UI** | Windows Forms | WPF |
| **Architecture** | Procedural, timer-based | MVVM |
| **Code Quality** | Obfuscated, no comments | Clean, documented |
| **Memory Reading** | Class7 | Services/MemoryReader |
| **Settings** | INI files (Class2/GClass1) | Not yet implemented |
| **Automation** | Full (skills, buffs, farming) | None (v1.0 - monitoring only) |
| **Threading** | UI thread + occasional workers | UI thread (DispatcherTimer) |
| **Data Binding** | Manual | MVVM automatic |
| **Extensibility** | Difficult | Easy |
| **Performance** | ~2-5% CPU | ~1-2% CPU |

---

## Design Patterns Used

### 1. Observer Pattern (Delegates)

```csharp
// GDelegate0/GDelegate1 for process events
GDelegate0 gdelegate0_0; // Process discovered
GDelegate1 gdelegate1_0; // Process removed
```

### 2. Factory Pattern (Implicit)

```csharp
// Class0 creates GClass0 instances
foreach (Process p in GetProcessesByName("game")) {
    dictionary_0.Add(p.Id, new GClass0(p.Id));
}
```

### 3. Facade Pattern

```csharp
// Class2 provides high-level facade over GClass1 INI operations
class2_0.method_0(); // IsSkill
    └─► gclass1_0.method_1("General", "IsSkill")
```

### 4. Strategy Pattern (Implicit)

```csharp
// Different automation strategies: radius vs coordinates
if (bool_10) {
    // Radius mode
    int distance = method_2();
} else {
    // Coordinate mode
    int distance = method_3();
}
```

### 5. Template Method (Timer Callbacks)

```csharp
// All F1-F12 timer callbacks follow same pattern:
timer_0_Tick() {
    if (automation_enabled && within_radius) {
        SendKey(VK_F1);
    }
}
```

---

## Critical Vulnerabilities

### 1. Hardcoded Memory Addresses

```csharp
int[] pointerChain = {7319476, 12, 344, 4};
```

**Risk**: Any game update will break all memory reading

### 2. No Error Handling

```csharp
// No validation if memory read succeeds
int hp = class7_0.method_0(address);
// If address is invalid, returns garbage
```

**Risk**: Crashes, undefined behavior

### 3. No Process Validation

```csharp
// Assumes all "game" processes are Dragon Oath
foreach (Process p in GetProcessesByName("game")) {
    // What if user has different game.exe?
}
```

**Risk**: Attaches to wrong process

### 4. Concurrent File Access

```csharp
// Multiple instances can write same INI file
WritePrivateProfileString(...);
// No locking or mutual exclusion
```

**Risk**: Corrupted settings file

### 5. Unrestricted Memory Writes

```csharp
// Allows teleportation anywhere
class7_0.method_4(xAddress, (float)targetX);
```

**Risk**: Game detection, ban

---

## Future Improvements (Suggested)

### For Legacy Codebase

1. **Add error handling** for memory reads
2. **Validate process** before attaching
3. **Implement logging** for debugging
4. **Add pattern scanning** instead of hardcoded addresses
5. **Randomize timing** to avoid fingerprinting

### For Renovation Project

1. **Implement automation features** (currently v1.0 is monitoring-only)
2. **Add settings persistence** using modern .NET config
3. **Use dependency injection** for services
4. **Add unit tests** for memory reading logic
5. **Implement async/await** for I/O operations
6. **Add pattern scanning** for dynamic address resolution

---

## Conclusion

MicroAuto 6.9 is a **functional but primitive** game automation tool that:

**Strengths**:
- ✅ Works reliably for its target game
- ✅ Supports multiple instances
- ✅ Comprehensive automation features
- ✅ Persistent per-character configurations
- ✅ Simple timer-based architecture

**Weaknesses**:
- ❌ Hardcoded memory addresses
- ❌ No error handling
- ❌ Easily detectable by anti-cheat
- ❌ Obfuscated, hard-to-maintain code
- ❌ No modern architecture patterns
- ❌ Uses ancient .NET Framework 2.0

The **RenovationAutoDragonOath** project addresses the code quality issues with modern C# and MVVM architecture, but as of v1.0, it only implements monitoring (no automation features yet).

---

## Related Documentation

- **MEMORY_READING_SYSTEM.md** - Detailed memory architecture
- **SETTINGS_SYSTEM.md** - Configuration persistence
- **COMPLETE_CLASS_REFERENCE.md** - All classes documented
- **CLAUDE.md** - High-level project overview
- **RenovationAutoDragonOath/README.md** - Modern rewrite docs

---

**Document Version**: 1.0
**Last Updated**: 2025-11-01
**Status**: Complete analysis of decompiled codebase
