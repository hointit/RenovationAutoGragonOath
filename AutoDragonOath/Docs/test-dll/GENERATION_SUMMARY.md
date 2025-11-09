# Generation Summary - ChatHookDLL Test Patterns

## Memory Scanner Results

Your scan found 2 verified addresses for HandleRecvTalkPacket:

### Pattern 1
- **Address:** 0x0048D6F0
- **Offset:** +0x8D6F0
- **Hex Bytes:** `55-8B-EC-81-EC-18-01-00-00-A1-04-49-64-00-53-8B-1D-84-A3-5E-00-56-57-89-45-FC-8B-D1-C6-85-E8-FE`

### Pattern 2
- **Address:** 0x0048D790
- **Offset:** +0x8D790
- **Hex Bytes:** `55-8B-EC-81-EC-1C-01-00-00-A1-04-49-64-00-53-8B-1D-84-A3-5E-00-56-57-8B-F1-89-45-FC-33-C0-C6-85`

### Key Difference
Only byte 5 differs:
- Pattern 1: `0x18` (stack size: 280 bytes)
- Pattern 2: `0x1C` (stack size: 284 bytes)

---

## Generated Files

### Source Code
1. **ChatHookDLL_Pattern1.cpp** - Hook DLL using Pattern 1
2. **ChatHookDLL_Pattern2.cpp** - Hook DLL using Pattern 2

### Batch Scripts
3. **compile_all.bat** - Compiles both DLLs and injector
4. **test_pattern1.bat** - Quick test for Pattern 1
5. **test_pattern2.bat** - Quick test for Pattern 2

### Documentation
6. **README.md** - Complete usage guide
7. **GENERATION_SUMMARY.md** - This file

---

## Quick Usage Guide

### Method 1: Automated Testing (Easiest)

1. **Open Visual Studio Developer Command Prompt (x86)**
   - Search in Start Menu: "Developer Command Prompt for VS 2019"
   - Or: "x86 Native Tools Command Prompt"

2. **Navigate to directory:**
   ```batch
   cd G:\microauto-6.9\AutoDragonOath\Docs\test-dll
   ```

3. **Compile everything:**
   ```batch
   compile_all.bat
   ```

4. **Start Game.exe** (if not already running)

5. **Test Pattern 1:**
   ```batch
   test_pattern1.bat
   ```

6. **If Pattern 1 doesn't work, restart game and test Pattern 2:**
   ```batch
   test_pattern2.bat
   ```

### Method 2: Manual Compilation

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

REM Test
ChatInjector.exe ChatHookDLL_Pattern1.dll
```

---

## Expected Results

### Successful Hook Installation

**Log file:** `C:\DragonOath_ChatLog_Pattern1.txt` or `C:\DragonOath_ChatLog_Pattern2.txt`

```
[14:32:15] === ChatHook DLL Loaded (Pattern X) ===
[14:32:15] === Pattern X Scanner ===
[14:32:15] Searching for HandleRecvTalkPacket (Pattern X: 0x0048DXXX)...
[14:32:15]   Base: 0x00400000, Size: 0x01A3C000
[14:32:15]   Expected offset: +0x8DXXX
[14:32:15]   Found at: 0x0048DXXX
[14:32:15]   Offset from base: +0x8DXXX
[14:32:15] SUCCESS: Hook installed at 0x0048DXXX
```

**After sending chat:**
```
[14:33:28] [Channel 1] YourName: Hello world!
[14:33:45] [Channel 2] TeamMate: Let's go!
[14:34:01] [Channel 3] GuildMember: Guild meeting tonight
```

### Pattern Not Found

```
[14:32:15] === ChatHook DLL Loaded (Pattern X) ===
[14:32:15] === Pattern X Scanner ===
[14:32:15] Searching for HandleRecvTalkPacket (Pattern X: 0x0048DXXX)...
[14:32:15]   Base: 0x00400000, Size: 0x01A3C000
[14:32:15]   Expected offset: +0x8DXXX
[14:32:15]   NOT FOUND - Pattern mismatch or game updated!
[14:32:15] ERROR: Could not find HandleRecvTalkPacket
```

**Solution:** Try the other pattern or re-run Memory Scanner.

---

## Pattern Analysis

Both patterns show typical function prologue with large stack allocation:

```asm
55                      ; push ebp
8B EC                   ; mov ebp, esp
81 EC 18 01 00 00       ; sub esp, 118h  (Pattern 1: 280 bytes)
   OR
