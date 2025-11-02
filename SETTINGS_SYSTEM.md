# Settings and Configuration System Documentation

**Files**: `Class2.cs`, `GClass1.cs`
**Purpose**: Persistent storage of per-character automation configurations using INI files

## Overview

The settings system provides persistent storage for all automation configurations. Each character/process has its own INI file with 89 different settings (44+ getter/setter pairs + helper methods). Settings are stored in the `Settings/` directory and loaded/saved automatically.

## Architecture

### Two-Layer Design

```
Class2 (High-level Settings Manager)
    ↓ uses
GClass1 (Low-level INI File Handler)
    ↓ uses
kernel32.dll (Windows INI File APIs)
```

## GClass1: INI File Handler

**File**: `GClass1.cs`
**Responsibility**: Low-level INI file read/write operations

### Structure

```csharp
public class GClass1 {
    public string string_0; // INI file path

    public GClass1(string filePath) {
        this.string_0 = filePath;
    }
}
```

### Win32 API Declarations

#### WritePrivateProfileString

```csharp
[DllImport("kernel32")]
private static extern long WritePrivateProfileString(
    string section,
    string key,
    string value,
    string filePath
);
```

**Purpose**: Writes a key-value pair to an INI file

**Parameters**:
- `section`: INI section name (e.g., "General")
- `key`: Setting key name (e.g., "IsSkill")
- `value`: String value to write
- `filePath`: Full path to INI file

#### GetPrivateProfileString

```csharp
[DllImport("kernel32")]
private static extern int GetPrivateProfileString(
    string section,
    string key,
    string defaultValue,
    StringBuilder buffer,
    int bufferSize,
    string filePath
);
```

**Purpose**: Reads a key-value pair from an INI file

**Parameters**:
- `section`: INI section name
- `key`: Setting key name
- `defaultValue`: Value to return if key doesn't exist
- `buffer`: StringBuilder to receive the value
- `bufferSize`: Maximum characters to read (255)
- `filePath`: Full path to INI file

### Public Methods

#### Write to INI

```csharp
public void method_0(string section, string key, string value)
```

**Usage**: Save a setting to the INI file

**Example**:
```csharp
gclass1_0.method_0("General", "IsSkill", "True");
```

#### Read from INI

```csharp
public string method_1(string section, string key)
```

**Usage**: Load a setting from the INI file

**Returns**: String value, or empty string if not found

**Example**:
```csharp
string value = gclass1_0.method_1("General", "IsSkill"); // Returns "True"
```

## Class2: Settings Manager

**File**: `Class2.cs`
**Responsibility**: High-level settings management with type-safe getters/setters

### Initialization

```csharp
public Class2(string characterId) {
    this.gclass1_0 = new GClass1(Class2.string_0 + "\\" + characterId + ".ini");
}
```

**Parameters**:
- `characterId`: Process ID or character name (used as filename)

**File Location**: `Settings/{characterId}.ini`

**Example**:
```csharp
Class2 settings = new Class2("12345"); // Creates/opens Settings/12345.ini
```

### Static Configuration

```csharp
private static string string_0 = "Settings"; // Directory name
private static string string_1 = "General";  // Section name
```

**All settings** are stored in the `[General]` section.

## Complete Settings Reference

### Master Settings (Automation Control)

#### IsSkill - Master skill automation toggle

```csharp
public bool method_0()  // Get IsSkill
public void method_1(bool value) // Set IsSkill
```

**INI Key**: `IsSkill`
**Purpose**: Enable/disable all skill automation (F1-F12)
**Type**: Boolean

#### RadiusEnable - Radius-based farming

```csharp
public bool method_2()  // Get RadiusEnable
public void method_3(bool value) // Set RadiusEnable
```

**INI Key**: `RadiusEnable`
**Purpose**: Only attack monsters within specified radius of saved coordinates
**Type**: Boolean

#### IsUseTarget - Target selection

```csharp
public bool method_4()  // Get IsUseTarget
public void method_5(bool value) // Set IsUseTarget
```

**INI Key**: `IsUseTarget`
**Purpose**: Enable target selection automation
**Type**: Boolean

#### IsOnlyAttackFixHP - HP-based targeting

```csharp
public bool method_6()  // Get IsOnlyAttackFixHP
public void method_7(bool value) // Set IsOnlyAttackFixHP
```

**INI Key**: `IsOnlyAttackFixHP`
**Purpose**: Only attack monsters with HP in specified range
**Type**: Boolean

### Buff Settings

#### BuffPetEnable - Pet buff automation

```csharp
public bool method_8()  // Get BuffPetEnable
public void method_9(bool value) // Set BuffPetEnable
```

**INI Key**: `BuffPetEnable`
**Purpose**: Auto-use pet buff when pet HP drops below threshold
**Type**: Boolean

