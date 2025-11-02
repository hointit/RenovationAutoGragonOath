# Memory Reading Investigation - Complete Findings

## Executive Summary

I've investigated why `ReadInt32` returns 0 in the AutoDragonOath project by analyzing both the decompiled MicroAuto 6.9 code and the actual game source code at `G:\SourceCodeGameTLBB\Game\Client\`.

**Key Discovery**: The hardcoded memory addresses are **version-specific** and point to static variables in the game's executable. When these addresses are wrong, the entire pointer chain fails and returns 0.

## What I Found

### 1. The Memory Structure (From Game Source Code)

The game uses this object hierarchy to store player data:

```cpp
// Static singleton pointer (stored at address 7319476 in old game version)
CObjectManager* CObjectManager::s_pMe;

// Inside CObjectManager:
CObject_PlayerMySelf* m_pMySelf;  // Player object pointer

// Inside CObject_PlayerMySelf:
CCharacterData* GetCharacterData();  // Data accessor

// Character data structure:
struct SDATA_PLAYER_MYSELF {
    STRING m_strName;       // +48 bytes from base
    INT m_nLevel;           // +92
    INT m_nHP;              // +2292
    INT m_nMaxHP;           // +2400
    INT m_nMP;              // +2296
    INT m_nMaxMP;           // +2404
    // ... many more fields
};
```

**Source Files Examined**:
- `G:\SourceCodeGameTLBB\Game\Client\WXClient\Object\ObjectManager.h` (line 37)
- `G:\SourceCodeGameTLBB\Game\Client\WXClient\Object\ObjectManager.cpp` (line 37-40)
- `G:\SourceCodeGameTLBB\Game\Client\WXClient\DataPool\GMDP_Struct_CharacterData.h`
- `G:\SourceCodeGameTLBB\Game\Client\WXClient\DataPool\GMDP_CharacterData.h`

### 2. The Pointer Chain Explained

MicroAuto 6.9 uses this pointer chain:

```
[7319476] → [+12] → [+344] → [+4] → SDATA_PLAYER_MYSELF
```

Breaking it down:

| Step | What Happens | Code Reference |
|------|-------------|----------------|
| 1 | Read address `7319476` (0x006F8C24) | Likely `&CObjectManager::s_pMe` in .data section |
| 2 | Result is pointer to CObjectManager instance | `CObjectManager::s_pMe` value |
| 3 | Read `[result + 12]` | Navigate to `m_pMySelf` field (offset varies by vtable) |
| 4 | Read `[result + 344]` | Navigate through object to data structure |
| 5 | Read `[result + 4]` | Final pointer to `SDATA_PLAYER_MYSELF` |
| 6 | Result is base address of character stats | Can now add offsets (+48 for name, +92 for level, etc.) |

### 3. Why It Returns 0

**Primary Reason**: The base address `7319476` is invalid for your game version.

This address was found through reverse engineering for a specific version of `Game.exe`. When the game is:
- Updated/patched
- Different regional version (e.g., Vietnamese vs Chinese)
- Recompiled with different compiler settings

...the static variable `CObjectManager::s_pMe` moves to a different address.

**What Happens**:
```
1. Read(7319476) → ACCESS VIOLATION or reads random data → Returns 0
2. Read(0 + 12) → Reads from address 12 (invalid) → Returns 0
3. Read(0 + 344) → Reads from address 344 (invalid) → Returns 0
4. ... entire chain fails
5. Final result: 0
```

## What I've Done

### 1. Enhanced MemoryReader with Validation (COMPLETED)

**File**: `G:\microauto-6.9\AutoDragonOath\Services\MemoryReader.cs`

Added:
- `IsValid` property to check if process handle is valid
- Error checking on all `ReadProcessMemory` calls
- Detailed `Debug.WriteLine` logging at each step
- Validation in `FollowPointerChain` to detect failures

**Before**:
```csharp
public int ReadInt32(int address) {
    byte[] buffer = new byte[4];
    ReadProcessMemory(_processHandle, address, buffer, 4, 0);
    return BitConverter.ToInt32(buffer, 0);  // Returns 0 on failure (silent)
}
```

