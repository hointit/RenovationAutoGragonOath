# Memory Reading System Documentation

**File**: `Class7.cs`
**Purpose**: Low-level memory reading and writing for the Dragon Oath game process

## Overview

Class7 is the core memory manipulation component that provides Win32 API wrappers for reading and writing game process memory. It enables the automation tool to extract real-time game data (HP, MP, coordinates, etc.) and manipulate character position.

## Architecture

### Initialization

```csharp
public Class7(int processId)
```

- Opens a handle to the target process with `PROCESS_VM_READ` and `PROCESS_VM_WRITE` permissions
- Access rights: `2035711` (decimal) = combination of multiple process access flags
- Stores the process handle in `intptr_0` for subsequent operations

### Core Methods

#### 1. Read Int32 from Memory

```csharp
public int method_0(int address)
```

**Purpose**: Reads a 32-bit integer from the specified memory address

**How it works**:
- Allocates 4-byte buffer
- Calls `ReadProcessMemory` Win32 API
- Converts bytes to Int32 using `BitConverter`

**Example**: Reading base pointers, offsets, integer values

#### 2. Follow Pointer Chain

```csharp
public int method_1(int[] pointerChain)
```

**Purpose**: Navigates through multi-level pointers to reach final memory address

**How it works**:
1. Reads the base address from `pointerChain[0]`
2. For each subsequent offset:
   - Dereferences the current address
   - Adds the offset
   - Reads the next address
3. Returns the final dereferenced address

**Example**:
```csharp
// Pointer chain: [7319476, 12, 344, 4]
// Step 1: Read memory at 7319476 → get address A
// Step 2: Read memory at (A + 12) → get address B
// Step 3: Read memory at (B + 344) → get address C
// Step 4: Read memory at (C + 4) → return final value
int result = method_1(new int[] {7319476, 12, 344, 4});
```

#### 3. Read Float via Pointer Chain

```csharp
public float method_2(int[] pointerChain)
```

**Purpose**: Follows a pointer chain and reads a float value at the end

**How it works**:
- Similar to `method_1` but reads a float at the final address
- Uses `method_3` to read the float value

**Example**: Reading coordinates (X, Y positions are stored as floats)

#### 4. Read Float from Address

```csharp
public float method_3(int address)
```

**Purpose**: Reads a 32-bit floating-point number from memory

**How it works**:
- Reads 4 bytes from memory
- Converts to float using `BitConverter.ToSingle()`

**Example**: Reading HP/MP percentages, coordinates

#### 5. Write Float to Memory

```csharp
public int method_4(int address, float value)
```

**Purpose**: Writes a floating-point value to game memory

**How it works**:
- Converts float to byte array using `BitConverter.GetBytes()`
- Calls `WriteProcessMemory` Win32 API
- Returns number of bytes written

**Use case**: Modifying character coordinates for teleportation

#### 6. Read String from Memory

```csharp
public string method_5(int address)
```

**Purpose**: Reads ASCII string from memory (up to 30 characters)

**How it works**:
- Reads 30 bytes from memory
- Converts to string using `Encoding.Default`
- Applies Vietnamese character conversion via `Class4.smethod_2()`

**Example**: Reading character name, map name

#### 7. Close Process Handle

```csharp
public void method_6()
```

**Purpose**: Releases the process handle when done

**When called**: When game process is terminated or tool is closed

## Win32 API Declarations

### OpenProcess

```csharp
[DllImport("kernel32.dll")]
public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
```

**Access Rights Used**: `2035711` = PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION | PROCESS_QUERY_INFORMATION

### ReadProcessMemory

```csharp
[DllImport("kernel32.dll")]
public static extern bool ReadProcessMemory(
    IntPtr hProcess,
    int lpBaseAddress,
    byte[] lpBuffer,
    int dwSize,
    int lpNumberOfBytesRead
);
```

**Parameters**:
- `hProcess`: Process handle from OpenProcess
- `lpBaseAddress`: Memory address to read from
- `lpBuffer`: Buffer to store read data
- `dwSize`: Number of bytes to read
- `lpNumberOfBytesRead`: Actual bytes read (can be 0)

### WriteProcessMemory

```csharp
[DllImport("kernel32.dll", SetLastError = true)]
public static extern bool WriteProcessMemory(
    IntPtr hProcess,
    int lpBaseAddress,
    byte[] lpBuffer,
    uint nSize,
    out int lpNumberOfBytesWritten
);
```

**Usage**: Writing character coordinates for teleportation features

### CloseHandle

```csharp
[DllImport("kernel32.dll")]
public static extern bool CloseHandle(IntPtr hObject);
```

**Purpose**: Release process handle to prevent resource leaks

## Memory Address Patterns

### Character Entity Base

**Base Pointer**: `7319476` (0x006F8C24)

**Pointer Chain**: `[7319476, 12]` → Entity Base Address

**Entity Offsets**:
- `+48`: Character Name (string, 30 bytes)
- `+68`: Unknown float value
- `+76`: Unknown float value
- `+92`: X Coordinate (float)
- `+100`: Y Coordinate (float)
- `+408`: Unknown float value
- `+412`: Unknown float value

### Character Stats Base

**Pointer Chain**: `[7319476, 12, 344, 4]` → Stats Base Address

**Stats Offsets**:
- `+2292`: Current HP (int32)
- `+2296`: Current MP (int32)
- `+2400`: Max HP (int32)
- `+2404`: Max MP (int32)

### Map Information

**Base Pointer**: `6870940` (0x0068B91C)

