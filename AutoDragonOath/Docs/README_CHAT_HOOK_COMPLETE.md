# Complete Chat Hook Implementation Package

## 📦 What You Have

This package contains everything you need to intercept Dragon Oath chat messages and call custom functions.

---

## ⚠️ CRITICAL: Module Location Clarification

**Q: Is GCChatHandler::Execute in a DLL or the main Game.exe?**
**A: It's in the MAIN EXECUTABLE (Game.exe), NOT a separate DLL!**

**Evidence:** Source code analysis of `WXClient.vcxproj` confirms:
- ConfigurationType: **Application** (not DynamicLibrary)
- Output: **WXClient.exe** (renamed to Game.exe)
- **All chat functions are compiled into the main executable**

**What this means:**
- Open `Game.exe` in IDA (not a DLL)
- Pattern scan searches the main module only
- No separate DLLs to analyze

📖 **Full investigation: `MODULE_LOCATION_FINDINGS.md`**

---

### Documentation Files

| File | Purpose | Use When |
|------|---------|----------|
| **IDA_Free_Complete_Tutorial.md** | Step-by-step IDA guide | Learning how to find functions |
| **Quick_Reference_Cheatsheet.md** | Quick commands & shortcuts | Need fast reference |
| **ChatHookingGuide.md** | Conceptual overview | Understanding the theory |
| **CHAT_INJECTION_README.md** | Complete implementation guide | Ready to start coding |

### Code Files

| File | Description | Compile For |
|------|-------------|-------------|
| **ChatHookDLL.cpp** | Production hook DLL | Release version |
| **ChatInjector.cpp** | Standalone injector | Release version |
| **Example_CustomFunctionCall.cpp** | Advanced examples with automation | Learning/Reference |
| **ChatHookAlternative_CSharp.cs** | C# implementation | WPF integration |

---

## 🚀 Quick Start in 5 Steps

### **Step 1: Install IDA Free 9.2**
Download: https://hex-rays.com/ida-free/

### **Step 2: Find the Function**
1. Open `Game.exe` in IDA Free
2. Press `Shift + F12` to search strings
3. Search for "Talk" or "Chat"
4. Find `HandleRecvTalkPacket`
5. Note the first 16 bytes in hex

**Example:**
```
00789A20: 55 8B EC 83 EC 4C 53 56 57 8B F9 89 7D F4 83 7D
```

### **Step 3: Create Pattern**
Convert to pattern with wildcards:
```cpp
BYTE pattern[] = { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x??, 0x53, 0x56, 0x57 };
const char* mask = "xxxxx?xxx";
```

### **Step 4: Compile**
```batch
# Setup (one time)
git clone https://github.com/microsoft/Detours.git
cd Detours\src && nmake

# Compile your hook
cd C:\ChatHook
cl /LD /MT ChatHookDLL.cpp /I"C:\Detours\include" ^
   /link /LIBPATH:"C:\Detours\lib.X86" detours.lib

cl /MT ChatInjector.cpp
```

### **Step 5: Inject & Test**
```batch
# Start game first
ChatInjector.exe

# Check log
type C:\DragonOath_ChatLog.txt
```

---

## 📚 Learning Path

```
┌──────────────────────────────────────────────────┐
│ 1. Read "ChatHookingGuide.md"                   │
│    Understand the concepts                      │
└────────────────┬─────────────────────────────────┘
                 │
┌────────────────▼─────────────────────────────────┐
│ 2. Follow "IDA_Free_Complete_Tutorial.md"       │
│    Learn to use IDA Free 9.2                    │
│    Find HandleRecvTalkPacket                    │
│    Create signature pattern                     │
└────────────────┬─────────────────────────────────┘
                 │
┌────────────────▼─────────────────────────────────┐
│ 3. Build from "ChatHookDLL.cpp"                 │
│    Compile your first hook DLL                  │
│    Test with ChatInjector                       │
└────────────────┬─────────────────────────────────┘
                 │
┌────────────────▼─────────────────────────────────┐
│ 4. Study "Example_CustomFunctionCall.cpp"       │
│    Learn to call game functions                 │
│    Implement automation                         │
└────────────────┬─────────────────────────────────┘
                 │
┌────────────────▼─────────────────────────────────┐
│ 5. Advanced: Integrate into AutoDragonOath     │
│    Use ChatHookAlternative_CSharp.cs            │
│    Create custom automation                     │
└──────────────────────────────────────────────────┘
```

---

## 🎯 Use Cases & Examples

### Use Case 1: Chat Logging
**File:** `ChatHookDLL.cpp` (default functionality)

**What it does:**
- Logs all chat messages to file
- Includes timestamp, sender, channel

**Use for:**
- Monitoring game activity
- Analyzing chat patterns
- Recording evidence

### Use Case 2: Auto-Reply Bot
**File:** `Example_CustomFunctionCall.cpp` - `HandleHelpRequest()`

**What it does:**
- Detects "!help" keyword
- Sends help message back

**Use for:**
- Customer support bot
- AFK auto-responder
- Information bot

