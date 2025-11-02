using AutoDragonOath.Helpers;
using AutoDragonOath.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace AutoDragonOath.ViewModels
{
    /// <summary>
    /// ViewModel for Map Scanner window
    /// Specialized tool to find the Map Object Pointer (CWorldManager::s_pMe)
    /// </summary>
    public class MapScannerViewModel : INotifyPropertyChanged
    {
        private readonly int _processId;
        private string _outputText = "";
        private bool _isScanning;
        private string _statusMessage = "";
        private List<ScanResult> _lastMapNameResults = new List<ScanResult>();

        #region Win32 API for Advanced Scanning

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PAGE_READWRITE = 0x04;
        private const uint PAGE_READONLY = 0x02;
        private const uint MEM_COMMIT = 0x1000;

        #endregion

        #region Properties

        public string OutputText
        {
            get => _outputText;
            set
            {
                _outputText = value;
                OnPropertyChanged();
            }
        }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                _isScanning = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand ScanMapNamesCommand { get; }
        public ICommand FindMapPointersCommand { get; }
        public ICommand TestPlayerChainCommand { get; }
        public ICommand MonitorMapChangesCommand { get; }
        public ICommand ScanTitlesCommand { get; }
        public ICommand ClearCommand { get; }

        #endregion

        public MapScannerViewModel(int processId)
        {
            _processId = processId;

            ScanMapNamesCommand = new RelayCommand(_ => ScanForMapNames(), _ => !IsScanning);
            FindMapPointersCommand = new RelayCommand(_ => FindMapPointers(), _ => !IsScanning && _lastMapNameResults.Count > 0);
            TestPlayerChainCommand = new RelayCommand(_ => TestPlayerChain(), _ => !IsScanning);
            MonitorMapChangesCommand = new RelayCommand(_ => MonitorMapChanges(), _ => !IsScanning);
            ScanTitlesCommand = new RelayCommand(_ => ScanForTitles(), _ => !IsScanning);
            ClearCommand = new RelayCommand(_ => { OutputText = ""; StatusMessage = ""; });

            AppendOutput("╔═══════════════════════════════════════════════════════════╗");
            AppendOutput("║       Map Scanner - Find Map Object Pointer               ║");
            AppendOutput("╚═══════════════════════════════════════════════════════════╝\n");
            AppendOutput($"Process ID: {processId}");
            AppendOutput($"Ready to scan for map data structures.\n");
            AppendOutput("INSTRUCTIONS:");
            AppendOutput("1. Make sure your character is logged in");
            AppendOutput("2. Note what map you're currently on");
            AppendOutput("3. Click 'Scan for Map Names' to find map strings");
            AppendOutput("4. Click 'Find Map Pointers' to locate pointers");
            AppendOutput("5. Use 'Test Player Chain' to verify your addresses\n");
        }

        /// <summary>
        /// Scenario 1: Scan for map name strings
        /// </summary>
        private void ScanForMapNames()
        {
            IsScanning = true;
            StatusMessage = "Scanning for map names...";
            _lastMapNameResults.Clear();

            try
            {
                AppendOutput("\n═══ SCENARIO 1: Scanning for Map Names ═══\n");

                string[] mapNames = new string[]
                {
                    "heaven",
                    "nhon nam",
                    "loc duong",
                    "don hoang",
                    "kiem cac",
                    "doi lu",
                    "nhi hai",
                    "bangthien",
                    // Vietnamese versions
                    "Heaven",
                    "Thiên Hạ",
                    "Nhân Nam",
                    "Lạc Dương",
                    "Đôn Hoàng",
                    "Kiếm Các",
                    "Đại Lý",
                    "Nhị Hải",
                    "Bạng Thiên"
                };

                IntPtr processHandle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, _processId);

                if (processHandle == IntPtr.Zero)
                {
                    AppendOutput("❌ ERROR: Failed to open process. Try running as Administrator.");
                    return;
                }

                int totalFound = 0;

                foreach (string mapName in mapNames)
                {
                    AppendOutput($"Searching for: \"{mapName}\"");
                    var addresses = ScanForString(processHandle, mapName);

                    foreach (var addr in addresses.Take(10)) // Limit to first 10 results per map
                    {
                        _lastMapNameResults.Add(new ScanResult
                        {
                            Type = "MapName",
                            Address = addr,
                            Value = mapName,
                            Description = $"Found map name at 0x{addr:X8}"
                        });

                        AppendOutput($"  ✓ Found at: 0x{addr:X8} ({addr})");
                        totalFound++;
                    }
                }

                CloseHandle(processHandle);

                AppendOutput($"\n✅ Scan complete! Found {totalFound} map name occurrences");
                AppendOutput($"Next: Click 'Find Map Pointers' to scan for pointers to these addresses\n");
                StatusMessage = $"Found {totalFound} map names";
            }
            catch (Exception ex)
            {
                AppendOutput($"\n❌ ERROR: {ex.Message}\n");
                StatusMessage = "Error occurred";
            }
            finally
            {
                IsScanning = false;
            }
        }

        /// <summary>
        /// Scenario 2: Find pointers pointing to map name strings
        /// </summary>
        private void FindMapPointers()
        {
            IsScanning = true;
            StatusMessage = "Finding pointers to map names...";

            try
            {
                AppendOutput("\n═══ SCENARIO 2: Finding Pointers to Map Names ═══\n");

                if (_lastMapNameResults.Count == 0)
                {
                    AppendOutput("❌ No map names found. Run 'Scan for Map Names' first.\n");
                    return;
                }

                IntPtr processHandle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, _processId);

                if (processHandle == IntPtr.Zero)
                {
                    AppendOutput("❌ ERROR: Failed to open process.\n");
                    return;
                }

                var candidatePointers = new List<long>();

                // Search for pointers to the first few map name addresses
                foreach (var mapNameResult in _lastMapNameResults.Take(5))
                {
                    AppendOutput($"\nSearching for pointers to 0x{mapNameResult.Address:X8} (\"{mapNameResult.Value}\")");
                    var pointers = FindPointersTo(processHandle, mapNameResult.Address);

                    foreach (var ptr in pointers.Take(20))
                    {
                        // Focus on addresses in lower memory range (likely static)
                        if (ptr < 10000000)
                        {
                            AppendOutput($"  ⭐ Pointer at: 0x{ptr:X8} ({ptr}) [LOW MEMORY - LIKELY STATIC]");
                            if (!candidatePointers.Contains(ptr))
                            {
                                candidatePointers.Add(ptr);
                            }
                        }
                        else
                        {
                            AppendOutput($"  → Pointer at: 0x{ptr:X8} ({ptr})");
                        }
                    }
                }

                CloseHandle(processHandle);

                AppendOutput($"\n═══ MOST LIKELY STATIC POINTERS ═══\n");
                foreach (var candidate in candidatePointers.OrderBy(c => c))
                {
                    AppendOutput($"📍 0x{candidate:X8} ({candidate})");
                }

                AppendOutput($"\n✅ Found {candidatePointers.Count} candidate static pointers");
                AppendOutput($"Next: Note these addresses, change maps, and run this again to see which changes.\n");
                StatusMessage = $"Found {candidatePointers.Count} candidates";
            }
            catch (Exception ex)
            {
                AppendOutput($"\n❌ ERROR: {ex.Message}\n");
                StatusMessage = "Error occurred";
            }
            finally
            {
                IsScanning = false;
            }
        }

        /// <summary>
        /// Scenario 3: Test the player pointer chain
        /// </summary>
        private void TestPlayerChain()
        {
            IsScanning = true;
            StatusMessage = "Testing player pointer chain...";

            try
            {
                AppendOutput("\n═══ SCENARIO 3: Testing Player Pointer Chain ═══\n");

                using var memoryReader = new MemoryReader(_processId);

                if (!memoryReader.IsValid)
                {
                    AppendOutput("❌ ERROR: Failed to open process.\n");
                    return;
                }

                const long PLAYER_BASE = 2381824; // 0x00245580

                AppendOutput($"Testing Player Base Pointer: 0x{PLAYER_BASE:X8} ({PLAYER_BASE})\n");

                // Level 1: Read entity object pointer
                int entityPtr = memoryReader.ReadInt32((int)PLAYER_BASE);
                if (entityPtr == 0)
                {
                    AppendOutput("❌ Failed to read Entity Object Pointer");
                    AppendOutput("   The base address might be incorrect!\n");
                    return;
                }

                AppendOutput($"✓ Entity Object Pointer: 0x{entityPtr:X8}");

                // Level 2: Entity base (+12)
                int entityBase = memoryReader.ReadInt32(entityPtr + 12);
                if (entityBase == 0)
                {
                    AppendOutput("❌ Failed to read Entity Base\n");
                    return;
                }

                AppendOutput($"✓ Entity Base Address: 0x{entityBase:X8}");

                // Read coordinates
                float x = memoryReader.ReadFloat(entityBase + 92);
                float y = memoryReader.ReadFloat(entityBase + 100);
                AppendOutput($"\n📍 Coordinates:");
                AppendOutput($"   X: {x:F2} (EntityBase + 92)");
                AppendOutput($"   Y: {y:F2} (EntityBase + 100)");

                // Level 3: Stats object pointer (+340)
                int statsPtr = memoryReader.ReadInt32(entityBase + 340);
                if (statsPtr == 0)
                {
                    AppendOutput("\n❌ Failed to read Stats Object Pointer");
                    AppendOutput("   The offset +340 might be incorrect!\n");
                    return;
                }

                AppendOutput($"\n✓ Stats Object Pointer: 0x{statsPtr:X8}");

                // Level 4: Stats base (+4)
                int statsBase = memoryReader.ReadInt32(statsPtr + 4);
                if (statsBase == 0)
                {
                    AppendOutput("❌ Failed to read Stats Base\n");
                    return;
                }

                AppendOutput($"✓ Stats Base Address: 0x{statsBase:X8}");

                // Read character data
                string charName = memoryReader.ReadString(statsBase + 48, 30);
                int currentHP = memoryReader.ReadInt32(statsBase + 1752);
                int currentMP = memoryReader.ReadInt32(statsBase + 1756);
                int maxHP = memoryReader.ReadInt32(statsBase + 1856);
                int maxMP = memoryReader.ReadInt32(statsBase + 1860);
                int exp = memoryReader.ReadInt32(statsBase + 92);

                AppendOutput($"\n📊 Character Data:");
                AppendOutput($"   Name: {charName}");
                AppendOutput($"   HP: {currentHP}/{maxHP}");
                AppendOutput($"   MP: {currentMP}/{maxMP}");
                AppendOutput($"   Exp: {exp}");

                if (string.IsNullOrEmpty(charName) || charName.Length < 2)
                {
                    AppendOutput($"\n⚠ WARNING: Character name is empty or invalid!");
                    AppendOutput($"   The pointer chain might be incorrect.\n");
                }
                else
                {
                    AppendOutput($"\n✅ SUCCESS! Pointer chain is working correctly!\n");
                }

                StatusMessage = "Player chain tested";
            }
            catch (Exception ex)
            {
                AppendOutput($"\n❌ ERROR: {ex.Message}\n");
                StatusMessage = "Error occurred";
            }
            finally
            {
                IsScanning = false;
            }
        }

        /// <summary>
        /// Scenario 4: Monitor map changes
        /// </summary>
        private void MonitorMapChanges()
        {
            IsScanning = true;
            StatusMessage = "Monitoring - change maps now...";

            try
            {
                AppendOutput("\n═══ SCENARIO 4: Monitor Map Changes ═══\n");
                AppendOutput("This feature requires manual intervention:");
                AppendOutput("1. Run 'Find Map Pointers' to get candidates");
                AppendOutput("2. Write down the candidate addresses");
                AppendOutput("3. Change maps in the game (teleport)");
                AppendOutput("4. Run 'Find Map Pointers' again");
                AppendOutput("5. The address that changed to point to the new map is your Map Object Pointer!\n");

                StatusMessage = "Monitor manually";
            }
            catch (Exception ex)
            {
                AppendOutput($"\n❌ ERROR: {ex.Message}\n");
                StatusMessage = "Error occurred";
            }
            finally
            {
                IsScanning = false;
            }
        }

        /// <summary>
        /// Scenario 5: Scan for Title Structure
        /// Finds the offset of character title data from Stats Base
        /// </summary>
        private void ScanForTitles()
        {
            IsScanning = true;
            StatusMessage = "Scanning for title structure...";

            try
            {
                AppendOutput("\n═══ SCENARIO 5: Scan for Title Structure ═══\n");
                AppendOutput("This scans memory to find where character titles are stored.");
                AppendOutput("Looking for patterns that match the _TITLE_ structure:\n");
                AppendOutput("  - m_nTitleNum (0-20)");
                AppendOutput("  - m_nCurTitleIndex (-1 to 19)");
                AppendOutput("  - Title array with bFlag (0, 1, or 2)\n");

                using (var memoryReader = new MemoryReader(_processId))
                {
                    // Get stats base
                    int[] statsBasePointer = { 2381824, 12, 340, 4 };
                    int statsBase = memoryReader.FollowPointerChain(statsBasePointer);

                    if (statsBase == 0)
                    {
                        AppendOutput("❌ Failed to read Stats Base. Make sure character is logged in.");
                        StatusMessage = "Failed - character not logged in";
                        return;
                    }

                    AppendOutput($"✓ Stats Base: 0x{statsBase:X8}\n");
                    AppendOutput("Scanning from Stats Base +0 to +5000...\n");

                    // Use TitleReader to scan
                    var titleReader = new TitleReader(memoryReader);

                    // Scan range from 0 to 5000 bytes
                    int foundCount = 0;
                    for (int offset = 0; offset < 5000; offset += 4)
                    {
                        try
                        {
                            int titleNum = memoryReader.ReadInt32(statsBase + offset);
                            int currentIndex = memoryReader.ReadInt32(statsBase + offset + 4);

                            // Check if values are reasonable for title data
                            if (titleNum >= 0 && titleNum <= 20 &&
                                currentIndex >= -1 && currentIndex < 20)
                            {
                                // Try to read first title flag
                                int firstTitleAddr = statsBase + offset + 8;
                                int firstTitleFlag = memoryReader.ReadInt32(firstTitleAddr);

                                // bFlag should be 0 (INVALID), 1 (ID), or 2 (STRING)
                                if (firstTitleFlag >= 0 && firstTitleFlag <= 2)
                                {
                                    foundCount++;
                                    AppendOutput($"╔═══ POTENTIAL MATCH #{foundCount} ═══");
                                    AppendOutput($"║ Offset from Stats Base: +{offset}");
                                    AppendOutput($"║ Address: 0x{(statsBase + offset):X8}");
                                    AppendOutput($"║");
                                    AppendOutput($"║ Title Count: {titleNum}");
                                    AppendOutput($"║ Current Index: {currentIndex}");
                                    AppendOutput($"║ First Title Flag: {firstTitleFlag}");

                                    // Try to read the current title if valid
                                    if (currentIndex >= 0 && currentIndex < titleNum)
                                    {
                                        int titleListAddr = statsBase + offset + 8;
                                        var titles = titleReader.ReadTitleList(titleListAddr);
                                        var currentTitle = titles[currentIndex];

                                        if (currentTitle.IsValid)
                                        {
                                            AppendOutput($"║");
                                            AppendOutput($"║ Current Title: {currentTitle.DisplayText}");
                                            AppendOutput($"║ Title Type: {currentTitle.Flag}");
                                        }
                                    }

                                    AppendOutput($"╚═══════════════════════════════════\n");
                                }
                            }
                        }
                        catch
                        {
                            // Skip invalid addresses
                        }
                    }

                    if (foundCount == 0)
                    {
                        AppendOutput("❌ No potential title structures found.");
                        AppendOutput("\nTroubleshooting:");
                        AppendOutput("- Make sure your character is logged in");
                        AppendOutput("- Try equipping or changing your title in-game");
                        AppendOutput("- The character might not have any titles yet");
                        StatusMessage = "No matches found";
                    }
                    else
                    {
                        AppendOutput($"✓ Found {foundCount} potential match(es)!");
                        AppendOutput("\nNEXT STEPS:");
                        AppendOutput("1. Note the offset value(s) from the matches above");
                        AppendOutput("2. Change your title in-game and run this scan again");
                        AppendOutput("3. The offset that shows your new title is the correct one!");
                        AppendOutput("4. Update TITLE_INFO_OFFSET in GameProcessMonitor.cs");
                        AppendOutput("5. Uncomment the title reading code in GameProcessMonitor.cs");
                        StatusMessage = $"Found {foundCount} potential matches";
                    }
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"\n❌ ERROR: {ex.Message}");
                AppendOutput($"Stack trace: {ex.StackTrace}");
                StatusMessage = "Error occurred";
            }
            finally
            {
                IsScanning = false;
            }
        }

        #region Memory Scanning Helpers

        /// <summary>
        /// Scan memory for a string pattern
        /// </summary>
        private List<long> ScanForString(IntPtr processHandle, string searchString)
        {
            var results = new List<long>();
            byte[] pattern = Encoding.ASCII.GetBytes(searchString);

            IntPtr address = IntPtr.Zero;
            MEMORY_BASIC_INFORMATION mbi;

            while (VirtualQueryEx(processHandle, address, out mbi, Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) != 0)
            {
                if (mbi.State == MEM_COMMIT &&
                    (mbi.Protect == PAGE_READWRITE || mbi.Protect == PAGE_READONLY))
                {
                    byte[] buffer = new byte[(int)mbi.RegionSize];
                    int bytesRead;

                    if (ReadProcessMemory(processHandle, mbi.BaseAddress, buffer, buffer.Length, out bytesRead))
                    {
                        for (int i = 0; i < bytesRead - pattern.Length; i++)
                        {
                            bool match = true;
                            for (int j = 0; j < pattern.Length; j++)
                            {
                                if (buffer[i + j] != pattern[j])
                                {
                                    match = false;
                                    break;
                                }
                            }

                            if (match)
                            {
                                long foundAddress = (long)mbi.BaseAddress + i;
                                results.Add(foundAddress);
                            }
                        }
                    }
                }

                address = new IntPtr((long)mbi.BaseAddress + (long)mbi.RegionSize);

                // Safety limit
                if (results.Count > 100)
                    break;
            }

            return results;
        }

        /// <summary>
        /// Find pointers that point to a specific address
        /// </summary>
        private List<long> FindPointersTo(IntPtr processHandle, long targetAddress)
        {
            var results = new List<long>();
            byte[] targetBytes = BitConverter.GetBytes((int)targetAddress);

            IntPtr address = IntPtr.Zero;
            MEMORY_BASIC_INFORMATION mbi;

            while (VirtualQueryEx(processHandle, address, out mbi, Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) != 0)
            {
                if (mbi.State == MEM_COMMIT && mbi.Protect == PAGE_READWRITE)
                {
                    byte[] buffer = new byte[(int)mbi.RegionSize];
                    int bytesRead;

                    if (ReadProcessMemory(processHandle, mbi.BaseAddress, buffer, buffer.Length, out bytesRead))
                    {
                        for (int i = 0; i < bytesRead - 4; i += 4)
                        {
                            if (buffer[i] == targetBytes[0] &&
                                buffer[i + 1] == targetBytes[1] &&
                                buffer[i + 2] == targetBytes[2] &&
                                buffer[i + 3] == targetBytes[3])
                            {
                                long foundAddress = (long)mbi.BaseAddress + i;
                                results.Add(foundAddress);

                                if (results.Count > 100)
                                    return results;
                            }
                        }
                    }
                }

                address = new IntPtr((long)mbi.BaseAddress + (long)mbi.RegionSize);
            }

            return results;
        }

        #endregion

        #region Helper Methods

        private void AppendOutput(string text)
        {
            OutputText += text + Environment.NewLine;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private class ScanResult
        {
            public string Type { get; set; }
            public long Address { get; set; }
            public object Value { get; set; }
            public string Description { get; set; }
        }
    }
}
