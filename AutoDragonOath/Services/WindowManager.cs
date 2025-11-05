using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AutoDragonOath.Services
{
    /// <summary>
    /// Service for managing game windows
    /// </summary>
    public static class WindowManager
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        /// <summary>
        /// Bring the window of a process to the front
        /// </summary>
        /// <param name="processId">The process ID of the window to bring to front</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool BringWindowToFront(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                if (process == null || process.MainWindowHandle == IntPtr.Zero)
                {
                    Debug.WriteLine($"Process {processId} not found or has no main window");
                    return false;
                }

                IntPtr windowHandle = process.MainWindowHandle;

                // If the window is minimized, restore it
                if (IsIconic(windowHandle))
                {
                    ShowWindow(windowHandle, SW_RESTORE);
                }
                else
                {
                    ShowWindow(windowHandle, SW_SHOW);
                }

                // Bring window to foreground
                bool result = SetForegroundWindow(windowHandle);

                if (result)
                {
                    Debug.WriteLine($"Successfully brought process {processId} to front");
                }
                else
                {
                    Debug.WriteLine($"Failed to bring process {processId} to front");
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error bringing window to front: {ex.Message}");
                return false;
            }
        }
    }
}