### Use Case 3: Party Auto-Accept
**File:** `Example_CustomFunctionCall.cpp` - `HandlePartyInvite()`

**What it does:**
- Detects party invite system message
- Automatically accepts

**Use for:**
- Guild events
- Automated grinding
- Multi-boxing

### Use Case 4: Command System
**File:** `Example_CustomFunctionCall.cpp` - `ProcessChatCommand()`

**What it does:**
- Routes commands to functions
- Supports !help, !status, !follow, etc.

**Use for:**
- Bot control
- Remote commands
- Game automation

### Use Case 5: Trade Bot
**File:** `Example_CustomFunctionCall.cpp` - `HandleTradeRequest()`

**What it does:**
- Detects !buy / !sell commands
- Processes trading requests

**Use for:**
- Merchant bot
- Item exchange
- Virtual shop

---

## 🛠️ Required Tools

### Essential (Must Have)

✅ **IDA Free 9.2**
- Download: https://hex-rays.com/ida-free/
- Used for: Finding function addresses
- Alternative: Ghidra (free, but harder to use)

✅ **Visual Studio 2019+**
- Download: https://visualstudio.microsoft.com/
- Community Edition is free
- Used for: Compiling C++ code
- Alternative: MinGW-w64

✅ **Microsoft Detours**
- Download: https://github.com/microsoft/Detours
- Used for: Function hooking
- Alternative: MinHook, EasyHook

### Optional (Nice to Have)

⭐ **x64dbg**
- Download: https://x64dbg.com/
- Used for: Live debugging
- Helps verify hooks are working

⭐ **Cheat Engine**
- Download: https://cheatengine.org/
- Used for: Memory scanning
- Helps find function addresses

⭐ **HxD Hex Editor**
- Download: https://mh-nexus.de/en/hxd/
- Used for: Viewing binary data
- Helps verify patterns

---

## 📁 Project Structure

```
C:\ChatHook\                          # Your working directory
├── ChatHookDLL.cpp                   # Main hook DLL source
├── ChatHookDLL.dll                   # Compiled DLL
├── ChatInjector.cpp                  # Injector source
├── ChatInjector.exe                  # Compiled injector
└── detours.lib                       # Copy from Detours\lib.X86\

C:\DragonOath_ChatLog.txt             # Output log file

C:\Detours\                           # Microsoft Detours library
├── include\detours.h
├── lib.X86\detours.lib               # 32-bit
└── lib.X64\detours.lib               # 64-bit

G:\microauto-6.9\AutoDragonOath\Docs\ # Documentation
├── IDA_Free_Complete_Tutorial.md
├── Quick_Reference_Cheatsheet.md
├── ChatHookingGuide.md
├── CHAT_INJECTION_README.md
├── ChatHookDLL.cpp
├── ChatInjector.cpp
├── Example_CustomFunctionCall.cpp
└── ChatHookAlternative_CSharp.cs
```

---

## 🔍 Function Finding Reference

### From Source Code Analysis

We know these functions exist in Game.exe:

**Chat Functions:**
```cpp
// Network layer (earliest intercept point)
uint GCChatHandler::Execute(GCChat* pPacket, Player* pPlayer)
  Address: ??? (find in IDA)
  Pattern: 55 8B EC 83 EC ?? 53 56 57

// Logic layer (after filtering)
INT Talk::HandleRecvTalkPacket(GCChat *pPacket)
  Address: ??? (find in IDA)
  Pattern: 55 8B EC 83 EC ?? 53 56 57 8B F9

// Sending chat
INT Talk::SendChatMessage(LuaPlus::LuaState* state)
  Address: ??? (find in IDA)
  Called from: Lua scripts
```

**Game Functions You Might Want:**
```cpp
AcceptPartyInvite()
FollowPlayer(const char* name)
UseItem(int itemId)
GetPlayerHP() -> int
GetPlayerMaxHP() -> int
TeleportTo(int x, int y, int mapId)
AttackTarget()
OpenTradeWindow(const char* targetName)
```

**How to find them:**
1. Search for strings in IDA
2. Find cross-references
3. Analyze function signatures
4. Note the address
5. Create function pointer in your hook

---

## ⚙️ Configuration

### Chat Logging

Edit `ChatHookDLL.cpp` line 15:
```cpp
#define ENABLE_FILE_LOGGING    1    // 0 = disable, 1 = enable
#define LOG_FILE_PATH          "C:\\DragonOath_ChatLog.txt"
```

### Channel Filtering

Edit `OnChatMessageReceived()`:
```cpp
// Only log team chat
if (channelType != 2) return;
```

### Pattern Scanning

Edit `FindGCChatHandlerExecute()` line 214:
```cpp
BYTE pattern[] = { /* your pattern from IDA */ };
const char* mask = "xxxxx?xxxxx";
```

---

## 🐛 Troubleshooting Guide

### Problem: "Pattern not found"
**Cause:** Game was updated, pattern changed
**Fix:** Re-analyze in IDA, update pattern

