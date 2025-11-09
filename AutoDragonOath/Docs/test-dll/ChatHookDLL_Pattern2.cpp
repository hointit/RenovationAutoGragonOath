// ChatHookDLL_Pattern2.cpp - DLL to intercept Dragon Oath chat messages
// Generated from Memory Scanner Results
// VERIFIED ADDRESS: 0x0048D790 (Offset: +0x8D790)
// Pattern: 55-8B-EC-81-EC-1C-01-00-00-A1-04-49-64-00-53-8B-1D-84-A3-5E-00...
//
// Compile with:
// cl /LD /MT /O2 ChatHookDLL_Pattern2.cpp /I"C:\Detours\include" /link /LIBPATH:"C:\Detours\lib.X86" detours.lib /OUT:ChatHookDLL_Pattern2.dll

#include <Windows.h>
#include <stdio.h>
#include <detours.h>  // Microsoft Detours library
#include <Psapi.h>    // For GetModuleInformation

// ============================================================================
// PACKET STRUCTURE DEFINITIONS (from source code analysis)
// ============================================================================

#define MAX_CHAT_SIZE 1024

// GCChat packet structure (reconstructed from source code)
class GCChat {
public:
    // These are the methods we observed being called
    virtual char* GetSourName() = 0;        // Sender name
    virtual int GetSourNameSize() = 0;      // Sender name size
    virtual char* GetContex() = 0;          // Message content
    virtual int GetContexSize() = 0;        // Content size
    virtual unsigned char GetChatType() = 0; // Channel type
    virtual unsigned char GetSourCamp() = 0; // Sender faction
};

// Player class (we don't need to know its structure)
class Player;

// ============================================================================
// FUNCTION POINTER TYPES
// ============================================================================

// Original function signature for GCChatHandler::Execute
// uint __thiscall GCChatHandler::Execute(GCChat* pPacket, Player* pPlayer)
typedef unsigned int (__thiscall *GCChatHandler_Execute_t)(void* thisPtr, GCChat* pPacket, Player* pPlayer);

// Original function pointer (will be set by Detours)
GCChatHandler_Execute_t Original_GCChatHandler_Execute = NULL;

// ============================================================================
// CONFIGURATION
// ============================================================================

#define ENABLE_FILE_LOGGING    1
#define ENABLE_CONSOLE_OUTPUT  0
#define LOG_FILE_PATH          "C:\\DragonOath_ChatLog_Pattern2.txt"

// ============================================================================
// LOGGING FUNCTIONS
// ============================================================================

void LogToFile(const char* format, ...) {
#if ENABLE_FILE_LOGGING
    FILE* logFile = fopen(LOG_FILE_PATH, "a");
    if (logFile) {
        SYSTEMTIME st;
        GetLocalTime(&st);

        fprintf(logFile, "[%02d:%02d:%02d] ", st.wHour, st.wMinute, st.wSecond);

        va_list args;
        va_start(args, format);
        vfprintf(logFile, format, args);
        va_end(args);

        fprintf(logFile, "\n");
        fclose(logFile);
    }
#endif
}

void OutputDebug(const char* format, ...) {
#if ENABLE_CONSOLE_OUTPUT
    char buffer[2048];
    va_list args;
    va_start(args, format);
    vsnprintf(buffer, sizeof(buffer), format, args);
    va_end(args);

    OutputDebugStringA(buffer);
#endif
}

// ============================================================================
// CHAT MESSAGE CALLBACK (CUSTOMIZE THIS)
// ============================================================================

void OnChatMessageReceived(const char* senderName, const char* messageText, unsigned char channelType) {
    // Log to file
    LogToFile("[Channel %d] %s: %s", channelType, senderName, messageText);

    // You can add custom processing here:
    // - Save to database
    // - Send to external application via named pipe/socket
    // - Trigger automation based on keywords
    // - Parse commands from specific players
    // - etc.

    // Example: Check for specific keyword
    if (strstr(messageText, "help") != NULL || strstr(messageText, "帮助") != NULL) {
        LogToFile("  -> Help request detected!");
        // Could trigger some automated response
    }

    // Example: Filter by channel
    if (channelType == 2) {  // Team chat
        LogToFile("  -> Team message logged");
    }
}