81 EC 1C 01 00 00       ; sub esp, 11Ch  (Pattern 2: 284 bytes)
A1 04 49 64 00          ; mov eax, [security_cookie]
53                      ; push ebx
8B 1D 84 A3 5E 00       ; mov ebx, [global_ptr]
56                      ; push esi
57                      ; push edi
...
```

This matches characteristics of:
- Chat message handler
- Network packet processor
- Large data structure manipulation

**Why large stack?**
- Local string buffers (sender name, message text)
- Temporary variables
- Function call parameters
- Security cookie check

---

## Which Pattern to Use?

### Test Order (Recommended)
1. **Try Pattern 1 first** - appears first in scanner results
2. If Pattern 1 works → **use it** (no need to test Pattern 2)
3. If Pattern 1 fails → try Pattern 2
4. If both fail → re-run scanner (game may have updated)

### Why Two Patterns?
The scanner found two functions with similar prologues. Possibilities:

1. **Both are valid** (same function, different times) → Either works
2. **One is correct, one is helper function** → Test to find out
3. **One handles send, one handles receive** → You want the receive one

**Most likely:** Pattern 1 is the actual chat handler, Pattern 2 is a related function.

---

## Troubleshooting

### Both patterns fail to find

**Possible causes:**
1. Game updated since you ran the scanner
2. ASLR randomized the base address differently
3. Scanner found false positives

**Solutions:**
1. Re-run Memory Scanner in AutoDragonOath
2. Compare old and new hex patterns
3. Use x64dbg to manually verify addresses

### Pattern found but game crashes

**Possible causes:**
1. Wrong calling convention
2. Packet structure mismatch
3. Hook is in wrong function

**Solutions:**
1. Use minimal hook (just log, no packet access)
2. Verify with x64dbg debugger
3. Try the other pattern

### Hook works but no messages

**Possible causes:**
1. Function is not the chat handler
2. Channel filtering issue
3. Packet extraction failing

**Solutions:**
1. Test all chat channels (near, team, guild, private)
2. Add debug logging in packet extraction
3. Verify with x64dbg that function is called on chat

---

## Customization

After finding the working pattern, you can customize `OnChatMessageReceived()`:

```cpp
void OnChatMessageReceived(const char* senderName, const char* messageText, unsigned char channelType) {
    // Your custom logic here

    // Example: Auto-reply to keywords
    if (strstr(messageText, "!help") != NULL) {
        // Trigger help response
        LogToFile("  -> Help command detected from %s", senderName);
    }

    // Example: Party invite detection
    if (strstr(messageText, "邀请你加入队伍") != NULL) {
        LogToFile("  -> Party invite from %s", senderName);
        // Auto-accept logic here
    }

    // Example: Filter by channel
    if (channelType == 3) {  // Guild chat
        LogToFile("  -> Guild message: %s", messageText);
    }
}
```

See `Example_CustomFunctionCall.cpp` for advanced examples.

---

## Success Indicators

✅ **Hook is working if:**
- Log shows "SUCCESS: Hook installed"
- Chat messages appear in log file
- All channels work (near, team, guild, private)
- No game crashes
- Messages show correct sender names
- Message text is readable

❌ **Hook is NOT working if:**
- Log shows "NOT FOUND - Pattern mismatch"
- No messages appear after sending chat
- Game crashes on injection
- Garbage data in log file
- Only system messages appear

---

## Next Steps

After successful hooking:

1. ✅ **Verify all channels work** - test near, team, guild, private chat
2. ✅ **Add keyword detection** - detect commands, help requests
3. ✅ **Find more functions** - use Memory Scanner for SendChatMessage, AcceptPartyInvite, etc.
4. ✅ **Build automation** - auto-reply, command system, party management
5. ✅ **Integrate with AutoDragonOath** - connect hook to your WPF app

---

## File Manifest

```
G:\microauto-6.9\AutoDragonOath\Docs\test-dll\
├── ChatHookDLL_Pattern1.cpp      (355 lines)
├── ChatHookDLL_Pattern2.cpp      (355 lines)
├── compile_all.bat                (Automated compilation)
├── test_pattern1.bat              (Automated testing)
├── test_pattern2.bat              (Automated testing)
├── README.md                      (Detailed usage guide)
└── GENERATION_SUMMARY.md          (This file)

After compilation:
├── ChatHookDLL_Pattern1.dll
├── ChatHookDLL_Pattern2.dll
├── ChatInjector.exe
└── *.obj, *.lib, *.exp (intermediate files)
```

---

**Generated:** 2025-01-XX
**Source:** AutoDragonOath Memory Scanner
**Addresses:** 0x0048D6F0 (+0x8D6F0), 0x0048D790 (+0x8D790)
**Recommendation:** Test Pattern 1 first
