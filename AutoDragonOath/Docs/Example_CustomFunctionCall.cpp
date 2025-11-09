// Example_CustomFunctionCall.cpp
// Demonstrates calling custom functions after intercepting chat messages
// Complete working example with multiple automation scenarios

#include <Windows.h>
#include <stdio.h>
#include <string.h>
#include <detours.h>

// ============================================================================
// GAME FUNCTION DEFINITIONS (Find these addresses in IDA)
// ============================================================================

// Example game functions you might want to call
// (These are placeholders - replace with real addresses from IDA)

// Send a chat message back to the channel
typedef void (__cdecl* SendChatMessage_t)(const char* message, int channel);
SendChatMessage_t SendChatMessage = (SendChatMessage_t)0x00000000;  // TODO: Find in IDA

// Accept party invite
typedef void (__cdecl* AcceptPartyInvite_t)();
AcceptPartyInvite_t AcceptPartyInvite = (AcceptPartyInvite_t)0x00000000;  // TODO: Find in IDA

// Follow a player by name
typedef void (__cdecl* FollowPlayer_t)(const char* playerName);
FollowPlayer_t FollowPlayer = (FollowPlayer_t)0x00000000;  // TODO: Find in IDA

// Use item from inventory
typedef void (__cdecl* UseItem_t)(int itemId);
UseItem_t UseItem = (UseItem_t)0x00000000;  // TODO: Find in IDA

// Get player stats
typedef int (__cdecl* GetPlayerHP_t)();
GetPlayerHP_t GetPlayerHP = (GetPlayerHP_t)0x00000000;  // TODO: Find in IDA

typedef int (__cdecl* GetPlayerMaxHP_t)();
GetPlayerMaxHP_t GetPlayerMaxHP = (GetPlayerMaxHP_t)0x00000000;  // TODO: Find in IDA

// ============================================================================
// PACKET STRUCTURE
// ============================================================================

class GCChat {
public:
    virtual char* GetSourName() = 0;
    virtual int GetSourNameSize() = 0;
    virtual char* GetContex() = 0;
    virtual int GetContexSize() = 0;
    virtual unsigned char GetChatType() = 0;
};

// ============================================================================
// HOOK SETUP
// ============================================================================

typedef int (__thiscall* HandleRecvTalkPacket_t)(void* thisPtr, GCChat* pPacket);
HandleRecvTalkPacket_t Original_HandleRecvTalkPacket = NULL;

// ============================================================================
// LOGGING
// ============================================================================

void Log(const char* format, ...) {
    FILE* f = fopen("C:\\ChatHookExample.log", "a");
    if (f) {
        SYSTEMTIME st;
        GetLocalTime(&st);
        fprintf(f, "[%02d:%02d:%02d] ", st.wHour, st.wMinute, st.wSecond);

        va_list args;
        va_start(args, format);
        vfprintf(f, format, args);
        va_end(args);

        fprintf(f, "\n");
        fclose(f);
    }
}

// ============================================================================
// CUSTOM AUTOMATION FUNCTIONS
// ============================================================================

// Example 1: Auto-reply bot
void HandleHelpRequest(const char* sender, const char* message) {
    Log("Help request from %s", sender);

    // Send help message back
    if (SendChatMessage) {
        SendChatMessage("Available commands: !help, !status, !follow, !heal", 1);  // Channel 1 = Near
    }
}

// Example 2: Status command
void HandleStatusCommand(const char* sender, const char* message) {
    Log("Status request from %s", sender);

    if (GetPlayerHP && GetPlayerMaxHP && SendChatMessage) {
        int hp = GetPlayerHP();
        int maxHp = GetPlayerMaxHP();
        int hpPercent = (hp * 100) / maxHp;

        char buffer[256];
        sprintf(buffer, "HP: %d/%d (%d%%)", hp, maxHp, hpPercent);
        SendChatMessage(buffer, 1);

        // If low HP, use healing item
        if (hpPercent < 30 && UseItem) {
            Log("  -> Low HP detected, using healing item");
            UseItem(12345);  // Replace with actual healing item ID
        }
    }
}