#### BuffHPEnable - HP buff automation

```csharp
public bool method_10() // Get BuffHPEnable
public void method_11(bool value) // Set BuffHPEnable
```

**INI Key**: `BuffHPEnable`
**Purpose**: Auto-use HP potion when player HP drops below threshold
**Type**: Boolean

#### BuffMPEnable - MP buff automation

```csharp
public bool method_12() // Get BuffMPEnable
public void method_13(bool value) // Set BuffMPEnable
```

**INI Key**: `BuffMPEnable`
**Purpose**: Auto-use MP potion when player MP drops below threshold
**Type**: Boolean

### Skill Key Toggles (F1-F12)

Each F-key has an enable/disable setting:

```csharp
// F1
public bool method_14() // Get F1Enable
public void method_15(bool value) // Set F1Enable

// F2
public bool method_16() // Get F2Enable
public void method_17(bool value) // Set F2Enable

// ... continuing through F12
// F12
public bool method_38() // Get F12Enable
public void method_39(bool value) // Set F12Enable
```

**INI Keys**: `F1Enable` through `F12Enable`
**Purpose**: Individual enable/disable for each skill hotkey
**Type**: Boolean

### Skill Timers (F1-F12)

Each F-key has a configurable delay/interval:

```csharp
// F1 Delay
public int method_40() // Get F1Delay (in 100ms units)
public void method_41(int value) // Set F1Delay

// F2 Delay
public int method_42() // Get F2Delay
public void method_43(int value) // Set F2Delay

// ... continuing through F12
// F12 Delay
public int method_64() // Get F12Delay
public void method_65(int value) // Set F12Delay
```

**INI Keys**: `F1Delay` through `F12Delay`
**Purpose**: Interval between skill uses (in 100ms units)
**Type**: Integer
**Range**: Typically 10-3600 (1 second to 6 minutes)

**Example**: Value of `50` = 5 seconds (50 × 100ms)

### Buff Thresholds

#### BuffPetPercent - Pet HP threshold

```csharp
public int method_66() // Get BuffPetPercent
public void method_67(int value) // Set BuffPetPercent
```

**INI Key**: `BuffPetPercent`
**Purpose**: Pet HP percentage trigger for buff (e.g., buff when pet HP < 60%)
**Type**: Integer
**Range**: 0-100

#### BuffHPPercent - Player HP threshold

```csharp
public int method_68() // Get BuffHPPercent
public void method_69(int value) // Set BuffHPPercent
```

**INI Key**: `BuffHPPercent`
**Purpose**: Player HP percentage trigger for buff
**Type**: Integer
**Range**: 0-100

#### BuffMPPercent - Player MP threshold

```csharp
public int method_70() // Get BuffMPPercent
public void method_71(int value) // Set BuffMPPercent
```

**INI Key**: `BuffMPPercent`
**Purpose**: Player MP percentage trigger for buff
**Type**: Integer
**Range**: 0-100

### Buff Hotkeys

#### BuffPetKey - Pet buff hotkey

```csharp
public string method_72() // Get BuffPetKey
public void method_73(string value) // Set BuffPetKey
```

**INI Key**: `BuffPetKey`
**Purpose**: Keyboard key to press for pet buff (e.g., "F1", "1", "A")
**Type**: String

#### BuffHPKey - HP buff hotkey

```csharp
public string method_74() // Get BuffHPKey
public void method_75(string value) // Set BuffHPKey
```

**INI Key**: `BuffHPKey`
**Purpose**: Keyboard key for HP potion
**Type**: String

#### BuffMPKey - MP buff hotkey

```csharp
public string method_76() // Get BuffMPKey
public void method_77(string value) // Set BuffMPKey
```

**INI Key**: `BuffMPKey`
**Purpose**: Keyboard key for MP potion
**Type**: String

### Radius Configuration

#### RadiusValue - Radius distance

```csharp
public int method_78() // Get RadiusValue
public void method_79(int value) // Set RadiusValue
```

**INI Key**: `RadiusValue`
**Purpose**: Maximum distance from saved coordinates to attack monsters
**Type**: Integer
**Range**: Typically 0-1000 (game units)

#### RadiusX - Saved X coordinate

```csharp
public int method_80() // Get RadiusX
public void method_81(int value) // Set RadiusX
```

**INI Key**: `RadiusX`
**Purpose**: Center X coordinate for radius farming
**Type**: Integer

#### RadiusY - Saved Y coordinate

```csharp
public int method_82() // Get RadiusY
public void method_83(int value) // Set RadiusY
```

**INI Key**: `RadiusY`
**Purpose**: Center Y coordinate for radius farming
**Type**: Integer

### Combat Settings

