# Chat Hook DLL - Test Patterns

## Overview

This directory contains 2 ChatHookDLL versions generated from your Memory Scanner results.

**Memory Scanner Results:**
```
✓ VERIFIED: 0x0048D6F0 (Offset: +0x8D6F0)
✓ VERIFIED: 0x0048D790 (Offset: +0x8D790)
```

Both addresses were verified by the scanner, but you should **test both** to see which one actually hooks the chat function.

---

## Files

| File | Verified Address | Pattern Bytes (First 8) | Log File |
|------|-----------------|-------------------------|----------|
| **ChatHookDLL_Pattern1.cpp** | 0x0048D6F0 | 55-8B-EC-81-EC-**18**-01-00 | C:\DragonOath_ChatLog_Pattern1.txt |
| **ChatHookDLL_Pattern2.cpp** | 0x0048D790 | 55-8B-EC-81-EC-**1C**-01-00 | C:\DragonOath_ChatLog_Pattern2.txt |

**Difference:** Only byte 5 differs (0x18 vs 0x1C - stack allocation size).

---

## Quick Start - Compile & Test

### Prerequisites

1. **Visual Studio 2019+** with C++ tools
2. **Microsoft Detours** installed at `C:\Detours\`
3. **Game.exe** running

### Step 1: Compile Both DLLs

Open **Visual Studio Developer Command Prompt (x86)**:

```batch
cd G:\microauto-6.9\AutoDragonOath\Docs\test-dll

REM Compile Pattern 1
cl /LD /MT /O2 ChatHookDLL_Pattern1.cpp ^
   /I"C:\Detours\include" ^
   /link /LIBPATH:"C:\Detours\lib.X86" detours.lib Psapi.lib ^
   /OUT:ChatHookDLL_Pattern1.dll

REM Compile Pattern 2
cl /LD /MT /O2 ChatHookDLL_Pattern2.cpp ^
   /I"C:\Detours\include" ^
   /link /LIBPATH:"C:\Detours\lib.X86" detours.lib Psapi.lib ^
   /OUT:ChatHookDLL_Pattern2.dll
```

**Expected Output:**
```
ChatHookDLL_Pattern1.dll created
ChatHookDLL_Pattern2.dll created
```

### Step 2: Test Pattern 1 First

```batch
REM Copy injector if you haven't already
copy ..\ChatInjector.cpp .
cl /MT /O2 ChatInjector.cpp /OUT:ChatInjector.exe

