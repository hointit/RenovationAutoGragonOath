# Quick Start: Using Source Code to Call Level-Up

You have the game source code at `G:\SourceCodeGameTLBB`. This is the BEST scenario!

## What You Need to Find (3 Quick Tests)

### Test 1: Find Packet ID (2 minutes)
```bash
find "G:/SourceCodeGameTLBB/Common" -name "*PacketDefine*" -o -name "*PacketID*"
```

Then open that file and search for:
```
PACKET_CG_REQLEVELUP
```

**Example result:**
```cpp
#define PACKET_CG_REQLEVELUP 0x0142
```

**Write it down:** `Packet ID = 0x____`

---

### Test 2: Check for Exported Functions (2 minutes)
```bash
cd "C:\Program Files (x86)\YourGame"  # Where Game.exe is
dumpbin /EXPORTS Game.exe | findstr -i "send packet level"
```

**If you see exports** like `SendPacket` or `SendNetworkPacket`:
→ ✅ **Use DllImport approach (EASIEST!)**

**If no exports:**
→ Continue to Test 3

---

### Test 3: Check for Lua Interface (3 minutes)
```bash
grep -r "RegisterFunction" "G:/SourceCodeGameTLBB/Game/Client/WXClient/Interface" --include="*.cpp" | grep -i "level\|char\|player"
```

**If you see something like:**
```cpp
RegisterFunction("LevelUp", &SomeClass::Lua_LevelUp);
```

→ ✅ **Use Lua approach (VERY EASY!)**

**If nothing found:**
→ Use Remote Function Call approach

---

## Implementation Based on Test Results

### If Test 2 Found Exports: Use DllImport

```csharp
using System;
using System.Runtime.InteropServices;

public class LevelUpCaller
{
    // TODO: Replace with actual export name from Test 2
    [DllImport("Game.exe", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool SendNetworkPacket(ushort packetId, IntPtr data, int size);

    public static bool RequestLevelUp()
    {
        ushort packetId = 0x0142; // TODO: Replace with value from Test 1
        return SendNetworkPacket(packetId, IntPtr.Zero, 0);
    }
}

// Usage:
bool success = LevelUpCaller.RequestLevelUp();
```

**That's it!** No memory hacking, no injection - just call the DLL function!

---

### If Test 3 Found Lua: Use Lua Script

```csharp
using System;
using System.Runtime.InteropServices;

public class LuaLevelUpCaller
{
    // Find lua51.dll or similar in game folder
    [DllImport("lua51.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int luaL_dostring(IntPtr L, string script);

    public static bool RequestLevelUp(int processId)
    {
        // 1. Find lua_State* pointer in game memory
        IntPtr luaState = FindLuaStatePointer(processId);

        // 2. Execute Lua script
        // TODO: Replace "Player:LevelUp()" with actual function name from Test 3
        string script = "Player:LevelUp()";

        return luaL_dostring(luaState, script) == 0;
    }

    private static IntPtr FindLuaStatePointer(int processId)
    {
        // TODO: Scan game memory for lua_State*
        // Usually it's stored in a global variable
        // Search source code for "lua_State*" declarations
        return IntPtr.Zero;
    }
}
```

---

### If Neither Found: Use Network Packet Injection

This is what I already created for you in previous files.

**Use:**
- `GameClientInterface.cs` - Main interface
- `GameNetworkClient.cs` - Network client
- `PacketBuilder.cs` - Packet construction

---

## Recommended Order

1. **Do Test 1** (find packet ID) → Takes 2 minutes
2. **Do Test 2** (check exports) → Takes 2 minutes
   - If found → Use DllImport (done in 10 minutes!)
   - If not found → Continue...
3. **Do Test 3** (check Lua) → Takes 3 minutes
   - If found → Use Lua approach (done in 30 minutes!)
   - If not found → Use memory/network approach

---

## Quick Commands to Run NOW

Open PowerShell and run these:

### Command 1: Find Packet ID
```powershell
Get-ChildItem "G:\SourceCodeGameTLBB\Common" -Recurse -Filter "*Packet*" | Select-String "PACKET_CG_REQLEVELUP" -List
```

### Command 2: Check Exports
```powershell
cd "C:\Path\To\Your\Game\Folder"
& "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Tools\MSVC\*\bin\Hostx64\x64\dumpbin.exe" /EXPORTS Game.exe | Select-String "send|packet|level"
```

### Command 3: Check Lua Functions
```powershell
Get-ChildItem "G:\SourceCodeGameTLBB\Game\Client\WXClient\Interface" -Recurse -Filter "*.cpp" | Select-String "RegisterFunction.*Level" -Context 0,2
```

---

## What I Found for You Already

From source code analysis:

✅ **Packet Name:** `CGReqLevelUp`
✅ **Packet Size:** 0 (empty packet, no parameters!)
✅ **Packet File:** `G:\SourceCodeGameTLBB\Common\Packets\CGReqLevelUp.h`
✅ **Handler File:** `G:\SourceCodeGameTLBB\Game\Client\WXClient\Network\PacketHandler\CGReqLevelUpHandler.cpp`
✅ **Network Manager:** `CNetManager::GetMe()->SendPacket(&packet)`
✅ **Lua Interface:** Game uses LuaPlus (found in `GMInterface_Lua.h`)

**What you still need:**
❓ Packet ID value (from PacketDefine.h)
❓ Whether functions are exported (from dumpbin)
❓ Lua function name (from RegisterFunction calls)

---

## Next Step

**Run Commands 1-3 above** and tell me:
1. What packet ID you found (Command 1)
2. What exports you found, if any (Command 2)
3. What Lua functions you found, if any (Command 3)

Then I'll give you the exact code to copy-paste!

---

## Files I Created for You

1. ✅ `GameClientInterface.cs` - Main interface with 3 methods
2. ✅ `USING_SOURCE_CODE_TO_CALL_FUNCTIONS.md` - Detailed guide
3. ✅ `SOURCE_CODE_QUICK_START.md` - This file

**All ready to use** - just need the values from Commands 1-3!