#### BaseSkill - Base attack skill hotkey

```csharp
public string method_84() // Get BaseSkill
public void method_85(string value) // Set BaseSkill
```

**INI Key**: `BaseSkill`
**Purpose**: Primary attack skill hotkey
**Type**: String

#### TargetKey - Target selection hotkey

```csharp
public string method_86() // Get TargetKey
public void method_87(string value) // Set TargetKey
```

**INI Key**: `TargetKey`
**Purpose**: Hotkey to select/switch targets (typically Tab)
**Type**: String

### HP-Based Targeting

#### OnlyAttackFixHPMinPercent

```csharp
// Methods not explicitly shown in excerpt, but pattern continues
```

**INI Key**: `OnlyAttackFixHPMinPercent`
**Purpose**: Minimum monster HP% to attack (e.g., only attack if monster HP >= 30%)
**Type**: Integer
**Range**: 0-100

#### OnlyAttackFixHPMaxPercent

**INI Key**: `OnlyAttackFixHPMaxPercent`
**Purpose**: Maximum monster HP% to attack (e.g., only attack if monster HP <= 80%)
**Type**: Integer
**Range**: 0-100

### Safety Settings

#### ExitWhenHPLowPercent

**INI Key**: `ExitWhenHPLowPercent`
**Purpose**: Exit map when player HP drops below this threshold
**Type**: Integer
**Range**: 0-100

### Helper Methods

#### method_88 - String to Boolean converter

```csharp
private bool method_88(string value)
```

**Purpose**: Converts INI string value to boolean

**Logic**:
- Returns `true` if value equals "True" (case-sensitive)
- Returns `false` otherwise

**Note**: No error handling for invalid values

## INI File Format

### Example Settings File

**File**: `Settings/12345.ini`

```ini
[General]
IsSkill=True
RadiusEnable=True
IsUseTarget=True
IsOnlyAttackFixHP=False
BuffPetEnable=True
BuffHPEnable=True
BuffMPEnable=True
F1Enable=True
F2Enable=True
F3Enable=False
F4Enable=False
F5Enable=False
F6Enable=False
F7Enable=False
F8Enable=False
F9Enable=False
F10Enable=False
F11Enable=False
F12Enable=False
F1Delay=50
F2Delay=100
F3Delay=30
F4Delay=30
F5Delay=30
F6Delay=30
F7Delay=30
F8Delay=30
F9Delay=30
F10Delay=30
F11Delay=30
F12Delay=30
BuffPetPercent=60
BuffHPPercent=50
BuffMPPercent=40
BuffPetKey=9
BuffHPKey=8
BuffMPKey=7
RadiusValue=300
RadiusX=1234
RadiusY=5678
BaseSkill=F1
TargetKey=Tab
OnlyAttackFixHPMinPercent=30
OnlyAttackFixHPMaxPercent=80
ExitWhenHPLowPercent=20
```

### File Naming Convention

- **Format**: `{ProcessID}.ini` or `{CharacterName}.ini`
- **Location**: `bin/Debug/Settings/` or `bin/x86/Debug/Settings/`

**Examples**:
- `12345.ini` - Settings for process ID 12345
- `MyCharacter.ini` - Settings for character "MyCharacter"

## Settings Lifecycle

### 1. Initialization

When a new game process is detected:

```csharp
// From GClass0.cs
public GClass0(int processId) {
    // Create settings manager for this process
    this.class2_0 = new Class2(processId.ToString());

    // Load all settings from INI file
    this.LoadAllSettings();
}
```

### 2. Loading Settings

Settings are loaded when:
- Game process is first detected
- User switches to a different character in the UI
- Application starts

**Example** (from GClass0):
```csharp
// Load skill enable states
this.timer_0.Enabled = this.class2_0.method_14(); // F1Enable
this.timer_1.Enabled = this.class2_0.method_16(); // F2Enable

// Load skill delays
this.timer_0.Interval = this.class2_0.method_40() * 100; // F1Delay
this.timer_1.Interval = this.class2_0.method_42() * 100; // F2Delay

// Load buff settings
this.int_0 = this.class2_0.method_66(); // BuffPetPercent
this.string_1 = this.class2_0.method_72(); // BuffPetKey
```

### 3. Saving Settings

Settings are saved when:
- User modifies a value in the UI
- User checks/unchecks a checkbox
- User changes a numeric value
- Application closes

**Example** (from FormMain):
```csharp
private void f1Enable_CheckedChanged(object sender, EventArgs e) {
    if (tabPageAcc.Tag != null) {
        int processId = (int)tabPageAcc.Tag;

        // Update setting in memory
        dictionary_0[processId].timer_0.Enabled = f1Enable.Checked;

        // Save to INI file
        dictionary_0[processId].class2_0.method_15(f1Enable.Checked);
    }
}
```

