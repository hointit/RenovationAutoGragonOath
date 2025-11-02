# Using Pointer Scan in Cheat Engine to Find Memory Addresses

## Document Information

**Project:** AutoDragonOath - WPF MVVM Character Monitor for Dragon Oath MMORPG
**Date Created:** 2025-11-02
**Purpose:** Quick reference guide for using Cheat Engine pointer scanning to find and calculate memory addresses
**Game:** Dragon Oath (Thiên Long Bát Bộ)

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Step-by-Step Pointer Scan Process](#step-by-step-pointer-scan-process)
3. [Interpreting Scan Results](#interpreting-scan-results)
4. [Converting to Code](#converting-to-code)
5. [Hex to Decimal Conversion](#hex-to-decimal-conversion)
6. [Common Patterns](#common-patterns)
7. [Quick Reference Tables](#quick-reference-tables)

---

## Prerequisites

**Required:**
- ✅ Cheat Engine 7.5+ installed (https://www.cheatengine.org/)
- ✅ Game running with character logged in
- ✅ Character in game world (not at login screen)
- ✅ Windows Calculator in Programmer mode (for hex/decimal conversion)

**Time Required:** 10-30 minutes per value

---

## Step-by-Step Pointer Scan Process

### Phase 1: Find the Value

#### Step 1: Prepare the Game
1. Launch the game
2. Log in with a character
3. Enter the game world
4. Note the **exact value** you want to find (e.g., Current MP = 1514, Scene ID = 10, Level = 50)

#### Step 2: Start Cheat Engine
1. Launch **Cheat Engine**
2. Click the **computer icon** (top-left)
3. Select **Game.exe** from process list
4. Click **Open**

**Troubleshooting:**
- If "Access Denied" → Run Cheat Engine as Administrator

#### Step 3: First Scan
In Cheat Engine main window:
```
Value Type: 4 Bytes
Scan Type: Exact Value
Value: [Your value, e.g., 1514]
```
Click **First Scan**

**Result:** Thousands/millions of addresses found

#### Step 4: Filter with Next Scan
1. **In game:** Change the value (e.g., use MP potion, move to different scene, level up)
2. **Note new value** (e.g., 1600, scene 11, level 51)
3. **In Cheat Engine:**
   ```
   Scan Type: Exact Value
   Value: [New value]
   ```
4. Click **Next Scan**

**Repeat** this 3-5 times until you have **1-10 addresses** left.

#### Step 5: Verify the Address
1. Double-click the address to add to bottom panel
2. Watch value in Cheat Engine while changing it in game
3. ✅ If it updates in real-time → Correct address found!

**Example Result:**
```
Address: 0x26458C44
Value: 1514 → 1500 → 1485 (matches in-game changes)
```

---

### Phase 2: Pointer Scan

#### Step 1: Start Pointer Scan
1. **Right-click** the address in bottom panel
2. Select **"Pointer scan for this address"**
3. Save as: `C:\Temp\pointerscan_[valuename].scandata`

#### Step 2: Configure Scan Settings
```
┌─────────────────────────────────────────┐
│ Max level: 5                            │
│ Max offset value (hex): 4096            │
│ ☑ Only find paths with static addresses │
│ Number of threads: 4                    │
└─────────────────────────────────────────┘
```
Click **OK**

**Wait:** 5-15 minutes for initial scan to complete

**Result:** Hundreds of thousands of potential pointer chains

---

### Phase 3: Filter Results (CRITICAL!)

#### Step 1: Restart Game and Find New Address
1. **Close game completely**
2. **Reopen game** and log in with same character
3. **Find the NEW address** of the same value using Steps 1.3-1.5 above
   - Address will be different due to ASLR (e.g., was `0x26458C44`, now `0x34567890`)

#### Step 2: Rescan Pointer Results
**In Pointer Scan window:**
1. Menu → **Pointer scanner** → **Rescan memory**
2. Enter the **NEW address** (e.g., `0x34567890`)
3. Click **OK**

**Result:** Pointer list shrinks dramatically (500,000 → 50,000)

#### Step 3: Repeat 3-4 More Times
```
┌─────────────────────────────────────────────┐
│ Restart #1: 500,000 → 50,000 pointers       │
│ Restart #2:  50,000 → 5,000 pointers        │
│ Restart #3:   5,000 → 500 pointers          │
│ Restart #4:     500 → 18 pointers ✅        │
└─────────────────────────────────────────────┘
```

**Goal:** Get down to **<100 results** (ideally <50)

---

## Interpreting Scan Results

### Understanding the Output

Cheat Engine shows results like this:

```csv
Module_Offset,Pointer_Offset,Address,Value
"Game.exe"+00245800,C,03295D2C,1514
"Game.exe"+00245804,C,03295D2C,1514
"Game.exe"+0024581C,C,03295D2C,1514
"UI_CEGUI.dll"+000371D4,64,03295D2C,1514
```

Or in table format:

```
Base Address        | Offset 0 | Offset 1 | Offset 2 | Points to
--------------------|----------|----------|----------|------------------
"Game.exe"+00245800 | C        | 154      | 4        | 26458C44 = 1514
"Game.exe"+00245804 | C        | 154      | 4        | 26458C44 = 1514
"UI_CEGUI.dll"+0x01 | 64       | 154      | 4        | 26458C44 = 1514
```

### Column Meanings

| Column | Meaning | Example |
|--------|---------|---------|
| **Module_Offset** or **Base Address** | Module + offset (static) | "Game.exe"+00245800 |
| **Pointer_Offset** or **Offset 0-N** | Pointer chain offsets (hex) | C, 154, 4 |
| **Address** | Final memory address | 26458C44 |
| **Value** | Current value at that address | 1514 |

### Reading a Pointer Chain

Example: `"Game.exe"+00245800 → C → 154 → 4`

**Means:**
1. Start at `Game.exe` base address
2. Add `0x245800` (2,381,824 decimal) → Read pointer
3. Add `0xC` (12 decimal) → Read pointer
4. Add `0x154` (340 decimal) → Read pointer
5. Add `0x4` (4 decimal) → Final value address!

---

## Converting to Code

### Selection Criteria

Choose the **BEST** pointer chain based on:

1. ✅ **Shortest chain** (fewer offsets = more stable)
2. ✅ **Starts with "Game.exe"+** (not DLLs like UI_CEGUI.dll or Render.dll)
3. ✅ **Appears multiple times** (indicates reliability)
4. ✅ **Close to known base addresses** (similar to existing pointers)

### Example Analysis

**Your Cheat Engine Results:**
```
"Game.exe"+00244938,17C,03295D2C,10
"Game.exe"+0024584C,17C,03295D2C,10  ← BEST CHOICE
"UI_CEGUI.dll"+000371D8,17C,03295D2C,10
"Render.dll"+00121ED0,17C,03295D2C,10
```

**Why "Game.exe"+0024584C is best:**
- ✅ Starts with "Game.exe"+ (not DLL)
- ✅ Short chain (only 1 offset: 0x17C)
- ✅ Appears in Game.exe module
- ✅ Close to existing StatsBasePointer (2381824)

**Calculation:**
```
Base Offset: 0x24584C = 2,381,900 (decimal)
Offset 1:    0x17C = 380 (decimal)
```

### Converting to C# Code

**Pattern 1: Simple Pointer Chain (1-2 levels)**

```csharp
// Cheat Engine: "Game.exe"+0024584C → 17C → Value
private static readonly int[] MapBasePointer = { 2381900 };  // 0x24584C
private const int OFFSET_MAP_ID = 380;  // 0x17C
```

**Pattern 2: Multi-Level Pointer Chain (3+ levels)**

```csharp
// Cheat Engine: "Game.exe"+00245800 → C → 154 → 4 → 6DC → Value
private static readonly int[] STATS_BASE_POINTER = { 2381824, 12, 340, 4 };
private const int OFFSET_CURRENT_MP = 1756;  // 0x6DC
```

### Understanding Pointer Arrays vs Offsets

**Pointer Chain Array:** Path to BASE address
**Offset Constant:** Distance FROM base to specific field

```csharp
// This pointer chain gets you to the CHARACTER STATS structure base
int[] STATS_BASE_POINTER = { 2381824, 12, 340, 4 };

// Then you add offsets to read specific fields:
OFFSET_CURRENT_MP = 1756;   // Character's current MP
OFFSET_CURRENT_HP = 1752;   // Character's current HP
OFFSET_LEVEL = 92;          // Character's level
```

**In code:**
```csharp
int statsBase = memoryReader.FollowPointerChain(STATS_BASE_POINTER);
int currentMP = memoryReader.ReadInt32(statsBase + OFFSET_CURRENT_MP);
int level = memoryReader.ReadInt32(statsBase + OFFSET_LEVEL);
```

---

## Hex to Decimal Conversion

### Method 1: Windows Calculator

1. Open Calculator
2. View → **Programmer**
3. Select **HEX**
4. Type: `6DC`
5. Select **DEC**
6. Result: `1756` ✅

### Method 2: Online Converter

https://www.rapidtables.com/convert/number/hex-to-decimal.html

### Method 3: In C# Code

```csharp
int decimalValue = Convert.ToInt32("6DC", 16);  // Returns 1756
```

### Common Conversions

| Hex | Decimal | Common Use |
|-----|---------|------------|
| 4 | 4 | Small offset |
| C | 12 | Object manager pointer |
| 10 | 16 | |
| 30 | 48 | Character name offset |
| 5C | 92 | Level / X coordinate |
| 64 | 100 | Y coordinate / Max HP offset |
| A0 | 160 | |
| 154 | 340 | Navigation offset |
| 17C | 380 | Map offset |
| 6D8 | 1752 | Current HP |
| 6DC | 1756 | Current MP |
| 740 | 1856 | Max HP |
| 744 | 1860 | Max MP |
| 968 | 2408 | Experience |

---

## Common Patterns

### Pattern 1: Character Stats Structure

**From Pointer Scan:**
```
"Game.exe"+00245800 → C → 154 → 4 → 6DC
```

**Convert to Code:**
```csharp
// Pointer chain to stats base
private static readonly int[] STATS_BASE_POINTER = { 2381824, 12, 340, 4 };

// Offsets from stats base
private const int OFFSET_CURRENT_MP = 1756;  // 0x6DC (the last offset)
```

**Why split?** Because MANY fields are at this base:
```csharp
private const int OFFSET_CHARACTER_NAME = 48;
private const int OFFSET_LEVEL = 92;
private const int OFFSET_CURRENT_HP = 1752;  // MP - 4
private const int OFFSET_CURRENT_MP = 1756;
private const int OFFSET_MAX_HP = 1856;      // MP + 100
private const int OFFSET_MAX_MP = 1860;      // MP + 104
```

### Pattern 2: Simple Value (Map ID, Scene ID)

**From Pointer Scan:**
```
"Game.exe"+0024584C → 17C
```

**Convert to Code:**
```csharp
private static readonly int[] MapBasePointer = { 2381900 };
private const int OFFSET_MAP_ID = 380;
```

**Usage:**
```csharp
int mapBase = memoryReader.FollowPointerChain(MapBasePointer);
int mapId = memoryReader.ReadInt32(mapBase + OFFSET_MAP_ID);
```

### Pattern 3: Entity Position

**From Pointer Scan:**
```
"Game.exe"+00245800 → C → 5C
```

**Convert to Code:**
```csharp
private static readonly int[] EntityBasePointer = { 2381824, 12 };
private const int OFFSET_X_COORDINATE = 92;   // 0x5C
private const int OFFSET_Y_COORDINATE = 100;  // 0x5C + 8 (usually)
```

### Pattern 4: Calculating Related Offsets

Once you find ONE value via Cheat Engine, you can calculate related values:

**Example:** Found Current MP at offset `1756`

```csharp
// In C++, INT fields are 4 bytes each, laid out sequentially:
private const int OFFSET_CURRENT_MP = 1756;                   // Found via CE
private const int OFFSET_CURRENT_HP = OFFSET_CURRENT_MP - 4;  // = 1752
private const int OFFSET_EXPERIENCE = OFFSET_CURRENT_MP + 4;  // = 1760
private const int OFFSET_MAX_HP = OFFSET_CURRENT_MP + 100;    // = 1856
private const int OFFSET_MAX_MP = OFFSET_CURRENT_MP + 104;    // = 1860
```

**Why this works:** C++ structs pack fields sequentially with 4-byte alignment.

---

## Quick Reference Tables

### Typical AutoDragonOath Memory Structure

| Data Type | Base Pointer | Common Offsets |
|-----------|-------------|----------------|
| **Character Stats** | `{ 2381824, 12, 340, 4 }` | Name: 48, Level: 92, HP: 1752, MP: 1756 |
| **Character Position** | `{ 2381824, 12 }` | X: 92, Y: 100 |
| **Map/Scene ID** | `{ 2381900 }` | Map ID: 380 |
| **Pet Data** | `{ 7319540, 299356 }` | HP: 40, Max HP: 44 |

### Offset Calculation Rules

| If You Found | You Can Calculate |
|--------------|-------------------|
| Current MP (INT) | Current HP = MP - 4<br>Experience = MP + 4<br>Max HP = MP + 100<br>Max MP = MP + 104 |
| X Coordinate (float) | Y Coordinate = X + 8 (usually Z coordinate in memory) |
| Map Base | Map ID usually within +0 to +500 range |

### Data Type Sizes

| Type | Size | Example Offset Pattern |
|------|------|----------------------|
| INT | 4 bytes | 1752, 1756, 1760 (sequential) |
| FLOAT | 4 bytes | 92, 96, 100 (x, y, z) |
| STRING | Variable | Usually starts at 4-byte boundary |
| Pointer | 4 bytes (x86) | Always 4-byte aligned |

---

## Workflow Summary

### When You Get Pointer Scan Results

**Example User Input:**
```
Module_Offset,Pointer_Offset,Address,Value
"Game.exe"+00244938,17C,03295D2C,10
"Game.exe"+0024584C,17C,03295D2C,10
"UI_CEGUI.dll"+000371D8,17C,03295D2C,10
```

**Step 1: Select Best Chain**
```
✅ Choose: "Game.exe"+0024584C,17C
❌ Reject: UI_CEGUI.dll (external DLL)
```

**Step 2: Convert Hex to Decimal**
```
0x24584C = 2,381,900
0x17C = 380
```

**Step 3: Determine Pointer Type**

**If only 1 offset (simple pointer):**
```csharp
private static readonly int[] MapBasePointer = { 2381900 };
private const int OFFSET_MAP_ID = 380;
```

**If multiple offsets (chain pointer):**
```csharp
private static readonly int[] BasePointer = { 2381900, 380 };
// Then add more offsets if needed
```

**Step 4: Update GameProcessMonitor.cs**

Find the relevant section and update:
```csharp
// OLD
private static readonly int[] MapBasePointer = { 2381824, 13692 };
private const int OFFSET_MAP_ID = 96;

// NEW (from your Cheat Engine scan)
private static readonly int[] MapBasePointer = { 2381900 };
private const int OFFSET_MAP_ID = 380;
```

**Step 5: Test**
1. Build project: `dotnet build`
2. Run as Administrator
3. Verify value matches in-game
4. Test after game restart to confirm stability

---

## Troubleshooting

### Issue: Too Many Results After Pointer Scan

**Symptom:** Still have 10,000+ pointer chains after 4 restarts

**Solution:**
- Restart game 2-3 more times and rescan
- Lower "Max level" to 4 or 3
- Lower "Max offset value" to 2048 or 1024

### Issue: Zero Results After Rescan

**Symptom:** Rescan drops to 0 pointers

**Solution:**
- Your initial scan might have been for wrong value
- Start over from Phase 1
- Make sure you're finding the EXACT same value type (level, MP, scene ID, etc.)

### Issue: Pointer Works Once, Fails After Game Update

**Symptom:** Code worked yesterday, returns 0 today

**Solution:**
- Game was updated/patched
- Memory structure changed
- Redo entire pointer scan process
- **This is normal and expected** - hardcoded addresses break on updates

---

## Examples

### Example 1: Finding Current MP

**Cheat Engine Result:**
```
"Game.exe"+00245800,C,154,4,6DC,26458C44,1514
```

**Code:**
```csharp
private static readonly int[] STATS_BASE_POINTER = { 2381824, 12, 340, 4 };
private const int OFFSET_CURRENT_MP = 1756;  // 0x6DC

// Usage:
int statsBase = memoryReader.FollowPointerChain(STATS_BASE_POINTER);
int currentMP = memoryReader.ReadInt32(statsBase + OFFSET_CURRENT_MP);
```

### Example 2: Finding Scene/Map ID

**Cheat Engine Result:**
```
"Game.exe"+0024584C,17C,03295D2C,10
```

**Code:**
```csharp
private static readonly int[] MapBasePointer = { 2381900 };  // 0x24584C
private const int OFFSET_MAP_ID = 380;  // 0x17C

// Usage:
int mapBase = memoryReader.FollowPointerChain(MapBasePointer);
int sceneId = memoryReader.ReadInt32(mapBase + OFFSET_MAP_ID);
```

### Example 3: Finding Character Level

**Cheat Engine Result:**
```
"Game.exe"+00245800,C,154,4,5C,26458ABC,50
```

**Code:**
```csharp
private static readonly int[] STATS_BASE_POINTER = { 2381824, 12, 340, 4 };
private const int OFFSET_LEVEL = 92;  // 0x5C

// Usage:
int statsBase = memoryReader.FollowPointerChain(STATS_BASE_POINTER);
int level = memoryReader.ReadInt32(statsBase + OFFSET_LEVEL);
```

---

## Related Documentation

- **COMPLETE_GUIDE_FIND_BASE_ADDRESS.md** - Comprehensive guide with architecture details
- **TROUBLESHOOTING.md** - Common errors and solutions
- **FINDINGS.md** - Technical memory structure analysis
- **GameProcessMonitor.cs** - Implementation code

---

## Quick Start Template

**When you get scan results, use this template:**

```
1. Cheat Engine Results:
   [Paste your scan results here]

2. Selected Chain:
   "Game.exe"+[HEX] → [OFFSET1] → [OFFSET2] → ...

3. Converted to Decimal:
   Base: [DECIMAL]
   Offset 1: [DECIMAL]
   Offset 2: [DECIMAL]

4. Code Update Location:
   File: GameProcessMonitor.cs
   Line: [XX]

5. New Code:
   private static readonly int[] [Name]BasePointer = { [BASE], [OFF1], [OFF2] };
   private const int OFFSET_[FIELD] = [VALUE];
```

---

**End of Guide**

Last Updated: 2025-11-02
Version: 1.0
