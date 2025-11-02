# Complete Map/Scene System Analysis - Dragon Oath

**Created**: 2025-11-02
**Purpose**: Document how the current map/scene is stored in game memory

---

## Table of Contents

1. [Overview](#overview)
2. [Map Data Type](#map-data-type)
3. [Memory Structure](#memory-structure)
4. [Source Code Analysis](#source-code-analysis)
5. [How to Find in Memory](#how-to-find-in-memory)
6. [Memory Scanner Instructions](#memory-scanner-instructions)
7. [Expected Results](#expected-results)

---

## Overview

### Key Findings

✅ **Current Map is stored as**: **INT** (int32), NOT a string
✅ **Map ID Type**: `SceneID_t` (typedef for INT)
✅ **Storage Location**: `CWorldManager::s_pMe->m_pActiveScene->GetSceneDefine()->nServerID`

### What This Means

The game stores the current map as an **integer ID** (like 1, 2, 3, etc.), not as a string name (like "heaven", "nhon nam").

However, there are also **string fields** available:
- `szName` - Scene name (LPCSTR)
- `szSceneMap` - Scene map name (LPCSTR)

---

## Map Data Type

### From Source Code: `_DBC_SCENE_DEFINE` Structure

```cpp
struct _DBC_SCENE_DEFINE
{
    static const int SCENE_SERVER_ID_COLUMN = 1;

    INT     nLocalID;           // +0   Local scene ID
    INT     nServerID;          // +4   *** SERVER SCENE ID (This is the map ID!) ***
    INT     nCityLevel;         // +8   City level
    LPCSTR  szName;             // +12  Scene name string (pointer)
    INT     nXSize;             // +16  X size
    INT     nZSize;             // +20  Z size
    LPCSTR  szWXObjectName;     // +24  Object name (pointer)
    LPCSTR  szRegionFile;       // +28  Region file (pointer)
    LPCSTR  szCollisionfile;    // +32  Collision file (pointer)
    LPCSTR  szMiniMap;          // +36  Minimap name (pointer)
    INT     nBackSound;         // +40  Background sound ID
    LPCSTR  szSceneMap;         // +44  *** Scene map name string (pointer) ***

    // Additional fields...
    INT     nWroldMapPosX;
    INT     nWroldMapPosY;
    INT     nNameWroldMapPosX;
    INT     nNameWroldMapPosY;
    LPCSTR  szSceneType;
    LPCSTR  szCityNameNormalImageSet;
    LPCSTR  szCityNameNormalImage;
    LPCSTR  szCityNameHoverImageSet;
    LPCSTR  szCityNameHoverImage;
};
```

### Key Fields for Map Detection

| Field | Offset | Type | Purpose |
|-------|--------|------|---------|
| **nServerID** | +4 | INT | **Primary map ID (integer)** |
| **szSceneMap** | +44 | LPCSTR | **Map name string (pointer to string)** |
| szName | +12 | LPCSTR | Scene display name |

---

## Memory Structure

### Complete Pointer Chain

```
┌─────────────────────────────────────────────────────────────────┐
│ LEVEL 0: Static Variable                                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [Static Address: ???] → CWorldManager::s_pMe                  │
│         │                                                        │
│         │  (Points to CWorldManager singleton instance)         │
│         ↓                                                        │
└─────────┼────────────────────────────────────────────────────────┘
          │
          │
┌─────────┼────────────────────────────────────────────────────────┐
│ LEVEL 1: CWorldManager Instance                                 │
├─────────┼────────────────────────────────────────────────────────┤
│         ↓                                                        │
│  CWorldManager object                                           │
│  {                                                               │
│      // ... other members ...                                   │
│      CScene* m_pActiveScene;  ← +offset_unknown                │
│      // ... other members ...                                   │
│  }                                                               │
│         │                                                        │
│         │  (Points to current active scene)                     │
│         ↓                                                        │
└─────────┼────────────────────────────────────────────────────────┘
          │
          │
┌─────────┼────────────────────────────────────────────────────────┐
│ LEVEL 2: CScene Instance                                        │
├─────────┼────────────────────────────────────────────────────────┤
│         ↓                                                        │
│  CScene object                                                  │
│  {                                                               │
│      const _DBC_SCENE_DEFINE* m_pTheDefine;  ← probably +0-8   │
│      int m_nZoneXSize;                                          │
│      int m_nZoneZSize;                                          │
│      // ... other members ...                                   │
│  }                                                               │
│         │                                                        │
│         │  (Points to scene definition structure)               │
│         ↓                                                        │
└─────────┼────────────────────────────────────────────────────────┘
          │
          │
┌─────────┼────────────────────────────────────────────────────────┐
│ LEVEL 3: _DBC_SCENE_DEFINE Structure                            │
├─────────┼────────────────────────────────────────────────────────┤
│         ↓                                                        │
│  _DBC_SCENE_DEFINE                                              │
│  {                                                               │
│      +0:  INT nLocalID;                                         │
│      +4:  INT nServerID;           *** CURRENT MAP ID ***       │
│      +8:  INT nCityLevel;                                       │
│      +12: LPCSTR szName;            → String pointer            │
│      +16: INT nXSize;                                           │
│      +20: INT nZSize;                                           │
│      ... (more fields) ...                                      │
│      +44: LPCSTR szSceneMap;        *** MAP NAME STRING ***     │
│      ... (more fields) ...                                      │
│  }                                                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## Source Code Analysis

### How GetActiveSceneID() Works

From `WorldManager.cpp`:

```cpp
INT CWorldManager::GetActiveSceneID(void) const
{
    if(m_pActiveScene)
        return m_pActiveScene->GetSceneDefine()->nServerID;
    else
        return 0;
}
```

**Breakdown**:
1. Check if `m_pActiveScene` is not NULL
2. Call `m_pActiveScene->GetSceneDefine()` → returns `_DBC_SCENE_DEFINE*`
3. Access `nServerID` field (offset +4)
4. Return the integer map ID

### CScene::GetSceneDefine()

From `Scene.h`:

```cpp
class CScene : public tScene
{
public:
    // Get scene definition
    virtual const _DBC_SCENE_DEFINE* GetSceneDefine(VOID) const
    {
        return m_pTheDefine;
    }

protected:
    // Scene definition structure, read from files
    const _DBC_SCENE_DEFINE* m_pTheDefine;

    // Zone data
    INT m_nZoneXSize;
    INT m_nZoneZSize;
    std::vector< CZone > m_theZoneBuf;
    // ... more members ...
};
```

### Singleton Pattern

```cpp
// WorldManager.h
class CWorldManager : public tWorldSystem
{
public:
    static CWorldManager* GetMe(VOID) { return s_pMe; }

protected:
    static CWorldManager* s_pMe;        // ← Singleton instance pointer
    CScene* m_pActiveScene;              // ← Current active scene
    WORLD_STATION m_Station;
    INT m_idNextScene;
    // ... other members ...
};

// WorldManager.cpp
CWorldManager* CWorldManager::s_pMe = NULL;

CWorldManager::CWorldManager()
{
    s_pMe = this;                        // Initialize singleton
    m_pActiveScene = NULL;
    m_Station = WS_NOT_ENTER;
    m_idNextScene = INVALID_ID;
}
```

---

## How to Find in Memory

### What You Need to Find

To read the current map, you need to find:

1. **Static address of `CWorldManager::s_pMe`** - This is a global variable
2. **Offset of `m_pActiveScene`** within CWorldManager instance
3. **Offset of `m_pTheDefine`** within CScene instance (probably +0)
4. **Offset of `nServerID`** within _DBC_SCENE_DEFINE (+4 confirmed)

### Unknown Values

❌ **Static address of CWorldManager::s_pMe** - UNKNOWN (need to find)
❌ **Offset of m_pActiveScene** in CWorldManager - UNKNOWN (need to scan)
⚠️ **Offset of m_pTheDefine** in CScene - Probably +0 or +4 (first member variable)
✅ **Offset of nServerID** in _DBC_SCENE_DEFINE - +4 (confirmed from structure)

---

## Memory Scanner Instructions

### Method 1: Scan for Map Name Strings (RECOMMENDED)

Since `szSceneMap` is a string pointer, we can scan for the string and work backwards.

**Steps**:

1. **Know your current map** (look at in-game UI)
   - Example: "nhon nam", "heaven", "loc duong"

2. **Run MemoryScanner Scenario 1** - Scan for map names

3. **Find the string in memory**
   ```
   Example result:
   String "nhon nam" found at: 0x12345ABC
   ```

4. **Find pointers to that string**
   ```
   Pointer at: 0x23456DEF (points to 0x12345ABC)
   This could be the szSceneMap field!
   ```

5. **Calculate backwards**
   - If pointer is at 0x23456DEF
   - And szSceneMap is at offset +44
   - Then _DBC_SCENE_DEFINE starts at: 0x23456DEF - 44 = 0x23456DC3
   - nServerID (offset +4) would be at: 0x23456DC3 + 4 = 0x23456DC7

6. **Verify by reading the integer at +4**
   ```
   Read int32 at 0x23456DC7
   Result: Some integer like 1, 2, 3, etc.
   ```

### Method 2: Pattern Scan for CWorldManager::s_pMe

The static variable `s_pMe` is stored in the `.data` section of the executable.

**Steps**:

1. **Find game.exe base address** (usually around 0x00400000)

2. **Scan for singleton pattern**:
   - Look for a pointer in the range 0x00400000 - 0x00800000
   - That points to an object containing another pointer to CScene

3. **Verify the chain**:
   ```
   Read [candidate_address] → WorldManager instance
   Read [WorldManager + offset] → CScene instance
   Read [CScene + 0] → _DBC_SCENE_DEFINE pointer
   Read [_DBC_SCENE_DEFINE + 4] → nServerID (int)
   ```

### Method 3: Monitor Map Changes

**Steps**:

1. **Scan for known map ID** (if you know the ID)
   - Example: If current map ID is 5
   - Scan for int32 value = 5

2. **Change maps** (teleport to different location)

3. **Narrow scan**
   - New map ID is 10
   - Narrow to addresses that changed from 5 to 10

4. **Work backwards**
   - Found address is nServerID
   - Subtract 4 to get _DBC_SCENE_DEFINE base
   - Find what points to this structure

### Method 4: Use Known Game Function Calls

If you can hook or trace game function calls:

```cpp
// When game calls GetActiveSceneID()
CWorldManager::GetMe()->GetActiveSceneID()

// Trace the memory access:
1. Read CWorldManager::s_pMe (static address ← THIS IS WHAT WE NEED!)
2. Read m_pActiveScene from CWorldManager
3. Read m_pTheDefine from CScene
4. Read nServerID from _DBC_SCENE_DEFINE
```

---

## Expected Results

### Map ID Values

The game likely uses sequential integers for map IDs:

```
Map ID 1 = ?
Map ID 2 = ?
Map ID 3 = ?
...
```

You'll need to build a mapping table by visiting different maps and recording the IDs.

### Old Memory Address (Obsolete)

Your old implementation had:
```csharp
public int method_25() // Get map base
{
    return class7_0.method_1(new int[] { 6870940, 14232 });
}

public string method_27() // Get map name
{
    return class7_0.method_5(method_25() + 96);
}
```

This used:
- Base: 6870940 (0x0068B91C)
- Offset: +14232
- Map string at: +96

But this no longer works, so we need to find the new addresses!

### Possible New Structure

Based on source code analysis, the new chain might be:

```csharp
// Step 1: Read CWorldManager singleton pointer
int worldMgrPtr = ReadInt32([STATIC_ADDRESS_UNKNOWN]);

// Step 2: Read m_pActiveScene from CWorldManager
int activeScenePtr = ReadInt32(worldMgrPtr + OFFSET_UNKNOWN);

// Step 3: Read m_pTheDefine from CScene
int sceneDefinePtr = ReadInt32(activeScenePtr + 0); // Probably first member

// Step 4: Read map ID
int mapID = ReadInt32(sceneDefinePtr + 4); // nServerID at offset +4

// Step 5: Read map name string (optional)
int mapNamePtr = ReadInt32(sceneDefinePtr + 44); // szSceneMap at offset +44
string mapName = ReadString(mapNamePtr);
```

---

## Using MemoryScanner to Find It

### Recommended Scenario: Scenario 1 + Analysis

1. **Run Scenario 1**: Scan for map names
   ```
   MemoryScanner.exe
   → Choose option 1 (Find Map Object Pointer)
   ```

2. **Analyze results**:
   - You'll find map name strings
   - You'll find pointers to those strings
   - These pointers are likely the `szSceneMap` field

3. **Calculate structure base**:
   ```
   If szSceneMap is at offset +44:
   Structure base = (pointer address) - 44
   ```

4. **Read map ID**:
   ```
   Map ID = Read int32 at (structure base + 4)
   ```

5. **Work backwards to find static pointer**:
   - Find what points to the _DBC_SCENE_DEFINE structure
   - Find what points to the CScene object
   - Find the static CWorldManager::s_pMe address

### Example

```
Step 1: Found "nhon nam" string at 0x15ABC000

Step 2: Found pointer to it at 0x14567DEF
        → This is szSceneMap field (offset +44)

Step 3: Calculate _DBC_SCENE_DEFINE base
        0x14567DEF - 44 = 0x14567DC3

Step 4: Read nServerID
        Read int32 at 0x14567DC3 + 4 = 0x14567DC7
        Result: 15 (map ID for Nhon Nam)

Step 5: Find what points to 0x14567DC3
        → This should be m_pTheDefine in CScene

Step 6: Find what points to CScene
        → This should be m_pActiveScene in CWorldManager

Step 7: Find static address
        → This should be CWorldManager::s_pMe (FOUND!)
```

---

## Integration

Once you find the addresses, update your code:

```csharp
public class MapReader
{
    // Found values (EXAMPLES - you need to find real values)
    private const int WORLDMANAGER_SINGLETON_PTR = ???;  // Static address
    private const int OFFSET_ACTIVESCENE = ???;           // m_pActiveScene offset
    private const int OFFSET_SCENEDEFINE = 0;            // m_pTheDefine offset (probably 0)
    private const int OFFSET_SERVERID = 4;               // nServerID offset (confirmed)
    private const int OFFSET_SCENEMAP = 44;              // szSceneMap offset (confirmed)

    public int GetCurrentMapID()
    {
        // Read CWorldManager singleton
        int worldMgr = memReader.method_0(WORLDMANAGER_SINGLETON_PTR);

        // Read m_pActiveScene
        int activeScene = memReader.method_0(worldMgr + OFFSET_ACTIVESCENE);

        // Read m_pTheDefine
        int sceneDefine = memReader.method_0(activeScene + OFFSET_SCENEDEFINE);

        // Read nServerID
        int mapID = memReader.method_0(sceneDefine + OFFSET_SERVERID);

        return mapID;
    }

    public string GetCurrentMapName()
    {
        // Follow same chain to szSceneMap
        int worldMgr = memReader.method_0(WORLDMANAGER_SINGLETON_PTR);
        int activeScene = memReader.method_0(worldMgr + OFFSET_ACTIVESCENE);
        int sceneDefine = memReader.method_0(activeScene + OFFSET_SCENEDEFINE);

        // Read szSceneMap pointer
        int mapNamePtr = memReader.method_0(sceneDefine + OFFSET_SCENEMAP);

        // Read string at pointer
        string mapName = memReader.method_5(mapNamePtr);

        return mapName;
    }
}
```

---

## Summary

### What We Know

✅ **Map data type**: INT (int32) for map ID, LPCSTR for map name
✅ **Structure**: `_DBC_SCENE_DEFINE` with nServerID at offset +4
✅ **Access path**: `CWorldManager::s_pMe->m_pActiveScene->m_pTheDefine->nServerID`

### What We Need to Find

❌ Static address of `CWorldManager::s_pMe`
❌ Offset of `m_pActiveScene` in CWorldManager
⚠️ Offset of `m_pTheDefine` in CScene (likely +0 or +4)

### Next Steps

1. **Use MemoryScanner Scenario 1** to find map name strings
2. **Work backwards** from string pointers to find structures
3. **Trace the pointer chain** to find static CWorldManager address
4. **Test stability** across game restarts
5. **Update GClass0.cs** with new addresses

---

**Good luck finding the Map Object Pointer!** 🗺️✨

