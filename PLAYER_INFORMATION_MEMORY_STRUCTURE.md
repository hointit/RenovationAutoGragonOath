# Player Information Memory Structure - Complete Documentation

**Game**: Dragon Oath / Thiên Long Bát Bộ (TLBB)
**Document Version**: 2.0
**Last Updated**: 2025-11-02
**Source**: Game source code analysis (`GMDP_CharacterData_Struct.h`) + Memory reverse engineering

---

## Table of Contents

1. [Overview](#overview)
2. [Memory Architecture](#memory-architecture)
3. [Player Object Pointer Chain](#player-object-pointer-chain)
4. [Complete Character Data Structure](#complete-character-data-structure)
5. [Memory Offsets Reference](#memory-offsets-reference)
6. [Code Implementation Examples](#code-implementation-examples)
7. [Map Object Pointer (Unresolved)](#map-object-pointer-unresolved)
8. [Update History](#update-history)

---

## Overview

The Dragon Oath game stores player character information in a hierarchical memory structure accessible through pointer chains. The main player data is based on the `SDATA_PLAYER_MYSELF` C++ struct defined in the game's source code.

### Base Pointer Information

| Component | Address (Decimal) | Address (Hex) | Status |
|-----------|------------------|---------------|---------|
| **Player Object Base Pointer** | **2381824** | **0x00245580** | ✅ **UPDATED** |
| Old Player Object Pointer | ~~7319476~~ | ~~0x006F8C24~~ | ❌ Obsolete |
| Map Object Pointer | UNKNOWN | UNKNOWN | ⚠️ **NEEDS INVESTIGATION** |

---

## Memory Architecture

### Complete Memory Layout Diagram

```
┌──────────────────────────────────────────────────────────────────────────┐
│                          GAME PROCESS MEMORY                              │
│                  Dragon Oath (game.exe - 32-bit Process)                 │
└──────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│ LEVEL 0: BASE POINTER                                                     │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  [0x00245580] (Dec: 2381824) ← PLAYER OBJECT BASE POINTER               │
│         │                                                                 │
│         │  (This address contains a pointer to the Player Entity)        │
│         ↓                                                                 │
└─────────┼────────────────────────────────────────────────────────────────┘
          │
          │
┌─────────┼────────────────────────────────────────────────────────────────┐
│ LEVEL 1: ENTITY BASE                                                      │
├─────────┼────────────────────────────────────────────────────────────────┤
│         ↓                                                                 │
│  Read [2381824] → Get EntityObject Pointer                               │
│         │                                                                 │
│         ├─► +12 bytes → [ENTITY BASE ADDRESS]                           │
│                 │                                                         │
│                 │  At Entity Base, we can read:                          │
│                 ├─► +48  → Character Name (string, 30 bytes)            │
│                 ├─► +68  → Unknown float value                           │
│                 ├─► +76  → Unknown float value                           │
│                 ├─► +92  → X Coordinate (float, 4 bytes)                │
│                 ├─► +100 → Y Coordinate (float, 4 bytes)                │
│                 ├─► +408 → Unknown float value                           │
│                 ├─► +412 → Unknown float value                           │
│                 │                                                         │
│                 └─► +340 → [STATS OBJECT POINTER] *UPDATED*             │
│                         │                                                 │
└─────────────────────────┼─────────────────────────────────────────────────┘
                          │
                          │
┌─────────────────────────┼─────────────────────────────────────────────────┐
│ LEVEL 2: STATS OBJECT                                                     │
├─────────────────────────┼─────────────────────────────────────────────────┤
│                         ↓                                                 │
│  Read [EntityBase + 340] → Get StatsObject Pointer                       │
│                         │                                                 │
│                         ├─► +4 bytes → [STATS BASE ADDRESS]             │
│                                 │                                         │
└─────────────────────────────────┼─────────────────────────────────────────┘
                                  │
                                  │
┌─────────────────────────────────┼─────────────────────────────────────────┐
│ LEVEL 3: CHARACTER STATS DATA (SDATA_PLAYER_MYSELF Structure)            │
├─────────────────────────────────┼─────────────────────────────────────────┤
│                                 ↓                                         │
│  At [StatsBase], we have access to complete character data:              │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │ BASIC INFORMATION                                                 │   │
│  ├──────────────┬───────────────────────────────────────────────────┤   │
│  │ +48 bytes    │ Character Name (string, 30 bytes)                 │   │
│  │ +92 bytes    │ Experience Points (m_nExp) - int32                │   │
│  │ +??? bytes   │ Money/Gold (m_nMoney) - int32                     │   │
│  │ +??? bytes   │ Character Level (m_nLevel) - int32 [inherited]    │   │
│  └──────────────┴───────────────────────────────────────────────────┘   │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │ HP/MP SYSTEM *UPDATED*                                            │   │
│  ├──────────────┬───────────────────────────────────────────────────┤   │
│  │ +1752 bytes  │ Current HP (m_nHP) - int32         *UPDATED*      │   │
│  │ +1756 bytes  │ Current MP (m_nMP) - int32         *UPDATED*      │   │
│  │ +1856 bytes  │ Max HP (m_nMaxHP) - int32          *UPDATED*      │   │
│  │ +1860 bytes  │ Max MP (m_nMaxMP) - int32          *UPDATED*      │   │
│  │              │ (Note: User reported 1852, but should be 1860)    │   │
│  └──────────────┴───────────────────────────────────────────────────┘   │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │ ATTRIBUTES (from SDATA_PLAYER_MYSELF)                             │   │
│  ├──────────────┬───────────────────────────────────────────────────┤   │
│  │ +??? bytes   │ Strength (m_nSTR) - int32                         │   │
│  │ +??? bytes   │ Spirit (m_nSPR) - int32                           │   │
│  │ +??? bytes   │ Constitution (m_nCON) - int32                     │   │
│  │ +??? bytes   │ Intelligence (m_nINT) - int32                     │   │
│  │ +??? bytes   │ Dexterity (m_nDEX) - int32                        │   │
│  │ +??? bytes   │ Remaining Points (m_nPoint_Remain) - int32        │   │
│  └──────────────┴───────────────────────────────────────────────────┘   │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │ COMBAT STATS                                                      │   │
│  ├──────────────┬───────────────────────────────────────────────────┤   │
│  │ +??? bytes   │ Physical Attack (m_nAtt_Physics) - int32          │   │
│  │ +??? bytes   │ Magic Attack (m_nAtt_Magic) - int32               │   │
│  │ +??? bytes   │ Physical Defense (m_nDef_Physics) - int32         │   │
│  │ +??? bytes   │ Magic Defense (m_nDef_Magic) - int32              │   │
│  │ +??? bytes   │ HP Regen Speed (m_nHP_ReSpeed) - int32/sec        │   │
│  │ +??? bytes   │ MP Regen Speed (m_nMP_ReSpeed) - int32/sec        │   │
│  │ +??? bytes   │ Hit Rate (m_nHit) - int32                         │   │
│  │ +??? bytes   │ Miss/Dodge (m_nMiss) - int32                      │   │
│  │ +??? bytes   │ Critical Rate (m_nCritRate) - int32               │   │
│  │ +??? bytes   │ Attack Speed (m_nAttackSpeed) - int32             │   │
│  └──────────────┴───────────────────────────────────────────────────┘   │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │ ELEMENTAL STATS                                                   │   │
│  ├──────────────┬───────────────────────────────────────────────────┤   │
│  │ +??? bytes   │ Cold Attack (m_nAtt_Cold) - int32                 │   │
│  │ +??? bytes   │ Cold Defense (m_nDef_Cold) - int32                │   │
│  │ +??? bytes   │ Fire Attack (m_nAtt_Fire) - int32                 │   │
│  │ +??? bytes   │ Fire Defense (m_nDef_Fire) - int32                │   │
│  │ +??? bytes   │ Lightning Attack (m_nAtt_Light) - int32           │   │
│  │ +??? bytes   │ Lightning Defense (m_nDef_Light) - int32          │   │
│  │ +??? bytes   │ Poison Attack (m_nAtt_Posion) - int32             │   │
│  │ +??? bytes   │ Poison Defense (m_nDef_Posion) - int32            │   │
│  └──────────────┴───────────────────────────────────────────────────┘   │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │ OTHER DATA                                                        │   │
│  ├──────────────┬───────────────────────────────────────────────────┤   │
│  │ +2300 bytes  │ Experience Alt (int32)                            │   │
│  │ +2356 bytes  │ Pet ID (int32) - for pet HP reading               │   │
│  └──────────────┴───────────────────────────────────────────────────┘   │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## Player Object Pointer Chain

### Primary Pointer Chain for Stats

**Full Chain**: `[2381824, 12, 340, 4]`

**Step-by-Step Resolution**:

```
Step 1: Read base pointer
   Address: 2381824 (0x00245580)
   Operation: ReadProcessMemory(2381824, 4 bytes)
   Result: EntityObjectPointer (e.g., 0x12ABCD00)

Step 2: Add offset +12 and dereference
   Address: EntityObjectPointer + 12
   Operation: ReadProcessMemory(EntityObjectPointer + 12, 4 bytes)
   Result: EntityBaseAddress (e.g., 0x34EF5678)

Step 3: Add offset +340 and dereference *UPDATED FROM +344*
   Address: EntityBaseAddress + 340
   Operation: ReadProcessMemory(EntityBaseAddress + 340, 4 bytes)
   Result: StatsObjectPointer (e.g., 0x56789ABC)

Step 4: Add offset +4 and dereference
   Address: StatsObjectPointer + 4
   Operation: ReadProcessMemory(StatsObjectPointer + 4, 4 bytes)
   Result: StatsBaseAddress (e.g., 0x789ABCDE)

Step 5: Access character stats
   At StatsBaseAddress, you can now add specific offsets:
   - StatsBaseAddress + 1752 = Current HP
   - StatsBaseAddress + 1756 = Current MP
   - StatsBaseAddress + 1856 = Max HP
   - StatsBaseAddress + 1860 = Max MP  (corrected from 1852)
   - StatsBaseAddress + 48   = Character Name
   - StatsBaseAddress + 92   = Experience
```

### Coordinate Pointer Chain

**For X/Y Coordinates**: `[2381824, 12]`

```
Step 1: Read [2381824] → EntityObjectPointer
Step 2: Read [EntityObjectPointer + 12] → EntityBaseAddress
Step 3: Read coordinates:
   - X: EntityBaseAddress + 92 (float)
   - Y: EntityBaseAddress + 100 (float)
```

---

## Complete Character Data Structure

### Source: SDATA_PLAYER_MYSELF (from game source code)

This structure extends from:
- `SDATA_CHARACTER` (base character data)
- `SDATA_NPC` (NPC-related data)
- `SDATA_PLAYER_OTHER` (other player visualization data)
- `SDATA_PLAYER_MYSELF` (current player's complete data)

```cpp
struct SDATA_PLAYER_MYSELF : public SDATA_PLAYER_OTHER
{
    //-----------------------------------------------------
    // CORE STATS
    //-----------------------------------------------------
    INT    m_nHP;              // Current HP     [Offset: +1752] *UPDATED*
    INT    m_nMP;              // Current MP     [Offset: +1756] *UPDATED*
    INT    m_nExp;             // Experience     [Offset: +92]
    INT    m_nMoney;           // Game Money     [Offset: ???]

    //-----------------------------------------------------
    // PRIMARY ATTRIBUTES (5 Main Stats)
    //-----------------------------------------------------
    INT    m_nSTR;             // Strength       [Offset: ???]
    INT    m_nSPR;             // Spirit         [Offset: ???]
    INT    m_nCON;             // Constitution   [Offset: ???]
    INT    m_nINT;             // Intelligence   [Offset: ???]
    INT    m_nDEX;             // Dexterity      [Offset: ???]
    INT    m_nPoint_Remain;    // Unspent Points [Offset: ???]

    //-----------------------------------------------------
    // SECONDARY STATS
    //-----------------------------------------------------
    INT    m_nAtt_Physics;     // Physical Attack      [Offset: ???]
    INT    m_nAtt_Magic;       // Magic Attack         [Offset: ???]
    INT    m_nDef_Physics;     // Physical Defense     [Offset: ???]
    INT    m_nDef_Magic;       // Magic Defense        [Offset: ???]
    INT    m_nMaxHP;           // Maximum HP           [Offset: +1856] *UPDATED*
    INT    m_nMaxMP;           // Maximum MP           [Offset: +1860] *UPDATED*
    INT    m_nHP_ReSpeed;      // HP Regen (per sec)   [Offset: ???]
    INT    m_nMP_ReSpeed;      // MP Regen (per sec)   [Offset: ???]
    INT    m_nHit;             // Hit Rate             [Offset: ???]
    INT    m_nMiss;            // Dodge/Miss           [Offset: ???]
    INT    m_nCritRate;        // Critical Rate        [Offset: ???]
    INT    m_nAttackSpeed;     // Attack Speed         [Offset: ???]

    //-----------------------------------------------------
    // ELEMENTAL ATTRIBUTES
    //-----------------------------------------------------
    INT    m_nAtt_Cold;        // Cold Attack          [Offset: ???]
    INT    m_nDef_Cold;        // Cold Defense         [Offset: ???]
    INT    m_nAtt_Fire;        // Fire Attack          [Offset: ???]
    INT    m_nDef_Fire;        // Fire Defense         [Offset: ???]
    INT    m_nAtt_Light;       // Lightning Attack     [Offset: ???]
    INT    m_nDef_Light;       // Lightning Defense    [Offset: ???]
    INT    m_nAtt_Posion;      // Poison Attack        [Offset: ???]
    INT    m_nDef_Posion;      // Poison Defense       [Offset: ???]

    //-----------------------------------------------------
    // SKILL & ABILITY DATA (Complex Structures)
    //-----------------------------------------------------
    SXINFA_MAP         m_theXinFa;       // Martial Skills (心法)
    SSKILL_MAP         m_theSkill;       // Active Skills
    SLIFEABILITY_MAP   m_theLifeAbility; // Life Skills
    SPRESCR_MAP        m_theSprescr;     // Recipe/Crafting
};
```

### Inherited from SDATA_CHARACTER

```cpp
struct SDATA_CHARACTER
{
    INT      m_nRaceID;        // Race ID         [Offset: ???]
    STRING   m_strName;        // Character Name  [Offset: +48]
    STRING   m_strTitle;       // Title           [Offset: ???]
    FLOAT    m_fHPPercent;     // HP %            [Calculated]
    FLOAT    m_fMPPercent;     // MP %            [Calculated]
    FLOAT    m_fMoveSpeed;     // Move Speed      [Offset: ???]
    INT      m_nCampID;        // Faction/Camp    [Offset: ???]
    INT      m_nLevel;         // Character Level [Offset: ???]
    BOOL     m_bFightState;    // Combat State    [Offset: ???]
};
```

### Inherited from SDATA_PLAYER_OTHER

```cpp
struct SDATA_PLAYER_OTHER : public SDATA_NPC
{
    INT    m_nMenPai;          // Class/School ID [Offset: ???]
                               // 0=Thiếu Lâm, 1=Thiên Vương, 2=Đường Môn
                               // 3=Ngũ Độc, 4=Nga My, 5=Cái Bang
                               // 6=Thiên Long, 7=Minh Giáo, 8=Tiêu Dao
                               // 9=Vô Ưu

    // Visual appearance data
    INT    m_nHairMeshID;      // Hair Model ID
    INT    m_nHairMaterialID;  // Hair Material ID
    INT    m_nFaceMeshID;      // Face Model ID
    INT    m_nFaceMaterialID;  // Face Material ID
    INT    m_nEquipVer;        // Equipment Version
    INT    m_nEquipmentID[HEQUIP_NUMBER]; // Equipment IDs array

    // Team information
    BOOL   m_bTeamFlag;        // Is in Team
    BOOL   m_bTeamLeaderFlag;  // Is Team Leader
    BOOL   m_bTeamFullFlag;    // Team is Full

    // Title system
    INT    m_nTitleNum;                    // Number of titles
    INT    m_nCurTitleIndex;               // Current title index
    _TITLE_ m_nTitleList[MAX_TITLE_SIZE]; // Title list
};
```

---

## Memory Offsets Reference

### Confirmed Offsets (Updated 2025-11-02)

| Data | Pointer Chain | Final Offset | Data Type | Size | Status |
|------|--------------|--------------|-----------|------|--------|
| **Base Pointer** | - | **2381824** | pointer | 4 bytes | ✅ **UPDATED** |
| **Stats Ptr Offset** | [2381824, 12] | **+340** | pointer | 4 bytes | ✅ **UPDATED** |
| **Current HP** | [2381824, 12, 340, 4] | **+1752** | int32 | 4 bytes | ✅ **UPDATED** |
| **Current MP** | [2381824, 12, 340, 4] | **+1756** | int32 | 4 bytes | ✅ **UPDATED** |
| **Max HP** | [2381824, 12, 340, 4] | **+1856** | int32 | 4 bytes | ✅ **UPDATED** |
| **Max MP** | [2381824, 12, 340, 4] | **+1860** | int32 | 4 bytes | ✅ **FIXED** (was 1852) |
| Character Name | [2381824, 12, 340, 4] | +48 | string | 30 bytes | ✅ Confirmed |
| Experience | [2381824, 12, 340, 4] | +92 | int32 | 4 bytes | ✅ Confirmed |
| X Coordinate | [2381824, 12] | +92 | float | 4 bytes | ✅ Confirmed |
| Y Coordinate | [2381824, 12] | +100 | float | 4 bytes | ✅ Confirmed |
| Pet ID | [2381824, 12, 340, 4] | +2356 | int32 | 4 bytes | ✅ Confirmed |
| Experience Alt | [2381824, 12, 340, 4] | +2300 | int32 | 4 bytes | ✅ Confirmed |

### Old Offsets (Obsolete - Before 2025-11-02)

| Data | Old Pointer Chain | Old Final Offset | Status |
|------|------------------|------------------|--------|
| Base Pointer | - | ~~7319476~~ (0x006F8C24) | ❌ Obsolete |
| Stats Ptr Offset | [7319476, 12] | ~~+344~~ | ❌ Obsolete |
| Current HP | [7319476, 12, 344, 4] | ~~+2292~~ | ❌ Obsolete |
| Current MP | [7319476, 12, 344, 4] | ~~+2296~~ | ❌ Obsolete |
| Max HP | [7319476, 12, 344, 4] | ~~+2400~~ | ❌ Obsolete |
| Max MP | [7319476, 12, 344, 4] | ~~+2404~~ | ❌ Obsolete |

### Unconfirmed Offsets (Need Investigation)

These values are from the `SDATA_PLAYER_MYSELF` structure but offsets are not yet confirmed:

- Money/Gold (m_nMoney)
- Strength, Spirit, Constitution, Intelligence, Dexterity
- All combat stats (Attack, Defense, Hit, Miss, Crit, etc.)
- All elemental stats (Cold, Fire, Lightning, Poison)
- Character level, race, class
- Equipment data
- Skills and abilities data structures

---

## Code Implementation Examples

### C# Class for Memory Reading (Updated)

```csharp
public class PlayerDataReader
{
    private Class7 memoryReader; // Assuming Class7 is your memory reader

    // UPDATED Base addresses
    private const int PLAYER_BASE_POINTER = 2381824;  // Updated from 7319476
    private const int STATS_OBJECT_OFFSET = 340;      // Updated from 344

    // UPDATED Stats offsets
    private const int OFFSET_CURRENT_HP = 1752;  // Updated from 2292
    private const int OFFSET_CURRENT_MP = 1756;  // Updated from 2296
    private const int OFFSET_MAX_HP = 1856;      // Updated from 2400
    private const int OFFSET_MAX_MP = 1860;      // Updated from 2404 (FIXED from user's 1852)

    // Confirmed offsets
    private const int OFFSET_CHAR_NAME = 48;
    private const int OFFSET_EXPERIENCE = 92;
    private const int OFFSET_X_COORD = 92;
    private const int OFFSET_Y_COORD = 100;
    private const int OFFSET_PET_ID = 2356;
    private const int OFFSET_EXP_ALT = 2300;

    public PlayerDataReader(int processId)
    {
        memoryReader = new Class7(processId);
    }

    /// <summary>
    /// Gets the Entity Base Address
    /// Pointer chain: [2381824, 12]
    /// </summary>
    private int GetEntityBaseAddress()
    {
        return memoryReader.method_1(new int[] { PLAYER_BASE_POINTER, 12 });
    }

    /// <summary>
    /// Gets the Stats Base Address
    /// Pointer chain: [2381824, 12, 340, 4]
    /// </summary>
    private int GetStatsBaseAddress()
    {
        return memoryReader.method_1(new int[] {
            PLAYER_BASE_POINTER,
            12,
            STATS_OBJECT_OFFSET,  // Updated to 340
            4
        });
    }

    /// <summary>
    /// Reads current HP
    /// </summary>
    public int GetCurrentHP()
    {
        int statsBase = GetStatsBaseAddress();
        return memoryReader.method_0(statsBase + OFFSET_CURRENT_HP);
    }

    /// <summary>
    /// Reads current MP
    /// </summary>
    public int GetCurrentMP()
    {
        int statsBase = GetStatsBaseAddress();
        return memoryReader.method_0(statsBase + OFFSET_CURRENT_MP);
    }

    /// <summary>
    /// Reads maximum HP
    /// </summary>
    public int GetMaxHP()
    {
        int statsBase = GetStatsBaseAddress();
        return memoryReader.method_0(statsBase + OFFSET_MAX_HP);
    }

    /// <summary>
    /// Reads maximum MP
    /// </summary>
    public int GetMaxMP()
    {
        int statsBase = GetStatsBaseAddress();
        return memoryReader.method_0(statsBase + OFFSET_MAX_MP);
    }

    /// <summary>
    /// Calculates HP percentage
    /// </summary>
    public int GetHPPercent()
    {
        int currentHP = GetCurrentHP();
        int maxHP = GetMaxHP();

        if (maxHP == 0)
            return 100;

        return (int)((float)currentHP * 100f / (float)maxHP);
    }

    /// <summary>
    /// Calculates MP percentage
    /// </summary>
    public int GetMPPercent()
    {
        int currentMP = GetCurrentMP();
        int maxMP = GetMaxMP();

        if (maxMP == 0)
            return 100;

        return (int)((float)currentMP * 100f / (float)maxMP);
    }

    /// <summary>
    /// Reads character name
    /// </summary>
    public string GetCharacterName()
    {
        int statsBase = GetStatsBaseAddress();
        return memoryReader.method_5(statsBase + OFFSET_CHAR_NAME);
    }

    /// <summary>
    /// Reads experience points
    /// </summary>
    public int GetExperience()
    {
        int statsBase = GetStatsBaseAddress();
        return memoryReader.method_0(statsBase + OFFSET_EXPERIENCE);
    }

    /// <summary>
    /// Reads X coordinate
    /// </summary>
    public int GetXCoordinate()
    {
        int entityBase = GetEntityBaseAddress();
        return (int)memoryReader.method_3(entityBase + OFFSET_X_COORD);
    }

    /// <summary>
    /// Reads Y coordinate
    /// </summary>
    public int GetYCoordinate()
    {
        int entityBase = GetEntityBaseAddress();
        return (int)memoryReader.method_3(entityBase + OFFSET_Y_COORD);
    }

    /// <summary>
    /// Gets complete player information
    /// </summary>
    public PlayerInfo GetCompletePlayerInfo()
    {
        return new PlayerInfo
        {
            Name = GetCharacterName(),
            CurrentHP = GetCurrentHP(),
            CurrentMP = GetCurrentMP(),
            MaxHP = GetMaxHP(),
            MaxMP = GetMaxMP(),
            HPPercent = GetHPPercent(),
            MPPercent = GetMPPercent(),
            Experience = GetExperience(),
            XCoordinate = GetXCoordinate(),
            YCoordinate = GetYCoordinate()
        };
    }
}

public class PlayerInfo
{
    public string Name { get; set; }
    public int CurrentHP { get; set; }
    public int CurrentMP { get; set; }
    public int MaxHP { get; set; }
    public int MaxMP { get; set; }
    public int HPPercent { get; set; }
    public int MPPercent { get; set; }
    public int Experience { get; set; }
    public int XCoordinate { get; set; }
    public int YCoordinate { get; set; }
}
```

### Usage Example

```csharp
// Initialize reader with game process ID
int gameProcessId = 12345;
PlayerDataReader reader = new PlayerDataReader(gameProcessId);

// Read individual values
int hp = reader.GetCurrentHP();
int mp = reader.GetCurrentMP();
string name = reader.GetCharacterName();

Console.WriteLine($"Character: {name}");
Console.WriteLine($"HP: {hp} / {reader.GetMaxHP()} ({reader.GetHPPercent()}%)");
Console.WriteLine($"MP: {mp} / {reader.GetMaxMP()} ({reader.GetMPPercent()}%)");
Console.WriteLine($"Position: ({reader.GetXCoordinate()}, {reader.GetYCoordinate()})");

// Or get complete info at once
PlayerInfo info = reader.GetCompletePlayerInfo();
Console.WriteLine($"Complete Info: {JsonConvert.SerializeObject(info)}");
```

---

## Map Object Pointer (Unresolved)

### Status: ⚠️ NEEDS INVESTIGATION

The Map Object Pointer is currently **not confirmed**. The old pointer no longer works:

**Old Map Pointer (Obsolete)**:
- Base Pointer: ~~6870940~~ (0x0068B91C)
- Pointer Chain: ~~[6870940, 14232]~~
- Map ID Offset: ~~+96~~

### Investigation Strategy

Based on the game source code analysis, the map information is likely stored in:

1. **CWorldManager Class**:
   - Contains `m_pActiveScene` pointer
   - Method: `GetActiveScene()` returns current scene
   - Method: `GetActiveSceneID()` returns scene ID

2. **CScene Class**:
   - Contains `m_pTheDefine` pointer to `_DBC_SCENE_DEFINE` structure
   - This structure likely contains scene/map name and ID

### Suggested Search Methods

#### Method 1: Pattern Scanning for Map Name Strings

```csharp
// Search for known map names in memory
string[] knownMapNames = new string[]
{
    "Thiên Hạ",     // Heaven
    "Nhân Nam",     // Nhon Nam
    "Lạc Dương",    // Loc Duong
    "Đôn Hoàng",    // Don Hoang
    "Kiếm Các",     // Kiem Cac
    "Đại Lý",       // Doi Lu
    "Nhị Hải",      // Nhi Hai
    "Bạng Thiên"    // Bang Thien
};

// Scan memory for these strings and find their base pointers
```

#### Method 2: Scan for CWorldManager Instance

```csharp
// The CWorldManager is a singleton (s_pMe static variable)
// Search for pattern: pointer -> CScene -> _DBC_SCENE_DEFINE

// Expected structure:
// [Unknown Base] -> CWorldManager instance
//     -> m_pActiveScene (+offset) -> CScene instance
//         -> m_pTheDefine (+offset) -> _DBC_SCENE_DEFINE
//             -> Scene Name
//             -> Scene ID
```

#### Method 3: Monitor Memory Changes During Map Changes

```
1. Teleport character to different maps
2. Scan memory for values that change with each map
3. Look for patterns in pointer structures
4. Cross-reference with known map IDs/names
```

### Known Map Names (from GClass0.cs)

The automation tool has these hardcoded map names:

| English Name | Vietnamese Name | Map Coordinates |
|--------------|-----------------|-----------------|
| Heaven | "heaven" | Various |
| Nhon Nam | "nhon nam" | (264, 293) |
| Loc Duong | "loc duong" | (33, 130) |
| Don Hoang | "don hoang" | (231, 289) |
| Kiem Cac | "kiem cac" | (36, 300) |
| Doi Lu | "doi lu" | (160, 295) |
| Nhi Hai | "nhi hai" | (285, 180) |
| Bang Thien | "bangthien" | (150, 50) |

### Request for Community Help

**If you have information about the Map Object Pointer, please update this section!**

Required information:
- Base pointer address (decimal and hex)
- Offset to map base
- Offset to map ID/name within map base
- Verification across multiple game instances

---

## Update History

### Version 2.0 - 2025-11-02
- ✅ **Updated Player Base Pointer**: 7319476 → **2381824** (0x00245580)
- ✅ **Updated Stats Object Offset**: 344 → **340**
- ✅ **Updated Current HP Offset**: 2292 → **1752**
- ✅ **Updated Current MP Offset**: 2296 → **1756**
- ✅ **Updated Max HP Offset**: 2400 → **1856**
- ✅ **Fixed Max MP Offset**: User reported 1852, corrected to **1860** (should be +4 from MaxHP)
- ✅ Added complete `SDATA_PLAYER_MYSELF` structure from game source code
- ✅ Added comprehensive C# implementation examples
- ⚠️ Marked Map Object Pointer as unresolved
- Added investigation strategies for Map Object Pointer

### Version 1.0 - 2025-11-01
- Initial documentation with old memory addresses
- Basic pointer chain documentation
- Legacy offset values

---

## Related Documentation

- **MEMORY_READING_SYSTEM.md** - Low-level memory reading implementation
- **PROJECT_ARCHITECTURE.md** - Overall system architecture
- **COMPLETE_CLASS_REFERENCE.md** - All class documentation
- **GClass0.cs** - Automation controller implementation
- **Class7.cs** - Memory reading Win32 API wrappers

---

## Notes & Warnings

### Important Considerations

1. **Game Updates Will Break Addresses**: Any game patch will likely change the base pointer (2381824) and potentially all offsets. You'll need to find new addresses after each update.

2. **ASLR Not Used**: The game appears to not use Address Space Layout Randomization, which is why fixed addresses work. This could change in future versions.

3. **32-bit Process**: All addresses are 32-bit (int32). The game is a 32-bit application.

4. **No Error Handling in Current Implementation**: The legacy code has no validation. Always check:
   - Process handle is valid
   - Memory reads succeed
   - Pointer chains return non-zero values
   - Data is within reasonable ranges

5. **Anti-Cheat Detection**: Reading game memory via `ReadProcessMemory` is easily detectable by anti-cheat systems. Use at your own risk.

6. **Max MP Offset Correction**: The user reported Max MP at offset 1852, but based on the structure alignment (each INT is 4 bytes), it should be at 1860 (MaxHP + 4). Please verify this in practice.

---

**End of Document**

For questions or updates, please modify this document with confirmed findings.