**Pointer Chain**: `[6870940, 14232]` → Map Base Address

**Map Offsets**:
- `+96`: Map ID (int32)

## Data Types in Game Memory

| Game Data Type | C# Type | Read Method | Size |
|---------------|---------|-------------|------|
| Integers (HP, MP, Map ID) | int32 | method_0 | 4 bytes |
| Coordinates (X, Y) | float | method_3 | 4 bytes |
| Character Name | string | method_5 | 30 bytes |
| Pointers | int32 | method_0 | 4 bytes |

## Pointer Chain Visualization

```
Game Memory Layout:

[0x006F8C24] = Pointer to Player Object
    ↓ +12
[Player Object Base]
    ↓ +92 → X Coordinate (float)
    ↓ +100 → Y Coordinate (float)
    ↓ +344 → Pointer to Stats Object
        ↓ +4
    [Stats Object Base]
        ↓ +48 → Character Name (string)
        ↓ +2292 → Current HP (int32)
        ↓ +2296 → Current MP (int32)
        ↓ +2400 → Max HP (int32)
        ↓ +2404 → Max MP (int32)
```

## Usage Examples in GClass0

### Reading Character HP Percentage

```csharp
// From GClass0.cs
public int method_33() {
    // Get stats base address
    int statsBase = class7_0.method_1(new int[] {7319476, 12, 344, 4});

    // Read current HP
    int currentHP = class7_0.method_0(statsBase + 2292);

    // Read max HP
    int maxHP = class7_0.method_0(statsBase + 2400);

    // Calculate percentage
    return (currentHP * 100) / maxHP;
}
```

### Reading Character Name

```csharp
// From GClass0.cs
public string method_38() {
    // Get stats base address
    int statsBase = class7_0.method_1(new int[] {7319476, 12, 344, 4});

    // Read character name (30 bytes string)
    return class7_0.method_5(statsBase + 48);
}
```

### Reading Character Coordinates

```csharp
// From GClass0.cs
public int method_13() { // Get X coordinate
    // Get entity base address
    int entityBase = class7_0.method_1(new int[] {7319476, 12});

    // Read X coordinate as float
    return (int)class7_0.method_3(entityBase + 92);
}

public int method_17() { // Get Y coordinate
    int entityBase = class7_0.method_1(new int[] {7319476, 12});
    return (int)class7_0.method_3(entityBase + 100);
}
```

### Writing Character Coordinates (Teleportation)

```csharp
// From GClass0.cs - method_10()
public void method_10() {
    // Get X coordinate address
    int xAddress = class7_0.method_1(new int[] {7319476, 12}) + 92;

    // Write new X coordinate
    class7_0.method_4(xAddress, (float)targetX);

    // Get Y coordinate address
    int yAddress = class7_0.method_1(new int[] {7319476, 12}) + 100;

    // Write new Y coordinate
    class7_0.method_4(yAddress, (float)targetY);
}
```

## Vietnamese Character Encoding

The `method_5()` string reader uses `Class4.smethod_2()` to convert proprietary Vietnamese character encoding to Unicode. This handles special characters in character names and map names.

**Conversion**: Game's custom encoding → Standard Vietnamese Unicode

## Performance Considerations

### Memory Read Frequency

The tool reads memory at different intervals:

- **Character info** (HP, MP, Name): Every 2 seconds (in Renovation) / Continuous in Legacy
- **Coordinates**: When needed for radius checks
- **Experience**: For XP/hour calculations

### Error Handling

The code does **not** have extensive error handling:
- No validation if process handle is valid
- No checks if memory addresses are accessible
- Failed reads return garbage data or 0

**Risk**: Game updates will break all hardcoded addresses

## Security & Anti-Cheat Implications

### Detection Vectors

1. **Process Handle**: Opening with VM_READ/VM_WRITE permissions is detectable
2. **Memory Reads**: Frequent ReadProcessMemory calls are suspicious
3. **Memory Writes**: WriteProcessMemory for coordinates is easily detected
4. **Pattern**: Regular polling pattern (every 2 seconds) is fingerprinting

### Current Status

The code makes **no effort to avoid detection**:
- No anti-debugging checks
- No memory obfuscation
- No randomized timing
- Direct Win32 API calls (not hooked internally)

## Limitations

1. **Hardcoded Addresses**: All memory addresses are hardcoded and will break on game updates
2. **No ASLR Handling**: Assumes fixed base addresses (game doesn't use ASLR)
3. **No 64-bit Support**: Uses int32 for addresses (only works on 32-bit processes)
4. **Single Process**: Only works with one process handle at a time per instance
5. **No Validation**: No checks if data read is valid/sensible

## Renovation Project Changes

The `RenovationAutoDragonOath` project ports Class7 to `Services/MemoryReader.cs` with:

- Cleaner method names (ReadInt32, ReadFloat, ReadString, FollowPointerChain)
- Same functionality, better documentation
- No additional safety checks or error handling
- Same memory addresses and pointer chains

## Related Files

- `GClass0.cs`: Uses Class7 extensively for all game data reading
- `Class4.cs`: Provides `smethod_2()` for Vietnamese character conversion
- `Class0.cs`: Creates Class7 instance for each game process discovered

## Technical Notes

- All addresses are in **decimal** notation in the code
- The game process is 32-bit (uses 32-bit pointers)
- The game does not use Address Space Layout Randomization (ASLR)
- Pointer chains rarely exceed 4 levels deep
- Most values are either int32 or float (4 bytes each)
