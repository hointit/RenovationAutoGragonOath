using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoDragonOath.Services
{
    /// <summary>
    /// Service for executing skills in the game by simulating keyboard input
    /// This is the RECOMMENDED approach - safer and more reliable than direct function calling
    /// </summary>
    public class SkillExecutor
    {
        private readonly int _processId;
        private IntPtr _gameWindowHandle;

        // Windows Message Constants
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;

        // F-key virtual codes (F1 = 0x70, F2 = 0x71, ..., F12 = 0x7B)
        private const int VK_F1 = 0x70;

        public SkillExecutor(int processId)
        {
            _processId = processId;
            _gameWindowHandle = GetGameWindowHandle(processId);

            if (_gameWindowHandle == IntPtr.Zero)
            {
                Debug.WriteLine($"Warning: Could not find window for process {processId}");
            }
            else
            {
                Debug.WriteLine($"SkillExecutor initialized for process {processId}, window handle: 0x{_gameWindowHandle:X}");
            }
        }

        /// <summary>
        /// Execute a skill by simulating F1-F12 keypress
        /// </summary>
        /// <param name="skillSlot">Skill slot (0-11 for F1-F12)</param>
        /// <returns>True if successful</returns>
        public bool ExecuteSkill(int skillSlot)
        {
            try
            {
                // Calculate F-key code (F1=0x70, F2=0x71, etc.)
                int vkCode = VK_F1 + skillSlot;
                string keyName = $"F{skillSlot + 1}";

                Debug.WriteLine($"Simulating {keyName} keypress (VK code: 0x{vkCode:X}) to window 0x{_gameWindowHandle:X}");

                // Send key down
                bool keyDownResult = PostMessage(_gameWindowHandle, WM_KEYDOWN, vkCode, 0);
                Debug.WriteLine($"PostMessage WM_KEYDOWN result: {keyDownResult}");

                // Small delay to simulate real keypress
                Thread.Sleep(50);

                // Send key up
                bool keyUpResult = PostMessage(_gameWindowHandle, WM_KEYUP, vkCode, 0);
                Debug.WriteLine($"PostMessage WM_KEYUP result: {keyUpResult}");

                if (keyDownResult && keyUpResult)
                {
                    Debug.WriteLine($"Successfully simulated {keyName} press");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"Failed to send {keyName} press (Down: {keyDownResult}, Up: {keyUpResult})");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error executing skill: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute a skill and wait for result
        /// </summary>
        public bool ExecuteSkillAndWait(int skillSlot, int waitMs = 100)
        {
            bool result = ExecuteSkill(skillSlot);
            if (result && waitMs > 0)
            {
                Thread.Sleep(waitMs);
            }
            return result;
        }

        /// <summary>
        /// Execute multiple skills in sequence
        /// </summary>
        public void ExecuteSkillCombo(int[] skillSlots, int delayBetweenSkills = 200)
        {
            foreach (int slot in skillSlots)
            {
                if (ExecuteSkill(slot))
                {
                    Thread.Sleep(delayBetweenSkills);
                }
                else
                {
                    Debug.WriteLine($"Skill combo interrupted at slot {slot}");
                    break;
                }
            }
        }

        /// <summary>
        /// Get the game window handle from process ID
        /// </summary>
        private IntPtr GetGameWindowHandle(int processId)
        {
            try
            {
                Process process = Process.GetProcessById(processId);

                // Method 1: Get main window handle
                IntPtr mainWindow = process.MainWindowHandle;
                if (mainWindow != IntPtr.Zero)
                {
                    Debug.WriteLine($"Found main window: 0x{mainWindow:X}");
                    return mainWindow;
                }

                // Method 2: Enumerate all windows for this process
                Debug.WriteLine("Main window not found, enumerating all windows...");
                IntPtr foundHandle = IntPtr.Zero;

                EnumWindows((hWnd, lParam) =>
                {
                    GetWindowThreadProcessId(hWnd, out uint windowProcessId);
                    if (windowProcessId == processId)
                    {
                        // Check if window is visible
                        if (IsWindowVisible(hWnd))
                        {
                            foundHandle = hWnd;
                            Debug.WriteLine($"Found visible window: 0x{hWnd:X}");
                            return false; // Stop enumeration
                        }
                    }
                    return true; // Continue enumeration
                }, IntPtr.Zero);

                return foundHandle;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting window handle: {ex.Message}");
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Refresh the game window handle (call if window changes)
        /// </summary>
        public void RefreshWindowHandle()
        {
            _gameWindowHandle = GetGameWindowHandle(_processId);
        }

        #region Win32 API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        #endregion
    }
}