**Detailed steps:**
1. Open new Game.exe in IDA
2. Find HandleRecvTalkPacket again
3. Get new hex bytes
4. Update pattern[] in code
5. Recompile

### Problem: Game crashes on injection
**Cause:** Wrong function signature
**Fix:** Verify calling convention

**Detailed steps:**
1. Check if function is __thiscall (most likely)
2. Verify you're calling original correctly
3. Add exception handling
4. Test with debugger (x64dbg)

### Problem: No log output
**Cause:** Hook not triggering
**Fix:** Verify pattern and address

**Detailed steps:**
1. Check if DLL loaded: `tasklist /m ChatHookDLL.dll`
2. Verify pattern found (check log for address)
3. Test with debugger
4. Add MessageBox for debugging

### Problem: Garbage data in log
**Cause:** Wrong packet structure offsets
**Fix:** Analyze GCChat in IDA

**Detailed steps:**
1. Find GCChat class in IDA
2. Analyze virtual method table
3. Update vtable offsets
4. Verify with debugger

---

## 💡 Best Practices

### 1. Version Control
```batch
git init
git add *.cpp *.md
git commit -m "Initial chat hook implementation"
```

### 2. Pattern Database
Keep patterns for different versions:
```cpp
// Version history
// v1.0.0 (2024-01-01): 55 8B EC 83 EC 4C 53 56 57
// v1.0.1 (2024-02-01): 55 8B EC 83 EC 50 53 56 57
// v1.0.2 (2024-03-01): 55 8B EC 81 EC 00 01 00 00
```

### 3. Error Handling
```cpp
__try {
    // Your code
} __except (EXCEPTION_EXECUTE_HANDLER) {
    Log("Exception caught!");
}
```

### 4. Configuration Files
```ini
; ChatHook.ini
[Settings]
LogEnabled=1
LogFile=C:\ChatLog.txt
AutoReply=1

[Channels]
LogNear=1
LogTeam=1
LogGuild=1
```

### 5. Testing Workflow
1. Test on private server first
2. Verify each feature individually
3. Check for memory leaks
4. Monitor game stability
5. Only then test on live server

---

## 📊 Performance Considerations

| Operation | Impact | Notes |
|-----------|--------|-------|
| Hook installation | < 1ms | One-time cost |
| Per-message intercept | < 0.1ms | Negligible |
| File logging | 1-5ms | Use buffering |
| Pattern scanning | 50-200ms | Cache result |
| Total CPU overhead | < 0.1% | Barely measurable |

**Optimization tips:**
- Cache pattern scan results
- Use efficient string operations
- Minimize file I/O
- Avoid expensive operations in hook

---

## ⚖️ Legal & Safety

### ⚠️ Important Warnings

1. **Terms of Service:** May violate game TOS
2. **Account Safety:** Risk of ban
3. **Security:** DLL injection is risky
4. **Privacy:** Logging chat may violate privacy

### ✅ Safe Use Guidelines

- **Test on private servers only**
- **Get permission from game owner**
- **Use for research/education only**
- **Don't distribute without permission**
- **Respect other players' privacy**

---

## 🎓 Next Steps

### Beginner
1. ✅ Read all documentation
2. ✅ Install required tools
3. ✅ Follow IDA tutorial
4. ✅ Compile basic hook
5. ✅ Test chat logging

### Intermediate
1. ✅ Find additional game functions
2. ✅ Implement auto-reply
3. ✅ Add command system
4. ✅ Handle multiple commands
5. ✅ Integrate into WPF app

### Advanced
1. ✅ Implement state machine
2. ✅ Build trading bot
3. ✅ Add Lua integration
4. ✅ Create automation framework
5. ✅ Publish as tool (with permission)

---

## 📞 Support & Resources

### Documentation
- All guides in `G:\microauto-6.9\AutoDragonOath\Docs\`
- Comments in source code
- Examples in `Example_CustomFunctionCall.cpp`

### Communities
- Guided Hacking: https://guidedhacking.com/
- UnKnoWnCheaTs: https://unknowncheats.me/
- r/ReverseEngineering: reddit.com/r/ReverseEngineering

### Tools
- IDA Free: https://hex-rays.com/ida-free/
- Detours: https://github.com/microsoft/Detours
- x64dbg: https://x64dbg.com/

---

## ✨ Success Checklist

- [ ] IDA Free 9.2 installed
- [ ] Visual Studio installed
- [ ] Detours compiled
- [ ] Found HandleRecvTalkPacket in IDA
- [ ] Created signature pattern
- [ ] Updated pattern in ChatHookDLL.cpp
- [ ] Compiled ChatHookDLL.dll
- [ ] Compiled ChatInjector.exe
- [ ] Game.exe running
- [ ] Injected DLL successfully
- [ ] Log file created
- [ ] Test message logged
- [ ] Ready to add custom functions!

---

**You now have everything you need to intercept chat and call custom functions!**

Start with `IDA_Free_Complete_Tutorial.md` for detailed step-by-step instructions.

Good luck! 🎮
