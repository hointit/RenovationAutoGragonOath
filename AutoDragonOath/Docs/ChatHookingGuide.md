# Dragon Oath Chat Hooking Guide

## Overview

This guide explains how to inject a function to intercept chat messages when they're received from the server via network packets in the Dragon Oath game.

---

## Hook Targets

Based on the source code analysis, there are **two main hooking points**:

### **Option 1: Hook GCChatHandler::Execute (Recommended)**
- **File**: `WXClient/Network/PacketHandler/GCChatHandler.cpp:12`
- **Function**: `uint GCChatHandler::Execute(GCChat* pPacket, Player* pPlayer)`
- **Advantages**:
  - Intercepts chat at the network layer (earliest point)
  - Raw packet data available
  - Can modify/block packets before processing
- **Signature**: `uint (__thiscall* )(void* thisPtr, GCChat* pPacket, Player* pPlayer)`

### **Option 2: Hook Talk::HandleRecvTalkPacket**
- **File**: `WXClient/Interface/GMInterface_Script_Talk.cpp:1727`
- **Function**: `INT Talk::HandleRecvTalkPacket(GCChat *pPacket)`
- **Advantages**:
  - Already past blacklist/ignore checks
  - Easier to find (static singleton `Talk::s_Talk`)
- **Signature**: `int (__thiscall* )(void* thisPtr, GCChat* pPacket)`

---

## GCChat Packet Structure

From analyzing `GMInterface_Script_Talk.cpp`, the packet has these methods:

```cpp
class GCChat {
public:
    CHAR* GetSourName();        // Sender's character name
    INT GetSourNameSize();      // Size of sender name

    CHAR* GetContex();          // Message content/text
    INT GetContexSize();        // Size of message content

    BYTE GetChatType();         // Channel type (near/team/guild/etc)
    BYTE GetSourCamp();         // Sender's faction/camp ID

    // Additional methods likely exist for:
    // - Target name (for private messages)
    // - Additional metadata
};
```

**Common channel types:**
- `CHAT_TYPE_NEAR` - Nearby/Local chat
- `CHAT_TYPE_SCENE` - Scene/World chat
- `CHAT_TYPE_TEAM` - Team/Party chat
- `CHAT_TYPE_GUILD` - Guild chat
- `CHAT_TYPE_TELL` - Private/Whisper
- `CHAT_TYPE_SYSTEM` - System messages

---

## Injection Methods

### **Method 1: DLL Injection + Detours (Recommended)**

This is the most reliable method for hooking game functions.

**Tools needed:**
- Microsoft Detours library
- Visual Studio C++ compiler
- DLL injector (we'll create one)

### **Method 2: Inline ASM Hook**

Direct assembly patching (more advanced, not recommended for beginners).

### **Method 3: Virtual Method Table (VMT) Hook**

If the functions are virtual (not the case here based on source code).

---

## Implementation

See the following files for complete implementations:

1. **ChatHookDLL.cpp** - The injected DLL that hooks the chat function
2. **ChatInjector.cpp** - Standalone injector to load the DLL into Game.exe
3. **ChatHookExample.h** - Header with packet structure definitions

---

## Safety Considerations

### **Anti-Cheat Detection**
The game may have anti-cheat systems that detect:
- DLL injection
- Function hooking
- Memory modifications

**Mitigation strategies:**
- Use manual mapping instead of LoadLibrary
- Hook from within the same process memory
- Avoid suspicious API calls
- Don't modify code in protected sections

### **Game Updates**
Function addresses change with each game update. You'll need to:
- Use signature scanning to find functions
- Update signatures when the game patches
- Test after each update

---

## Finding Function Addresses

Since the source code is available, you can:

### **1. IDA Pro / Ghidra Analysis**
- Load `Game.exe` into IDA Pro
- Search for string references like "HandleRecvTalkPacket"
- Cross-reference to find the function

### **2. Pattern Scanning**
Use unique byte patterns to locate functions at runtime:

```cpp
// Example pattern for GCChatHandler::Execute
// Based on disassembly of the function
BYTE pattern[] = { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x??, 0x53, 0x56, 0x57 };
DWORD address = FindPattern(pattern, "xxxxxxxxx?xxx");
```

### **3. Exported Function Scanning**
If functions are exported, use `GetProcAddress()`.

### **4. Signature from Source**
Create a signature from the compiled source code and scan for it in the running game.

---

## Usage Workflow

```
1. Compile ChatHookDLL.dll
2. Run Game.exe
3. Run ChatInjector.exe [Game.exe PID]
4. DLL hooks into chat system
5. Chat messages are logged to file or callback
6. Eject DLL when done (optional)
```

---

## Debugging

### **Attach Debugger**
```cpp
// Add to DLL entry point for debugging
while (!IsDebuggerPresent()) Sleep(100);
__debugbreak();
```

### **Log to File**
```cpp
void LogToFile(const char* msg) {
    FILE* f = fopen("C:\\chat_hook_log.txt", "a");
    fprintf(f, "[%s] %s\n", GetTimestamp(), msg);
    fclose(f);
}
```

### **Message Box Debugging**
```cpp
char buffer[512];
sprintf(buffer, "Chat from %s: %s", senderName, messageText);
MessageBoxA(NULL, buffer, "Chat Hook", MB_OK);
```

---

## Legal & Ethical Considerations

**⚠️ WARNING**: Modifying game clients may violate Terms of Service and can result in:
- Account suspension/ban
- Legal action from the game publisher
- Security vulnerabilities in your system

This guide is for **educational purposes only** to understand:
- Reverse engineering techniques
- Game client architecture
- Network protocol analysis
- Anti-cheat systems

**Use responsibly and only on:**
- Private servers you own
- Local testing environments
- Educational research projects
- With explicit permission from the game publisher

---

## Next Steps

1. Study the example code in `ChatHookDLL.cpp`
2. Compile and test the injector
3. Modify the hook to suit your needs (logging, filtering, etc.)
4. Implement signature scanning for game update resilience
5. Consider using a more advanced injection method (manual mapping)

---

## References

- Microsoft Detours: https://github.com/microsoft/Detours
- Guided Hacking Forum: https://guidedhacking.com
- Game Hacking Academy: https://gamehacking.academy
- ReClass.NET (for structure reversing): https://github.com/ReClassNET/ReClass.NET

---

## Troubleshooting

### Hook not triggering?
- Verify function address is correct
- Check if anti-cheat is blocking
- Ensure DLL is loaded into correct process
- Verify calling convention (__thiscall, __stdcall, etc.)

### Game crashes on injection?
- Check function signature matches
- Verify original function is called correctly
- Don't modify stack/registers
- Use proper calling convention

### Can't find function address?
- Use IDA Pro to analyze Game.exe
- Search for string references in the binary
- Use Cheat Engine to scan for patterns
- Check if function is inlined (won't exist)