REM Inject Pattern 1
ChatInjector.exe ChatHookDLL_Pattern1.dll
```

**Check Result:**
```batch
type C:\DragonOath_ChatLog_Pattern1.txt
```

**Expected Log:**
```
[14:32:15] === ChatHook DLL Loaded (Pattern 1) ===
[14:32:15] === Pattern 1 Scanner ===
[14:32:15] Searching for HandleRecvTalkPacket (Pattern 1: 0x0048D6F0)...
[14:32:15]   Base: 0x00400000, Size: 0x01A3C000
[14:32:15]   Expected offset: +0x8D6F0
[14:32:15]   Found at: 0x0048D6F0
[14:32:15]   Offset from base: +0x8D6F0
[14:32:15] SUCCESS: Hook installed at 0x0048D6F0
```

**Test in Game:**
- Send a chat message
- Check log file again

**If Working:**
```
[14:33:28] [Channel 1] PlayerName: Hello!
[14:33:45] [Channel 2] TeamMate: Let's go!
```

✅ **Pattern 1 works! Use this DLL.**

### Step 3: If Pattern 1 Doesn't Work, Try Pattern 2

```batch
REM First, restart the game to unload Pattern 1
REM Then inject Pattern 2
ChatInjector.exe ChatHookDLL_Pattern2.dll
```

**Check Result:**
```batch
type C:\DragonOath_ChatLog_Pattern2.txt
```

Test the same way as Pattern 1.

---

## Troubleshooting

### Issue: "Pattern not found" in log

**Both patterns fail to find function**

**Possible Causes:**
1. Game was updated since you ran the scanner
2. ASLR changed the base address
3. Scanner found false positives

**Solutions:**
1. Re-run the Memory Scanner in AutoDragonOath
2. Check if new patterns are different
3. Use x64dbg to manually verify the addresses

### Issue: Pattern found but game crashes

**Hook installs but game crashes when chat arrives**

**Possible Causes:**
1. Wrong function signature
2. GCChat structure doesn't match

**Solutions:**
1. Check if function has different parameters
2. Use x64dbg to verify the calling convention
3. Try simpler hook (just log, don't read packet data)

**Minimal test hook:**
```cpp
unsigned int __fastcall Hooked_GCChatHandler_Execute(void* thisPtr, void* edx, GCChat* pPacket, Player* pPlayer) {
    LogToFile("HOOK TRIGGERED!");  // Just log, don't touch packet

    unsigned int result;
    __asm {
        mov ecx, thisPtr
        push pPlayer
        push pPacket
        call Original_GCChatHandler_Execute
        mov result, eax
    }
    return result;
}
```

### Issue: Pattern found but no messages in log

**Hook installed successfully but no chat messages appear**

**Possible Causes:**
1. Wrong channel numbers
2. Packet structure is different
3. Function is not the chat handler

**Solutions:**
1. Try sending chat in different channels (near, team, guild, private)
2. Check if log file has permission issues
3. Verify this is actually the chat function (use x64dbg breakpoint)

### Issue: Which pattern should I use?

**Both patterns verified by scanner**

**Recommendation:**
1. **Try Pattern 1 first** (appears first in scanner results)
2. If Pattern 1 works, use it (no need to test Pattern 2)
3. Only try Pattern 2 if Pattern 1 fails
4. If both fail, re-run scanner to get fresh patterns

**Why 2 patterns?**
- Scanner found 2 functions with similar prologues
- One might be the actual chat handler, the other might be a helper function
- Testing both ensures you find the right one

---

## Pattern Analysis

Both patterns start with:
```
55 8B EC 81 EC ?? 01 00 00  // push ebp; mov ebp, esp; sub esp, 1XXh
```

This is a **standard function prologue** with large stack allocation (>256 bytes).

**Pattern 1:** `sub esp, 118h` (280 bytes)
**Pattern 2:** `sub esp, 11Ch` (284 bytes)

Both are typical for functions that:
- Create local variables
- Call other functions
- Handle complex data structures

This matches the chat handler profile from source code analysis.

---

## Advanced: Creating a Wildcard Pattern

If both patterns work (same function at different times), create a wildcard pattern:

```cpp
BYTE pattern[] = {
    0x55, 0x8B, 0xEC, 0x81, 0xEC, 0xFF, 0x01, 0x00,  // Wildcard at byte 5
    0x00, 0xA1, 0x04, 0x49, 0x64, 0x00, 0x53, 0x8B,
    0x1D, 0x84, 0xA3, 0x5E
};
const char* mask = "xxxxx?xxxxxxxxxxxxxx";  // ? at position 5
```

This will match both 0x18 and 0x1C stack sizes.

---

## Next Steps

After you find the working pattern:

1. **Verify chat logging works** (all channels)
2. **Customize OnChatMessageReceived()** (add your automation logic)
3. **Test keyword detection** (help commands, party invites, etc.)
4. **Find more game functions** (use scanner for each function)
5. **Build complete automation system** (see Example_CustomFunctionCall.cpp)

---

## Success Checklist

- [ ] Both DLLs compiled successfully
- [ ] Pattern 1 tested
- [ ] Pattern 2 tested (if needed)
- [ ] One pattern successfully hooked the function
- [ ] Chat messages appearing in log file
- [ ] All chat channels working (near, team, guild, private)
- [ ] No game crashes
- [ ] Ready to add custom automation logic

---

**Generated:** 2025-01-XX
**Scanner Results:** 2 verified addresses found
**Recommended:** Test Pattern 1 first