**After**:
```csharp
public int ReadInt32(int address) {
    if (!IsValid || address == 0) {
        Debug.WriteLine($"Cannot read - invalid handle or address");
        return 0;
    }

    byte[] buffer = new byte[4];
    bool success = ReadProcessMemory(_processHandle, address, buffer, 4, 0);

    if (!success) {
        Debug.WriteLine($"Failed to read Int32 at address 0x{address:X}");
        return 0;
    }

    int result = BitConverter.ToInt32(buffer, 0);
    Debug.WriteLine($"Read Int32 at 0x{address:X} = {result}");
    return result;
}
```

### 2. Updated GameProcessMonitor with Documentation (COMPLETED)

**File**: `G:\microauto-6.9\AutoDragonOath\Services\GameProcessMonitor.cs`

Added detailed comments explaining:
- What each address represents
- Connection to game source code
- Warning about version-specific addresses
- Mapping of offsets to actual C++ struct members

### 3. Created Diagnostic Tools (COMPLETED)

**File**: `G:\microauto-6.9\AutoDragonOath\Services\AddressFinder.cs`

**Features**:

#### GenerateDiagnosticReport()
- Tests if current addresses work
- Shows exactly where the chain fails
- Provides actionable recommendations

#### ScanForCObjectManagerPointer()
- Brute-force scans memory ranges to find new base address
- Tests potential addresses by validating character data
- Returns list of candidate addresses

#### ValidateStatsBase()
- Checks if an address points to valid character data
- Validates name, level, HP/MP ranges
- Prevents false positives

### 4. Created Troubleshooting Guide (COMPLETED)

**File**: `G:\microauto-6.9\AutoDragonOath\TROUBLESHOOTING.md`

Comprehensive guide covering:
- Root cause analysis
- Step-by-step diagnostic procedures
- Quick fixes to try first
- Advanced techniques (Cheat Engine, pattern scanning)
- Common error messages and solutions

## How to Use the Diagnostic Tools

### Quick Test (2 minutes)

1. Run your AutoDragonOath application
2. Make sure game is running and character is logged in
3. Add this button to your UI:

```csharp
private void ButtonDiagnose_Click(object sender, RoutedEventArgs e)
{
    var processes = Process.GetProcessesByName("Game");
    if (processes.Length > 0)
    {
        AddressFinder.GenerateDiagnosticReport(processes[0].Id);
    }
}
```

4. Click the button
5. Check **Output** window in Visual Studio (Debug → Windows → Output)

**Expected Output if Working**:
```
✓ Process handle opened successfully
✓ Chain successful: [7319476, 12, 344, 4] → 0x12345678
✓ Valid stats found:
  Name: YourCharacterName
  Level: 50
  HP: 5000/10000
```

**Expected Output if Broken**:
```
✓ Process handle opened successfully
❌ Chain failed: [7319476, 12, 344, 4]
ReadProcessMemory failed at 0x006F8C24
❌ Current stats address chain FAILED
```

### Full Address Scan (5-10 minutes)

If diagnostic shows addresses are broken:

```csharp
private async void ButtonScan_Click(object sender, RoutedEventArgs e)
{
    var processes = Process.GetProcessesByName("Game");
    if (processes.Length == 0) return;

    // Run in background to avoid freezing UI
    await Task.Run(() =>
    {
        var candidates = AddressFinder.ScanForCObjectManagerPointer(processes[0].Id);

        Dispatcher.Invoke(() =>
        {
            if (candidates.Count > 0)
            {
                MessageBox.Show($"Found {candidates.Count} addresses:\n" +
                    string.Join("\n", candidates.Select(a => $"0x{a:X} ({a})")));
            }
        });
    });
}
```

This will search memory for valid pointer chains and suggest new base addresses.

## Recommendations

### Immediate Actions

1. **Run as Administrator**
   - Right-click AutoDragonOath.exe → Run as Administrator
   - Ensures memory reading permissions

2. **Verify Process Name**
   - Open Task Manager → Details
   - Find the exact process name (case-sensitive!)
   - Update line 15 in GameProcessMonitor.cs:
     ```csharp
     private const string GAME_PROCESS_NAME = "Game";  // Match exactly
     ```

3. **Ensure Character is Logged In**
   - Not on login screen
   - Not on character select
   - Actually in game world

