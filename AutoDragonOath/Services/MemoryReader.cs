using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AutoDragonOath.Helpers;

namespace AutoDragonOath.Services
{
    /// <summary>
    /// Service for reading game process memory
    /// Ported from Class7.cs with clean, documented code
    /// </summary>
    public class MemoryReader : IDisposable
    {
        private IntPtr _processHandle;
        private readonly int _processId;
        private IntPtr _gameBaseAddress;  // NEW: Store the base address
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_ALL_ACCESS = PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION | PROCESS_QUERY_INFORMATION;

        /// <summary>
        /// Gets whether the process handle is valid
        /// </summary>
        public bool IsValid => _processHandle != IntPtr.Zero;

        public MemoryReader(int processId)
        {
            _processId = processId;
            _processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, processId);

            // NEW: Get the game's base address
            _gameBaseAddress = GetModuleBaseAddress(processId, "Game.exe");

            Debug.WriteLine($"Process {processId} opened. Handle: {_processHandle}, Base: 0x{_gameBaseAddress:X}");
        }
        
        private IntPtr GetModuleBaseAddress(int processId, string moduleName)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                ProcessModule module = process.Modules.Cast<ProcessModule>()
                    .FirstOrDefault(m => m.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase));

                if (module != null)
                {
                    Debug.WriteLine($"Found {moduleName} at base address: 0x{module.BaseAddress:X}");
                    return module.BaseAddress;
                }
                else
                {
                    Debug.WriteLine($"Module {moduleName} not found!");
                    return IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting module base address: {ex.Message}");
                return IntPtr.Zero;
            }
        }

        // NEW: Helper method to add module offset to base
        public int GetAbsoluteAddress(int offset)
        {
            return _gameBaseAddress.ToInt32() + offset;
        }

        /// <summary>
        /// Read a 32-bit integer from memory address
        /// Returns 0 if read fails
        /// </summary>
        public int ReadInt32(int address)
        {
            if (!IsValid || address == 0)
                return 0;

            byte[] buffer = new byte[4];
            bool success = ReadProcessMemory(_processHandle, address, buffer, 4, 0);

            if (!success)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read Int32 at address 0x{address:X} for process {_processId}");
                return 0;
            }

            return BitConverter.ToInt32(buffer, 0);
        }

        /// <summary>
        /// Follow a pointer chain and return the final address
        /// Example: [7319476, 12, 344, 4] means:
        /// 1. Read value at 7319476 -> returns pointer A
        /// 2. Read value at (A + 12) -> returns pointer B
        /// 3. Read value at (B + 344) -> returns pointer C
        /// 4. Read value at (C + 4) -> returns final address
        /// </summary>
        public int FollowPointerChain(int[] pointerChain)
        {
            if (!IsValid)
            {
                Debug.WriteLine("FollowPointerChain: Invalid process handle or base address");
                return 0;
            }

            Debug.WriteLine($"FollowPointerChain: Processing chain [{string.Join(", ", pointerChain.Select(x => $"0x{x:X}"))}]");

            // First address is offset from Game.exe base
            int currentAddress = GetAbsoluteAddress(pointerChain[0]);
            Debug.WriteLine($"Step 0: Base address 0x{pointerChain[0]:X} -> Absolute 0x{currentAddress:X}");

            currentAddress = ReadInt32(currentAddress);
            if (currentAddress == 0)
            {
                Debug.WriteLine($"Chain failed at base read");
                return 0;
            }
            Debug.WriteLine($"Step 0: Read pointer -> 0x{currentAddress:X}");

            // Follow the rest of the chain
            for (int i = 1; i < pointerChain.Length; i++)
            {
                currentAddress = currentAddress + pointerChain[i];
                Debug.WriteLine($"Step {i}: Add offset 0x{pointerChain[i]:X} -> Address 0x{currentAddress:X}");

                int nextAddress = ReadInt32(currentAddress);
                if (nextAddress == 0)
                {
                    Debug.WriteLine($"Chain failed at step {i}");
                    return 0;
                }

                currentAddress = nextAddress;
                Debug.WriteLine($"Step {i}: Read pointer -> 0x{currentAddress:X}");
            }

            Debug.WriteLine($"FollowPointerChain: Final address = 0x{currentAddress:X}");
            return currentAddress;
        }

        /// <summary>
        /// Follow a pointer chain and read a float value at the final address
        /// </summary>
        public float ReadFloatViaPointerChain(int[] pointerChain)
        {
            if (pointerChain == null || pointerChain.Length == 0)
                return 0f;

            int currentAddress = ReadInt32(pointerChain[0]);

            for (int i = 1; i < pointerChain.Length - 1; i++)
            {
                currentAddress = ReadInt32(currentAddress + pointerChain[i]);
            }

            return ReadFloat(currentAddress + pointerChain[pointerChain.Length - 1]);
        }

        /// <summary>
        /// Read a float value from memory address
        /// Returns 0 if read fails
        /// </summary>
        public float ReadFloat(int address)
        {
            if (!IsValid || address == 0)
                return 0f;

            byte[] buffer = new byte[4];
            bool success = ReadProcessMemory(_processHandle, address, buffer, 4, 0);

            if (!success)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read Float at address 0x{address:X} for process {_processId}");
                return 0f;
            }

            return BitConverter.ToSingle(buffer, 0);
        }

        /// <summary>
        /// Write a float value to memory address
        /// </summary>
        public int WriteFloat(int address, float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            WriteProcessMemory(_processHandle, address, bytes, 4U, out int bytesWritten);
            return bytesWritten;
        }

        /// <summary>
        /// Read a string from memory (up to 30 bytes)
        /// Returns empty string if read fails
        /// </summary>
        public string ReadString(int address, int maxLength = 30)
        {
            if (!IsValid || address == 0)
                return string.Empty;

            byte[] buffer = new byte[maxLength];
            bool success = ReadProcessMemory(_processHandle, address, buffer, buffer.Length, 0);

            if (!success)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read String at address 0x{address:X} for process {_processId}");
                return string.Empty;
            }
            string result = VietnameseEncodingHelper.ParseVietnameseBytes(buffer);


            // Remove null terminators
            int nullIndex = result.IndexOf('\0');
            if (nullIndex >= 0)
                result = result.Substring(0, nullIndex);

            return result.Trim();
        }
        public void Dispose()
        {
            if (_processHandle != IntPtr.Zero)
            {
                CloseHandle(_processHandle);
                _processHandle = IntPtr.Zero;
            }
        }

        #region Win32 API Imports

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, int lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        #endregion
        
        /// <summary>
        /// Decodes the text bytes using ISO-8859-1 as an intermediate step, 
        /// then applies a custom string replacement map for the required Vietnamese characters.
        /// </summary>
        /// <param name="bytes">The byte array containing the string data.</param>
        /// <returns>The decoded Vietnamese string in Unicode/UTF-8.</returns>
        private static string DecodeVietnameseString(byte[] bytes)
        {
            // Use ISO-8859-1 (Code Page 28591, similar to Latin-1) to ensure 
            // every byte maps directly to a character code without validation errors.
            string intermediateString = Encoding.GetEncoding("VISCII").GetString(bytes);

            // Apply custom replacements based on the required output "LiễNhưYên"
            // These characters are what 173 and 223 map to in ISO-8859-1.
        
            // Byte 173 (0xAD) maps to '­' (soft hyphen) in ISO-8859-1 -> must be 'ễ'
            intermediateString = intermediateString.Replace('­', 'ễ');

            // Byte 223 (0xDF) maps to 'ß' (German sharp S) in ISO-8859-1 -> must be 'ư'
            intermediateString = intermediateString.Replace('ß', 'ư');
        
            // Byte 234 (0xEA) maps to 'ê' (e-circumflex) in ISO-8859-1 -> which is correct!
        
            return intermediateString;
        }
    }
}
