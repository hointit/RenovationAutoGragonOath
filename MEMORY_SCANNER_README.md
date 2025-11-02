# Memory Scanner Tool - Quick Start Guide

**Purpose**: Find Map Object Pointer and unknown attribute offsets (STR, SPR, CON, INT, DEX, etc.)

---

## Files Created

1. **MemoryScanner.cs** - Core scanning library
2. **MemoryScannerUsageGuide.cs** - Interactive console program with 5 scenarios
3. **MEMORY_SCANNER_README.md** - This guide

---

## Quick Start

### Step 1: Compile the Program

**Option A: Using Visual Studio**
1. Create a new Console Application (.NET Framework or .NET Core)
2. Add both `.cs` files to your project
3. Build → Build Solution
4. Run as **Administrator**

**Option B: Using Command Line (csc.exe)**

```bash
# Navigate to the folder
cd G:\microauto-6.9

# Compile (Framework 2.0 compatible)
csc.exe /out:MemoryScanner.exe MemoryScanner.cs MemoryScannerUsageGuide.cs

# Run as Administrator
MemoryScanner.exe
```

**Option C: Add to Existing MicroAuto Project**

Copy both files to your MicroAuto 6.0 solution folder, then:
- Right-click solution → Add → Existing Item
- Add both `.cs` files
- Set as startup project or create new console app project

---

## 5 Usage Scenarios

### 🎯 Scenario 1: Find Map Object Pointer

**What it does**: Scans memory for map name strings and finds pointers to them

**Steps**:
1. Login to your character in game
2. Run option `1` in the menu
3. The program will:
   - Scan for map names like "heaven", "nhon nam", etc.
   - Find pointers pointing to those map names
   - Show addresses in low memory range (likely static pointers)
4. Write down the candidate addresses
5. **Change maps in game** (teleport somewhere)
6. Run the scan again
7. The address that now points to the NEW map name is your Map Object Pointer!

**Example Output**:
```
Found map name at 0x12ABCD00 (312345600)
  Pointer at: 0x00689ABC (6855360)  ← This is a candidate!
```

---

### 🎯 Scenario 2: Find Single Attribute Offset (Step-by-Step)

**What it does**: Uses value comparison to find a specific attribute like STR

**Steps**:
1. Check your character's STR in game (e.g., 150)
2. Run option `2`
3. Enter your STR value (150)
4. Program scans and finds 500+ addresses with value 150
5. **In game**: Add 1 point to STR (now 151)
6. Enter new value (151)
7. Program narrows down to only addresses that changed to 151
8. Calculate offset from your known Stats Base Address
9. Verify by checking memory dump

**Example**:
```
Initial STR: 150 → Found 543 addresses
New STR: 151 → Narrowed to 3 addresses

Address: 0x56789ABC
  Offset from stats base: +184 (0xB8)
  *** LIKELY CANDIDATE: StatsBase + 184 ***
```

---

### 🎯 Scenario 3: Test Your Player Pointer Chain

**What it does**: Validates if your updated addresses are correct

**Steps**:
1. Run option `3`
2. The program will:
   - Read `[2381824]` (your new base pointer)
   - Follow the chain: `[2381824, 12, 340, 4]`
   - Read character name, HP, MP, experience
   - Show you if the chain is working
3. If character name is empty or values are wrong → chain is incorrect
4. If data looks correct → chain is valid! ✓

**Example Output**:
```
✓ Entity Object Pointer: 0x12345678
✓ Entity Base Address: 0x23456789
✓ Stats Object Pointer: 0x34567890
✓ Stats Base Address: 0x45678901

Character Name: MyCharacter
HP: 4523/5000
MP: 2100/3500
Experience: 123456789

✅ SUCCESS! Pointer chain is working correctly!
```

---

### 🎯 Scenario 4: Find ALL Attributes at Once (Pattern Method)

**What it does**: Finds STR, SPR, CON, INT, DEX by matching the pattern of 5 consecutive values

**Requirements**: Know all 5 attribute values

**Steps**:
1. Check all 5 attributes in game:
   - STR: 150
   - SPR: 120
   - CON: 140
   - INT: 100
   - DEX: 130
2. Run option `4`
3. Enter all 5 values
4. Program searches for locations where these 5 int32 values appear consecutively
5. If found, calculates offsets for all 5 attributes

**Advantages**:
- Finds all 5 attributes in one scan
- Very accurate (pattern of 5 values is unique)
- No need to add points or change values

**Example Output**:
```
✓ FOUND PATTERN at 0x45678ABC:
  +0  STR = 150
  +4  SPR = 120
  +8  CON = 140
  +12 INT = 100
  +16 DEX = 130

STR Offset: +200 (0xC8)
SPR Offset: +204 (0xCC)
CON Offset: +208 (0xD0)
INT Offset: +212 (0xD4)
DEX Offset: +216 (0xD8)
```

---

### 🎯 Scenario 5: Monitor Map Changes

**What it does**: Watches specific addresses during map changes to verify which one is the Map Object Pointer

**When to use**: After Scenario 1, when you have candidate addresses but need to verify

**Steps**:
1. Run option `5`
2. Enter your candidate addresses (from Scenario 1):
   ```
   Address: 6855360
   Address: 6901234
   Address: done
   ```
3. Program reads initial map name from each address
4. **Change maps in game** (use teleport)
5. Press ENTER
6. Program checks all addresses again
7. The one that changed to new map name is your Map Object Pointer!

**Example Output**:
```
INITIAL STATE:
0x00689ABC:
  Pointer: 0x12ABCD00
  Map: heaven

AFTER MAP CHANGE:
0x00689ABC:
  Pointer: 0x13BCDE00
  Map: nhon nam
  ✓✓✓ CHANGED! (was 'heaven') ***
  >>> THIS IS LIKELY YOUR MAP OBJECT POINTER! <<<
```