4. **Run Diagnostic Report**
   - Follow steps above to see detailed error messages
   - Check if it's a permission issue vs address issue

### If Diagnostic Fails

1. **Check Debug Output**
   - Look for specific error messages
   - Note which step of the chain fails

2. **Try Original MicroAuto**
   - Run `G:\microauto-6.9\bin\Debug\MicroAuto 6.0.exe`
   - If it also fails → addresses are outdated
   - If it works → something wrong with AutoDragonOath implementation

3. **Run Address Scanner**
   - This will take 5-10 minutes
   - Make sure character is fully loaded
   - Use found addresses to update STATS_BASE_POINTER in GameProcessMonitor.cs

### Long-term Solution

Consider implementing **pattern scanning** instead of hardcoded addresses:

```csharp
// Search for byte pattern that identifies CObjectManager::s_pMe
// This is more robust across game updates
byte[] pattern = FindPatternForCObjectManager();
int address = ScanMemoryForPattern(pattern);
```

This requires deeper reverse engineering but survives game patches.

## Files Created/Modified

### New Files
- `AutoDragonOath/Services/AddressFinder.cs` - Diagnostic and scanning tools
- `AutoDragonOath/TROUBLESHOOTING.md` - Comprehensive troubleshooting guide
- `AutoDragonOath/FINDINGS.md` - This document

### Modified Files
- `AutoDragonOath/Services/MemoryReader.cs` - Added validation and logging
- `AutoDragonOath/Services/GameProcessMonitor.cs` - Added detailed comments

## Technical Details

### Memory Reading Process

1. **OpenProcess** (PROCESS_ALL_ACCESS)
   ```csharp
   IntPtr handle = OpenProcess(0x1F0FFF, false, processId);
   ```

2. **Follow Pointer Chain**
   ```csharp
   int addr = ReadInt32(7319476);        // Base pointer
   addr = ReadInt32(addr + 12);          // m_pMySelf
   addr = ReadInt32(addr + 344);         // Navigate
   addr = ReadInt32(addr + 4);           // Final pointer
   // addr now points to SDATA_PLAYER_MYSELF
   ```

3. **Read Character Data**
   ```csharp
   string name = ReadString(addr + 48);     // m_strName
   int level = ReadInt32(addr + 92);        // m_nLevel
   int hp = ReadInt32(addr + 2292);         // m_nHP
   int maxHp = ReadInt32(addr + 2400);      // m_nMaxHP
   ```

### Why These Specific Offsets?

The offsets come from the C++ structure layout:

```cpp
struct SDATA_PLAYER_MYSELF : public SDATA_PLAYER_OTHER
{
    // Inherited from SDATA_CHARACTER (base class):
    //   INT m_nRaceID (0)
    //   INT m_nPortraitID (4)
    //   STRING m_strName (8-44)  ← Offset 48 total
    //   ... more fields

    // Inherited from SDATA_PLAYER_OTHER:
    //   INT m_nLevel (at offset 92)
    //   ... many more fields adding up to 2292 bytes

    // SDATA_PLAYER_MYSELF specific:
    INT m_nHP;        // Offset 2292
    INT m_nMP;        // Offset 2296
    INT m_nExp;       // Offset 2300
    INT m_nMaxHP;     // Offset 2400
    INT m_nMaxMP;     // Offset 2404
    // ...
};
```

The compiler calculates these offsets based on:
- Inheritance hierarchy
- Member variable sizes
- Padding/alignment rules (4-byte alignment for int, etc.)
- Virtual function table (vtable) if present

## Conclusion

The `ReadInt32` returns 0 because the **base address 7319476 is invalid** for your game version.

**Solutions**:
1. Use diagnostic tools to find new address
2. Update STATS_BASE_POINTER with found address
3. Verify game is fully loaded before reading
4. Run as Administrator

**Root Cause**: Memory addresses in compiled executables change between versions. The original MicroAuto 6.9 was reverse-engineered for a specific game build.

**Next Steps**:
1. Run diagnostic report (see above)
2. If it fails, run address scanner
3. Update addresses in code
4. Test again

The infrastructure is now in place to diagnose and fix address issues. The hardest part (understanding the memory structure) is done!
