# Quick Reference Cheat Sheet - Chat Hook Injection

## 🚀 Quick Start (Copy-Paste Commands)

```batch
# 1. Setup Detours
cd C:\
git clone https://github.com/microsoft/Detours.git
cd Detours\src
nmake

# 2. Create project
mkdir C:\ChatHook
cd C:\ChatHook
copy G:\microauto-6.9\AutoDragonOath\Docs\ChatHookDLL.cpp .
copy G:\microauto-6.9\AutoDragonOath\Docs\ChatInjector.cpp .

# 3. Compile (32-bit)
cl /LD /MT /O2 ChatHookDLL.cpp /I"C:\Detours\include" /link /LIBPATH:"C:\Detours\lib.X86" detours.lib /OUT:ChatHookDLL.dll
cl /MT /O2 ChatInjector.cpp /OUT:ChatInjector.exe

# 4. Inject
ChatInjector.exe

# 5. Check log
type C:\DragonOath_ChatLog.txt
```

---

## 📋 IDA Free 9.2 Shortcuts

| Action | Shortcut |
|--------|----------|
| Search strings | `Shift + F12` |
| Search text | `Ctrl + F` |
| Functions list | `Shift + F3` |
| Cross-references | `X` |
| Rename symbol | `N` |
| Go to address | `G` |
| Hex view | `Alt + T` |
| Search bytes | `Alt + B` |
| Jump to function | `Enter` (on call) |
| Go back | `Esc` |

---

## 🎯 Pattern Creation Quick Guide

### Step 1: Get hex bytes from IDA
```
Address   Hex bytes                    Assembly
00789A20  55                           push    ebp
00789A21  8B EC                        mov     ebp, esp
00789A23  83 EC 4C                     sub     esp, 4Ch
00789A26  53                           push    ebx
00789A27  56                           push    esi
00789A28  57                           push    edi
00789A29  8B F9                        mov     edi, ecx
```

### Step 2: Extract pattern
```cpp
// Exact bytes:
55 8B EC 83 EC 4C 53 56 57 8B F9
```

### Step 3: Add wildcards
```cpp
// Wildcarded (4C might change):
BYTE pattern[] = { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x??, 0x53, 0x56, 0x57, 0x8B, 0xF9 };
const char* mask = "xxxxx?xxxxx";
```

---

## 🔍 Finding HandleRecvTalkPacket

### Method 1: String search
1. `Shift + F12` (Strings)
2. Search: "Talk", "Chat", "Message"
3. `X` to see cross-references
4. Look for function with correct signature

### Method 2: Known caller
```
GCChatHandler::Execute calls HandleRecvTalkPacket
↓
Look for pattern:
    mov     ecx, [Talk::s_Talk]    ; Singleton
    push    [ebp+pPacket]          ; Parameter
    call    HandleRecvTalkPacket    ; TARGET!
```

### Method 3: Function characteristics
- Takes 1 parameter (GCChat*)
- Returns INT
- Checks for NULL
- Calls IsBlackName()
- Creates HistoryMsg

---

## 💉 Injection Workflow

```
┌─────────────────────────────────────────────────────┐
│ 1. Find Function in IDA                            │
│    - Open Game.exe                                  │
│    - Search for strings/patterns                   │
│    - Identify HandleRecvTalkPacket                 │
│    - Note address: 0x00789A20                      │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────┐
│ 2. Create Signature                                │
│    - Copy first 12-16 bytes                        │
│    - Add wildcards for variable bytes              │
│    - Pattern: 55 8B EC 83 EC ?? 53 56 57           │
│    - Mask:    xxxxx?xxx                            │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────┐
│ 3. Update Hook Code                                │
│    - Edit ChatHookDLL.cpp                          │
│    - Update pattern[] array                        │
│    - Update mask string                            │
│    - Customize OnChatMessageReceived()             │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────┐
│ 4. Compile DLL                                     │
│    cl /LD ChatHookDLL.cpp ...                      │
│    → ChatHookDLL.dll                               │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────┐
│ 5. Inject into Game                                │
│    - Run Game.exe                                  │
│    - Run ChatInjector.exe                          │
│    - Check C:\DragonOath_ChatLog.txt               │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────┐
│ 6. Test & Verify                                   │
│    - Send chat in game                             │
│    - Check log for message                         │
│    - Verify hook is working                        │
└─────────────────────────────────────────────────────┘
```

---

## 🛠️ Common Code Snippets

### Log to file
```cpp
LogToFile("[%d] %s: %s", channelType, senderName, messageText);
```

### Auto-reply on keyword
```cpp
if (strstr(messageText, "help") != NULL) {
    SendChatMessage("I can help!", 1);
}
```

### Execute on specific channel
```cpp
if (channelType == 2) {  // Team chat
    // Do something
}
```

### Call game function
```cpp
typedef void (__cdecl* GameFunc_t)(const char*);
GameFunc_t GameFunc = (GameFunc_t)0x00ABC123;
GameFunc("Hello from hook!");
```

