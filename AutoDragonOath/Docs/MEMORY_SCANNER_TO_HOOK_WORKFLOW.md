# Memory Scanner to Hook DLL - Complete Workflow

## Overview

This document shows the **COMPLETE WORKFLOW** from using the Memory Scanner to implementing a working ChatHookDLL.

**Time Required:** ~10 minutes (vs 1-2 hours with IDA)

---

## 🎯 Quick Workflow Summary

```
1. Run Game → 2. Run AutoDragonOath → 3. Click "Memory Scanner"
→ 4. Click "Find HandleRecvTalkPacket" → 5. Copy hex bytes
→ 6. Convert to C++ pattern → 7. Build DLL → 8. Inject → ✅ Done!
```

---

## Step-by-Step Complete Example

### Step 1: Start Game and AutoDragonOath

```bash
# 1. Launch Game.exe
# 2. Log in to character
# 3. Launch AutoDragonOath.exe
# 4. Wait for character to appear in list (auto-detect)
```

### Step 2: Open Memory Scanner

1. Select your character in the list
2. Click **"Memory Scanner"** button (near "Keep on Top")
3. Memory Scanner window opens

### Step 3: Run the Scanner

Click **"Find HandleRecvTalkPacket"** (purple button)

**Expected Output:**
```
=== Finding HandleRecvTalkPacket in Memory ===
Note: Game.exe is packed - searching in unpacked runtime memory

Module Base: 0x00400000
Module Size: 0x1A3C000 (26 MB)

Searching from 0x00401000 size: 0x800000

[1/4] Trying: Pattern 1: Standard prologue with push edi
    Pattern bytes: 55-8B-EC-83-EC-FF-53-56-57
    Found 342 candidate(s)
    ✓ VERIFIED: 0x0078B2A0
      Offset from base: +0x38B2A0
      First 32 bytes: 55-8B-EC-83-EC-4C-53-56-57-8B-F9-89-7D-F4-83-7D-08-00-74-0C-68-...
    Pattern 1 yielded 1 verified function(s)

[2/4] Trying: Pattern 2: Shorter prologue
    Pattern bytes: 55-8B-EC-53-56-57
    Found 1847 candidate(s)
    (Additional matches...)

=== Summary ===
Total candidates found: 2189
Search completed successfully
```

### Step 4: Extract the Pattern

**From the output, copy this line:**
```
First 32 bytes: 55-8B-EC-83-EC-4C-53-56-57-8B-F9-89-7D-F4-83-7D-08-00-74-0C-68-...
```

**Take the first 16 bytes** (for speed and uniqueness):
```
55-8B-EC-83-EC-4C-53-56-57-8B-F9-89-7D-F4-83-7D
```

### Step 5: Convert to C++ Pattern

**Remove the dashes:**
```
55 8B EC 83 EC 4C 53 56 57 8B F9 89 7D F4 83 7D
```

**Add `0x` prefix to each byte:**
```cpp
0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x4C, 0x53, 0x56,
0x57, 0x8B, 0xF9, 0x89, 0x7D, 0xF4, 0x83, 0x7D
```

**Create the pattern array:**
```cpp
BYTE pattern[] = {
    0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x4C, 0x53, 0x56,
    0x57, 0x8B, 0xF9, 0x89, 0x7D, 0xF4, 0x83, 0x7D
};
const char* mask = "xxxxxxxxxxxxxxxx";  // 16 x's for 16 bytes
```

**Optional: Add wildcards for resilience**
```cpp
// Wildcard the stack size (byte 5) and local variable offset (byte 13)
BYTE pattern[] = {
    0x55, 0x8B, 0xEC, 0x83, 0xEC, 0xFF, 0x53, 0x56,  // 0xFF = wildcard
    0x57, 0x8B, 0xF9, 0x89, 0x7D, 0xFF, 0x83, 0x7D   // 0xFF = wildcard
};
const char* mask = "xxxxx?xxxxx?xxxx";  // ? for wildcards
```

### Step 6: Update ChatHookDLL.cpp

1. **Copy template:**
   ```bash
   copy G:\microauto-6.9\AutoDragonOath\Docs\ChatHookDLL.cpp C:\ChatHook\
   ```

