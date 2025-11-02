# Complete Guide: Finding Memory Addresses for AutoDragonOath

## Document Information

**Project:** AutoDragonOath - WPF MVVM Character Monitor for Dragon Oath MMORPG
**Date Created:** 2025-11-01
**Game:** Dragon Oath (Thiên Long Bát Bộ)
**Purpose:** Document the complete process of finding and updating memory addresses

---

## Table of Contents

1. [Problem Overview](#problem-overview)
2. [Initial Investigation](#initial-investigation)
3. [Root Cause Analysis](#root-cause-analysis)
4. [Solution: Using Cheat Engine](#solution-using-cheat-engine)
5. [Step-by-Step Process](#step-by-step-process)
6. [Converting Results to Code](#converting-results-to-code)
7. [Understanding the Memory Structure](#understanding-the-memory-structure)
8. [Final Configuration](#final-configuration)
9. [Verification Steps](#verification-steps)
10. [Troubleshooting Reference](#troubleshooting-reference)

---

## Problem Overview

### Initial Symptom

The `MemoryReader.ReadInt32()` method always returned **0** for all memory reads, causing the application to display:
- Character Name: (empty)
- Level: 0
- HP%: 0
- MP%: 0
- Coordinates: 0, 0

### Code That Wasn't Working

```csharp
// GameProcessMonitor.cs - OLD VALUES
private static readonly int[] STATS_BASE_POINTER = { 7319476, 12, 344, 4 };
private const int OFFSET_CURRENT_MP = 2296;
private const int OFFSET_CURRENT_HP = 2292;
// ... all reads returned 0
```

### Why Users Need This Guide

When the game updates or you use a different game version, the hardcoded memory addresses become invalid. This guide teaches you how to find new addresses using Cheat Engine and update your code accordingly.

---

## Initial Investigation

### Step 1: Added Error Checking to MemoryReader

First, we enhanced `MemoryReader.cs` to diagnose the problem:

```csharp
public int ReadInt32(int address)
{
    if (!IsValid || address == 0)
    {
        Debug.WriteLine($"Cannot read - invalid handle or address");
        return 0;
    }

    byte[] buffer = new byte[4];
    bool success = ReadProcessMemory(_processHandle, address, buffer, 4, 0);

    if (!success)
    {
        Debug.WriteLine($"❌ Failed to read Int32 at address 0x{address:X}");
        return 0;
    }

    int result = BitConverter.ToInt32(buffer, 0);
    Debug.WriteLine($"✓ Read Int32 at 0x{address:X} = {result}");
    return result;
}
```

**What we found:**
```
FollowPointerChain: Processing chain [7319476, 12, 344, 4]
❌ Failed to read Int32 at address 0x006F8C24
ReadProcessMemory failed - returns 0
```

### Step 2: Examined Game Source Code

We analyzed the actual game source code at `G:\SourceCodeGameTLBB\Game\Client\` to understand the memory structure:

**Key files examined:**
- `WXClient/Object/ObjectManager.h` - Shows CObjectManager class
- `WXClient/Object/ObjectManager.cpp` - Shows static pointer `s_pMe`
- `WXClient/DataPool/GMDP_Struct_CharacterData.h` - Shows SDATA_PLAYER_MYSELF structure
- `WXClient/Object/Object.h` - Shows CObject base class with position data

**Critical discovery:**

```cpp
// ObjectManager.cpp line 37
CObjectManager *CObjectManager::s_pMe = NULL;  // Static singleton pointer

// This pointer is stored at a fixed address in the .exe
// Old address: 7319476 (0x006F8C24)
// But this changes when game is updated!
```

---

## Root Cause Analysis

### The Memory Architecture

```
Game.exe Base Address (changes each game run)
    ↓
Static Data Section (.data segment - fixed offset from base)
    ↓
CObjectManager::s_pMe (static pointer at "Game.exe"+offset)
    ↓
CObjectManager instance
    ↓ (+12 bytes)
m_pMySelf → CObject_PlayerMySelf* (player object pointer)
    │
    ├─→ CObject base members (Entity data)
    │   └─ m_fvPosition (X, Y, Z coordinates)
    │
    └─→ SDATA_PLAYER_MYSELF* (via +340, +4 navigation)
        └─ Character stats (HP, MP, Level, Name, etc.)
```

### Why Address 7319476 Failed

**Problem:** The address `7319476` was reverse-engineered for a specific game version.

**What changed:**
- Game was updated/patched
- Static variable `CObjectManager::s_pMe` moved to a different address
- The old address `7319476` now points to invalid/random memory
- Reading from invalid address returns 0

**Visualization:**

```
Old Game Version:
[7319476] → CObjectManager::s_pMe ✓ Valid pointer
    ↓
[...] → Player data

New Game Version:
[7319476] → ??? Random memory ❌ Invalid!
    ↓
Returns 0

[2381824] → CObjectManager::s_pMe ✓ NEW valid pointer!
    ↓
[...] → Player data
```

---

## Solution: Using Cheat Engine

### Why Cheat Engine?

Cheat Engine is the industry-standard tool for:
1. Finding memory addresses of game values
2. Discovering pointer chains that survive game restarts
3. Reverse engineering memory structures

### Prerequisites

**Required:**
- ✅ Cheat Engine 7.5+ installed (https://www.cheatengine.org/)
- ✅ Game running with character logged in
- ✅ Character in game world (not at login screen)
- ✅ Basic understanding of hexadecimal numbers

**Recommended:**
- Windows Calculator in Programmer mode (for hex/decimal conversion)
- Notepad for recording values
- 30-60 minutes of time

---

## Step-by-Step Process

### Phase 1: Finding the Current MP Address

#### Step 1.1: Prepare the Game

1. Launch the game
2. Log in with a character
3. Enter the game world
4. Note your **current MP value** (e.g., 1514)
   - Look at the MP bar in-game
   - Write down the exact number

#### Step 1.2: Start Cheat Engine

1. Launch **Cheat Engine**
2. Click the **computer icon** (top-left corner)
3. Process list appears → Select **Game.exe**
4. Click **Open**

**Troubleshooting:**
- If "Game.exe" doesn't appear → Check Task Manager for exact process name
- If "Access Denied" → Run Cheat Engine as Administrator

#### Step 1.3: First Scan for MP Value

In Cheat Engine main window:

```
Value Type: 4 Bytes
Scan Type: Exact Value
Value: [Your current MP, e.g., 1514]
```

Click **First Scan** button.

**Expected result:** Thousands or millions of addresses found.

```
Address         Value
0x12345678      1514
0x23456789      1514
0x3456789A      1514
... (50,000 more)
```

#### Step 1.4: Filter Results with Next Scan

1. **In game:** Use an MP potion OR wait for MP to change naturally
2. **Note new MP value** (e.g., 1600)
3. **In Cheat Engine:**
   ```
   Scan Type: Exact Value
   Value: [New MP value, e.g., 1600]
   ```
4. Click **Next Scan**

**Result:** Address list shrinks dramatically.

```
Found: 50,000 → 5,000 addresses
```

#### Step 1.5: Repeat Until One Address

Repeat Step 1.4 several times:

```
Scan 1: 50,000 addresses
Scan 2: 5,000 addresses
Scan 3: 500 addresses
Scan 4: 50 addresses
Scan 5: 5 addresses
Scan 6: 1 address ✅ Found it!
```

**Tips:**
- Use HP potions to change MP faster
- Get hit by monsters to use MP
- Meditate to regenerate MP

#### Step 1.6: Verify the Address

Double-click the found address to add it to the **bottom panel**.

**Verify it's correct:**
- Watch the value in Cheat Engine
- Use MP in game
- Value should update in real-time
- ✅ If it matches → Correct address!

**Example:**
```
Address: 0x26458C44
Value: 1514 → 1500 → 1485 (matches in-game MP changes)
```

---

### Phase 2: Pointer Scan

#### Step 2.1: Start Pointer Scan

1. **Right-click** the MP address in bottom panel
2. Select **"Pointer scan for this address"**
3. File dialog appears → Save as: `C:\Temp\mp_pointerscan.scandata`

#### Step 2.2: Configure Scan Settings

Dialog box appears with settings:

```
┌─────────────────────────────────────────┐
│ Pointerscan Settings                    │
├─────────────────────────────────────────┤
│ Max level: 5                            │
│ Max offset value (hex): 4096            │
│                                         │
│ ☑ Only find paths with static addresses │
│ ☐ No looping pointers                  │
│ ☐ Only find paths with a negative offset│
│                                         │
│ Heap base address: [auto]               │
│                                         │
│ [Number of threads: 4        ]          │
│                                         │
│            [ OK ] [ Cancel ]            │
└─────────────────────────────────────────┘
```

**Recommended settings:**
- ✅ Max level: **5** (search 5-level deep pointer chains)
- ✅ Max offset: **4096** (0x1000 hex)
- ✅ Check "Only find paths with static addresses"
- ✅ Leave other options default

Click **OK**.

#### Step 2.3: Wait for Initial Scan

**Progress window appears:**

```
Scanning: 45%
Pointers found: 1,245,678
Estimated time: 3 minutes remaining
```

**Time:** 5-15 minutes depending on computer speed.

**Result:** Scan completes with hundreds of thousands of potential pointer chains.

```
Pointer scan completed!
Pointers found: 500,000
```

**Don't close this window!** We'll filter these results next.

---

### Phase 3: Filter Pointer Results (Most Important!)

The key to success is finding pointers that **remain valid** after restarting the game.

#### Step 3.1: Understand Why Filtering is Needed

**Problem:** The scan found 500,000 pointer chains, but most are invalid because:
- They point to temporary heap memory
- They use dynamic addresses that change on restart
- They're false positives

**Solution:** Restart the game multiple times. Only **static pointer chains** will continue pointing to the MP address.

#### Step 3.2: First Restart and Rescan

**Close the game completely** (don't minimize - actually exit).

**Reopen the game:**
1. Launch game
2. Log in with **same character**
3. Enter game world
4. Wait until fully loaded

**Find NEW MP address:**
1. In Cheat Engine main window (NOT pointer scan window)
2. New Scan → Value Type: 4 Bytes → Value: [Current MP]
3. Scan until you find the **new MP address**
4. Example: Was `0x26458C44`, now `0x34567890`

**Why it changed:** Game loaded at different base address in memory (ASLR - Address Space Layout Randomization).

#### Step 3.3: Rescan Pointer Results

**In Pointer Scan window** (the one with 500,000 results):

1. Menu → **Pointer scanner** → **Rescan memory**
2. Dialog appears: "Address to find"
3. Enter the **NEW MP address** (e.g., `0x34567890`)
4. Click **OK**

**Progress:**
```
Rescanning pointers...
Testing: 250,000 / 500,000
```

**Result:**
```
Before rescan: 500,000 pointers
After rescan: 50,000 pointers ✅ Filtered!
```

Cheat Engine removed pointers that no longer point to the MP value.

#### Step 3.4: Repeat Restart Process

**Critical:** Repeat Steps 3.2-3.3 at least **3-4 more times**.

```
┌─────────────────────────────────────────────┐
│ Restart #1: 500,000 → 50,000 pointers       │
│ Restart #2:  50,000 → 5,000 pointers        │
│ Restart #3:   5,000 → 500 pointers          │
│ Restart #4:     500 → 18 pointers ✅ Good!  │
└─────────────────────────────────────────────┘
```

**Goal:** Get down to **<100 results** (ideally <50).

**Tips:**
- Be patient - each restart takes time
- Make sure character is fully loaded before finding new address
- The more restarts, the more accurate the results

---

### Phase 4: Analyze Results

#### Step 4.1: Final Pointer Scan Results

After 4 restarts, you should have a clean list. **Our actual results:**

```csv
Base Address,Offset 0,Offset 1,Offset 2,Offset 3,Offset 4,Offset 5,Points to:
"Game.exe"+00245800,C,160,4,154,4,6DC,26458C44 = 1514
"Game.exe"+00245804,C,160,4,154,4,6DC,26458C44 = 1514
"Game.exe"+0024581C,C,160,4,154,4,6DC,26458C44 = 1514
"Game.exe"+00245834,64,160,4,154,4,6DC,26458C44 = 1514
"Game.exe"+00246564,64,160,4,154,4,6DC,26458C44 = 1514
"UI_CEGUI.dll"+000371D4,64,160,4,154,4,6DC,26458C44 = 1514
"Game.exe"+00245834,58,4,8,10,154,4,6DC,26458C44 = 1514
"Game.exe"+00246564,58,4,8,10,154,4,6DC,26458C44 = 1514
"UI_CEGUI.dll"+000371D4,58,4,8,10,154,4,6DC,26458C44 = 1514
"Game.exe"+00245800,C,154,4,6DC,,,26458C44 = 1514  ← BEST!
"Game.exe"+00245804,C,154,4,6DC,,,26458C44 = 1514
"Game.exe"+00245834,64,154,4,6DC,,,26458C44 = 1514
"Game.exe"+00246564,64,154,4,6DC,,,26458C44 = 1514
"UI_CEGUI.dll"+000371D4,64,154,4,6DC,,,26458C44 = 1514
"Game.exe"+00245804,58,8,10,154,4,6DC,26458C44 = 1514
"Game.exe"+00246564,58,8,10,154,4,6DC,26458C44 = 1514
"UI_CEGUI.dll"+000371D4,58,8,10,154,4,6DC,26458C44 = 1514
```

#### Step 4.2: Understanding the Columns

| Column | Meaning | Example |
|--------|---------|---------|
| **Base Address** | Module + offset (static) | "Game.exe"+00245800 |
| **Offset 0-5** | Pointer chain offsets (hex) | C, 154, 4, 6DC |
| **Points to** | Final address and current value | 26458C44 = 1514 |

**Read as:**
```
"Game.exe"+00245800 → C → 154 → 4 → 6DC

Means:
1. Start at Game.exe base address
2. Add 0x245800 → Read pointer
3. Add 0xC (12) → Read pointer
4. Add 0x154 (340) → Read pointer
5. Add 0x4 → Read pointer
6. Add 0x6DC (1756) → Final MP value!
```

#### Step 4.3: Selecting the Best Pointer Chain

**Criteria for selection:**

1. ✅ **Shortest chain** (fewer offsets = more stable across updates)
2. ✅ **Base starts with "Game.exe"+** (not other DLLs)
3. ✅ **Consistent pattern** (appears in multiple rows)
4. ✅ **Matches expected structure** (from source code analysis)

**Our selection:**

```
"Game.exe"+00245800 → C → 154 → 4 → 6DC
```

**Why this is best:**
- ✅ Starts with "Game.exe"+
- ✅ Only 4 offsets (shortest in the list)
- ✅ Appears in multiple rows with same base
- ✅ First offset is `C` (12 decimal) - matches old pattern
- ✅ No looping or circular references

**Comparison with old pattern:**

```
Old:  [7319476] → [12] → [344] → [4] → [+2296 for MP]
New:  [2381824] → [12] → [340] → [4] → [+1756 for MP]

Changes:
- Base address: 7319476 → 2381824 (different location in .exe)
- Offset 2: 344 → 340 (slightly changed)
- MP offset: 2296 → 1756 (structure layout changed by -540 bytes)
```

---

## Converting Results to Code

### Step 5.1: Hex to Decimal Conversion

Cheat Engine shows all values in **hexadecimal**, but C# code uses **decimal**.

**Conversion table for our results:**

| Hex | Decimal | What it represents |
|-----|---------|-------------------|
| 00245800 | 2,381,824 | Base offset in Game.exe |
| C | 12 | Offset to m_pMySelf in CObjectManager |
| 154 | 340 | Navigation offset in object hierarchy |
| 4 | 4 | Final pointer offset |
| 6DC | 1756 | Current MP offset in SDATA_PLAYER_MYSELF |

**How to convert:**

**Method 1: Windows Calculator**
1. Open Calculator
2. View → Programmer
3. Select **HEX**
4. Type: `6DC`
5. Select **DEC**
6. Result: `1756` ✅

**Method 2: Online converter**
- https://www.rapidtables.com/convert/number/hex-to-decimal.html

**Method 3: In C# code**
```csharp
int decimalValue = Convert.ToInt32("6DC", 16);  // Returns 1756
```

### Step 5.2: Understanding Relative vs Absolute Addresses

**Critical concept:**

The base address `"Game.exe"+00245800` is **NOT** an absolute memory address!

**What it means:**

```
"Game.exe"      = Base address where Game.exe is loaded (changes each run)
+00245800       = Offset from that base (constant)

Absolute Address = Game.exe Base + 0x245800
```

**Example:**

```
Run 1: Game.exe loaded at 0x00400000
       Absolute = 0x00400000 + 0x00245800 = 0x00645800

Run 2: Game.exe loaded at 0x01000000
       Absolute = 0x01000000 + 0x00245800 = 0x01245800
```

**In code, we use the OFFSET (0x245800 = 2381824), not absolute address!**

### Step 5.3: Breaking Down the Pointer Chain

From our Cheat Engine result:
```
"Game.exe"+00245800 → C → 154 → 4 → 6DC
```

**This needs to be split into TWO pointer chains:**

#### Chain 1: STATS_BASE_POINTER (for character stats)

```csharp
// [Game.exe+0x245800] → [+C] → [+154] → [+4] = Base of SDATA_PLAYER_MYSELF
private static readonly int[] STATS_BASE_POINTER = { 2381824, 12, 340, 4 };

// Then we add offsets to read specific stats:
private const int OFFSET_CURRENT_MP = 1756;  // The 0x6DC from scan
```

**What this reads:**
```
statsBase = FollowPointerChain([2381824, 12, 340, 4])
currentMP = ReadInt32(statsBase + 1756)
```

#### Chain 2: ENTITY_BASE_POINTER (for coordinates)

```csharp
// [Game.exe+0x245800] → [+C] = Base of CObject_PlayerMySelf
private static readonly int[] ENTITY_BASE_POINTER = { 2381824, 12 };

// Then we add offsets for position:
private const int OFFSET_X_COORDINATE = 92;   // CObject::m_fvPosition.x
private const int OFFSET_Y_COORDINATE = 100;  // CObject::m_fvPosition.z
```

**What this reads:**
```
entityBase = FollowPointerChain([2381824, 12])
xCoord = ReadFloat(entityBase + 92)
yCoord = ReadFloat(entityBase + 100)
```

**Why two chains?**

From source code analysis, we discovered:
- **CObject** (base class) contains position data
- **SDATA_PLAYER_MYSELF** (separate structure) contains stats
- They're accessed through different levels of pointer indirection

### Step 5.4: Calculate Related Offsets

Once you have the MP offset (1756), you can calculate other stats using the structure layout:

**From source code `GMDP_Struct_CharacterData.h`:**

```cpp
struct SDATA_PLAYER_MYSELF : public SDATA_PLAYER_OTHER
{
    // ... inherited fields ...

    INT m_nHP;        // Current HP
    INT m_nMP;        // Current MP ← We found this at offset 1756
    INT m_nExp;       // Experience
    // ... more fields ...
    INT m_nMaxHP;     // Max HP
    INT m_nMaxMP;     // Max MP
};
```

**C++ INT = 4 bytes**, so fields are laid out sequentially:

```
Offset 1752: m_nHP (4 bytes)     ← CurrentHP = CurrentMP - 4
Offset 1756: m_nMP (4 bytes)     ← CurrentMP (our reference)
Offset 1760: m_nExp (4 bytes)    ← Experience = CurrentMP + 4
...
Offset 1856: m_nMaxHP (4 bytes)  ← MaxHP = CurrentMP + 100
Offset 1860: m_nMaxMP (4 bytes)  ← MaxMP = CurrentMP + 104
```

**In code:**

```csharp
private const int OFFSET_CURRENT_MP = 1756;                   // Found via CE
private const int OFFSET_CURRENT_HP = OFFSET_CURRENT_MP - 4;  // 1752
private const int OFFSET_EXPERIENCE = OFFSET_CURRENT_MP + 4;  // 1760
private const int OFFSET_MAX_HP = OFFSET_CURRENT_MP + 100;    // 1856
private const int OFFSET_MAX_MP = OFFSET_CURRENT_MP + 104;    // 1860
```

---

## Understanding the Memory Structure

### The Complete Memory Map

```
┌─────────────────────────────────────────────────────────────┐
│ Game.exe Process Memory                                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  .text section (code)                                       │
│  .data section (static variables)                           │
│      │                                                       │
│      ├─→ [Game.exe + 0x245800] = 0x00645800 (example)      │
│      │       │                                              │
│      │       └─→ CObjectManager::s_pMe = 0x12345000        │
│      │                                                       │
│  Heap (dynamic memory)                                      │
│      │                                                       │
│      ├─→ [0x12345000] CObjectManager instance              │
│      │       │                                              │
│      │       ├─ vtable pointer                             │
│      │       ├─ ... (12 bytes of other data)               │
│      │       ├─→ [+12] m_pMySelf = 0x23456000              │
│      │       └─ ... (other members)                         │
│      │                                                       │
│      ├─→ [0x23456000] CObject_PlayerMySelf instance        │
│      │       │                                              │
│      │       ├─ CObject base members:                      │
│      │       │   ├─→ [+92] m_fvPosition.x (float)          │
│      │       │   ├─→ [+96] m_fvPosition.y (float)          │
│      │       │   ├─→ [+100] m_fvPosition.z (float)         │
│      │       │   └─ ...                                     │
│      │       │                                              │
│      │       ├─ ... (340 bytes of object hierarchy)        │
│      │       │                                              │
│      │       ├─→ [+340] pointer = 0x34567000               │
│      │                                                       │
│      ├─→ [0x34567000] + 4 = pointer = 0x45678000           │
│      │                                                       │
│      └─→ [0x45678000] SDATA_PLAYER_MYSELF structure        │
│              ├─→ [+48] m_strName = "PlayerName"            │
│              ├─→ [+92] m_nLevel = 50                       │
│              ├─→ [+1752] m_nHP = 5000                      │
│              ├─→ [+1756] m_nMP = 1514 ✅ Found this!       │
│              ├─→ [+1760] m_nExp = 123456                   │
│              ├─→ [+1856] m_nMaxHP = 10000                  │
│              └─→ [+1860] m_nMaxMP = 3000                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Two Different Data Structures

**1. CObject (Entity/Object data):**

```cpp
class CObject {
    tEntityNode* m_pRenderInterface;
    INT m_idServer;
    INT m_ID;
    fVector3 m_fvPosition;    // Offset ~92 from CObject base
        float x;              // +92: X coordinate
        float y;              // +96: Y coordinate (height)
        float z;              // +100: Z coordinate (2D map Y)
    fVector3 m_fvRotation;
    // ...
};
```

**Accessed via:** `ENTITY_BASE_POINTER = [2381824, 12]`

**2. SDATA_PLAYER_MYSELF (Character stats):**

```cpp
struct SDATA_PLAYER_MYSELF {
    STRING m_strName;         // +48: Character name
    INT m_nLevel;             // +92: Level (different 92!)
    INT m_nHP;                // +1752: Current HP
    INT m_nMP;                // +1756: Current MP
    INT m_nExp;               // +1760: Experience
    INT m_nMaxHP;             // +1856: Max HP
    INT m_nMaxMP;             // +1860: Max MP
    // ... many more fields
};
```

**Accessed via:** `STATS_BASE_POINTER = [2381824, 12, 340, 4]`

### Why Offsets Changed

**Old game version:**
```cpp
struct SDATA_PLAYER_MYSELF : public SDATA_PLAYER_OTHER {
    // ... inherited ~2240 bytes of data ...
    INT m_nHP;     // Offset 2292
    INT m_nMP;     // Offset 2296
};
```

**New game version (modified structure):**
```cpp
struct SDATA_PLAYER_MYSELF : public SDATA_PLAYER_OTHER {
    // ... inherited ~1700 bytes of data (540 bytes less!)
    INT m_nHP;     // Offset 1752 (2292 - 540)
    INT m_nMP;     // Offset 1756 (2296 - 540)
};
```

**Reason for change:**
- Developer removed/changed inherited fields
- Compiler optimizations changed
- Structure packing/alignment modified
- **Result:** All offsets shifted by -540 bytes

---

## Final Configuration

### Complete Code Update

#### File: `GameProcessMonitor.cs`

```csharp
using AutoDragonOath.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AutoDragonOath.Services
{
    public class GameProcessMonitor
    {
        private const string GAME_PROCESS_NAME = "Game";

        // ================================================================
        // MEMORY ADDRESSES - Updated 2025-11-01
        // ================================================================
        // Found using Cheat Engine pointer scan method
        // Game version: [Your version here]
        // ================================================================

        // Base pointers (offset from Game.exe base address)
        private static readonly int[] ENTITY_BASE_POINTER = { 2381824, 12 };
        private static readonly int[] STATS_BASE_POINTER = { 2381824, 12, 340, 4 };
        private static readonly int[] MAP_BASE_POINTER = { 6870940, 14232 };  // TODO: Verify

        // Character data offsets (from STATS_BASE)
        private const int OFFSET_CHARACTER_NAME = 48;        // STRING m_strName
        private const int OFFSET_CHARACTER_TITLE = 160;      // STRING m_strTitle
        private const int OFFSET_LEVEL = 92;                 // INT m_nLevel

        // HP/MP offsets (calculated from OFFSET_CURRENT_MP)
        private const int OFFSET_CURRENT_MP = 1756;          // INT m_nMP (0x6DC hex)
        private const int OFFSET_CURRENT_HP = 1752;          // OFFSET_CURRENT_MP - 4
        private const int OFFSET_MAX_HP = 1856;              // OFFSET_CURRENT_MP + 100
        private const int OFFSET_MAX_MP = 1860;              // OFFSET_CURRENT_MP + 104

        // Other character data
        private const int OFFSET_EXPERIENCE = 1760;          // INT m_nExp (estimated)
        private const int OFFSET_PET_ID = 1816;              // PET_GUID_t (estimated)

        // Position offsets (from ENTITY_BASE, in CObject)
        private const int OFFSET_X_COORDINATE = 92;          // fVector3.x
        private const int OFFSET_Y_COORDINATE = 100;         // fVector3.z (2D map Y)

        // Map offset (from MAP_BASE)
        private const int OFFSET_MAP_ID = 96;                // INT map ID

        // ... rest of your methods ...
    }
}
```

#### File: `MemoryReader.cs`

**Critical addition - Handle Game.exe base address:**

```csharp
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AutoDragonOath.Services
{
    public class MemoryReader : IDisposable
    {
        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        private IntPtr _processHandle;
        private int _processId;
        private IntPtr _gameBaseAddress;  // ✅ CRITICAL: Store Game.exe base

        public MemoryReader(int processId)
        {
            _processId = processId;
            _processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, processId);

            // ✅ Get the base address of Game.exe module
            _gameBaseAddress = GetModuleBaseAddress(processId, "Game.exe");

            Debug.WriteLine($"Process {processId} opened");
            Debug.WriteLine($"  Handle: 0x{_processHandle:X}");
            Debug.WriteLine($"  Game.exe base: 0x{_gameBaseAddress:X}");
        }

        // ✅ Check if both handle and base address are valid
        public bool IsValid => _processHandle != IntPtr.Zero &&
                               _gameBaseAddress != IntPtr.Zero;

        // ✅ Get the base address of a module (DLL/EXE)
        private IntPtr GetModuleBaseAddress(int processId, string moduleName)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                ProcessModule module = process.Modules
                    .Cast<ProcessModule>()
                    .FirstOrDefault(m => m.ModuleName.Equals(moduleName,
                        StringComparison.OrdinalIgnoreCase));

                if (module != null)
                {
                    Debug.WriteLine($"✓ Found {moduleName} at 0x{module.BaseAddress:X}");
                    return module.BaseAddress;
                }
                else
                {
                    Debug.WriteLine($"❌ Module {moduleName} not found!");
                    return IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting module base: {ex.Message}");
                return IntPtr.Zero;
            }
        }

        // ✅ Follow pointer chain with proper base address handling
        public int FollowPointerChain(int[] pointerChain)
        {
            if (!IsValid)
            {
                Debug.WriteLine("❌ FollowPointerChain: Invalid handle or base address");
                return 0;
            }

            Debug.WriteLine($"FollowPointerChain: [{string.Join(", ",
                pointerChain.Select(x => $"0x{x:X}"))}]");

            // ✅ CRITICAL: First value is OFFSET from Game.exe base!
            int currentAddress = _gameBaseAddress.ToInt32() + pointerChain[0];
            Debug.WriteLine($"  Step 0: Game.exe(0x{_gameBaseAddress:X}) + " +
                          $"0x{pointerChain[0]:X} = 0x{currentAddress:X}");

            currentAddress = ReadInt32(currentAddress);
            if (currentAddress == 0)
            {
                Debug.WriteLine($"  ❌ Failed at base read");
                return 0;
            }

            // Follow the rest of the pointer chain
            for (int i = 1; i < pointerChain.Length; i++)
            {
                currentAddress = currentAddress + pointerChain[i];
                Debug.WriteLine($"  Step {i}: 0x{currentAddress:X} " +
                              $"(+0x{pointerChain[i]:X})");

                int nextAddress = ReadInt32(currentAddress);
                if (nextAddress == 0)
                {
                    Debug.WriteLine($"  ❌ Failed at step {i}");
                    return 0;
                }

                currentAddress = nextAddress;
            }

            Debug.WriteLine($"  ✓ Final address: 0x{currentAddress:X}");
            return currentAddress;
        }

        public int ReadInt32(int address)
        {
            if (!IsValid || address == 0)
                return 0;

            byte[] buffer = new byte[4];
            bool success = ReadProcessMemory(_processHandle, address, buffer, 4, 0);

            if (!success)
            {
                Debug.WriteLine($"  ❌ ReadInt32(0x{address:X}) failed");
                return 0;
            }

            return BitConverter.ToInt32(buffer, 0);
        }

        public float ReadFloat(int address)
        {
            if (!IsValid || address == 0)
                return 0f;

            byte[] buffer = new byte[4];
            bool success = ReadProcessMemory(_processHandle, address, buffer, 4, 0);

            if (!success)
            {
                Debug.WriteLine($"  ❌ ReadFloat(0x{address:X}) failed");
                return 0f;
            }

            return BitConverter.ToSingle(buffer, 0);
        }

        public string ReadString(int address, int maxLength = 30)
        {
            if (!IsValid || address == 0)
                return string.Empty;

            byte[] buffer = new byte[maxLength];
            bool success = ReadProcessMemory(_processHandle, address, buffer, maxLength, 0);

            if (!success)
                return string.Empty;

            // Convert from game encoding (likely GB2312 for Chinese)
            string result = Encoding.Default.GetString(buffer);

            // Trim at null terminator
            int nullIndex = result.IndexOf('\0');
            if (nullIndex >= 0)
                result = result.Substring(0, nullIndex);

            return result.Trim();
        }

        public void Dispose()
        {
            if (_processHandle != IntPtr.Zero)
            {
                CloseHandle(_processHandle);
                _processHandle = IntPtr.Zero;
            }
        }

        // Win32 API imports
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess,
            bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess,
            int lpBaseAddress, byte[] lpBuffer, int dwSize, int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
```

---

## Verification Steps

### Step 6.1: Build and Run

1. **Build the project:**
   ```bash
   cd G:\microauto-6.9\AutoDragonOath
   dotnet build
   ```

2. **Run as Administrator:**
   - Right-click `AutoDragonOath.exe`
   - Select "Run as Administrator"
   - (Required for reading other process memory)

### Step 6.2: Check Debug Output

In Visual Studio:
1. View → Output (or Ctrl+Alt+O)
2. Show output from: **Debug**

**Expected output:**

```
Process 12345 opened
  Handle: 0x1234
  Game.exe base: 0x00400000
FollowPointerChain: [0x245800, 0xC, 0x154, 0x4]
  Step 0: Game.exe(0x400000) + 0x245800 = 0x645800
  Step 1: 0x12345678 (+0xC)
  Step 2: 0x23456789 (+0x154)
  Step 3: 0x34567890 (+0x4)
  ✓ Final address: 0x45678ABC

Character Data:
  Name: TestCharacter
  Level: 50
  HP: 5000/10000 (50%)
  MP: 1514/3000 (50%)
  Position: (1234, 5678)
  Map: Dai Li
```

**If you see errors:**

```
❌ Failed to open process - may need Administrator rights
❌ Module Game.exe not found!
❌ ReadInt32(0x00000000) failed
```

→ See troubleshooting section below.

### Step 6.3: Verify All Values

Create a checklist comparing in-game vs application:

| Field | In-Game | Application | Status |
|-------|---------|-------------|--------|
| Character Name | "MyChar" | "MyChar" | ✅ |
| Level | 50 | 50 | ✅ |
| Current HP | 5000 | 5000 | ✅ |
| Max HP | 10000 | 10000 | ✅ |
| Current MP | 1514 | 1514 | ✅ |
| Max MP | 3000 | 3000 | ✅ |
| X Coordinate | 1234 | 1234 | ✅ |
| Y Coordinate | 5678 | 5678 | ✅ |
| Map Name | "Dai Li" | "Dai Li" | ✅ |

**Test dynamic changes:**
1. Use HP/MP potions → Values update?
2. Move character → Coordinates update?
3. Teleport to different map → Map name updates?

### Step 6.4: Test After Game Restart

**Critical test:**
1. Close AutoDragonOath
2. Close and restart the game
3. Log in with same character
4. Run AutoDragonOath again

**Expected:** All values still read correctly ✅

**If they fail:** Your pointer chain isn't stable → Redo Cheat Engine scan with more restarts.

---

## Troubleshooting Reference

### Issue 1: All Values Return 0

**Symptoms:**
```
Character Name: (empty)
Level: 0
HP: 0/0
```

**Possible causes:**

**A. Permission issue**
- ❌ Not running as Administrator
- ✅ Solution: Right-click → Run as Administrator

**B. Process name mismatch**
- ❌ Code says "Game" but actual process is "game.exe" or "GameClient"
- ✅ Solution: Check Task Manager → Details tab
  ```csharp
  private const string GAME_PROCESS_NAME = "GameClient";  // Update
  ```

**C. Character not loaded**
- ❌ Reading too soon after game start
- ✅ Solution: Wait until character is fully in game world

**D. Wrong addresses**
- ❌ Game updated, addresses changed
- ✅ Solution: Redo Cheat Engine scan

### Issue 2: Base Address Returns 0

**Debug output shows:**
```
Game.exe base: 0x00000000
❌ Module Game.exe not found!
```

**Solutions:**

**A. Check process name:**
```csharp
// Try variations:
GetModuleBaseAddress(processId, "Game.exe");
GetModuleBaseAddress(processId, "game.exe");
GetModuleBaseAddress(processId, "GameClient.exe");
```

**B. List all modules:**
```csharp
Process process = Process.GetProcessById(processId);
foreach (ProcessModule module in process.Modules)
{
    Debug.WriteLine($"Module: {module.ModuleName} at 0x{module.BaseAddress:X}");
}
```

### Issue 3: Pointer Chain Returns 0

**Debug output:**
```
Step 0: Game.exe(0x400000) + 0x245800 = 0x645800
❌ Failed at base read
```

**Cause:** Address `0x645800` doesn't contain a valid pointer.

**Solutions:**

**A. Verify in Cheat Engine:**
1. Open Cheat Engine
2. Attach to game
3. Go to: Address → Go to address → `Game.exe+245800`
4. Check if value looks like a pointer (e.g., `0x12345678`)
5. If not → Wrong base address, redo scan

**B. Check if address changed:**
- Game may have updated
- Redo pointer scan process

### Issue 4: Some Values Correct, Others Wrong

**Example:**
```
Name: MyChar ✅
Level: 50 ✅
HP: -123456 ❌
MP: 1514 ✅
```

**Cause:** Some offsets are correct, others are wrong.

**Solutions:**

**A. Use Cheat Engine to find HP:**
1. Scan for current HP value
2. Pointer scan for HP
3. Compare with MP pointer chain
4. Calculate new HP offset

**B. Verify structure alignment:**
```csharp
// If MP is correct but HP is wrong:
// Try different offsets:
private const int OFFSET_CURRENT_HP = OFFSET_CURRENT_MP - 4;   // Try this
private const int OFFSET_CURRENT_HP = OFFSET_CURRENT_MP - 8;   // Or this
private const int OFFSET_CURRENT_HP = OFFSET_CURRENT_MP + 4;   // Or this
```

### Issue 5: Values Lag Behind Game

**Symptom:** Application shows old values that update slowly.

**Cause:** Not a memory reading issue - it's the refresh timer.

**Solution:**

In `MainViewModel.cs`:
```csharp
// Increase refresh rate
_refreshTimer.Interval = TimeSpan.FromSeconds(1);  // Was 2, now 1
```

### Issue 6: Application Crashes

**Error:** `AccessViolationException` or `SEHException`

**Cause:** Reading from invalid memory address.

**Solution:**

Add try-catch in `ReadCharacterInfo`:
```csharp
public CharacterInfo? ReadCharacterInfo(int processId)
{
    try
    {
        using var memoryReader = new MemoryReader(processId);

        if (!memoryReader.IsValid)
            return null;

        // ... rest of reading logic ...
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"❌ Exception reading character: {ex.Message}");
        return null;
    }
}
```

---

## Summary

### What We Learned

1. **Memory addresses are version-specific** and change when games update
2. **Cheat Engine pointer scanning** is the standard method to find stable addresses
3. **Pointer chains** must survive game restarts to be valid
4. **Hexadecimal vs decimal** - Cheat Engine uses hex, C# uses decimal
5. **Relative offsets** - "Game.exe"+offset is not an absolute address
6. **Two data structures** - CObject (position) and SDATA_PLAYER_MYSELF (stats)

### Key Addresses Found (2025-11-01)

```csharp
// Base pointers
ENTITY_BASE_POINTER = { 2381824, 12 };          // For coordinates
STATS_BASE_POINTER = { 2381824, 12, 340, 4 };   // For character stats

// Character stats (from STATS_BASE)
OFFSET_CHARACTER_NAME = 48;
OFFSET_LEVEL = 92;
OFFSET_CURRENT_HP = 1752;
OFFSET_CURRENT_MP = 1756;
OFFSET_MAX_HP = 1856;
OFFSET_MAX_MP = 1860;

// Position (from ENTITY_BASE)
OFFSET_X_COORDINATE = 92;
OFFSET_Y_COORDINATE = 100;
```

### Process Summary

```
1. Find value in Cheat Engine (MP = 1514)
   ↓
2. Pointer scan for that value
   ↓
3. Restart game 3-4 times, rescan each time
   ↓
4. Filter down to <50 stable pointer chains
   ↓
5. Pick shortest chain starting with "Game.exe"+
   ↓
6. Convert hex offsets to decimal
   ↓
7. Update code with new addresses
   ↓
8. Test and verify all values match in-game
   ✅ Success!
```

### Time Investment

- **First time:** 30-60 minutes
- **After learning:** 10-15 minutes
- **Worth it:** Your app works even after game updates! 🎉

---

## Appendix

### A. Hex/Decimal Quick Reference

| Hex | Decimal | Hex | Decimal | Hex | Decimal |
|-----|---------|-----|---------|-----|---------|
| 4 | 4 | 10 | 16 | 64 | 100 |
| 8 | 8 | 20 | 32 | C8 | 200 |
| C | 12 | 40 | 64 | 154 | 340 |
| 10 | 16 | 80 | 128 | 6DC | 1756 |

### B. Common Offset Patterns

| Data Type | Size | Alignment |
|-----------|------|-----------|
| INT | 4 bytes | 4-byte boundary |
| FLOAT | 4 bytes | 4-byte boundary |
| STRING | Variable | Usually starts at 4-byte boundary |
| Pointer | 4 bytes (x86) | 4-byte boundary |

### C. Useful Cheat Engine Shortcuts

| Action | Shortcut |
|--------|----------|
| Attach to process | Ctrl+Alt+O |
| New scan | Ctrl+N |
| Next scan | Enter |
| Add address to list | Double-click |
| Go to address | Ctrl+G |
| Memory view | Ctrl+M |

### D. Related Files

- `TROUBLESHOOTING.md` - Detailed error solutions
- `FINDINGS.md` - Technical analysis of memory structure
- `Services/AddressFinder.cs` - Diagnostic tools
- `Services/MemoryReader.cs` - Core memory reading
- `Services/GameProcessMonitor.cs` - Game process scanning

---

## Document Version History

| Date | Version | Changes |
|------|---------|---------|
| 2025-11-01 | 1.0 | Initial documentation of address finding process |

---

**End of Guide**

For questions or issues, refer to `TROUBLESHOOTING.md` or review the diagnostic output from `AddressFinder.cs`.
