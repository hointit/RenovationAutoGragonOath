// ChatInjector.cpp - Injects ChatHookDLL.dll into Game.exe process
// Compile with: cl ChatInjector.cpp
// Usage: ChatInjector.exe [ProcessID]
//    or: ChatInjector.exe (will auto-find Game.exe)

#include <Windows.h>
#include <TlHelp32.h>
#include <stdio.h>
#include <string>

// ============================================================================
// PROCESS UTILITIES
// ============================================================================

DWORD FindProcessByName(const char* processName) {
    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snapshot == INVALID_HANDLE_VALUE) {
        printf("[-] Failed to create process snapshot\n");
        return 0;
    }

    PROCESSENTRY32 entry;
    entry.dwSize = sizeof(PROCESSENTRY32);

    if (!Process32First(snapshot, &entry)) {
        CloseHandle(snapshot);
        printf("[-] Failed to enumerate processes\n");
        return 0;
    }

    DWORD processId = 0;
    do {
        if (_stricmp(entry.szExeFile, processName) == 0) {
            processId = entry.th32ProcessID;
            break;
        }
    } while (Process32Next(snapshot, &entry));

    CloseHandle(snapshot);
    return processId;
}

void ListGameProcesses() {
    printf("\n[*] Searching for Game.exe processes...\n");

    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snapshot == INVALID_HANDLE_VALUE) {
        return;
    }

    PROCESSENTRY32 entry;
    entry.dwSize = sizeof(PROCESSENTRY32);

    if (!Process32First(snapshot, &entry)) {
        CloseHandle(snapshot);
        return;
    }

    int count = 0;
    do {
        if (_stricmp(entry.szExeFile, "Game.exe") == 0) {
            printf("  [%d] PID: %d\n", ++count, entry.th32ProcessID);
        }
    } while (Process32Next(snapshot, &entry));

    CloseHandle(snapshot);

    if (count == 0) {
        printf("  No Game.exe processes found!\n");
    }
}

// ============================================================================
// DLL INJECTION
// ============================================================================

bool InjectDLL(DWORD processId, const char* dllPath) {
    printf("\n[*] Starting injection...\n");
    printf("  Target PID: %d\n", processId);
    printf("  DLL Path: %s\n", dllPath);

    // 1. Open target process
    HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, processId);
    if (!hProcess) {
        printf("[-] Failed to open process (Error: %d)\n", GetLastError());
        printf("    Make sure you're running as Administrator!\n");
        return false;
    }
    printf("[+] Process opened\n");

    // 2. Allocate memory in target process for DLL path
    SIZE_T dllPathSize = strlen(dllPath) + 1;
    LPVOID remoteDllPath = VirtualAllocEx(hProcess, NULL, dllPathSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    if (!remoteDllPath) {
        printf("[-] Failed to allocate memory in target process (Error: %d)\n", GetLastError());
        CloseHandle(hProcess);
        return false;
    }
    printf("[+] Allocated memory at 0x%p\n", remoteDllPath);

    // 3. Write DLL path to target process
    SIZE_T bytesWritten;
    if (!WriteProcessMemory(hProcess, remoteDllPath, dllPath, dllPathSize, &bytesWritten)) {
        printf("[-] Failed to write DLL path (Error: %d)\n", GetLastError());
        VirtualFreeEx(hProcess, remoteDllPath, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }
    printf("[+] Wrote DLL path (%d bytes)\n", bytesWritten);

    // 4. Get address of LoadLibraryA
    HMODULE kernel32 = GetModuleHandleA("kernel32.dll");
    LPTHREAD_START_ROUTINE loadLibraryAddr = (LPTHREAD_START_ROUTINE)GetProcAddress(kernel32, "LoadLibraryA");
    if (!loadLibraryAddr) {
        printf("[-] Failed to get LoadLibraryA address\n");
        VirtualFreeEx(hProcess, remoteDllPath, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }
    printf("[+] LoadLibraryA at 0x%p\n", loadLibraryAddr);

    // 5. Create remote thread to load DLL
    HANDLE hThread = CreateRemoteThread(hProcess, NULL, 0, loadLibraryAddr, remoteDllPath, 0, NULL);
    if (!hThread) {
        printf("[-] Failed to create remote thread (Error: %d)\n", GetLastError());
        VirtualFreeEx(hProcess, remoteDllPath, 0, MEM_RELEASE);
        CloseHandle(hProcess);
        return false;
    }
    printf("[+] Remote thread created\n");

    // 6. Wait for the thread to finish
    printf("[*] Waiting for DLL to load...\n");
    WaitForSingleObject(hThread, INFINITE);

    // 7. Get thread exit code (module handle of loaded DLL)
    DWORD exitCode;
    GetExitCodeThread(hThread, &exitCode);

    // 8. Clean up
    CloseHandle(hThread);
    VirtualFreeEx(hProcess, remoteDllPath, 0, MEM_RELEASE);
    CloseHandle(hProcess);

    if (exitCode == 0) {
        printf("[-] DLL failed to load (LoadLibrary returned NULL)\n");
        printf("    Check that the DLL path is correct and the DLL is compatible\n");
        return false;
    }

    printf("[+] DLL loaded successfully! (Module handle: 0x%08X)\n", exitCode);
    return true;
}

// ============================================================================
// DLL EJECTION (OPTIONAL)
// ============================================================================

bool EjectDLL(DWORD processId, const char* dllName) {
    printf("\n[*] Starting ejection...\n");

    HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, processId);
    if (!hProcess) {
        printf("[-] Failed to open process\n");
        return false;
    }

    // Find the DLL module in target process
    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE, processId);
    if (snapshot == INVALID_HANDLE_VALUE) {
        printf("[-] Failed to create module snapshot\n");
        CloseHandle(hProcess);
        return false;
    }

    MODULEENTRY32 entry;
    entry.dwSize = sizeof(MODULEENTRY32);
    bool found = false;
    HMODULE dllModule = NULL;

    if (Module32First(snapshot, &entry)) {
        do {
            if (_stricmp(entry.szModule, dllName) == 0) {
                dllModule = entry.hModule;
                found = true;
                break;
            }
        } while (Module32Next(snapshot, &entry));
    }

    CloseHandle(snapshot);

    if (!found) {
        printf("[-] DLL not found in process\n");
        CloseHandle(hProcess);
        return false;
    }

    printf("[+] Found DLL at 0x%p\n", dllModule);

    // Get address of FreeLibrary
    HMODULE kernel32 = GetModuleHandleA("kernel32.dll");
    LPTHREAD_START_ROUTINE freeLibraryAddr = (LPTHREAD_START_ROUTINE)GetProcAddress(kernel32, "FreeLibrary");

    // Create remote thread to unload DLL
    HANDLE hThread = CreateRemoteThread(hProcess, NULL, 0, freeLibraryAddr, dllModule, 0, NULL);
    if (!hThread) {
        printf("[-] Failed to create remote thread\n");
        CloseHandle(hProcess);
        return false;
    }

    WaitForSingleObject(hThread, INFINITE);
    CloseHandle(hThread);
    CloseHandle(hProcess);

    printf("[+] DLL ejected successfully\n");
    return true;
}

