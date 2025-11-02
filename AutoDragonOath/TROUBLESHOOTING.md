# AutoDragonOath Memory Reading Troubleshooting Guide

## Problem: FollowPointerChain returns 0 (ReadInt32 always fails)

### Root Cause Analysis

After analyzing the game source code at `G:\SourceCodeGameTLBB\Game\Client\`, I discovered the memory structure:

```
CObjectManager::s_pMe (static singleton pointer)
    ↓
CObjectManager instance
    ↓ (offset +12 or similar)
m_pMySelf (CObject_PlayerMySelf* pointer)
    ↓ (navigate through object hierarchy)
SDATA_PLAYER_MYSELF structure (character data)
```

### The Hardcoded Addresses Explained

The original MicroAuto 6.9 used these addresses (from decompiled code):

```csharp
// Base address (likely CObjectManager::s_pMe location in memory)
7319476 (0x006F8C24)

// Full pointer chain to character stats:
[7319476] → Read pointer value
[result + 12] → Navigate to m_pMySelf
[result + 344] → Navigate to character data
[result + 4] → Final SDATA_PLAYER_MYSELF structure
```

### Why It Returns 0

**Scenario 1: Wrong Game Version**
- The addresses were reverse-engineered for a specific game version
- If your game version is different, these addresses are invalid
- Reading from invalid addresses returns 0

**Scenario 2: Game Not Fully Loaded**
- CObjectManager may not be initialized yet
- Character not logged in
- Pointers are null

**Scenario 3: Insufficient Permissions**
- Process.OpenProcess fails without Administrator rights
- All memory reads return 0

**Scenario 4: Different Process Name**
- You changed it to "Game" (capital G)
- Make sure this matches the actual process name exactly

## Diagnostic Steps

### Step 1: Run Diagnostic Report

Add this code to your MainWindow.xaml.cs or create a test button:

```csharp
using AutoDragonOath.Services;
using System.Diagnostics;

private void ButtonDiagnose_Click(object sender, RoutedEventArgs e)
{
    // Find a game process
    var processes = Process.GetProcessesByName("Game");

    if (processes.Length == 0)
    {
        MessageBox.Show("No game process found!");
        return;
    }

    // Run diagnostic
    AddressFinder.GenerateDiagnosticReport(processes[0].Id);

    MessageBox.Show("Check Debug Output window for diagnostic report");
}
```

### Step 2: Check Debug Output

Run your application with debugger attached (F5 in Visual Studio) and watch the **Output** window for messages like:

```
✓ Chain successful: [7319476, 12, 344, 4] → 0x12345678
✓ Valid stats found:
  Name: PlayerName
  Level: 50
  HP: 5000/10000
```

OR

```
❌ Chain failed: [7319476, 12, 344, 4]
ReadProcessMemory failed at 0x006F8C24
```

### Step 3: Scan for New Addresses (If Current Addresses Fail)

```csharp
private void ButtonScan_Click(object sender, RoutedEventArgs e)
{
    var processes = Process.GetProcessesByName("Game");

    if (processes.Length == 0)
    {
        MessageBox.Show("No game process found!");
        return;
    }

    // WARNING: This will take several minutes!
    MessageBox.Show("Starting address scan. This may take 5-10 minutes...");

    var candidates = AddressFinder.ScanForCObjectManagerPointer(processes[0].Id);

    if (candidates.Count > 0)
    {
        string result = "Found addresses:\n";
        foreach (var addr in candidates)
        {
            result += $"0x{addr:X} ({addr})\n";
        }
        MessageBox.Show(result);
    }
    else
    {
        MessageBox.Show("No valid addresses found. Make sure character is logged in!");
    }
}
```

## Quick Fixes to Try First

### Fix 1: Run as Administrator

Right-click your AutoDragonOath.exe → "Run as Administrator"

This ensures OpenProcess has permission to read game memory.

### Fix 2: Verify Process Name

Check Task Manager → Details tab → find the exact process name.

Update in GameProcessMonitor.cs line 15:

```csharp
private const string GAME_PROCESS_NAME = "Game";  // Change to exact name
```

### Fix 3: Ensure Character is Logged In

The memory structures only exist after:
1. Game is running
2. Character is selected
3. Character is in the game world (not at character select screen)

### Fix 4: Test with Original MicroAuto 6.9

Run the original `MicroAuto 6.0.exe` from the `bin/Debug` folder.

If it also fails, the addresses are outdated for your game version.

## Understanding the Memory Offsets

From the game source code analysis:

```cpp
// G:\SourceCodeGameTLBB\Game\Client\WXClient\DataPool\GMDP_Struct_CharacterData.h