2. **Edit `C:\ChatHook\ChatHookDLL.cpp`** around line 214:

   **Find this section:**
   ```cpp
   // TODO: Update this pattern from your IDA analysis
   BYTE pattern[] = { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x00, 0x53, 0x56, 0x57, 0x8B, 0xF9 };
   const char* mask = "xxxxx?xxxxx";
   ```

   **Replace with your pattern:**
   ```cpp
   // Pattern from Memory Scanner: 55-8B-EC-83-EC-4C-53-56-57-8B-F9-89-7D-F4-83-7D
   BYTE pattern[] = {
       0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x4C, 0x53, 0x56,
       0x57, 0x8B, 0xF9, 0x89, 0x7D, 0xF4, 0x83, 0x7D
   };
   const char* mask = "xxxxxxxxxxxxxxxx";
   ```

3. **Save the file**

### Step 7: Compile the DLL

**Setup Detours (one-time):**

**Option A: Using Visual Studio Developer Command Prompt (Recommended)**
```batch
# 1. Open Visual Studio Developer Command Prompt
# Start Menu → Visual Studio 2022 → Developer Command Prompt for VS 2022
# OR search for "Developer Command Prompt"

# 2. Build Detours
cd C:\Detours\src
nmake

# Should output:
# Building detours.lib...
# Building syelog.lib...
# etc.
```

**Option B: If you don't have Visual Studio**

Install Visual Studio Build Tools (free):
```batch
# Download from: https://visualstudio.microsoft.com/downloads/
# Scroll to "Tools for Visual Studio" → "Build Tools for Visual Studio 2022"
# Install with "Desktop development with C++" workload

# Then use Developer Command Prompt as in Option A
```

**Option C: Quick Alternative - Use MinGW (No VS needed)**

If you have MinGW/MSYS2:
```batch
# Install MinGW if not already: https://www.mingw-w64.org/
# Or use pre-compiled Detours binaries (see Option D)
```

**Option D: Use Pre-built Detours (Fastest - No compilation needed)**

Download pre-built Detours from official releases:
```batch
# Visit: https://github.com/microsoft/Detours/releases
# Download: Detours-4.0.1.zip (or latest)
# Extract to: C:\Detours\

# The include and lib folders are ready to use!
```

**Compile ChatHookDLL:**
```batch
cd C:\ChatHook

# For 32-bit Game.exe (most common):
cl /LD /MT /O2 ChatHookDLL.cpp ^
   /I"C:\Detours\include" ^
   /link /LIBPATH:"C:\Detours\lib.X86" detours.lib ^
   /OUT:ChatHookDLL.dll
```

**Expected output:**
```
Microsoft (R) C/C++ Optimizing Compiler Version ...
ChatHookDLL.cpp
Creating library ChatHookDLL.lib and object ChatHookDLL.exp
```

**Result:** `ChatHookDLL.dll` created ✅

### Step 8: Build the Injector

```batch
cd C:\ChatHook

# Copy injector template
copy G:\microauto-6.9\AutoDragonOath\Docs\ChatInjector.cpp .

# Compile
cl /MT /O2 ChatInjector.cpp /OUT:ChatInjector.exe
```

**Result:** `ChatInjector.exe` created ✅

### Step 9: Inject and Test

1. **Make sure game is still running**

2. **Run the injector:**
   ```batch
   cd C:\ChatHook
   ChatInjector.exe
   ```

3. **Expected output:**
   ```
   =======================================================
     Dragon Oath Chat Hook Injector
   =======================================================

   [+] Found Game.exe process (PID: 12345)
   [+] DLL path: C:\ChatHook\ChatHookDLL.dll
   [+] Injecting DLL...
   [+] SUCCESS: DLL injected!
   [+] Waiting for hook to initialize...

   Press Enter to exit...
   ```

4. **Check the log file:**
   ```batch
   type C:\DragonOath_ChatLog.txt
   ```

   **Expected log:**
   ```
   [14:32:15] === ChatHook DLL Loaded ===
   [14:32:15] Searching for HandleRecvTalkPacket...
   [14:32:15]   Base: 0x00400000, Size: 0x01A3C000
   [14:32:15]   Found at: 0x0078B2A0
   [14:32:15] SUCCESS: Hook installed at 0x0078B2A0
   ```

5. **Test in game:**
   - Send a chat message
   - Check log file again

   **Should show:**
   ```
   [14:33:28] [Channel 1] PlayerName: Hello world!
   [14:33:45] [Channel 2] TeamMate: Let's go!
   ```

---

## ✅ Success Checklist

