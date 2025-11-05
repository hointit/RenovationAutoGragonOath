using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AutoDragonOath.Services
{
    /// <summary>
    /// Extends MemoryReader with write capabilities for test automation.
    /// Allows setting up test scenarios by modifying game state in memory.
    /// </summary>
    public class MemoryWriter : MemoryReader
    {
        private IntPtr _processHandle;
        private readonly int _processId;
        private IntPtr _gameBaseAddress;  // NEW: Store the base address
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_ALL_ACCESS = PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION | PROCESS_QUERY_INFORMATION;
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            out IntPtr lpNumberOfBytesWritten);

        public MemoryWriter(int processId) : base(processId)
        {
            _processId = processId;
            _processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, processId);

            // NEW: Get the game's base address
            _gameBaseAddress = GetModuleBaseAddress(processId, "Game.exe");
        }

        /// <summary>
        /// Write a 32-bit integer to the specified memory address.
        /// </summary>
        /// <param name="address">Target memory address</param>
        /// <param name="value">Value to write</param>
        /// <returns>True if write succeeded</returns>
        public bool WriteInt32(IntPtr address, int value)
        {
            if (_processHandle == IntPtr.Zero)
                return false;

            byte[] buffer = BitConverter.GetBytes(value);
            bool success = WriteProcessMemory(_processHandle, address, buffer, buffer.Length, out IntPtr bytesWritten);

            return success && bytesWritten.ToInt32() == buffer.Length;
        }

        /// <summary>
        /// Write a float value to the specified memory address.
        /// </summary>
        public bool WriteFloat(IntPtr address, float value)
        {
            if (_processHandle == IntPtr.Zero)
                return false;

            byte[] buffer = BitConverter.GetBytes(value);
            bool success = WriteProcessMemory(_processHandle, address, buffer, buffer.Length, out IntPtr bytesWritten);

            return success && bytesWritten.ToInt32() == buffer.Length;
        }

        /// <summary>
        /// Write a string to the specified memory address.
        /// </summary>
        public bool WriteString(IntPtr address, string value, int maxLength)
        {
            if (_processHandle == IntPtr.Zero)
                return false;

            byte[] buffer = new byte[maxLength];
            byte[] stringBytes = Encoding.ASCII.GetBytes(value);

            // Copy string bytes (truncate if necessary)
            Array.Copy(stringBytes, buffer, Math.Min(stringBytes.Length, maxLength - 1));

            bool success = WriteProcessMemory(_processHandle, address, buffer, buffer.Length, out IntPtr bytesWritten);

            return success && bytesWritten.ToInt32() == buffer.Length;
        }

        /// <summary>
        /// Write raw bytes to memory.
        /// </summary>
        public bool WriteBytes(IntPtr address, byte[] data)
        {
            if (_processHandle == IntPtr.Zero)
                return false;

            bool success = WriteProcessMemory(_processHandle, address, data, data.Length, out IntPtr bytesWritten);

            return success && bytesWritten.ToInt32() == data.Length;
        }

        // ==============================================
        // High-Level Test Setup Methods
        // ==============================================

        /// <summary>
        /// Set character experience to a specific value for testing.
        /// </summary>
        /// <param name="experience">Experience value to set</param>
        /// <returns>True if successful</returns>
        public bool SetCharacterExperience(int experience)
        {
            try
            {
                // Follow pointer chain to get stats base
                IntPtr statsBase = (IntPtr)FollowPointerChain(new int[] { 2381824, 12, 340, 4 });
                if (statsBase == IntPtr.Zero)
                    return false;

                // Write experience at offset 2408
                const int OFFSET_EXPERIENCE = 2408;
                IntPtr expAddress = IntPtr.Add(statsBase, OFFSET_EXPERIENCE);

                return WriteInt32(expAddress, experience);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Set character HP to a specific value for testing.
        /// </summary>
        public bool SetCharacterHP(int currentHP, int maxHP)
        {
            try
            {
                IntPtr statsBase = (IntPtr)FollowPointerChain(new int[] { 2381824, 12, 340, 4 });
                if (statsBase == IntPtr.Zero)
                    return false;

                const int OFFSET_CURRENT_HP = 1752;
                const int OFFSET_MAX_HP = 1856;

                bool success1 = WriteInt32(IntPtr.Add(statsBase, OFFSET_CURRENT_HP), currentHP);
                bool success2 = WriteInt32(IntPtr.Add(statsBase, OFFSET_MAX_HP), maxHP);

                return success1 && success2;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Set character MP to a specific value for testing.
        /// </summary>
        public bool SetCharacterMP(int currentMP, int maxMP)
        {
            try
            {
                IntPtr statsBase = (IntPtr)FollowPointerChain(new int[] { 2381824, 12, 340, 4 });
                if (statsBase == IntPtr.Zero)
                    return false;

                const int OFFSET_CURRENT_MP = 1756;
                const int OFFSET_MAX_MP = 1860;

                bool success1 = WriteInt32(IntPtr.Add(statsBase, OFFSET_CURRENT_MP), currentMP);
                bool success2 = WriteInt32(IntPtr.Add(statsBase, OFFSET_MAX_MP), maxMP);

                return success1 && success2;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Set character position for testing (e.g., test teleport or movement).
        /// </summary>
        public bool SetCharacterPosition(float x, float y)
        {
            try
            {
                IntPtr entityBase = (IntPtr)FollowPointerChain(new int[] { 2381824, 12 });
                if (entityBase == IntPtr.Zero)
                    return false;

                const int OFFSET_X = 92;
                const int OFFSET_Y = 100;

                bool success1 = WriteFloat(IntPtr.Add(entityBase, OFFSET_X), x);
                bool success2 = WriteFloat(IntPtr.Add(entityBase, OFFSET_Y), y);

                return success1 && success2;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Set character level directly (useful for testing level-dependent features).
        /// </summary>
        public bool SetCharacterLevel(int level)
        {
            try
            {
                IntPtr statsBase = (IntPtr)FollowPointerChain(new int[] { 2381824, 12, 340, 4 });
                if (statsBase == IntPtr.Zero)
                    return false;

                const int OFFSET_LEVEL = 92;
                IntPtr levelAddress = IntPtr.Add(statsBase, OFFSET_LEVEL);

                return WriteInt32(levelAddress, level);
            }
            catch
            {
                return false;
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
        public IntPtr GetModuleBaseAddress(int processId, string moduleName)
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
        #endregion
    }
}
