namespace AutoDragonOath.Services;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class LuaPlusRemoteCaller
{
    // Win32 API functions for process/memory/thread manipulation
    [DllImport("kernel32.dll")] public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
    [DllImport("kernel32.dll", SetLastError = true)] public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
    [DllImport("kernel32.dll", SetLastError = true)] public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);
    [DllImport("kernel32.dll")] public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);
    [DllImport("kernel32.dll", SetLastError = true)] public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
    [DllImport("kernel32.dll")] public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
    const uint MEM_COMMIT = 0x1000;
    const uint PAGE_EXECUTE_READWRITE = 0x40;
    const uint PAGE_READWRITE = 0x04;
    const uint INFINITE = 0xFFFFFFFF;

    public static IntPtr GetDllBaseAddress(int pid)
    {
        string dllName = "LuaPlus.dll";  // Name of your DLL

        Process process = Process.GetProcessById(pid);

        IntPtr dllBaseAddress = IntPtr.Zero;

        foreach (ProcessModule module in process.Modules)
        {
            if (module.ModuleName.Equals(dllName, StringComparison.OrdinalIgnoreCase))
            {
                dllBaseAddress = module.BaseAddress;
                Console.WriteLine($"DLL '{dllName}' base address: 0x{dllBaseAddress.ToInt64():X}");
                break;
            }
        }

        if (dllBaseAddress == IntPtr.Zero)
        {
            Console.WriteLine("DLL not found in process.");
        }

        return dllBaseAddress;
    }

    public static IntPtr InjectAndCall(int targetPid)
    {
        // 1. Open process
        IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, targetPid);

        // 2. Allocate remote memory for result (void* returned)
        IntPtr resultAddr = VirtualAllocEx(hProcess, IntPtr.Zero, 4, MEM_COMMIT, PAGE_READWRITE);

        // 3. Allocate remote memory for shellcode
        IntPtr shellcodeAddr = VirtualAllocEx(hProcess, IntPtr.Zero, 100, MEM_COMMIT, PAGE_EXECUTE_READWRITE);

        // 4. Build x86 shellcode
        // mov ecx, objectAddr (B9 xx xx xx xx)
        // call methodAddr (E8 xx xx xx xx)
        // mov [resultAddr], eax (A3 xx xx xx xx)
        // ret (C3)

        var objectAddr = GetDllBaseAddress(targetPid);
        IntPtr methodAddr = IntPtr.Add(objectAddr, 6832);
        
        var shellcode = new byte[16];
        

        shellcode[0] = 0xB9; // mov ecx, imm32
        BitConverter.GetBytes((int)objectAddr).CopyTo(shellcode, 1);

        shellcode[5] = 0xE8; // call rel32

        // Calculate relative offset for call: target - (next instruction)
        // shellcodeAddr + 9 is address after call instruction
        int callRel = (int)methodAddr - ((int)shellcodeAddr + 9); // 5 bytes + 4 bytes before call
        BitConverter.GetBytes(callRel).CopyTo(shellcode, 6);

        shellcode[10] = 0xA3; // mov [imm32], eax
        BitConverter.GetBytes((int)resultAddr).CopyTo(shellcode, 11);

        shellcode[15] = 0xC3; // ret

        // 5. Write shellcode
        IntPtr nBytes1;
        if (!WriteProcessMemory(hProcess, shellcodeAddr, shellcode, shellcode.Length, out nBytes1))
            throw new Exception("Can't write shellcode");

        // 6. Create remote thread at shellcode start
        uint threadId = 0;
        IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, shellcodeAddr, IntPtr.Zero, 0, out threadId);

        // 7. Wait for thread
        WaitForSingleObject(hThread, INFINITE);

        // 8. Read result pointer
        var resultBytes = new byte[4];
        IntPtr nBytesRead;
        if (!ReadProcessMemory(hProcess, resultAddr, resultBytes, 4, out nBytesRead))
            throw new Exception("Can't read result");

        int userDataPtr = BitConverter.ToInt32(resultBytes, 0);
        return (IntPtr)userDataPtr;
    }
}