### 4. Auto-Save Behavior

**Immediate**: Settings are saved immediately when changed (no "Save" button required)

**Per-Character**: Each character has independent settings

**Persistent**: Settings survive application restart

## Usage Examples

### Creating Settings for New Character

```csharp
// Character with process ID 12345
Class2 settings = new Class2("12345");

// Configure skill automation
settings.method_1(true); // Enable IsSkill
settings.method_15(true); // Enable F1
settings.method_17(true); // Enable F2
settings.method_41(50); // F1 Delay = 5 seconds
settings.method_43(100); // F2 Delay = 10 seconds

// Configure buff automation
settings.method_9(true); // Enable BuffPet
settings.method_67(60); // Pet buff at 60% HP
settings.method_73("9"); // Pet buff hotkey = 9

// Configure radius farming
settings.method_3(true); // Enable radius
settings.method_79(300); // Radius = 300 units
settings.method_81(1234); // X = 1234
settings.method_83(5678); // Y = 5678
```

### Reading All Settings

```csharp
Class2 settings = new Class2("12345");

// Check if skill automation is enabled
if (settings.method_0()) {  // IsSkill
    Console.WriteLine("Skill automation: ON");

    // Check which skills are enabled
    if (settings.method_14()) Console.WriteLine("F1: Enabled");
    if (settings.method_16()) Console.WriteLine("F2: Enabled");
    if (settings.method_18()) Console.WriteLine("F3: Enabled");
}

// Check buff configuration
if (settings.method_8()) {  // BuffPetEnable
    int threshold = settings.method_66(); // BuffPetPercent
    string hotkey = settings.method_72(); // BuffPetKey
    Console.WriteLine($"Pet buff: ON, Trigger at {threshold}%, Key: {hotkey}");
}
```

### Bulk Configuration

```csharp
Class2 settings = new Class2("MyCharacter");

// Enable all F1-F6 skills with 3-second delay
for (int i = 1; i <= 6; i++) {
    // Note: Actual code would use specific method numbers
    // This is pseudo-code for illustration
    settings.SetFKeyEnable(i, true);
    settings.SetFKeyDelay(i, 30); // 30 * 100ms = 3 seconds
}
```

## Class8: Global Application Settings

**File**: `Class8.cs`
**Purpose**: Application-wide settings (not per-character)

**Settings Stored**:
- Window position (X, Y coordinates)
- Music file path
- Experience tracking values
- Other global preferences

**File**: `General.ini` (separate from character settings)

## Error Handling

### Current Behavior

**No error handling** for:
- Missing INI files (returns empty strings)
- Invalid boolean values (defaults to false)
- Invalid integer values (returns 0 or garbage)
- File access permission errors (silent failure)

### Implications

- First run with new character creates empty INI file
- Invalid values in INI file cause unpredictable behavior
- Corruption of INI file requires manual deletion

## Performance Considerations

### File I/O

- Each setting change = 1 disk write operation
- No batching or caching of writes
- No in-memory dirty flag tracking
- Settings loaded multiple times (once per UI refresh)

### Optimization Opportunities (Not Implemented)

- Cache settings in memory, flush on timer
- Batch multiple setting changes
- Only save changed values
- Use binary format instead of text INI

## Security Considerations

### Data Storage

- **Plaintext**: All settings stored in readable text format
- **No encryption**: Hotkeys, thresholds visible in INI files
- **No validation**: User can manually edit INI files

### Potential Issues

- User can create invalid configurations by editing INI files
- No protection against concurrent access (multiple instances editing same file)
- No backup mechanism for corrupted files

## Renovation Project Changes

The `RenovationAutoDragonOath` project (v1.0):

- **Does NOT implement settings persistence** yet
- Settings are runtime-only (lost on restart)
- Planned for future versions using modern C# configuration APIs

**Future plans**:
- JSON configuration files
- Application-level settings (appsettings.json)
- User-level settings (user.config)
- Character profiles with import/export

## Related Files

- `GClass0.cs`: Main consumer of settings (loads/saves automation config)
- `FormMain.cs`: UI that displays and modifies settings
- `Class8.cs`: Global application settings (separate from per-character settings)

## Summary

The settings system is a simple but effective persistence layer using Windows INI files. It provides 89 configuration options per character, enabling comprehensive customization of automation behavior. The two-layer architecture (Class2 → GClass1 → kernel32.dll) separates high-level typed settings from low-level INI operations.

**Key characteristics**:
- ✅ Simple and reliable
- ✅ Human-readable format
- ✅ Per-character isolation
- ❌ No error handling
- ❌ No validation
- ❌ No caching/optimization
- ❌ No concurrent access protection