// Example 3: Follow command
void HandleFollowCommand(const char* sender, const char* message) {
    Log("Follow request from %s", sender);

    // Parse: "!follow PlayerName"
    const char* targetName = strchr(message, ' ');
    if (targetName) {
        targetName++;  // Skip space

        if (strlen(targetName) > 0 && FollowPlayer) {
            Log("  -> Following player: %s", targetName);
            FollowPlayer(targetName);

            if (SendChatMessage) {
                char reply[128];
                sprintf(reply, "Now following %s", targetName);
                SendChatMessage(reply, 1);
            }
        }
    } else {
        // Follow the sender if no target specified
        if (FollowPlayer) {
            Log("  -> Following sender: %s", sender);
            FollowPlayer(sender);
        }
    }
}

// Example 4: Party invite auto-accept
void HandlePartyInvite(const char* message) {
    // System message format: "玩家 [PlayerName] 邀请你加入队伍"
    // Translation: "Player [PlayerName] invites you to party"

    if (strstr(message, "邀请你加入队伍") || strstr(message, "invites you to party")) {
        Log("Party invite detected!");

        if (AcceptPartyInvite) {
            Log("  -> Auto-accepting party invite");
            AcceptPartyInvite();

            Sleep(500);  // Wait a bit

            if (SendChatMessage) {
                SendChatMessage("Thanks for the invite!", 2);  // Channel 2 = Team
            }
        }
    }
}

// Example 5: Trade bot
void HandleTradeRequest(const char* sender, const char* message) {
    // Example: "!buy sword 1000" or "!sell potion 50"

    if (strncmp(message, "!buy", 4) == 0) {
        Log("Buy request from %s: %s", sender, message);

        // Parse item and price
        // In real implementation, check inventory, prices, etc.

        if (SendChatMessage) {
            SendChatMessage("Sorry, I'm not selling that item right now.", 4);  // Channel 4 = Private
        }
    }
    else if (strncmp(message, "!sell", 5) == 0) {
        Log("Sell request from %s: %s", sender, message);

        if (SendChatMessage) {
            SendChatMessage("I can buy that! Let's trade.", 4);
        }

        // Could call OpenTradeWindow(sender) here
    }
}

// Example 6: Keyword trigger
void HandleKeywordTrigger(const char* sender, const char* message, int channel) {
    // Monitor for specific keywords and react

    // Example: Guild gathering announcement
    if (channel == 3 && strstr(message, "公会集合")) {  // "Guild gathering" in Chinese
        Log("Guild gathering announcement detected!");

        // Auto-respond
        if (SendChatMessage) {
            SendChatMessage("收到！马上来！", 3);  // "Received! Coming now!" in guild chat
        }

        // Could auto-navigate to meeting point here
        // TeleportToGuildHall();
    }

    // Example: Boss spawn notification
    if (strstr(message, "BOSS刷新") || strstr(message, "Boss spawned")) {
        Log("Boss spawn detected!");

        // Alert or auto-navigate
        if (SendChatMessage) {
            SendChatMessage("On my way to boss!", 1);
        }

        // NavigateToBossLocation();
    }
}

// ============================================================================
// COMMAND DISPATCHER
// ============================================================================

void ProcessChatCommand(const char* sender, const char* message, int channel) {
    // Command router - calls appropriate function based on message content

    // Help command
    if (strcmp(message, "!help") == 0 || strstr(message, "帮助")) {
        HandleHelpRequest(sender, message);
    }
    // Status command
    else if (strcmp(message, "!status") == 0) {
        HandleStatusCommand(sender, message);
    }
    // Follow command
    else if (strncmp(message, "!follow", 7) == 0) {
        HandleFollowCommand(sender, message);
    }
    // Trade commands
    else if (strncmp(message, "!buy", 4) == 0 || strncmp(message, "!sell", 5) == 0) {
        HandleTradeRequest(sender, message);
    }
    // System messages (party invites, etc.)
    else if (channel == 5) {  // System channel
        HandlePartyInvite(message);
    }
    // Keyword monitoring
    else {
        HandleKeywordTrigger(sender, message, channel);
    }
}

