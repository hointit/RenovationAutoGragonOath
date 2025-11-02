# Dragon Oath Scene ID System - Complete Technical Documentation

## Overview

This document explains how Scene IDs are generated, stored, and used in the Dragon Oath (Thiên Long Bát Bộ) MMORPG server and client system.

## Table of Contents

1. [Scene ID Generation](#scene-id-generation)
2. [Configuration File Structure](#configuration-file-structure)
3. [Server Data Structures](#server-data-structures)
4. [Scene Loading Process](#scene-loading-process)
5. [Client Memory Structure](#client-memory-structure)
6. [Map File Format](#map-file-format)
7. [Complete Data Flow](#complete-data-flow)
8. [Example Scene Mappings](#example-scene-mappings)

---

## Scene ID Generation

### Principle

**Scene IDs are directly derived from the INI section index number.**

- `[scene0]` → Scene ID = **0**
- `[scene1]` → Scene ID = **1**
- `[scene100]` → Scene ID = **100**
- Maximum: **1024 scenes** (defined by `MAX_SCENE` constant)

### Source Code Reference

File: `G:\SourceCodeGameTLBB\Server\Common\ServerBase\Config.cpp:481-525`

```cpp
VOID Config::LoadSceneInfo_Only()
{
    Ini ini( FILE_SCENE_INFO );  // Reads "SceneInfo.ini"

    // Read total scene count
    m_SceneInfo.m_SceneCount = ini.ReadInt("system", "scenenumber");

    // Allocate array for all scenes
    m_SceneInfo.m_pScene = new _SCENE_DATA[m_SceneInfo.m_SceneCount];

    // Load each scene
    for( uint i=0; i < m_SceneInfo.m_SceneCount; i++ )
    {
        sprintf( szSection, "scene%d", i );  // [scene0], [scene1], etc.

        // *** SCENE ID = SECTION INDEX ***
        m_SceneInfo.m_pScene[i].m_SceneID = (SceneID_t)i;

        m_SceneInfo.m_pScene[i].m_IsActive = ini.ReadInt(szSection, "active");
        ini.ReadText(szSection, "name", m_SceneInfo.m_pScene[i].m_szName, _MAX_PATH);
        ini.ReadText(szSection, "file", m_SceneInfo.m_pScene[i].m_szFileName, _MAX_PATH);
        m_SceneInfo.m_pScene[i].m_ServerID = ini.ReadInt(szSection, "serverId");
        m_SceneInfo.m_pScene[i].m_Type = ini.ReadInt(szSection, "type");
        m_SceneInfo.m_pScene[i].m_ThreadIndex = ini.ReadInt(szSection, "threadindex");
    }

    // Build hash table for O(1) lookups
    for(UINT i=0; i < m_SceneInfo.m_SceneCount; i++)
    {
        SceneID_t SceneID = m_SceneInfo.m_pScene[i].m_SceneID;
        m_SceneInfo.m_HashScene[SceneID] = i;  // Hash: ID → array index
    }
}
```

---

## Configuration File Structure

### SceneInfo.ini Format

Location: `F:\Games\TLBB server\Server\Config\SceneInfo.ini`

```ini
[system]
scenenumber=724                 # Total number of scenes in the game

[scene0]                        # Section name determines Scene ID (ID = 0)
threadindex=30                  # Thread assignment for load balancing
clientres=242                   # Client resource ID (may differ from Scene ID)
name=Lục Dương                  # Display name (Vietnamese)
active=1                        # 0=disabled, 1=enabled
file=luoyang.scn                # Scene configuration file
serverid=0                      # Which server hosts this scene
type=0                          # Scene type (0=normal, 1=copy, 2=battlefield)
PvpRuler=0                      # PvP rules
BeginPlus=8010100               # Optional: Begin position
EndPlus=39030100                # Optional: End position
IsReLive=0                      # Can players respawn here?

[scene1]                        # Scene ID = 1
threadindex=60
clientres=1
name=Tô Châu
active=1
file=suzhou.scn
serverid=0
type=0
PvpRuler=0

[scene2]                        # Scene ID = 2
threadindex=0
clientres=2
name=Đại Lý
active=1
file=dali.scn
serverid=0
type=0
PvpRuler=0
```

### Field Descriptions

| Field | Type | Description |
|-------|------|-------------|
| `threadindex` | int | Which server thread processes this scene (load balancing) |
| `clientres` | int | Client resource ID (may be used for asset loading) |
| `name` | string | Human-readable scene name in Vietnamese |
| `active` | 0/1 | Whether this scene is currently enabled |
| `file` | string | Scene configuration file (.scn) containing map data |
| `serverid` | int | Which game server instance hosts this scene |
| `type` | int | Scene type: 0=normal, 1=instance copy, 2=battlefield |
| `PvpRuler` | int | PvP rules: 0=safe zone, 1=PvP enabled, etc. |
| `IsReLive` | 0/1 | Can players respawn in this scene? |

---

## Server Data Structures

### _SCENE_DATA Structure

File: `G:\SourceCodeGameTLBB\Server\Common\ServerBase\Config.h:403-428`

```cpp
struct _SCENE_DATA
{
    SceneID_t  m_SceneID;               // Scene ID (0-1023)
    INT        m_IsActive;              // 0 = disabled, 1 = enabled
    CHAR       m_szName[_MAX_PATH];     // Display name (e.g., "Lục Dương")
    CHAR       m_szFileName[_MAX_PATH]; // Scene file (e.g., "luoyang.scn")
    ID_t       m_ServerID;              // Which server hosts this scene
    ID_t       m_Type;                  // Scene type (normal/copy/battlefield)
    ID_t       m_ThreadIndex;           // Which thread processes this scene
};
```

### _SCENE_INFO Structure

File: `G:\SourceCodeGameTLBB\Server\Common\ServerBase\Config.h:430-448`

```cpp
struct _SCENE_INFO
{
    _SCENE_DATA*  m_pScene;              // Array of all scene data
    uint          m_SceneCount;          // Total scenes (e.g., 724)
    INT           m_HashScene[MAX_SCENE]; // Fast lookup: SceneID → array index

    // MAX_SCENE = 1024 (defined in GameDefine.h)
};
```

### Hash Table for O(1) Lookups

The `m_HashScene[]` array provides constant-time scene lookups:

```cpp
// Example: Get data for Scene ID 11 (Lục Dương)
int arrayIndex = g_Config.m_SceneInfo.m_HashScene[11];
_SCENE_DATA* pScene = &g_Config.m_SceneInfo.m_pScene[arrayIndex];
// pScene->m_szName == "Lục Dương"
// pScene->m_szFileName == "luoyang.scn"
```

---

## Scene Loading Process

### 1. Scene Configuration Loading (.scn file)

File: `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\Scene_Core.cpp:280-360`

Each `.scn` file is an INI-format configuration containing:

```ini
[System]
navmapname=luoyang.nav          # Binary navigation map
monsterfile=luoyang_monster.ini # Monster spawn points
eventfile=luoyang_event.ini     # Trigger areas
platformfile=luoyang_platform.ini # Interactive objects
growpointdata=luoyang_grow.ini  # Resource gathering points
patrolpoint=luoyang_patrol.ini  # NPC patrol routes
stallinfodata=luoyang_stall.ini # Player market stalls
```

### 2. Navigation Map Loading

File: `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\Map.cpp:41-60`

```cpp
BOOL Map::Load( CHAR* filename )
{
    if( m_pPathFinder == NULL )
    {
        // PathFinder reads the .nav binary file
        m_pPathFinder = new PathFinder(this, filename, m_CX, m_CZ);
    }
    return TRUE;
}
```

### 3. Navigation Map File Format (.nav)

File: `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\PathFinder.cpp:35-86`

**Binary Structure:**

```
Offset  | Size | Description
--------|------|------------------------------------------
0x0000  | 2    | WORD width  (grid cells horizontally)
0x0002  | 2    | WORD height (grid cells vertically)
0x0004  | 4×N  | Grid data (width × height cells)
```

**Grid Cell Format:**
- Each cell = 4 bytes (terrain state + route info)
- Grid size = **0.5 world units per cell**
- Actual map dimensions: `worldX = width × 0.5`, `worldZ = height × 0.5`

**Example:**
- Navigation map: 1024 × 768 cells
- World size: 512 × 384 units
- Total cells: 786,432

**Terrain States:**
- `0` = UNKNOWN (unexplored)
- `1` = IMPASSABLE (walls, obstacles)
- `20` = OPEN (walkable)
- `30` = CLOSED (temporarily blocked)

### 4. Zone Subdivision

File: `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\Scene_Core.cpp:427-448`

After loading the map, the server divides it into **Zones** for spatial optimization:

```cpp
// Calculate zone grid dimensions
INT cx = (INT)(m_pMap->CX() / g_Config.m_ConfigInfo.m_ZoneSize);
INT cz = (INT)(m_pMap->CZ() / g_Config.m_ConfigInfo.m_ZoneSize);

if( (INT)m_pMap->CX() % g_Config.m_ConfigInfo.m_ZoneSize > 0 ) cx++;
if( (INT)m_pMap->CZ() % g_Config.m_ConfigInfo.m_ZoneSize > 0 ) cz++;

m_ZoneInfo.m_wZoneSize = (WORD)(cx * cz);  // Total zones
m_ZoneInfo.m_wZoneW = (WORD)cx;            // Zone grid width
m_ZoneInfo.m_wZoneH = (WORD)cz;            // Zone grid height

// Create zone array
m_pZone = new Zone[m_ZoneInfo.m_wZoneSize];

for( WORD i=0; i < m_ZoneInfo.m_wZoneSize; i++ )
{
    m_pZone[i].SetZoneID( (ZoneID_t)i );
}
```

**Purpose of Zones:**
- Spatial indexing for fast object queries
- When a player moves or casts a skill, only nearby zones are scanned
- Zone size typically 10-20 world units (configurable)

### 5. Scene Component Loading

File: `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\Scene_Core.cpp:362-624`

```cpp
BOOL Scene::Load( SCENE_LOAD* load )
{
    // 1. Load navigation map
    GET_SCENE_FULL_PATH( szTemp, load->m_szMap );
    m_pMap->Load( szTemp );

    // 2. Load patrol paths
    GET_SCENE_FULL_PATH( szTemp, load->m_szPatrolPointData );
    m_pPatrolPathMgr->LoadPatrolPoint(szTemp);

    // 3. Load monsters
    GET_SCENE_FULL_PATH( szTemp, load->m_szMonster );
    m_pMonsterManager->LoadMonster( szTemp );

    // 4. Load platforms (interactive objects)
    if( load->m_IsPlatformExist )
    {
        GET_SCENE_FULL_PATH( szTemp, load->m_szPlatform );
        m_pPlatformManager->LoadPlatform( szTemp );
    }

    // 5. Load stall positions
    if( load->m_IsStallInfoExist )
    {
        GET_SCENE_FULL_PATH( szTemp, load->m_szStallInfo );
        m_pStallInfoManager->Load( szTemp );
    }

    // 6. Load growth points (resource nodes)
    if( load->m_IsGrowPointExist )
    {
        GET_SCENE_FULL_PATH( szTemp, load->m_szGrowPointData );
        m_GrowPointGroup.Load(szGrowPointData, szGrowPointSetup);
    }

    // 7. Load event areas (triggers)
    GET_SCENE_FULL_PATH( szTemp, load->m_szArea );
    m_pAreaManager->Init( szTemp );

    // 8. Initialize Lua scripting
    m_pLuaInterface->Init(this);

    return TRUE;
}
```

---

## Client Memory Structure

### Map ID Memory Location

File: `G:\microauto-6.9\AutoDragonOath\Services\GameProcessMonitor.cs:30-31, 142-147`

```csharp
// Pointer chain to current map structure
private static readonly int[] MapBasePointer = { 2381824, 13692 };
private const int OFFSET_MAP_ID = 96;

// Reading map ID from game client memory
int mapBase = memoryReader.FollowPointerChain(MapBasePointer);
if (mapBase != 0)
{
    int mapId = memoryReader.ReadInt32(mapBase + OFFSET_MAP_ID);
    characterInfo.MapName = mapId.ToString();
}
```

### Pointer Chain Breakdown

1. **Base Address**: Read pointer at `Game.exe base + 2381824`
   - This points to the game's scene manager object

2. **Scene Manager Offset**: Add `13692` to previous pointer
   - This points to the current active map data structure

3. **Map ID Offset**: Add `96` to previous pointer
   - This is the actual **Map ID** (int32 value)

**Memory Layout:**
```
[Game.exe Base + 2381824] → Pointer A (Scene Manager)
    ↓
[Pointer A + 13692] → Pointer B (Current Map Structure)
    ↓
[Pointer B + 96] → INT32 mapId (Current Map ID)
```

### Important Note

The **Map ID stored in client memory** may correspond to the **`clientres` field** rather than the Scene ID. This requires verification through memory analysis.

---

## Map File Format

### Pathfinding System

The navigation map uses A* pathfinding with an 8-directional grid:

```
Direction layout:
    5   2   6

    1   *   3

    4   0   7
```

**Movement Costs:**
- Straight (N, S, E, W): Cost = 10
- Diagonal: Cost = 14 (approximation of √2 × 10)

### Map Dimensions

File: `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\Map.h:139-140`

```cpp
class Map
{
private:
    uint m_CX;  // Map width in world units
    uint m_CZ;  // Map height in world units

    PathFinder* m_pPathFinder;  // A* pathfinding engine
};
```

**Coordinate System:**
```
                (0, m_CZ)      (m_CX, m_CZ)
    y  z            ┌───────────────┐
    │ /             │               │
    │/              │               │
    +────── x       │               │
                    │               │
                    └───────────────┘
                (0, 0)         (m_CX, 0)
```

---

## Complete Data Flow

### Server Startup → Player Enters Scene

```
┌─────────────────────────────────────────────────────────────────┐
│ PHASE 1: Server Initialization                                  │
└─────────────────────────────────────────────────────────────────┘
    │
    ├─► Read SceneInfo.ini
    │   └─► scenenumber=724
    │       [scene0], [scene1], ..., [scene723]
    │
    ├─► Generate Scene IDs
    │   └─► Scene ID = section index
    │       [scene0] → ID=0, [scene1] → ID=1, etc.
    │
    ├─► Load into memory
    │   └─► g_Config.m_SceneInfo.m_pScene[724]
    │       Each entry has: ID, name, file, serverID, type, thread
    │
    └─► Build hash table
        └─► m_HashScene[SceneID] = array_index
            Enables O(1) lookup by Scene ID

┌─────────────────────────────────────────────────────────────────┐
│ PHASE 2: Scene Loading (When Scene Becomes Active)              │
└─────────────────────────────────────────────────────────────────┘
    │
    ├─► Get scene configuration
    │   └─► sceneData = g_Config.m_SceneInfo.m_pScene[0]
    │       fileName = sceneData.m_szFileName  // "luoyang.scn"
    │
    ├─► Read .scn file
    │   └─► Extract file paths:
    │       - navmapname → "luoyang.nav"
    │       - monsterfile → "luoyang_monster.ini"
    │       - eventfile → "luoyang_event.ini"
    │       - etc.
    │
    ├─► Load navigation map
    │   └─► PathFinder reads "luoyang.nav" binary:
    │       - Header: width=1024, height=768
    │       - Grid: 786,432 cells (0.5 units each)
    │       - Map size: 512 × 384 world units
    │
    ├─► Create zone grid
    │   └─► Subdivide map into spatial zones:
    │       - Zone size: 10 units (configurable)
    │       - Zones: (512÷10) × (384÷10) = 52 × 39 = 2,028 zones
    │
    ├─► Load scene components
    │   ├─► Monsters: Spawn points, AI, patrol routes
    │   ├─► NPCs: Merchants, quest givers
    │   ├─► Platforms: Interactive objects
    │   ├─► Events: Trigger zones, quest areas
    │   ├─► Resources: Mining, herbs, fishing spots
    │   └─► Stalls: Player market locations
    │
    ├─► Initialize Lua scripts
    │   └─► Scene logic, event handlers, quest scripts
    │
    └─► Scene ready
        └─► Status: SCENE_STATUS_RUNNING
            Thread: threadindex=30
            Type: SCENE_TYPE_GAMELOGIC

┌─────────────────────────────────────────────────────────────────┐
│ PHASE 3: Player Enters Scene                                    │
└─────────────────────────────────────────────────────────────────┘
    │
    ├─► Server assigns player to Scene 0
    │   └─► Player spawns at coordinates (256.5, 192.3)
    │
    ├─► Server sends scene data to client
    │   └─► Packet includes:
    │       - Scene ID (or clientres)
    │       - Nearby objects (NPCs, players, monsters)
    │       - Map boundaries
    │
    ├─► Client loads scene assets
    │   └─► Uses clientres=242 to load:
    │       - 3D terrain mesh
    │       - Textures, lighting
    │       - Object models
    │
    └─► Client stores Map ID in memory
        └─► Address: [base+2381824]+13692+96
            Value: 242 (clientres) or 0 (Scene ID)?

┌─────────────────────────────────────────────────────────────────┐
│ PHASE 4: Automation Tools Read Map ID                           │
└─────────────────────────────────────────────────────────────────┘
    │
    ├─► GameProcessMonitor.ReadCharacterInfo()
    │   └─► FollowPointerChain([2381824, 13692])
    │       Read int32 at +96 offset
    │       Result: mapId = 242 (or 0)
    │
    └─► Display to user
        └─► "Current Map: Lục Dương (242)"
```

---

## Example Scene Mappings

### From SceneInfo.ini (First 20 Scenes)

| Scene ID | clientres | Name (Vietnamese) | File | Type | PvP |
|----------|-----------|-------------------|------|------|-----|
| 0 | 242 | Lục Dương | luoyang.scn | 0 | 0 |
| 1 | 1 | Tô Châu | suzhou.scn | 0 | 0 |
| 2 | 2 | Đại Lý | dali.scn | 0 | 0 |
| 3 | 3 | Tung Sơn | songshan.scn | 0 | 1 |
| 4 | 4 | Thái Hồ | taihu.scn | 0 | 1 |
| 5 | 5 | Kính Hồ | jinghu.scn | 0 | 6 |
| 6 | 6 | Vô Lượng Sơn | wuliang.scn | 0 | 1 |
| 7 | 7 | Kiếm Các | jiange.scn | 0 | 1 |
| 8 | 8 | Đôn Hoàng | dunhuang.scn | 0 | 1 |
| 9 | 9 | Thiếu Lâm Tự | shaolin.scn | 0 | 0 |
| 10 | 10 | Cái Bang Tổng Đà | gaibang.scn | 0 | 0 |
| 11 | 11 | Quang Minh Điện | mingjiao.scn | 0 | 0 |
| 12 | 12 | Vũ Đang Sơn | wudang.scn | 0 | 0 |
| 13 | 13 | Thiên Long Tự | tianlong.scn | 0 | 0 |
| 14 | 14 | Lăng Ba Động | xiaoyao.scn | 0 | 0 |
| 15 | 15 | Nga Mi Sơn | emei.scn | 0 | 0 |
| 16 | 16 | Tinh Tú Hải | xingxiu.scn | 0 | 0 |
| 17 | 17 | Thiên Sơn | tianshan.scn | 0 | 0 |
| 18 | 18 | Nhân Nam | yannan.scn | 0 | 2 |

**Legend:**
- **Type**: 0=Normal scene, 1=Instance copy, 2=Battlefield
- **PvP**: 0=Safe zone, 1=PvP enabled, 2=Special rules, 6=Event zone

### Important Observations

1. **Scene ID ≠ clientres** (for Scene 0)
   - Scene ID: 0
   - clientres: 242
   - This suggests the client memory stores `clientres` rather than Scene ID

2. **Most scenes have matching IDs**
   - Scene 1 → clientres 1
   - Scene 2 → clientres 2
   - Scene 3 → clientres 3
   - etc.

3. **Scene 0 is special** (main city)
   - Large clientres value (242)
   - May have been added later or uses different asset system

---

## Summary

### Key Points

1. **Scene ID Generation**
   - Scene IDs are simply the index number from `[sceneN]` sections in SceneInfo.ini
   - Range: 0 to 1023 (MAX_SCENE)
   - Sequential and deterministic

2. **Two ID Systems**
   - **Server Scene ID**: Section index (0-723)
   - **Client Resource ID**: The `clientres` field (may differ for special scenes)

3. **Data Structures**
   - Server uses `_SCENE_DATA` array with hash table for O(1) lookups
   - Each scene has: ID, name, file, server assignment, type, thread

4. **Loading Process**
   - Server reads SceneInfo.ini → loads .scn files → loads .nav maps
   - Creates zone grid for spatial optimization
   - Loads NPCs, monsters, events, scripts

5. **Client Memory**
   - Map ID stored at: `[Game.exe+2381824]+13692+96`
   - Value may be `clientres` rather than Scene ID
   - Automation tools use pointer chains to read this value

6. **Navigation System**
   - Binary .nav files contain grid-based pathfinding data
   - 0.5 units per grid cell
   - 8-directional movement with A* pathfinding

### Technical Limits

- **Maximum scenes**: 1,024 (MAX_SCENE constant)
- **Current scene count**: 724 (from config)
- **Grid cell size**: 0.5 world units
- **Zone size**: 10 units (typical, configurable)

### For Automation Developers

When reading the current map from game client memory:

```csharp
// Read map ID from client memory
int[] mapPointer = { 2381824, 13692 };
int mapBase = memoryReader.FollowPointerChain(mapPointer);
int mapId = memoryReader.ReadInt32(mapBase + 96);

// Note: mapId may be clientres, not Scene ID
// For Scene 0: mapId might be 242, not 0
// For Scene 1+: mapId usually equals Scene ID
```

**Recommendation**: Build a lookup table from SceneInfo.ini that maps both Scene IDs and clientres values to scene names.

---

## References

### Source Files Analyzed

**Server Configuration:**
- `G:\SourceCodeGameTLBB\Server\Common\ServerBase\Config.h`
- `G:\SourceCodeGameTLBB\Server\Common\ServerBase\Config.cpp`
- `F:\Games\TLBB server\Server\Config\SceneInfo.ini`

**Scene System:**
- `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\Scene.h`
- `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\Scene.cpp`
- `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\Scene_Core.cpp`
- `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\Map.h`
- `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\Map.cpp`

**Pathfinding:**
- `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\PathFinder.h`
- `G:\SourceCodeGameTLBB\Server\Server\GameServer\Server\Scene\PathFinder.cpp`

**Client Memory:**
- `G:\microauto-6.9\AutoDragonOath\Services\GameProcessMonitor.cs`
- `G:\microauto-6.9\AutoDragonOath\Services\MemoryReader.cs`

### Constants

```cpp
#define MAX_SCENE 1024              // Maximum scenes
#define SCENE_TYPE_GAMELOGIC 0      // Normal scene
#define SCENE_TYPE_COPY 1           // Instance copy
#define SCENE_TYPE_BATTLEFIELD 2    // PvP battlefield
#define SCENE_STATUS_RUNNING 4      // Scene is active
```

---

**Document Version**: 1.0
**Date**: 2025-01-XX
**Analysis Based On**: Dragon Oath server source code (TLBB)
