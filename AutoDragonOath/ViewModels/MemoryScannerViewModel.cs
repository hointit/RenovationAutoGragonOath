using AutoDragonOath.Helpers;
using AutoDragonOath.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace AutoDragonOath.ViewModels
{
    /// <summary>
    /// ViewModel for Memory Scanner window
    /// Used to scan game memory for debugging and finding new addresses
    /// </summary>
    public class MemoryScannerViewModel : INotifyPropertyChanged
    {
        private readonly int _processId;
        private string _fromOffset = "0";
        private string _toOffset = "200";
        private string _stepSize = "4";
        private string _selectedType = "INT";
        private string _selectedBasePointer = "STATS_BASE";
        private string _outputText = "";
        private bool _isScanning;

        public string FromOffset
        {
            get => _fromOffset;
            set
            {
                _fromOffset = value;
                OnPropertyChanged();
            }
        }

        public string ToOffset
        {
            get => _toOffset;
            set
            {
                _toOffset = value;
                OnPropertyChanged();
            }
        }

        public string SelectedBasePointer
        {
            get => _selectedBasePointer;
            set
            {
                _selectedBasePointer = value;
                OnPropertyChanged();
            }
        }

        public string StepSize
        {
            get => _stepSize;
            set
            {
                _stepSize = value;
                OnPropertyChanged();
            }
        }

        public string SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                OnPropertyChanged();
            }
        }

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

        public string[] DataTypes { get; } = { "INT", "FLOAT", "STRING" };
        public string[] BasePointers { get; } = { "ENTITY_BASE", "STATS_BASE", "MAP_BASE", "PET_BASE" };

        public ICommand ScanCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ScanCommonAddressesCommand { get; }

        public MemoryScannerViewModel(int processId)
        {
            _processId = processId;

            ScanCommand = new RelayCommand(_ => PerformScan(), _ => !IsScanning);
            ClearCommand = new RelayCommand(_ => OutputText = "");
            ScanCommonAddressesCommand = new RelayCommand(_ => ScanCommonAddresses(), _ => !IsScanning);

            AppendOutput($"Memory Scanner initialized for Process ID: {processId}");
            AppendOutput($"Game.exe base address will be automatically resolved\n");
        }

        /// <summary>
        /// Perform memory scan based on user input
        /// </summary>
        private void PerformScan()
        {
            IsScanning = true;

            try
            {
                // Parse offset values
                if (!int.TryParse(FromOffset, out int fromOffset) || fromOffset < 0)
                {
                    AppendOutput($"ERROR: Invalid 'From' offset: {FromOffset}");
                    return;
                }

                if (!int.TryParse(ToOffset, out int toOffset) || toOffset < 0)
                {
                    AppendOutput($"ERROR: Invalid 'To' offset: {ToOffset}");
                    return;
                }

                if (!int.TryParse(StepSize, out int step) || step <= 0)
                {
                    AppendOutput($"ERROR: Invalid step size: {StepSize}");
                    return;
                }

                if (fromOffset >= toOffset)
                {
                    AppendOutput("ERROR: 'From' offset must be less than 'To' offset");
                    return;
                }

                using var memoryReader = new MemoryReader(_processId);

                if (!memoryReader.IsValid)
                {
                    AppendOutput("ERROR: Failed to open process. Try running as Administrator.");
                    return;
                }

                // Get the base address based on selection
                int[] pointerChain = GetPointerChain(SelectedBasePointer);
                if (pointerChain == null)
                {
                    AppendOutput($"ERROR: Unknown base pointer type: {SelectedBasePointer}");
                    return;
                }

                AppendOutput($"\n========================================");
                AppendOutput($"Resolving {SelectedBasePointer} pointer chain...");
                int baseAddress = memoryReader.FollowPointerChain(pointerChain);

                if (baseAddress == 0)
                {
                    AppendOutput($"ERROR: Failed to resolve {SelectedBasePointer} pointer chain");
                    AppendOutput($"Chain: [{string.Join(", ", pointerChain)}]");
                    AppendOutput($"Make sure the character is logged in.");
                    return;
                }

                AppendOutput($"✓ Base Address: 0x{baseAddress:X8}");
                AppendOutput($"Scanning from offset +{fromOffset} to +{toOffset}");
                AppendOutput($"Step: {step}, Type: {SelectedType}");
                AppendOutput($"========================================\n");

                int resultsFound = 0;
                var sw = Stopwatch.StartNew();

                for (int offset = fromOffset; offset <= toOffset; offset += step)
                {
                    try
                    {
                        int scanAddress = baseAddress + offset;

                        switch (SelectedType)
                        {
                            case "INT":
                                int intValue = memoryReader.ReadInt32(scanAddress);
                                if (intValue != 0) // Only show non-zero values
                                {
                                    AppendOutput($"+{offset,4} (0x{scanAddress:X8}) -> {intValue}");
                                    resultsFound++;
                                }
                                break;

                            case "FLOAT":
                                float floatValue = memoryReader.ReadFloat(scanAddress);
                                if (Math.Abs(floatValue) > 0.0001f) // Only show non-zero values
                                {
                                    AppendOutput($"+{offset,4} (0x{scanAddress:X8}) -> {floatValue:F2}");
                                    resultsFound++;
                                }
                                break;

                            case "STRING":
                                string stringValue = memoryReader.ReadString(scanAddress, 30);
                                if (!string.IsNullOrWhiteSpace(stringValue))
                                {
                                    AppendOutput($"+{offset,4} (0x{scanAddress:X8}) -> \"{stringValue}\"");
                                    resultsFound++;
                                }
                                break;
                        }

                        // Limit results to prevent UI freeze
                        if (resultsFound >= 1000)
                        {
                            AppendOutput("\n[!] Result limit reached (1000). Stopping scan.");
                            break;
                        }
                    }
                    catch
                    {
                        // Silently skip invalid addresses
                    }
                }

                sw.Stop();
                AppendOutput($"\n========================================");
                AppendOutput($"Scan complete. Found {resultsFound} results in {sw.ElapsedMilliseconds}ms");
                AppendOutput($"========================================\n");
            }
            catch (Exception ex)
            {
                AppendOutput($"ERROR: {ex.Message}");
            }
            finally
            {
                IsScanning = false;
            }
        }

        /// <summary>
        /// Get the pointer chain for the selected base pointer type
        /// Based on GameProcessMonitor constants
        /// </summary>
        private int[] GetPointerChain(string basePointerType)
        {
            return basePointerType switch
            {
                "ENTITY_BASE" => new[] { 2381824, 12 },
                "STATS_BASE" => new[] { 2381824, 12, 340, 4 },
                "MAP_BASE" => new[] { 2381824, 13692 },
                "PET_BASE" => new[] { 7319540, 299356 },
                _ => null
            };
        }

        /// <summary>
        /// Scan common game addresses based on GameProcessMonitor
        /// </summary>
        private void ScanCommonAddresses()
        {
            IsScanning = true;

            try
            {
                AppendOutput($"\n========================================");
                AppendOutput($"Scanning Common Game Addresses");
                AppendOutput($"========================================\n");

                using var memoryReader = new MemoryReader(_processId);

                if (!memoryReader.IsValid)
                {
                    AppendOutput("ERROR: Failed to open process. Try running as Administrator.");
                    return;
                }

                // Test known pointer chains from GameProcessMonitor
                AppendOutput("Testing ENTITY_BASE_POINTER [2381824, 12]:");
                int entityBase = memoryReader.FollowPointerChain(new[] { 2381824, 12 });
                if (entityBase != 0)
                {
                    AppendOutput($"  ✓ Entity Base: 0x{entityBase:X8}");

                    // Read coordinates
                    float x = memoryReader.ReadFloat(entityBase + 92);
                    float y = memoryReader.ReadFloat(entityBase + 100);
                    AppendOutput($"    X Coordinate (+92): {x:F2}");
                    AppendOutput($"    Y Coordinate (+100): {y:F2}");
                }
                else
                {
                    AppendOutput("  ✗ Failed to follow entity base pointer chain");
                }

                AppendOutput("\nTesting STATS_BASE_POINTER [2381824, 12, 340, 4]:");
                int statsBase = memoryReader.FollowPointerChain(new[] { 2381824, 12, 340, 4 });
                if (statsBase != 0)
                {
                    AppendOutput($"  ✓ Stats Base: 0x{statsBase:X8}");

                    // Read character info
                    string name = memoryReader.ReadString(statsBase + 48, 30);
                    int level = memoryReader.ReadInt32(statsBase + 92);
                    int currentHp = memoryReader.ReadInt32(statsBase + 1752);
                    int maxHp = memoryReader.ReadInt32(statsBase + 1856);
                    int currentMp = memoryReader.ReadInt32(statsBase + 1756);
                    int maxMp = memoryReader.ReadInt32(statsBase + 1860);

                    AppendOutput($"    Character Name (+48): \"{name}\"");
                    AppendOutput($"    Level (+92): {level}");
                    AppendOutput($"    HP (+1752/+1856): {currentHp}/{maxHp}");
                    AppendOutput($"    MP (+1756/+1860): {currentMp}/{maxMp}");
                }
                else
                {
                    AppendOutput("  ✗ Failed to follow stats base pointer chain");
                }

                AppendOutput("\nTesting MAP_BASE_POINTER [2381824, 13692]:");
                int mapBase = memoryReader.FollowPointerChain(new[] { 2381824, 13692 });
                if (mapBase != 0)
                {
                    AppendOutput($"  ✓ Map Base: 0x{mapBase:X8}");

                    int mapId = memoryReader.ReadInt32(mapBase + 96);
                    AppendOutput($"    Map ID (+96): {mapId}");
                }
                else
                {
                    AppendOutput("  ✗ Failed to follow map base pointer chain");
                }

                AppendOutput("\nTesting PET_BASE_POINTER [7319540, 299356]:");
                int petBase = memoryReader.FollowPointerChain(new[] { 7319540, 299356 });
                if (petBase != 0)
                {
                    AppendOutput($"  ✓ Pet Base: 0x{petBase:X8}");

                    // Check first few pet slots
                    for (int i = 0; i < 3; i++)
                    {
                        int petId = memoryReader.ReadInt32(petBase + i * 92 + 36);
                        if (petId > 0)
                        {
                            int petHp = memoryReader.ReadInt32(petBase + i * 92 + 40);
                            int petMaxHp = memoryReader.ReadInt32(petBase + i * 92 + 44);
                            AppendOutput($"    Pet Slot {i}: ID={petId}, HP={petHp}/{petMaxHp}");
                        }
                    }
                }
                else
                {
                    AppendOutput("  ✗ Failed to follow pet base pointer chain");
                }

                AppendOutput($"\n========================================");
                AppendOutput($"Common address scan complete");
                AppendOutput($"========================================\n");
            }
            catch (Exception ex)
            {
                AppendOutput($"ERROR: {ex.Message}");
            }
            finally
            {
                IsScanning = false;
            }
        }

        /// <summary>
        /// Append text to output with scrolling
        /// </summary>
        private void AppendOutput(string text)
        {
            OutputText += text + Environment.NewLine;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