// ============================================================================
// HOOKED FUNCTION
// ============================================================================

unsigned int __fastcall Hooked_GCChatHandler_Execute(void* thisPtr, void* edx, GCChat* pPacket, Player* pPlayer) {
    // Extract data from packet BEFORE calling original function
    char senderName[MAX_CHAT_SIZE] = {0};
    char messageText[MAX_CHAT_SIZE] = {0};
    unsigned char channelType = 0;

    if (pPacket) {
        __try {
            // Safely extract sender name
            char* sourName = pPacket->GetSourName();
            int sourNameSize = pPacket->GetSourNameSize();
            if (sourName && sourNameSize > 0 && sourNameSize < MAX_CHAT_SIZE) {
                memcpy(senderName, sourName, sourNameSize);
                senderName[sourNameSize] = '\0';
            }

            // Safely extract message content
            char* context = pPacket->GetContex();
            int contextSize = pPacket->GetContexSize();
            if (context && contextSize > 0 && contextSize < MAX_CHAT_SIZE) {
                memcpy(messageText, context, contextSize);
                messageText[contextSize] = '\0';
            }

            // Get channel type
            channelType = pPacket->GetChatType();

            // Call our callback
            OnChatMessageReceived(senderName, messageText, channelType);

        } __except (EXCEPTION_EXECUTE_HANDLER) {
            LogToFile("ERROR: Exception while extracting packet data");
        }
    }

    // IMPORTANT: Call the original function to maintain normal game behavior
    // Using __thiscall convention manually (thisPtr in ECX, edx is dummy)
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

// ============================================================================
// PATTERN SCANNING (to find function addresses dynamically)
// ============================================================================

DWORD FindPattern(BYTE* pattern, const char* mask, DWORD startAddress, DWORD searchSize) {
    DWORD patternLength = strlen(mask);

    for (DWORD i = 0; i < searchSize - patternLength; i++) {
        bool found = true;
        for (DWORD j = 0; j < patternLength; j++) {
            if (mask[j] != '?' && pattern[j] != *(BYTE*)(startAddress + i + j)) {
                found = false;
                break;
            }
        }
        if (found) {
            return startAddress + i;
        }
    }

    return 0;
}

DWORD FindGCChatHandlerExecute() {
    // Get Game.exe module base
    HMODULE gameModule = GetModuleHandleA("Game.exe");
    if (!gameModule) {
        LogToFile("ERROR: Could not find Game.exe module");
        return 0;
    }

    // Get module info
    MODULEINFO modInfo;
    GetModuleInformation(GetCurrentProcess(), gameModule, &modInfo, sizeof(modInfo));

    DWORD baseAddress = (DWORD)modInfo.lpBaseOfDll;
    DWORD moduleSize = modInfo.SizeOfImage;

    // ========================================================================
    // PATTERN FROM MEMORY SCANNER - VERIFIED ADDRESS: 0x0048D790
    // Scanner output: 55-8B-EC-81-EC-1C-01-00-00-A1-04-49-64-00-53-8B-1D-84-A3-5E-00...
    // Using first 20 bytes for uniqueness
    // ========================================================================
    BYTE pattern[] = {
        0x55, 0x8B, 0xEC, 0x81, 0xEC, 0x1C, 0x01, 0x00,  // push ebp; mov ebp, esp; sub esp, 11Ch
        0x00, 0xA1, 0x04, 0x49, 0x64, 0x00, 0x53, 0x8B,  // mov eax, [security cookie]; push ebx; mov ebx
        0x1D, 0x84, 0xA3, 0x5E                            // continuing...
    };
    const char* mask = "xxxxxxxxxxxxxxxxxxxx";  // All exact matches

    LogToFile("=== Pattern 2 Scanner ===");
    LogToFile("Searching for HandleRecvTalkPacket (Pattern 2: 0x0048D790)...");
    LogToFile("  Base: 0x%08X, Size: 0x%08X", baseAddress, moduleSize);
    LogToFile("  Expected offset: +0x8D790");

    DWORD address = FindPattern(pattern, mask, baseAddress, moduleSize);

    if (address) {
        LogToFile("  Found at: 0x%08X", address);
        LogToFile("  Offset from base: +0x%X", address - baseAddress);
    } else {
        LogToFile("  NOT FOUND - Pattern mismatch or game updated!");
    }

    return address;
}

// ============================================================================
// HOOK INSTALLATION
// ============================================================================

bool InstallHook() {
    LogToFile("=== ChatHook DLL Loaded (Pattern 2) ===");

    // Find address dynamically using pattern scanning
    DWORD functionAddress = FindGCChatHandlerExecute();
    if (functionAddress == 0) {
        LogToFile("ERROR: Could not find HandleRecvTalkPacket");
        MessageBoxA(NULL,
            "Failed to find chat handler function!\n"
            "Pattern 2 did not match.\n"
            "Try Pattern 1 or check if game was updated.",
            "Chat Hook Error - Pattern 2",
            MB_OK | MB_ICONERROR);
        return false;
    }

    Original_GCChatHandler_Execute = (GCChatHandler_Execute_t)functionAddress;

    // Install Detours hook
    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());
    DetourAttach(&(PVOID&)Original_GCChatHandler_Execute, Hooked_GCChatHandler_Execute);
    LONG error = DetourTransactionCommit();

    if (error == NO_ERROR) {
        LogToFile("SUCCESS: Hook installed at 0x%08X", functionAddress);
        MessageBoxA(NULL,
            "Chat hook successfully installed!\n"
            "Using Pattern 2: 0x0048D790\n"
            "Check log: C:\\DragonOath_ChatLog_Pattern2.txt",
            "Chat Hook - Pattern 2",
            MB_OK | MB_ICONINFORMATION);
        return true;
    } else {
        LogToFile("ERROR: Detours failed with error code %d", error);
        MessageBoxA(NULL, "Failed to install hook!", "Chat Hook Error", MB_OK | MB_ICONERROR);
        return false;
    }
}