// ============================================================================
// HOOKED FUNCTION
// ============================================================================

int __fastcall Hooked_HandleRecvTalkPacket(void* thisPtr, void* edx, GCChat* pPacket) {
    char senderName[256] = {0};
    char messageText[1024] = {0};
    unsigned char channelType = 0;

    if (pPacket) {
        __try {
            // Extract data from packet
            char* sender = pPacket->GetSourName();
            int senderSize = pPacket->GetSourNameSize();
            if (sender && senderSize > 0 && senderSize < 256) {
                memcpy(senderName, sender, senderSize);
            }

            char* message = pPacket->GetContex();
            int messageSize = pPacket->GetContexSize();
            if (message && messageSize > 0 && messageSize < 1024) {
                memcpy(messageText, message, messageSize);
            }

            channelType = pPacket->GetChatType();

            // Log the message
            Log("[Channel %d] %s: %s", channelType, senderName, messageText);

            // Process commands and automation
            ProcessChatCommand(senderName, messageText, channelType);

        } __except (EXCEPTION_EXECUTE_HANDLER) {
            Log("ERROR: Exception in hook");
        }
    }

    // Call original function
    int result;
    __asm {
        mov ecx, thisPtr
        push pPacket
        call Original_HandleRecvTalkPacket
        mov result, eax
    }

    return result;
}

// ============================================================================
// ADVANCED EXAMPLE: STATE MACHINE BOT
// ============================================================================

enum BotState {
    STATE_IDLE,
    STATE_TRADING,
    STATE_FOLLOWING,
    STATE_IN_PARTY,
    STATE_COMBAT
};

class SimpleBot {
private:
    BotState currentState;
    char currentTarget[64];
    DWORD lastActionTime;

public:
    SimpleBot() : currentState(STATE_IDLE), lastActionTime(0) {
        memset(currentTarget, 0, sizeof(currentTarget));
    }

    void OnChatReceived(const char* sender, const char* message, int channel) {
        DWORD currentTime = GetTickCount();

        switch (currentState) {
            case STATE_IDLE:
                if (strstr(message, "!trade")) {
                    Log("Entering TRADING state");
                    currentState = STATE_TRADING;
                    strcpy_s(currentTarget, sender);
                    // OpenTradeWith(sender);
                }
                else if (strstr(message, "!follow")) {
                    Log("Entering FOLLOWING state");
                    currentState = STATE_FOLLOWING;
                    strcpy_s(currentTarget, sender);
                    // FollowPlayer(sender);
                }
                break;

            case STATE_TRADING:
                if (strcmp(sender, currentTarget) == 0) {
                    if (strstr(message, "!cancel")) {
                        Log("Trade cancelled");
                        currentState = STATE_IDLE;
                        // CancelTrade();
                    }
                    else if (strstr(message, "!accept")) {
                        Log("Accepting trade");
                        // AcceptTrade();
                        currentState = STATE_IDLE;
                    }
                }
                break;

            case STATE_FOLLOWING:
                if (strcmp(sender, currentTarget) == 0) {
                    if (strstr(message, "!stop")) {
                        Log("Stopped following");
                        currentState = STATE_IDLE;
                        // StopFollowing();
                    }
                    else if (strstr(message, "!attack")) {
                        Log("Switching to combat mode");
                        currentState = STATE_COMBAT;
                        // StartCombat();
                    }
                }
                break;

            case STATE_COMBAT:
                // Auto-reply to chat while in combat
                if (currentTime - lastActionTime > 5000) {  // Every 5 seconds
                    if (SendChatMessage) {
                        SendChatMessage("I'm in combat, will respond later!", 4);
                    }
                    lastActionTime = currentTime;
                }
                break;
        }
    }
};

// Global bot instance
SimpleBot g_Bot;

// ============================================================================
// PATTERN SCANNING & HOOK INSTALLATION
// ============================================================================