---

## Common Issues & Solutions

### Issue: "Failed to open process"

**Solution**: Run as Administrator
```bash
# Right-click → Run as Administrator
MemoryScanner.exe
```

### Issue: "Game process not found"

**Solution**:
- Make sure game.exe is running
- Check the process name in Task Manager
- If it's not "game.exe", modify the code:
  ```csharp
  Process[] processes = Process.GetProcessesByName("YourGameProcessName");
  ```

### Issue: Too many scan results (>1000)

**Solution**: Use narrowing technique
1. First scan finds 1000+ addresses
2. Change the value in game
3. Run narrow scan
4. Repeat until <20 results

### Issue: Pattern not found (Scenario 4)

**Possible reasons**:
- Attributes might not be stored consecutively
- Values might be stored as different data types (float, short, etc.)
- Use Scenario 2 instead (find one at a time)

### Issue: Max MP offset is wrong (1860 vs 1852)

**To verify**:
```csharp
// In Scenario 3, add this code:
int? maxHP = scanner.ReadInt32(statsBase.Value + 1856);
int? testMP1 = scanner.ReadInt32(statsBase.Value + 1852);
int? testMP2 = scanner.ReadInt32(statsBase.Value + 1860);

Console.WriteLine($"MaxHP: {maxHP}");
Console.WriteLine($"Test offset 1852: {testMP1}");
Console.WriteLine($"Test offset 1860: {testMP2}");
// Whichever matches your in-game Max MP is correct
```

---

## Integration with MicroAuto

Once you find the offsets, update your code:

### Update Class7.cs pointer chains:

```csharp
// OLD (before 2025-11-02)
int[] oldPlayerBase = { 7319476, 12, 344, 4 };

// NEW (updated 2025-11-02)
int[] newPlayerBase = { 2381824, 12, 340, 4 };
```

### Update GClass0.cs offsets:

```csharp
public int GetCurrentHP()
{
    // OLD: statsBase + 2292
    // NEW: statsBase + 1752
    return class7_0.method_0(this.method_29() + 1752);
}

public int GetMaxHP()
{
    // OLD: statsBase + 2400
    // NEW: statsBase + 1856
    return class7_0.method_0(this.method_29() + 1856);
}
```

### Add new attribute methods:

```csharp
// Once you find the offsets from Scenario 4:
public int GetStrength()
{
    return class7_0.method_0(this.method_29() + OFFSET_STR);
}

public int GetSpirit()
{
    return class7_0.method_0(this.method_29() + OFFSET_SPR);
}

// etc.
```

---

## Recommended Workflow

**For Map Object Pointer:**

1. Run **Scenario 1** → Get candidate addresses
2. Run **Scenario 5** → Verify which one changes with map
3. Test the pointer by reading map names
4. Update `method_25()` in GClass0.cs with new base address

**For Attributes (STR, SPR, CON, INT, DEX):**

**Method A** (if you know all 5 values):
1. Run **Scenario 4** → Pattern matching finds all at once
2. Verify with memory dump
3. Add methods to GClass0.cs

**Method B** (if you can add attribute points):
1. Run **Scenario 2** for STR → Find offset
2. Repeat for SPR, CON, INT, DEX
3. Verify they're 4 bytes apart (+0, +4, +8, +12, +16)
4. Add methods to GClass0.cs

**Always verify with Scenario 3** after making changes!

---

## Understanding the Output

### Memory Dump Format

```
Offset  | Dec Value   | Hex Value  | ASCII
--------|-------------|------------|--------
+    0 |      150    | 0x00000096 | ....
+    4 |      120    | 0x00000078 | ....
+    8 |      140    | 0x0000008C | ....
```

- **Offset**: Distance from start address
- **Dec Value**: Integer value (useful for stats like HP, STR)
- **Hex Value**: Same value in hexadecimal
- **ASCII**: If the bytes represent text

### Pointer Chain Notation

`[2381824, 12, 340, 4]` means:

1. Read int32 at address 2381824 → Get pointer A
2. Read int32 at (A + 12) → Get pointer B
3. Read int32 at (B + 340) → Get pointer C
4. Read int32 at (C + 4) → Get final address

---

## Expected Results

### Map Object Pointer (what to expect)

- **Base Address**: Probably in range 6000000 - 8000000 (0x5B8D80 - 0x7A1200)
- **Structure**: Likely `[BaseAddr, LargeOffset]` where LargeOffset > 10000
- **What it points to**: A structure containing map ID and map name string

### Attribute Offsets (what to expect)

- **Location**: Inside Stats Base structure
- **Likely range**: +100 to +500 from Stats Base
- **Pattern**: Probably consecutive (STR at +X, SPR at +X+4, CON at +X+8, etc.)
- **Data type**: int32 (4 bytes each)

---

## Next Steps

1. **Find the addresses** using the scanner
2. **Verify** they're stable (restart game, check again)
3. **Document** your findings in `PLAYER_INFORMATION_MEMORY_STRUCTURE.md`
4. **Update** the automation code (GClass0.cs, Class7.cs)
5. **Test** thoroughly with Scenario 3

---

## Support & Contributions

If you successfully find the Map Object Pointer or attribute offsets:

1. Update `PLAYER_INFORMATION_MEMORY_STRUCTURE.md`
2. Replace `???` with actual offsets
3. Share your findings!

**Questions?** Check the code comments in `MemoryScanner.cs` for detailed explanations.

---

**Good luck with your memory scanning!** 🔍