void UninstallHook() {
    if (Original_GCChatHandler_Execute) {
        DetourTransactionBegin();
        DetourUpdateThread(GetCurrentThread());
        DetourDetach(&(PVOID&)Original_GCChatHandler_Execute, Hooked_GCChatHandler_Execute);
        DetourTransactionCommit();

        LogToFile("Hook uninstalled");
    }
}

// ============================================================================
// DLL ENTRY POINT
// ============================================================================

BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID lpReserved) {
    switch (reason) {
        case DLL_PROCESS_ATTACH:
            // Disable DLL_THREAD_ATTACH/DETACH notifications for performance
            DisableThreadLibraryCalls(hModule);

            // Install the hook
            InstallHook();
            break;

        case DLL_PROCESS_DETACH:
            // Clean up
            UninstallHook();
            LogToFile("=== ChatHook DLL Unloaded (Pattern 2) ===");
            break;
    }

    return TRUE;
}

/*
 * COMPILATION INSTRUCTIONS:
 *
 * 1. Open Visual Studio Developer Command Prompt (x86)
 *
 * 2. Navigate to this directory:
 *    cd G:\microauto-6.9\AutoDragonOath\Docs\test-dll
 *
 * 3. Compile the DLL:
 *    cl /LD /MT /O2 ChatHookDLL_Pattern2.cpp ^
 *       /I"C:\Detours\include" ^
 *       /link /LIBPATH:"C:\Detours\lib.X86" detours.lib Psapi.lib ^
 *       /OUT:ChatHookDLL_Pattern2.dll
 *
 * 4. Use ChatInjector to inject:
 *    ChatInjector.exe ChatHookDLL_Pattern2.dll
 *
 * 5. Check log file:
 *    type C:\DragonOath_ChatLog_Pattern2.txt
 *
 * If this pattern doesn't work, try ChatHookDLL_Pattern1.cpp
 */
