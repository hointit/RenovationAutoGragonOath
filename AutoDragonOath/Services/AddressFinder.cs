using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AutoDragonOath.Services
{
    /// <summary>
    /// Diagnostic tool to help find correct memory addresses when game updates
    /// This is used when the hardcoded addresses no longer work
    /// </summary>
    public class AddressFinder
    {
        /// <summary>
        /// Test if a potential address chain is valid by checking if it returns reasonable values
        /// </summary>
        public static bool TestAddressChain(MemoryReader reader, int[] chain, out int result)
        {
            result = reader.FollowPointerChain(chain);

            if (result == 0)
            {
                Debug.WriteLine($"Chain failed: [{string.Join(", ", chain)}]");
                return false;
            }

            // Test if the result looks like a valid pointer (in reasonable memory range)
            if (result < 0x400000 || result > 0x7FFFFFFF)
            {
                Debug.WriteLine($"Chain returned invalid address: 0x{result:X}");
                return false;
            }

            Debug.WriteLine($"✓ Chain successful: [{string.Join(", ", chain)}] → 0x{result:X}");
            return true;
        }

        /// <summary>
        /// Try to read and validate character name from a suspected stats base address
        /// </summary>
        public static bool ValidateStatsBase(MemoryReader reader, int statsBase)
        {
            const int OFFSET_NAME = 48;
            const int OFFSET_LEVEL = 92;
            const int OFFSET_HP = 2292;
            const int OFFSET_MAX_HP = 2400;

            try
            {
                // Try to read character name
                string name = reader.ReadString(statsBase + OFFSET_NAME);

                // Name should be 1-20 characters and contain printable characters
                if (string.IsNullOrEmpty(name) || name.Length > 20)
                {
                    Debug.WriteLine($"Invalid name length: {name?.Length ?? 0}");
                    return false;
                }

                // Check if name contains mostly valid characters
                if (!name.All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)))
                {
                    Debug.WriteLine($"Invalid name characters: {name}");
                    return false;
                }

                // Try to read level (should be 1-150)
                int level = reader.ReadInt32(statsBase + OFFSET_LEVEL);
                if (level < 1 || level > 150)
                {
                    Debug.WriteLine($"Invalid level: {level}");
                    return false;
                }

                // Try to read HP (should be positive and less than 1 million)
                int hp = reader.ReadInt32(statsBase + OFFSET_HP);
                int maxHp = reader.ReadInt32(statsBase + OFFSET_MAX_HP);

                if (hp < 0 || maxHp < 0 || hp > maxHp || maxHp > 1000000)
                {
                    Debug.WriteLine($"Invalid HP: {hp}/{maxHp}");
                    return false;
                }

                Debug.WriteLine($"✓ Valid stats found:");
                Debug.WriteLine($"  Name: {name}");
                Debug.WriteLine($"  Level: {level}");
                Debug.WriteLine($"  HP: {hp}/{maxHp}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Validation error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Scan memory region for potential CObjectManager::s_pMe pointer
        /// This is a brute-force approach and may take some time
        /// </summary>
        public static List<int> ScanForCObjectManagerPointer(int processId)
        {
            var candidates = new List<int>();

            Debug.WriteLine("Starting scan for CObjectManager::s_pMe...");
            Debug.WriteLine("This may take a while. Looking for static pointers in .data section.");

            using var reader = new MemoryReader(processId);

            if (!reader.IsValid)
            {
                Debug.WriteLine("Failed to open process");
                return candidates;
            }

            // Scan likely address ranges for game static data
            // Typical game .data section is around 0x400000 - 0x800000
            int[] baseRanges =
            {
                6000000,  // Around 0x5B8D80
                7000000,  // Around 0x6ACFC0 (near original 7319476)
                8000000   // Around 0x7A1200
            };

            foreach (int baseAddr in baseRanges)
            {
                Debug.WriteLine($"\nScanning range starting at 0x{baseAddr:X}...");

                // Scan in increments of 4 bytes (pointer alignment)
                for (int offset = 0; offset < 100000; offset += 4)
                {
                    int testAddr = baseAddr + offset;

                    try
                    {
                        // Try the pointer chain: [testAddr, 12, 344, 4]
                        int[] chain = { testAddr, 12, 344, 4 };

                        if (TestAddressChain(reader, chain, out int statsBase))
                        {
                            if (ValidateStatsBase(reader, statsBase))
                            {
                                Debug.WriteLine($"✓✓✓ FOUND POTENTIAL ADDRESS: 0x{testAddr:X} ({testAddr})");
                                candidates.Add(testAddr);
                            }
                        }
                    }
                    catch
                    {
                        // Silently continue - many addresses will fail to read
                    }
                }
            }

            if (candidates.Count == 0)
            {
                Debug.WriteLine("\n❌ No valid addresses found.");
                Debug.WriteLine("Possible reasons:");
                Debug.WriteLine("  - Game is not logged in yet");
                Debug.WriteLine("  - Game version has changed significantly");
                Debug.WriteLine("  - Offsets (+12, +344, +4) have changed");
            }
            else
            {
                Debug.WriteLine($"\n✓ Found {candidates.Count} potential address(es):");
                foreach (var addr in candidates)
                {
                    Debug.WriteLine($"  0x{addr:X} ({addr})");
                }
            }

            return candidates;
        }

        /// <summary>
        /// Diagnostic report for current configuration
        /// </summary>
        public static void GenerateDiagnosticReport(int processId)
        {
            Debug.WriteLine("=============================================================");
            Debug.WriteLine("AutoDragonOath Memory Diagnostic Report");
            Debug.WriteLine("=============================================================");

            using var reader = new MemoryReader(processId);

            if (!reader.IsValid)
            {
                Debug.WriteLine("❌ Failed to open process. Try running as Administrator.");
                return;
            }

            Debug.WriteLine($"✓ Process handle opened successfully (PID: {processId})");
            Debug.WriteLine("");

            // Test current working addresses
            Debug.WriteLine("Testing CURRENT working addresses:");
            Debug.WriteLine("-----------------------------------");

            int[] statsChain = { 2381824, 12, 340, 4 };
            Debug.WriteLine($"Stats chain: [{string.Join(", ", statsChain)}]");

            if (TestAddressChain(reader, statsChain, out int statsBase))
            {
                Debug.WriteLine($"Stats base address: 0x{statsBase:X}");
                ValidateStatsBase(reader, statsBase);
            }
            else
            {
                Debug.WriteLine("❌ Current stats address chain FAILED");
                Debug.WriteLine("");
                Debug.WriteLine("Recommendation: Run address scan to find new base address");
            }

            int[] entityChain = { 2381824, 12 };
            Debug.WriteLine("");
            Debug.WriteLine($"Entity chain: [{string.Join(", ", entityChain)}]");
            TestAddressChain(reader, entityChain, out int entityBase);

            int[] mapChain = { 1933288, 14232 };
            Debug.WriteLine("");
            Debug.WriteLine($"Map chain (LEGACY - likely broken): [{string.Join(", ", mapChain)}]");
            TestAddressChain(reader, mapChain, out int mapBase);

            Debug.WriteLine("");
            Debug.WriteLine("=============================================================");
        }

        /// <summary>
        /// Scan for map base pointer by testing variations based on working entity base
        /// Tests multiple potential pointer chains to find where map data is stored
        /// </summary>
        public static List<int[]> ScanForMapPointer(int processId)
        {
            Debug.WriteLine("=============================================================");
            Debug.WriteLine("Scanning for Map Pointer Chain");
            Debug.WriteLine("=============================================================");

            var validChains = new List<int[]>();

            using var reader = new MemoryReader(processId);

            if (!reader.IsValid)
            {
                Debug.WriteLine("❌ Failed to open process");
                return validChains;
            }

            // Known working base address
            const int WORKING_BASE = 2381824;

            Debug.WriteLine($"Using working base: 0x{WORKING_BASE:X} ({WORKING_BASE})");
            Debug.WriteLine("");

            // Test various offset combinations
            Debug.WriteLine("Testing potential map pointer chains...");
            Debug.WriteLine("");

            // Pattern 1: Direct offset from base (like map might be at a different root offset)
            int[] testOffsets1 = { 8, 12, 16, 20, 24, 28, 32 };
            foreach (int offset1 in testOffsets1)
            {
                int[] chain = { WORKING_BASE, offset1 };
                if (TestAddressChain(reader, chain, out int result))
                {
                    // Try reading potential map ID at various offsets
                    if (ValidateMapData(reader, result))
                    {
                        Debug.WriteLine($"✓✓✓ FOUND POTENTIAL MAP CHAIN: [{string.Join(", ", chain)}]");
                        validChains.Add(chain);
                    }
                }
            }

            // Pattern 2: Two-level offset from base
            Debug.WriteLine("");
            Debug.WriteLine("Testing two-level chains...");
            int[] testOffsets2 = { 4, 8, 12, 16, 20, 100, 200, 300, 340 };
            foreach (int offset2 in testOffsets2)
            {
                int[] chain = { WORKING_BASE, 12, offset2 };
                if (TestAddressChain(reader, chain, out int result))
                {
                    if (ValidateMapData(reader, result))
                    {
                        Debug.WriteLine($"✓✓✓ FOUND POTENTIAL MAP CHAIN: [{string.Join(", ", chain)}]");
                        validChains.Add(chain);
                    }
                }
            }

            // Pattern 3: Scan nearby static addresses (map might have separate root pointer)
            Debug.WriteLine("");
            Debug.WriteLine("Scanning nearby static addresses...");
            for (int baseOffset = -50000; baseOffset <= 50000; baseOffset += 4)
            {
                int testBase = WORKING_BASE + baseOffset;

                // Test simple two-level chain
                int[] chain = { testBase, 12 };
                if (TestAddressChain(reader, chain, out int result))
                {
                    if (ValidateMapData(reader, result))
                    {
                        Debug.WriteLine($"✓✓✓ FOUND POTENTIAL MAP CHAIN: [{string.Join(", ", chain)}]");
                        validChains.Add(chain);
                    }
                }
            }

            Debug.WriteLine("");
            Debug.WriteLine("=============================================================");
            Debug.WriteLine($"Scan complete. Found {validChains.Count} potential map pointer chain(s)");

            foreach (var chain in validChains)
            {
                Debug.WriteLine($"  [{string.Join(", ", chain)}]");
            }

            Debug.WriteLine("=============================================================");

            return validChains;
        }

        /// <summary>
        /// Validate if an address contains map data by checking for reasonable map ID values
        /// </summary>
        private static bool ValidateMapData(MemoryReader reader, int baseAddr)
        {
            // Test common map ID offsets
            int[] testOffsets = { 0, 4, 8, 12, 16, 20, 24, 28, 32, 64, 96, 100, 128 };

            foreach (int offset in testOffsets)
            {
                try
                {
                    int mapId = reader.ReadInt32(baseAddr + offset);

                    // Map IDs in the game are typically small positive integers (1-100)
                    // Based on ConvertMapIdToName: 1, 10, 11, 20, 21, 30, 31, 37, 40, 50, 60
                    if (mapId >= 1 && mapId <= 100)
                    {
                        Debug.WriteLine($"    Potential map ID {mapId} at offset +{offset}");
                        return true;
                    }
                }
                catch
                {
                    // Continue testing other offsets
                }
            }

            return false;
        }
    }
}