### Parse command
```cpp
if (strncmp(messageText, "!status", 7) == 0) {
    char buffer[128];
    sprintf(buffer, "HP: %d", GetPlayerHP());
    SendChatMessage(buffer, 1);
}
```

---

## 🔧 Compile Commands

### 32-bit DLL
```batch
cl /LD /MT /O2 ChatHookDLL.cpp ^
   /I"C:\Detours\include" ^
   /link /LIBPATH:"C:\Detours\lib.X86" detours.lib ^
   /OUT:ChatHookDLL.dll
```

### 64-bit DLL
```batch
cl /LD /MT /O2 ChatHookDLL.cpp ^
   /I"C:\Detours\include" ^
   /link /LIBPATH:"C:\Detours\lib.X64" detours.lib ^
   /OUT:ChatHookDLL.dll
```

### Injector
```batch
cl /MT /O2 ChatInjector.cpp /OUT:ChatInjector.exe
```

### With Debug Symbols
```batch
cl /LD /MTd /Zi /Od ChatHookDLL.cpp ^
   /I"C:\Detours\include" ^
   /link /LIBPATH:"C:\Detours\lib.X86" detours.lib /DEBUG ^
   /OUT:ChatHookDLL.dll
```

---

## 🐛 Debugging Commands

### Check if DLL loaded
```batch
tasklist /m ChatHookDLL.dll
```

### View log in real-time
```powershell
Get-Content C:\DragonOath_ChatLog.txt -Wait
```

### Find Game.exe PID
```batch
tasklist | findstr Game.exe
```

### Kill hung process
```batch
taskkill /F /IM Game.exe
```

### Check DLL dependencies
```batch
dumpbin /dependents ChatHookDLL.dll
```

---

## 📊 Channel Type Reference

| Channel ID | Name | Description |
|-----------|------|-------------|
| 0 | Near | Nearby chat |
| 1 | Scene | World/Scene chat |
| 2 | Team | Party/Team chat |
| 3 | Guild | Guild chat |
| 4 | Private | Whisper/Tell |
| 5 | System | System messages |
| 6 | Faction | Menpai/Faction |

---

## ⚠️ Common Errors & Fixes

| Error | Cause | Fix |
|-------|-------|-----|
| Pattern not found | Game updated | Re-analyze in IDA, new pattern |
| Game crashes | Wrong signature | Check calling convention |
| Access denied | No admin rights | Run as Administrator |
| LNK2019 error | Missing library | Add detours.lib to linker |
| No log output | Hook not triggering | Verify pattern, check address |
| Garbage data | Wrong offsets | Analyze packet structure in IDA |

---

## 🎯 Testing Checklist

- [ ] Compiled DLL successfully
- [ ] Compiled injector successfully
- [ ] Game.exe is running
- [ ] Running as Administrator
- [ ] DLL injected (check log)
- [ ] Hook installed (check log address)
- [ ] Sent test chat message
- [ ] Message appears in log file
- [ ] Sender name correct
- [ ] Message text correct
- [ ] Channel type correct

---

## 📁 File Locations

```
C:\ChatHook\
├── ChatHookDLL.cpp         (Source code)
├── ChatHookDLL.dll         (Compiled DLL)
├── ChatInjector.cpp        (Injector source)
├── ChatInjector.exe        (Compiled injector)
└── detours.lib             (From C:\Detours\lib.X86\)

C:\DragonOath_ChatLog.txt   (Output log)

G:\microauto-6.9\AutoDragonOath\Docs\
├── ChatHookingGuide.md
├── IDA_Free_Complete_Tutorial.md
├── ChatHookDLL.cpp
├── ChatInjector.cpp
└── ChatHookAlternative_CSharp.cs
```

---

## 🔗 Useful Links

- IDA Free: https://hex-rays.com/ida-free/
- Detours: https://github.com/microsoft/Detours
- x64dbg: https://x64dbg.com/
- Visual Studio: https://visualstudio.microsoft.com/
- Guided Hacking: https://guidedhacking.com/

---

## 💡 Pro Tips

1. **Keep multiple pattern versions** for different game updates
2. **Log everything** during development
3. **Test on private server** before production
4. **Use version control** (Git) for your hook code
5. **Comment your patterns** with game version info
6. **Backup working DLLs** with version numbers
7. **Use configuration files** instead of hardcoded values

---

## 📞 Need Help?

1. Check `IDA_Free_Complete_Tutorial.md` for detailed steps
2. Review troubleshooting section
3. Search Guided Hacking forums
4. Ask on UnKnoWnCheats forum
5. Use x64dbg to debug live

---

**Last Updated:** 2025-01-XX
**Game Version:** Dragon Oath (Thiên Long Bát Bộ)
**IDA Version:** 9.2 Free
**Detours Version:** Latest from GitHub