struct SDATA_PLAYER_MYSELF : public SDATA_PLAYER_OTHER
{
    STRING  m_strName;          // Offset 48 from base
    INT     m_nLevel;           // Offset 92
    INT     m_nHP;              // Offset 2292
    INT     m_nMaxHP;           // Offset 2400
    INT     m_nMP;              // Offset 2296
    INT     m_nMaxMP;           // Offset 2404
    // ... more fields
};
```

These offsets are calculated based on:
- Structure inheritance (SDATA_CHARACTER → SDATA_NPC → SDATA_PLAYER_OTHER → SDATA_PLAYER_MYSELF)
- C++ compiler padding/alignment rules
- Member variable sizes

If the game was recompiled with different settings, these offsets change.

## Advanced: Finding Addresses Manually with Cheat Engine

### Method 1: Known Value Scan

1. Download Cheat Engine
2. Attach to Game.exe
3. Search for your character's current HP (exact value)
4. Change HP in game (get hit, heal, etc.)
5. Next scan for new HP value
6. Repeat until you find the HP address
7. Use "Pointer Scanner" to find pointer chains leading to it

### Method 2: String Scan

1. In Cheat Engine: Scan Type → "String"
2. Search for your character name
3. Found address + nearby addresses may be the structure base
4. Test offsets to find other stats

### Method 3: Code Injection

1. Use Cheat Engine to find "what accesses this address"
2. This shows game code that reads/writes HP
3. Reverse engineer to find the pointer chain

## Next Steps if Addresses Cannot Be Found

### Option 1: Pattern Scanning

Instead of hardcoded addresses, scan for byte patterns:

```csharp
// Example: Find CObjectManager::s_pMe by scanning for the vtable pattern
byte[] pattern = { 0x??, 0x??, 0x??, 0x?? };  // CObjectManager vtable signature
```

This is more reliable across game updates but requires reverse engineering.

### Option 2: Use Game Hooks

Instead of reading memory, hook into game functions:

- Hook `CObjectManager::GetMySelf()`
- Hook `CCharacterData::Get_HP()`
- Hook `CCharacterData::Get_Level()`

This requires C++ DLL injection.

### Option 3: Request Updated Addresses

Contact the original MicroAuto developer or game automation community for updated addresses.

## Common Error Messages

| Error | Meaning | Solution |
|-------|---------|----------|
| `Failed to open process - may need Administrator rights` | OpenProcess failed | Run as Admin |
| `Chain failed: [7319476, ...]` | Base address is wrong | Scan for new address |
| `Invalid name characters` | Reading garbage data | Wrong offsets or wrong structure |
| `Invalid level: -1` or `0` | Character not loaded | Wait for login |
| `ReadProcessMemory failed at 0x00000000` | Null pointer in chain | Game not initialized |

## Debugging with Visual Studio

### Enable More Detailed Output

In MemoryReader.cs, the Debug.WriteLine statements are already added. Make sure you're viewing them:

1. Debug → Windows → Output
2. Show output from: "Debug"
3. Watch for detailed logging:

```
FollowPointerChain: Processing chain [7319476, 12, 344, 4]
Step 1: Read(0x006F8C24) -> 0x12345678
Step 2: Read(0x1234568A) -> 0x23456789
Step 3: Read(0x234567CD) -> 0x3456789A
Step 4: Read(0x3456789E) -> 0x456789AB
Final address: 0x456789AB
```

OR error messages:

```
FollowPointerChain: Invalid process handle
```

## Success Criteria

You know it's working when:

1. `IsValid` returns `true`
2. `FollowPointerChain` returns non-zero address (e.g., `0x12345678`)
3. Character name reads correctly (not empty, not garbage)
4. Level is 1-150
5. HP/MP percentages are 0-100%
6. Coordinates are reasonable numbers

## Contact & Resources

- Original game source: `G:\SourceCodeGameTLBB\Game\Client\`
- MicroAuto source: `G:\microauto-6.9\`
- Key files to study:
  - `ObjectManager.cpp` - Shows CObjectManager structure
  - `GMDP_CharacterData.h` - Shows character data interface
  - `GMDP_Struct_CharacterData.h` - Shows SDATA_PLAYER_MYSELF structure

## Summary

The memory reading works by following a pointer chain from a static address in the game executable. When this fails:

1. **First**: Verify permissions (run as Admin)
2. **Second**: Verify process name matches exactly
3. **Third**: Ensure character is logged in
4. **Fourth**: Run diagnostic report
5. **Last resort**: Scan for new base address

The diagnostic tools in `AddressFinder.cs` will help identify which step is failing.