DWORD FindPattern(BYTE* pattern, const char* mask, DWORD base, DWORD size) {
    DWORD patternLen = strlen(mask);
    for (DWORD i = 0; i < size - patternLen; i++) {
        bool found = true;
        for (DWORD j = 0; j < patternLen; j++) {
            if (mask[j] != '?' && pattern[j] != *(BYTE*)(base + i + j)) {
                found = false;
                break;
            }
        }
        if (found) return base + i;
    }
    return 0;
}

bool InstallHook() {
    Log("=== Chat Hook Example DLL Loaded ===");

    // Find HandleRecvTalkPacket
    HMODULE gameModule = GetModuleHandleA("Game.exe");
    if (!gameModule) {
        Log("ERROR: Game.exe module not found");
        return false;
    }

    MODULEINFO modInfo;
    GetModuleInformation(GetCurrentProcess(), gameModule, &modInfo, sizeof(modInfo));

    // TODO: Update this pattern from your IDA analysis
    BYTE pattern[] = { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x00, 0x53, 0x56, 0x57, 0x8B, 0xF9 };
    const char* mask = "xxxxx?xxxxx";

    DWORD address = FindPattern(pattern, mask, (DWORD)modInfo.lpBaseOfDll, modInfo.SizeOfImage);

    if (!address) {
        Log("ERROR: Pattern not found");
        MessageBoxA(NULL, "Failed to find HandleRecvTalkPacket!\nUpdate the pattern in the code.", "Error", MB_OK);
        return false;
    }

    Log("Found HandleRecvTalkPacket at 0x%08X", address);

    Original_HandleRecvTalkPacket = (HandleRecvTalkPacket_t)address;

    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());
    DetourAttach(&(PVOID&)Original_HandleRecvTalkPacket, Hooked_HandleRecvTalkPacket);
    LONG error = DetourTransactionCommit();

    if (error == NO_ERROR) {
        Log("SUCCESS: Hook installed!");
        return true;
    } else {
        Log("ERROR: Detours failed with code %d", error);
        return false;
    }
}

void UninstallHook() {
    if (Original_HandleRecvTalkPacket) {
        DetourTransactionBegin();
        DetourUpdateThread(GetCurrentThread());
        DetourDetach(&(PVOID&)Original_HandleRecvTalkPacket, Hooked_HandleRecvTalkPacket);
        DetourTransactionCommit();
        Log("Hook uninstalled");
    }
}

// ============================================================================
// DLL ENTRY POINT
// ============================================================================

BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID lpReserved) {
    if (reason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hModule);
        InstallHook();

        // Initialize game function pointers here
        // TODO: Find these addresses in IDA and update
        /*
        SendChatMessage = (SendChatMessage_t)0x12345678;
        AcceptPartyInvite = (AcceptPartyInvite_t)0x23456789;
        FollowPlayer = (FollowPlayer_t)0x34567890;
        UseItem = (UseItem_t)0x45678901;
        GetPlayerHP = (GetPlayerHP_t)0x56789012;
        GetPlayerMaxHP = (GetPlayerMaxHP_t)0x67890123;
        */
    }
    else if (reason == DLL_PROCESS_DETACH) {
        UninstallHook();
        Log("=== Chat Hook Example DLL Unloaded ===");
    }

    return TRUE;
}

/*
 * USAGE:
 *
 * 1. Find function addresses in IDA Pro:
 *    - HandleRecvTalkPacket (update pattern)
 *    - SendChatMessage
 *    - AcceptPartyInvite
 *    - FollowPlayer
 *    - UseItem
 *    - GetPlayerHP / GetPlayerMaxHP
 *
 * 2. Update the function pointers at the top of this file
 *
 * 3. Compile:
 *    cl /LD /MT Example_CustomFunctionCall.cpp /I"C:\Detours\include" ^
 *       /link /LIBPATH:"C:\Detours\lib.X86" detours.lib
 *
 * 4. Inject into Game.exe
 *
 * 5. Test in-game by typing:
 *    - !help
 *    - !status
 *    - !follow PlayerName
 *    - !buy item 100
 *
 * Log file will be created at: C:\ChatHookExample.log
 */