// ============================================================================
// MAIN
// ============================================================================

void PrintUsage() {
    printf("=======================================================\n");
    printf("  Dragon Oath Chat Hook Injector\n");
    printf("=======================================================\n");
    printf("\nUsage:\n");
    printf("  ChatInjector.exe              - Auto-find Game.exe and inject\n");
    printf("  ChatInjector.exe <PID>        - Inject into specific process ID\n");
    printf("  ChatInjector.exe -eject <PID> - Eject DLL from process\n");
    printf("  ChatInjector.exe -list        - List all Game.exe processes\n");
    printf("\nExamples:\n");
    printf("  ChatInjector.exe\n");
    printf("  ChatInjector.exe 12345\n");
    printf("  ChatInjector.exe -eject 12345\n");
    printf("\n");
}

int main(int argc, char* argv[]) {
    PrintUsage();

    // Get DLL path (same directory as injector)
    char dllPath[MAX_PATH];
    GetCurrentDirectoryA(MAX_PATH, dllPath);
    strcat_s(dllPath, "\\ChatHookDLL.dll");

    // Check if DLL exists
    if (GetFileAttributesA(dllPath) == INVALID_FILE_ATTRIBUTES) {
        printf("[-] ERROR: ChatHookDLL.dll not found in current directory!\n");
        printf("    Expected path: %s\n", dllPath);
        system("pause");
        return 1;
    }

    printf("[+] Found DLL: %s\n", dllPath);

    // Parse command line
    if (argc == 2 && strcmp(argv[1], "-list") == 0) {
        ListGameProcesses();
        system("pause");
        return 0;
    }

    if (argc == 3 && strcmp(argv[1], "-eject") == 0) {
        DWORD processId = atoi(argv[2]);
        if (processId == 0) {
            printf("[-] Invalid process ID\n");
            system("pause");
            return 1;
        }
        EjectDLL(processId, "ChatHookDLL.dll");
        system("pause");
        return 0;
    }

    // Get target process ID
    DWORD processId = 0;

    if (argc >= 2) {
        // Use provided PID
        processId = atoi(argv[1]);
        if (processId == 0) {
            printf("[-] Invalid process ID: %s\n", argv[1]);
            system("pause");
            return 1;
        }
    } else {
        // Auto-find Game.exe
        printf("[*] Searching for Game.exe...\n");
        processId = FindProcessByName("Game.exe");

        if (processId == 0) {
            printf("[-] Game.exe not found!\n");
            printf("    Please start the game first, or specify a process ID manually.\n\n");
            ListGameProcesses();
            system("pause");
            return 1;
        }

        printf("[+] Found Game.exe (PID: %d)\n", processId);
    }

    // Inject DLL
    bool success = InjectDLL(processId, dllPath);

    if (success) {
        printf("\n========================================\n");
        printf("  INJECTION SUCCESSFUL!\n");
        printf("========================================\n");
        printf("  Chat messages will be logged to:\n");
        printf("  C:\\DragonOath_ChatLog.txt\n");
        printf("\n");
        printf("  To eject the DLL:\n");
        printf("  ChatInjector.exe -eject %d\n", processId);
        printf("========================================\n");
    } else {
        printf("\n========================================\n");
        printf("  INJECTION FAILED!\n");
        printf("========================================\n");
        printf("  Common issues:\n");
        printf("  - Not running as Administrator\n");
        printf("  - Anti-virus blocking injection\n");
        printf("  - Anti-cheat detecting injection\n");
        printf("  - Wrong process ID\n");
        printf("  - DLL is not compatible (x86 vs x64)\n");
        printf("========================================\n");
    }

    system("pause");
    return success ? 0 : 1;
}
