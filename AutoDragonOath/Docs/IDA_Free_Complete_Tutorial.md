# Complete IDA Free 9.2 Tutorial - Find & Hook HandleRecvTalkPacket

## 📋 Table of Contents

1. [Prerequisites](#prerequisites)
2. [🚀 RECOMMENDED: Using Memory Scanner (Packed Game)](#recommended-using-memory-scanner-packed-game)
3. [Part 1: Finding the Function in IDA](#part-1-finding-the-function-in-ida)
4. [Part 2: Creating a Signature Pattern](#part-2-creating-a-signature-pattern)
5. [Part 3: Building the Hook DLL](#part-3-building-the-hook-dll)
6. [Part 4: Injecting and Testing](#part-4-injecting-and-testing)
7. [Part 5: Calling Custom Functions](#part-5-calling-custom-functions)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

**Required Software:**
- ✅ IDA Free 9.2: https://hex-rays.com/ida-free/
- ✅ Visual Studio 2019 or later (Community Edition is free)
- ✅ Microsoft Detours: https://github.com/microsoft/Detours
- ✅ x64dbg (optional, for debugging): https://x64dbg.com/

**Game Files:**
- ✅ Game.exe from `G:\SourceCodeGameTLBB\Game\Client\bin\`
- ✅ Running game process for testing

**Knowledge Required:**
- ⚠️ Basic understanding of assembly (x86)
- ⚠️ Basic C/C++ programming
- ⚠️ Administrator access to your PC

---

## ⚠️ CRITICAL: Which File to Analyze?

**IMPORTANT:** `GCChatHandler::Execute` and `HandleRecvTalkPacket` are in the **MAIN EXECUTABLE**, not a DLL!

Based on source code investigation (`WXClient.vcxproj`):
- ConfigurationType: **Application** (not DynamicLibrary)
- Build output: **WXClient.exe** → renamed to **Game.exe** in distribution
- **All chat functions are compiled into the main executable**

**What this means:**
- ✅ Open `Game.exe` in IDA Free 9.2
- ❌ Do NOT look for chat.dll, network.dll, or other DLLs
- ✅ All functions are in ONE file: Game.exe

📖 **See `MODULE_LOCATION_FINDINGS.md` for detailed evidence.**

---

## 🚀 RECOMMENDED: Using Memory Scanner (Packed Game)

### ⚠️ Important: Game.exe is PACKED!

If you tried to open Game.exe in IDA Free 9.2, you probably noticed:
- ❌ Very few functions found (only 1-2 in 467K lines)
- ❌ Most sections show only data (`dd` declarations)
- ❌ `.aspack` section indicates the game is packed/encrypted

**This means:**
- The actual game code is compressed and encrypted on disk
- IDA cannot disassemble packed code
- String searches won't work
- **Solution: Use the runtime memory scanner instead!**

### Step-by-Step: Using AutoDragonOath Memory Scanner

This is the **FASTEST and EASIEST** way to find HandleRecvTalkPacket in a packed game.

#### Step 1: Start the Game

1. Launch Game.exe normally
2. Log in to a character
3. Leave the game running in the background

#### Step 2: Open AutoDragonOath

1. Launch `AutoDragonOath.exe`
2. Wait for it to detect your game character (auto-refresh every 2 seconds)
3. Your character should appear in the list with HP/MP/coordinates

#### Step 3: Open Memory Scanner

1. **Select your character** in the list
2. Click the **"Memory Scanner"** button (near "Keep on Top")
3. A new window opens showing memory scanner tools

#### Step 4: Run the Scanner

1. In the Memory Scanner window, click **"Find HandleRecvTalkPacket"** (purple button)
2. The scanner will:
   - Search unpacked game memory (8MB range)
   - Try 4 different function prologue patterns
   - Verify candidates using characteristic code patterns
   - Display all verified addresses

#### Step 5: Interpret the Results

**Example output:**
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

=== Summary ===
Total candidates found: 342
Search completed successfully
```

**What this means:**
- ✅ **Address found:** `0x0078B2A0` (absolute memory address)
- ✅ **Offset from base:** `+0x38B2A0` (relative to module base)
- ✅ **First 32 bytes:** The hex pattern you need for hooking

#### Step 6: Extract Pattern for Hook DLL

From the scanner output, copy the **"First 32 bytes"** line:
```
55-8B-EC-83-EC-4C-53-56-57-8B-F9-89-7D-F4-83-7D-08-00-74-0C-68-...
```

**Convert to C++ byte array:**
```cpp
// Full exact pattern (from scanner)
BYTE pattern[] = {
    0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x4C, 0x53, 0x56,
    0x57, 0x8B, 0xF9, 0x89, 0x7D, 0xF4, 0x83, 0x7D
};
const char* mask = "xxxxxxxxxxxxxxxx";  // All exact matches
```

**Or use wildcards for resilience:**
```cpp
// Wildcarded pattern (more resilient to updates)
BYTE pattern[] = {
    0x55, 0x8B, 0xEC, 0x83, 0xEC, 0xFF, 0x53, 0x56,
    0x57, 0x8B, 0xF9, 0x89, 0x7D, 0xFF, 0x83, 0x7D
};
const char* mask = "xxxxx?xxxxx?xxxx";
// Wildcards at positions: 5 (stack size), 13 (local variable offset)
```

#### Step 7: Skip to Part 3 (Building the Hook)

**You can now skip Part 1 and Part 2!**
- ✅ You already have the function address pattern
- ✅ You already have verified it's correct (scanner verified it)
- ➡️ Go directly to [Part 3: Building the Hook DLL](#part-3-building-the-hook-dll)

In Part 3, use the pattern from Step 6 in your `ChatHookDLL.cpp`.

---

### Why Memory Scanner is Better for Packed Games

| Method | Packed Game | Pros | Cons |
|--------|-------------|------|------|
| **Memory Scanner** | ✅ Works | Fast (< 1 min), No IDA needed, Automatic verification | Requires game to be running |
| **IDA Free 9.2** | ❌ Fails | Can analyze dead code | Cannot disassemble packed executables |
| **x64dbg + Manual** | ✅ Works | Very accurate | Slow, Requires debugging knowledge |
| **Unpacking Tools** | ⚠️ Complex | Can work | Risky, May trigger anti-cheat, Time-consuming |

**Verdict:** For packed Game.exe, **always use the Memory Scanner first!**

---

## Part 1: Finding the Function in IDA

**⚠️ WARNING:** This section assumes Game.exe is **NOT packed**. If it's packed (ASPack, UPX, etc.), use the [Memory Scanner](#recommended-using-memory-scanner-packed-game) instead!

**When to use this section:**
- ✅ You have an unpacked Game.exe
- ✅ IDA successfully disassembled the code (many functions found)
- ✅ You want to learn reverse engineering with IDA

**When NOT to use this section:**
- ❌ Game.exe is packed (IDA shows only data, no functions)
- ❌ You want quick results (use Memory Scanner instead)

### Step 1.1: Load Game.exe into IDA Free

1. **Open IDA Free 9.2**
   - Launch IDA from Start menu or desktop

2. **Load Game.exe**
   - Click `New` in the welcome screen
   - Or: File → New → Select `Game.exe`
   - Browse to: `G:\SourceCodeGameTLBB\Game\Client\bin\Game.exe`

3. **Wait for Analysis**
   - IDA will ask: "This file is for 80386 architecture. Load anyway?"
   - Click `Yes`
   - Wait for auto-analysis to complete (may take 2-5 minutes)
   - Status bar will show "AU: idle" when complete

### Step 1.2: Search for String References

Since we know from source code that `HandleRecvTalkPacket` is called, we'll search for related strings.

**Method A: Search for Direct String (May Not Work)**

1. Press `Shift + F12` to open "Strings window"
2. In the Strings window, press `Ctrl + F`
3. Search for: `HandleRecvTalkPacket`
4. If found: Double-click to jump to that string
5. Press `X` to see cross-references (where it's used)

**Method B: Search for Related Strings (Recommended)**

Since `HandleRecvTalkPacket` might not be directly in strings, search for chat-related text:

1. Press `Shift + F12` (Strings window)
2. Press `Ctrl + F`
3. Search for these (try each one):
   ```
   - "Talk"
   - "Chat"
   - "Message"
   - "Channel"
   - Any Chinese text you see in game chat
   ```

4. When you find a relevant string:
   - Double-click to view it
   - Press `X` to see cross-references
   - This shows functions that use this string

### Step 1.3: Identify HandleRecvTalkPacket

From the source code, we know:
```cpp
INT Talk::HandleRecvTalkPacket(GCChat *pPacket)
```

**Signature characteristics:**
- Takes 1 parameter (GCChat* pPacket)
- Returns INT
- Called from `GCChatHandler::Execute`
- Accesses `Talk::s_Talk` singleton

**Method A: Find via GCChatHandler::Execute**

1. In Functions window (`Shift + F3`), search for functions
2. Look for patterns in function names or addresses
3. Right-click a function → `Rename` to organize

**Method B: Pattern Matching (More Reliable)**

We'll create a signature from what we know:

From `GCChatHandler.cpp:18`:
```cpp
SCRIPT_SANDBOX::Talk::s_Talk.HandleRecvTalkPacket(pPacket);
```

This translates to assembly like:
```asm
push    [pPacket]              ; Push packet pointer
mov     ecx, [Talk::s_Talk]    ; Load singleton into ECX (this pointer)
call    HandleRecvTalkPacket   ; Call the function
```

### Step 1.4: Locate the Function Manually

**Step-by-step navigation:**

1. **Find GCChatHandler::Execute first:**

   From source code (`GCChatHandler.cpp:12`):
   ```cpp
   uint GCChatHandler::Execute( GCChat* pPacket, Player* pPlayer )
   ```

   Look for:
   - Function with 2 parameters
   - Returns uint
   - Has a call to another function near the start

2. **Look for the call instruction:**

   In IDA's disassembly view (main window):
   - Scroll through functions
   - Look for pattern:
     ```asm
     push    ebp
     mov     ebp, esp
     sub     esp, XX
     ...
     mov     ecx, dword_XXXXXXXX    ; Loading Talk::s_Talk
     push    [ebp+pPacket]
     call    sub_XXXXXXXX           ; This is HandleRecvTalkPacket!
     ```

3. **When you find a suspicious call:**
   - Click on the `call sub_XXXXXXXX` line
   - Press `Enter` or double-click to jump into that function
   - Check if it looks like HandleRecvTalkPacket

### Step 1.5: Verify It's the Right Function

From source code, `HandleRecvTalkPacket` has these characteristics:

1. **Parameter checks:**
   ```asm
   cmp     [ebp+pPacket], 0    ; Check if pPacket is NULL
   jnz     short loc_XXXXX
   ; Call TDAssert(FALSE) or similar
   ```

2. **Blacklist check:**
   ```asm
   ; Calls CDataPool::GetMe()->GetRelation()->IsBlackName()
   call    sub_XXXXX
   test    al, al
   jz      short loc_XXXXX
   xor     eax, eax
   ret
   ```

3. **Creates HistoryMsg:**
   ```asm
   ; Local variable allocation for HistoryMsg
   sub     esp, 14h or similar
   ```

4. **Returns 0 or -1:**
   ```asm
   xor     eax, eax    ; return 0
   ret
   ; OR
   mov     eax, -1     ; return -1
   ret
   ```

**If you see these patterns, you found it!**

### Step 1.6: Note the Function Address

Once you've identified HandleRecvTalkPacket:

1. **Click on the function name** in disassembly
2. **Look at the address** (shown in left column or status bar)
   - Example: `sub_00789A20` means address is `0x00789A20`
3. **Write it down:**
   ```
   HandleRecvTalkPacket address: 0x00789A20
   ```

4. **Rename the function** (optional but helpful):
   - Press `N` key while cursor is on function name
   - Type: `HandleRecvTalkPacket`
   - Press Enter
   - Now it's easier to find later

---

## Part 2: Creating a Signature Pattern

A signature pattern allows us to find the function even after game updates (as long as the code doesn't change too much).

### 🚀 Alternative: Use Memory Scanner Output (FASTER)

**If you already used the Memory Scanner** (see [Part 0](#recommended-using-memory-scanner-packed-game)), you can skip this entire Part 2!

The scanner already gave you:
```
First 32 bytes: 55-8B-EC-83-EC-4C-53-56-57-8B-F9-89-7D-F4-83-7D-08-00-74-0C-68-...
```

**Just convert it to C++ pattern** (see Step 6 in Part 0) and skip to [Part 3](#part-3-building-the-hook-dll).

---

### Traditional Method: Manual Pattern Creation from IDA

**Use this method only if:**
- You have unpacked Game.exe
- You're analyzing with IDA Free 9.2
- You want to learn reverse engineering

### Step 2.1: Understand the Function Prologue

Most functions start with a standard prologue:

```asm
push    ebp                 ; 55
mov     ebp, esp            ; 8B EC
sub     esp, XXh            ; 83 EC XX  (XX varies)
push    ebx                 ; 53
push    esi                 ; 56
push    edi                 ; 57
```

This is a very common pattern. We need unique bytes.

### Step 2.2: Find Unique Bytes

1. **In IDA, with HandleRecvTalkPacket open:**

2. **Switch to Hex View:**
   - Click `View` menu → `Open subviews` → `Hex dump`
   - Or press `Alt + T`

3. **Look at the function start:**
   - Position cursor at start of function
   - Note the hex bytes on the right side
   - Example:
     ```
     Address   Hex bytes                    Assembly
     00789A20  55                           push    ebp
     00789A21  8B EC                        mov     ebp, esp
     00789A23  83 EC 4C                     sub     esp, 4Ch
     00789A26  53                           push    ebx
     00789A27  56                           push    esi
     00789A28  57                           push    edi
     00789A29  8B F9                        mov     edi, ecx
     00789A2B  89 7D F4                     mov     [ebp-0Ch], edi
     00789A2E  83 7D 08 00                  cmp     [ebp+8], 0
     ```

4. **Copy first 16-20 bytes:**
   ```
   55 8B EC 83 EC 4C 53 56 57 8B F9 89 7D F4 83 7D 08 00
   ```

### Step 2.3: Create Wildcard Pattern

Some bytes change between runs (like immediate values), so we use wildcards:

**Original bytes:**
```
55 8B EC 83 EC 4C 53 56 57 8B F9 89 7D F4 83 7D 08 00
```

**Identify wildcards:**
- `4C` in `sub esp, 4Ch` might change → make it `??`
- `F4` in `mov [ebp-0Ch], edi` might change → make it `??`
- `08` in `cmp [ebp+8], 0` is parameter offset → keep it

**Final pattern:**
```
55 8B EC 83 EC ?? 53 56 57 8B F9 89 7D ?? 83 7D 08 00
```

**Mask string:**
```
xxxxx?xxxxx?xxx
```

Where:
- `x` = exact match
- `?` = wildcard (any byte)

### Step 2.4: Verify Pattern Uniqueness

1. **In IDA, use Search → Sequence of bytes:**
   - Press `Alt + B`
   - Enter your pattern: `55 8B EC 83 EC ?? 53 56 57 8B F9`
   - Use space for wildcard: `55 8B EC 83 EC    53 56 57 8B F9`
   - Click `Find`

2. **Check results:**
   - Should find only 1-2 matches
   - If too many matches: Add more unique bytes
   - If no matches: Check your pattern again

3. **Refine if needed:**
   - Add more bytes from the function
   - Or use later bytes that are more unique

---

## Part 3: Building the Hook DLL

### Step 3.1: Install Microsoft Detours

**Download and Build:**

```batch
# Open Visual Studio Developer Command Prompt (search in Start menu)
cd C:\
git clone https://github.com/microsoft/Detours.git
cd Detours\src
nmake
```

**Verify installation:**
```batch
dir C:\Detours\lib\*.lib
# Should see: detours.lib
```

### Step 3.2: Create the Hook Project

1. **Create project folder:**
   ```batch
   mkdir C:\ChatHook
   cd C:\ChatHook
   ```

2. **Copy the template DLL code:**
   - Copy `ChatHookDLL.cpp` from `G:\microauto-6.9\AutoDragonOath\Docs\`
   - Save to `C:\ChatHook\ChatHookDLL.cpp`

3. **Update the pattern:**

   Edit `ChatHookDLL.cpp` around line 214:

   **Option A: From Memory Scanner (Recommended)**

   If you used the Memory Scanner from [Part 0](#recommended-using-memory-scanner-packed-game):

   ```cpp
   // Pattern from Memory Scanner output:
   // "First 32 bytes: 55-8B-EC-83-EC-4C-53-56-57-8B-F9-89-7D-F4-83-7D-08-00"

   // Exact pattern (use first 16 bytes for speed):
   BYTE pattern[] = {
       0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x4C, 0x53, 0x56,
       0x57, 0x8B, 0xF9, 0x89, 0x7D, 0xF4, 0x83, 0x7D
   };
   const char* mask = "xxxxxxxxxxxxxxxx";

   // OR wildcarded for resilience:
   BYTE pattern[] = {
       0x55, 0x8B, 0xEC, 0x83, 0xEC, 0xFF, 0x53, 0x56,
       0x57, 0x8B, 0xF9, 0x89, 0x7D, 0xFF, 0x83, 0x7D
   };
   const char* mask = "xxxxx?xxxxx?xxxx";
   ```

   **Option B: From IDA Free 9.2 (Traditional)**

   If you manually analyzed with IDA:

   ```cpp
   // OLD (template):
   BYTE pattern[] = { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x00, 0x53 };
   const char* mask = "xxxxx?x";

   // NEW (your pattern from IDA):
   BYTE pattern[] = { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x??, 0x53, 0x56, 0x57, 0x8B, 0xF9, 0x89, 0x7D, 0x?? };
   const char* mask = "xxxxx?xxxxx?xx";
   ```

   **How to convert scanner hex output to C++ array:**

   Scanner output:
   ```
   55-8B-EC-83-EC-4C-53-56-57
   ```

   Convert each hex pair to `0x` format:
   ```cpp
   55    → 0x55
   8B    → 0x8B
   EC    → 0x83
   ...
   ```

   Result:
   ```cpp
   BYTE pattern[] = { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x4C, 0x53, 0x56, 0x57 };
   ```

### Step 3.3: Compile the DLL

**Using Visual Studio:**

1. Open Visual Studio 2019+
2. Create new project: `File` → `New` → `Project`
3. Choose: `Dynamic-Link Library (DLL)`
4. Name: `ChatHookDLL`
5. Add existing item: `ChatHookDLL.cpp`
6. Configure properties:
   - Right-click project → `Properties`
   - `Configuration Properties` → `C/C++` → `General`
   - `Additional Include Directories`: `C:\Detours\include`
   - `Configuration Properties` → `Linker` → `General`
   - `Additional Library Directories`: `C:\Detours\lib.X86`
   - `Configuration Properties` → `Linker` → `Input`
   - `Additional Dependencies`: Add `detours.lib`
7. Build: `Build` → `Build Solution` (F7)

**Using Command Line (Faster):**

```batch
cd C:\ChatHook

# For 32-bit Game.exe (most likely):
cl /LD /MT /O2 ChatHookDLL.cpp ^
   /I"C:\Detours\include" ^
   /link /LIBPATH:"C:\Detours\lib.X86" detours.lib ^
   /OUT:ChatHookDLL.dll

# For 64-bit Game.exe:
cl /LD /MT /O2 ChatHookDLL.cpp ^
   /I"C:\Detours\include" ^
   /link /LIBPATH:"C:\Detours\lib.X64" detours.lib ^
   /OUT:ChatHookDLL.dll
```

**Result:**
```
ChatHookDLL.dll created in C:\ChatHook\
```

### Step 3.4: Build the Injector

```batch
cd C:\ChatHook

# Copy the injector code
copy G:\microauto-6.9\AutoDragonOath\Docs\ChatInjector.cpp .

# Compile
cl /MT /O2 ChatInjector.cpp /OUT:ChatInjector.exe
```

---

## Part 4: Injecting and Testing

### Step 4.1: Prepare for Testing

1. **Create test environment:**
   ```batch
   C:\ChatHook\
   ├── ChatHookDLL.dll       (Your compiled DLL)
   ├── ChatInjector.exe      (Your compiled injector)
   └── detours.dll           (Copy from C:\Detours\bin.X86\)
   ```

2. **Start the game:**
   - Launch Dragon Oath normally
   - Log in to a character
   - Go to a populated area (so you can see chat)

3. **Find the process ID:**
   ```batch
   tasklist | findstr Game.exe
   ```
   Output:
   ```
   Game.exe                  12345 Console                 1    123,456 K
   ```
   Note the PID: `12345`

### Step 4.2: Inject the DLL

**Method A: Using ChatInjector.exe**

```batch
cd C:\ChatHook
ChatInjector.exe
```

Expected output:
```
=======================================================
  Dragon Oath Chat Hook Injector
=======================================================

[+] Found DLL: C:\ChatHook\ChatHookDLL.dll
[*] Searching for Game.exe...
[+] Found Game.exe (PID: 12345)

[*] Starting injection...
  Target PID: 12345
  DLL Path: C:\ChatHook\ChatHookDLL.dll
[+] Process opened
[+] Allocated memory at 0x12340000
[+] Wrote DLL path (33 bytes)
[+] LoadLibraryA at 0x76D51234
[+] Remote thread created
[*] Waiting for DLL to load...
[+] DLL loaded successfully! (Module handle: 0x10000000)

========================================
  INJECTION SUCCESSFUL!
========================================
  Chat messages will be logged to:
  C:\DragonOath_ChatLog.txt
========================================
```

**Method B: Manual injection (for debugging)**

1. Open x64dbg
2. Attach to Game.exe
3. Plugins → Scylla → DLL Inject
4. Select ChatHookDLL.dll
5. Inject

### Step 4.3: Verify the Hook

1. **Check the log file:**
   ```batch
   type C:\DragonOath_ChatLog.txt
   ```

   Expected output:
   ```
   [14:32:15] === ChatHook DLL Loaded ===
   [14:32:15] Searching for GCChatHandler::Execute...
   [14:32:15]   Base: 0x00400000, Size: 0x01A3C000
   [14:32:15]   Found at: 0x00789A20
   [14:32:15] SUCCESS: Hook installed at 0x00789A20
   ```

2. **In game, send a chat message:**
   - Type in chat: "Test message"
   - Press Enter

3. **Check the log again:**
   ```batch
   type C:\DragonOath_ChatLog.txt
   ```

   Should now show:
   ```
   [14:33:01] [Channel 1] YourCharacterName: Test message
   ```

**If you see the message, SUCCESS! 🎉**

### Step 4.4: Monitor Real-Time

Keep the log file open in real-time:

**Windows PowerShell:**
```powershell
Get-Content C:\DragonOath_ChatLog.txt -Wait
```

**Or use a tool:**
- Notepad++ with Document Monitor plugin
- Tail for Windows: https://github.com/tailtools/tail

---

## Part 5: Calling Custom Functions

Now that you can intercept chat, let's call custom functions when specific messages arrive.

### Step 5.1: Simple Example - Auto Reply

Edit `ChatHookDLL.cpp`, modify `OnChatMessageReceived`:

```cpp
void OnChatMessageReceived(const char* senderName, const char* messageText, unsigned char channelType) {
    LogToFile("[Channel %d] %s: %s", channelType, senderName, messageText);

    // Example: Auto-reply when someone says "help"
    if (strstr(messageText, "help") != NULL || strstr(messageText, "帮助") != NULL) {
        LogToFile("  -> Help request detected from %s!", senderName);

        // Call game's send chat function
        SendChatResponse(senderName, "I can help you!");
    }
}
```

### Step 5.2: Finding Game Functions to Call

To send a chat message back, we need to find the game's send function.

**In IDA:**

1. From source code, we know there's a `CGChat` (Client → Game Chat packet)
2. Search for strings like:
   - "send"
   - "CGChat"
   - Or the actual chat command characters

3. Find `Talk::SendChatMessage` or similar

**Example from source:**
```cpp
// GMInterface_Script_Talk.h:543
INT SendChatMessage(LuaPlus::LuaState* state);
```

This calls:
```cpp
CGChat packet;
packet.SetSourName(...);
packet.SetContex(...);
SendPacket(&packet);
```

### Step 5.3: Call Game Function from Hook

Once you find the address in IDA:

```cpp
// Type definition for the function
typedef void (__cdecl* SendChatMessage_t)(const char* message, int channelType);

// Function pointer (set to address found in IDA)
SendChatMessage_t SendChatMessage = (SendChatMessage_t)0x00ABC123;

void OnChatMessageReceived(const char* senderName, const char* messageText, unsigned char channelType) {
    LogToFile("[%d] %s: %s", channelType, senderName, messageText);

    if (strstr(messageText, "ping") != NULL) {
        // Call the game's send chat function
        SendChatMessage("pong!", 1);  // Channel 1 = Near
    }
}
```

### Step 5.4: More Complex Example - Party Invite Auto-Accept

```cpp
// Find these function addresses in IDA:
typedef void (__cdecl* AcceptPartyInvite_t)();
AcceptPartyInvite_t AcceptPartyInvite = (AcceptPartyInvite_t)0x00DEF456;

typedef bool (__cdecl* IsPlayerInParty_t)();
IsPlayerInParty_t IsPlayerInParty = (IsPlayerInParty_t)0x00DEF789;

void OnChatMessageReceived(const char* senderName, const char* messageText, unsigned char channelType) {
    LogToFile("[%d] %s: %s", channelType, senderName, messageText);

    // Check for party invite system message
    if (channelType == 5 &&  // System channel
        strstr(messageText, "邀请你加入队伍") != NULL) {  // "invites you to party"

        LogToFile("  -> Party invite detected!");

        // Auto-accept if not already in party
        if (!IsPlayerInParty()) {
            LogToFile("  -> Auto-accepting party invite...");
            AcceptPartyInvite();
        } else {
            LogToFile("  -> Already in a party, ignoring invite");
        }
    }
}
```

### Step 5.5: Calling Lua Functions (Advanced)

Many game functions are exposed to Lua. You can call them directly:

```cpp
// Find lua_State* global variable
typedef int (__cdecl* luaL_dostring_t)(void* L, const char* str);
luaL_dostring_t luaL_dostring = (luaL_dostring_t)0x00ABC000;

void* g_LuaState = (void*)0x01234567;  // Find this in IDA

void OnChatMessageReceived(const char* senderName, const char* messageText, unsigned char channelType) {
    if (strstr(messageText, "!status") != NULL) {
        // Execute Lua code to show character status
        luaL_dostring(g_LuaState,
            "SendChatMessage(\"near\", \"My HP: \" .. GetPlayerHP())");
    }
}
```

### Step 5.6: Full Example - Keyword Command System

```cpp
#include <map>
#include <string>
#include <functional>

// Command registry
std::map<std::string, std::function<void(const char*)>> g_Commands;

// Initialize commands
void InitializeCommands() {
    g_Commands["!help"] = [](const char* sender) {
        SendChatMessage("Available commands: !help, !status, !time", 1);
    };

    g_Commands["!status"] = [](const char* sender) {
        char buffer[256];
        sprintf(buffer, "HP: %d/%d, MP: %d/%d",
                GetPlayerHP(), GetPlayerMaxHP(),
                GetPlayerMP(), GetPlayerMaxMP());
        SendChatMessage(buffer, 1);
    };

    g_Commands["!time"] = [](const char* sender) {
        SYSTEMTIME st;
        GetLocalTime(&st);
        char buffer[64];
        sprintf(buffer, "Server time: %02d:%02d:%02d",
                st.wHour, st.wMinute, st.wSecond);
        SendChatMessage(buffer, 1);
    };
}

void OnChatMessageReceived(const char* senderName, const char* messageText, unsigned char channelType) {
    LogToFile("[%d] %s: %s", channelType, senderName, messageText);

    // Parse commands
    for (auto& cmd : g_Commands) {
        if (strncmp(messageText, cmd.first.c_str(), cmd.first.length()) == 0) {
            LogToFile("  -> Executing command: %s", cmd.first.c_str());
            cmd.second(senderName);
            break;
        }
    }
}

// Call in DllMain:
BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID lpReserved) {
    if (reason == DLL_PROCESS_ATTACH) {
        InitializeCommands();
        InstallHook();
    }
    return TRUE;
}
```

---

## Troubleshooting

### 🆕 Memory Scanner Specific Issues

#### Scanner Issue 1: "No candidates found with any pattern"

**Symptoms:**
```
× No candidates found with any pattern
  Suggestions:
  1. Game might use different calling convention
  2. Try using x64dbg to find the function dynamically
  3. Check if game is 64-bit (this scanner assumes 32-bit)
```

**Solutions:**

1. **Wrong game architecture:**
   - Check if game is 64-bit:
     ```batch
     dumpbin /headers Game.exe | findstr "machine"
     ```
   - If 64-bit: Scanner needs to be updated for x64
   - Most Dragon Oath games are 32-bit

2. **Game code location different:**
   - Scanner searches from `base+0x1000` to `base+0x800000` (8MB)
   - Some games have code elsewhere
   - Try expanding search range in `MemoryScannerViewModel.cs:621`

3. **Function uses different prologue:**
   - Scanner looks for standard `push ebp; mov ebp, esp; sub esp, XX`
   - Some compilers use different patterns
   - Use x64dbg manually to find the actual bytes

#### Scanner Issue 2: "Too many candidates found"

**Symptoms:**
```
[1/4] Trying: Pattern 1: Standard prologue with push edi
    Found 342 candidate(s)
    Pattern 1 yielded 0 verified function(s)
```

**Meaning:** Scanner found many functions with similar prologues but none matched the verification criteria.

**Solutions:**

1. **Lower verification requirements:**
   - Edit `VerifyHandleRecvTalkPacket()` in `MemoryScannerViewModel.cs:747`
   - Reduce required call count from 3 to 2
   - Or disable verification temporarily:
     ```csharp
     return true;  // Accept all candidates
     ```

2. **Use x64dbg to verify manually:**
   - Launch x64dbg
   - Attach to Game.exe
   - Search for string "IsBlackName" or similar
   - Find cross-references to identify the function
   - Note the address
   - Compare with scanner candidates

#### Scanner Issue 3: Scanner verified address but hook doesn't work

**Symptoms:**
- Scanner shows: `✓ VERIFIED: 0x0078B2A0`
- But when you use that pattern in ChatHookDLL, pattern not found

**Solutions:**

1. **Address vs Pattern mismatch:**
   - Scanner gives you the **runtime address** (0x0078B2A0)
   - This address changes each time game starts (ASLR)
   - You need the **hex bytes**, not the address
   - Use the "First 32 bytes" output from scanner

2. **Correct usage:**
   ```cpp
   // DON'T DO THIS:
   HandleRecvTalkPacket = (Func_t)0x0078B2A0;  // ❌ Wrong! Address changes

   // DO THIS:
   BYTE pattern[] = {  // ✅ Use hex bytes from scanner
       0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x4C, 0x53, 0x56
   };
   ```

3. **Pattern from scanner to DLL:**

   Scanner output:
   ```
   First 32 bytes: 55-8B-EC-83-EC-4C-53-56-57-8B-F9
   ```

   Convert to C++:
   ```cpp
   BYTE pattern[] = { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x4C, 0x53, 0x56, 0x57, 0x8B, 0xF9 };
   const char* mask = "xxxxxxxxxxx";  // All exact matches
   ```

#### Scanner Issue 4: Multiple verified addresses found

**Symptoms:**
```
✓ VERIFIED: 0x0078B2A0
✓ VERIFIED: 0x0078C150
✓ VERIFIED: 0x0078D200
```

**Meaning:** Scanner found multiple functions that match the pattern.

**Solutions:**

1. **Use the first one:**
   - Usually the first match is correct
   - Test with that pattern first

2. **Verify with x64dbg:**
   - Set breakpoint at each address
   - Send chat in game
   - See which breakpoint hits

3. **Use longer pattern:**
   - Use more bytes from scanner (20-24 bytes instead of 16)
   - This makes the pattern more unique

#### Scanner Issue 5: Process access denied

**Symptoms:**
- Scanner fails to open memory
- ERROR: Cannot open process handle

**Solutions:**

1. **Run as Administrator:**
   - Right-click AutoDragonOath.exe
   - "Run as administrator"

2. **Anti-virus blocking:**
   - Add exception for AutoDragonOath
   - Temporarily disable anti-virus

3. **Game has anti-cheat:**
   - Game might block memory reading
   - Try different memory reading method
   - Test on private server first

---

### Traditional Hook Issues

#### Issue 1: "Pattern not found"

**Symptoms:**
```
[14:32:15] Searching for GCChatHandler::Execute...
[14:32:15]   NOT FOUND - Pattern needs updating!
```

**Solutions:**

1. **Verify game version:**
   - Game was updated
   - Reopen Game.exe in IDA
   - Find function again
   - Create new pattern

2. **Pattern too specific:**
   - Add more wildcards
   - Try shorter pattern (8-12 bytes)

3. **Wrong function:**
   - You might be hooking the wrong function
   - Double-check in IDA that it's HandleRecvTalkPacket

### Issue 2: Game crashes on injection

**Symptoms:**
- Game closes immediately after injection
- No log file created

**Solutions:**

1. **Wrong calling convention:**
   ```cpp
   // Try different conventions:
   typedef int (__thiscall* Func_t)(void*, void*);  // Most likely
   typedef int (__cdecl* Func_t)(void*);
   typedef int (__stdcall* Func_t)(void*);
   ```

2. **Stack corruption:**
   - Make sure you're calling original function correctly
   - Preserve all registers
   - Use `__asm` blocks carefully

3. **Anti-cheat detection:**
   - Some games have protection
   - Try injecting after login
   - Use manual mapping instead of LoadLibrary

### Issue 3: Hook triggers but no data

**Symptoms:**
```
[14:33:01] [Channel 0] :
[14:33:05] [Channel 0] :
```

**Solutions:**

1. **Wrong packet structure:**
   - vtable offsets are incorrect
   - Use IDA to analyze GCChat class
   - Update vtable indices

2. **Null pointer:**
   - Add null checks everywhere
   ```cpp
   if (pPacket && pPacket->GetContex()) {
       // Safe to access
   }
   ```

3. **Wrong encoding:**
   - Vietnamese text uses VISCII encoding
   - Use proper encoding conversion

### Issue 4: Can't compile DLL

**Symptoms:**
```
error LNK2019: unresolved external symbol DetourAttach
```

**Solutions:**

1. **Detours not linked:**
   - Check library path: `/LIBPATH:"C:\Detours\lib.X86"`
   - Check library name: `detours.lib`

2. **Wrong architecture:**
   - Game is 32-bit → use lib.X86
   - Game is 64-bit → use lib.X64

3. **Missing include:**
   ```cpp
   #include <detours.h>
   ```

### Issue 5: Permission denied

**Symptoms:**
```
[-] Failed to open process (Error: 5)
```

**Solutions:**

1. **Run as Administrator:**
   - Right-click ChatInjector.exe
   - "Run as administrator"

2. **Anti-virus blocking:**
   - Add exception for C:\ChatHook\
   - Temporarily disable anti-virus

3. **Anti-cheat:**
   - Game has anti-tamper protection
   - Try different injection method
   - Test on private server first

---

## Advanced Tips

### Tip 1: Use IDA Debugger

IDA Free has a built-in debugger:

1. `Debugger` → `Select debugger` → `Local Win32 debugger`
2. `Debugger` → `Process options` → Select Game.exe
3. `Debugger` → `Start process`
4. Set breakpoint on HandleRecvTalkPacket (F2)
5. When chat arrives, debugger breaks
6. Inspect parameters in real-time

### Tip 2: Log Everything

While developing, log everything:

```cpp
void Hooked_HandleRecvTalkPacket(void* thisPtr, void* pPacket) {
    LogToFile("=== HOOK TRIGGERED ===");
    LogToFile("  thisPtr = 0x%08X", thisPtr);
    LogToFile("  pPacket = 0x%08X", pPacket);

    __try {
        // Your code here
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        LogToFile("  EXCEPTION CAUGHT!");
    }

    LogToFile("  Calling original...");
    int result = Original_HandleRecvTalkPacket(thisPtr, pPacket);
    LogToFile("  Result = %d", result);
    return result;
}
```

### Tip 3: Signature Database

Keep a database of signatures for different game versions:

```cpp
struct FunctionSignature {
    const char* version;
    BYTE pattern[32];
    const char* mask;
    DWORD knownAddress;  // For verification
};

FunctionSignature signatures[] = {
    { "v1.0.0", {0x55, 0x8B, 0xEC, 0x83, ...}, "xxxxx?xxx", 0x00789A20 },
    { "v1.0.1", {0x55, 0x8B, 0xEC, 0x81, ...}, "xxxxx?xxx", 0x0078A130 },
};
```

### Tip 4: External Configuration

Load settings from config file:

```cpp
// ChatHookConfig.ini:
[Settings]
LogToFile=1
LogChannel=1,2,3
AutoReply=1
CommandPrefix=!

[Commands]
help=Available commands: help, status, time
status=HP: {hp}/{maxhp}
```

Read with `GetPrivateProfileString()`.

---

## Next Steps

✅ You now know how to:
1. **🚀 Use Memory Scanner to find functions in packed executables** (fastest method)
2. Find functions in IDA Free 9.2 (traditional method)
3. Create signature patterns from hex bytes
4. Convert scanner output to C++ hook patterns
5. Build and inject hook DLLs
6. Intercept chat messages
7. Call game functions
8. Build custom command systems

**Recommended workflow for packed games:**
1. ✅ Use AutoDragonOath Memory Scanner first (< 1 minute)
2. ✅ Copy hex pattern from scanner output
3. ✅ Convert to C++ byte array
4. ✅ Build ChatHookDLL with the pattern
5. ✅ Inject and test

**Recommended practice:**
1. Start with simple logging
2. Add keyword detection
3. Find more game functions (use scanner for each)
4. Build automation system
5. Test thoroughly before production use

**Remember:**
- Always test on private servers first
- Respect game Terms of Service
- Use for educational purposes only
- Keep your code organized and documented

Good luck! 🎮