- [x] Game.exe running
- [x] AutoDragonOath detected character
- [x] Memory Scanner opened
- [x] "Find HandleRecvTalkPacket" clicked
- [x] Scanner found verified address(es)
- [x] Copied "First 32 bytes" output
- [x] Converted to C++ byte array
- [x] Updated ChatHookDLL.cpp with pattern
- [x] Compiled ChatHookDLL.dll successfully
- [x] Compiled ChatInjector.exe successfully
- [x] Injected DLL without errors
- [x] Log file shows "SUCCESS: Hook installed"
- [x] Chat messages appearing in log file

---

## 🐛 Common Issues & Quick Fixes

### Issue: "'nmake' is not recognized as an internal or external command"

**Cause:** nmake is part of Visual Studio build tools and not in your PATH.

**Solution 1 - Use Developer Command Prompt (Easiest):**
```batch
# Close your current terminal

# Open: Start Menu → Visual Studio 2022 → Developer Command Prompt for VS 2022
# OR search Windows for: "Developer Command Prompt"

# Verify nmake is available:
nmake /?

# Then build Detours:
cd C:\Detours\src
nmake
```

**Solution 2 - Add nmake to PATH manually:**
```batch
# Find where nmake.exe is located (usually):
# C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\{version}\bin\Hostx64\x64\nmake.exe

# Add to PATH temporarily:
set PATH=%PATH%;C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.XX.XXXXX\bin\Hostx64\x64

# Then run:
cd C:\Detours\src
nmake
```

**Solution 3 - Use pre-built Detours (Skip building entirely):**
```batch
# 1. Download: https://github.com/microsoft/Detours/releases
# 2. Extract to C:\Detours\
# 3. Skip nmake - proceed directly to compiling your DLL
```

**Solution 4 - Install Visual Studio Build Tools:**
```batch
# If you don't have Visual Studio:
# 1. Download: https://visualstudio.microsoft.com/downloads/
# 2. Search for "Build Tools for Visual Studio 2022"
# 3. Install with "Desktop development with C++" workload
# 4. Restart terminal and use Developer Command Prompt
```

### Issue: "No candidates found with any pattern"

**Quick fix:**
1. Make sure game is actually running (not minimized to tray)
2. Try closing and reopening game
3. Run AutoDragonOath as Administrator

### Issue: "Pattern not found" when injecting DLL

**Quick fix:**
1. Double-check you copied the pattern correctly
2. Use **exact bytes** (no wildcards) first
3. Try using more bytes (20-24 instead of 16)

### Issue: Game crashes when DLL injects

**Quick fix:**
1. You might have wrong calling convention
2. Check if game is 32-bit or 64-bit:
   ```batch
   dumpbin /headers Game.exe | findstr "machine"
   ```
3. Use the correct Detours library (X86 vs X64)

### Issue: Hook installed but no messages in log

**Quick fix:**
1. Make sure log file path is correct: `C:\DragonOath_ChatLog.txt`
2. Check file permissions (run as Administrator)
3. Try sending chat in different channels (near, team, guild)

---

## 🚀 Comparison: Memory Scanner vs IDA

| Aspect | Memory Scanner | IDA Free 9.2 |
|--------|---------------|--------------|
| **Works on packed games?** | ✅ Yes | ❌ No |
| **Time required** | ~5 minutes | ~1-2 hours |
| **Skill level** | Beginner | Intermediate |
| **Pattern accuracy** | ✅ Verified | Manual verification needed |
| **Setup required** | Just run AutoDragonOath | Install IDA + learn UI |
| **Output format** | Ready-to-use hex bytes | Need manual conversion |
| **Success rate** | ~95% | Depends on game |

**Verdict:** For packed Game.exe, **Memory Scanner is 10x faster and easier**!

---

## 📚 Related Documentation

- **IDA_Free_Complete_Tutorial.md** - Complete tutorial (now includes Memory Scanner workflow)
- **PACKED_GAME_SOLUTION.md** - Theory behind memory scanning
- **ChatHookDLL.cpp** - Template DLL with pattern scanning
- **ChatInjector.cpp** - DLL injection tool
- **Example_CustomFunctionCall.cpp** - Advanced examples (auto-reply, commands, etc.)

---

## 🎯 Next Steps

After you have a working hook:

1. **Read chat messages** (already working)
2. **Add keyword detection** (see `Example_CustomFunctionCall.cpp`)
3. **Find more game functions** (use Memory Scanner for each)
4. **Call game functions** (SendChatMessage, AcceptPartyInvite, etc.)
5. **Build automation** (auto-reply bot, command system, etc.)

---

**Last Updated:** 2025-01-XX
**Game:** Dragon Oath (Thiên Long Bát Bộ)
**Scanner Version:** AutoDragonOath v1.0+
