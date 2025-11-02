using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// Memory Scanner Usage Guide and Examples
/// How to find Map Object Pointer and Unknown Attribute Offsets
/// </summary>
public class MemoryScannerUsageGuide
{
    /// <summary>
    /// SCENARIO 1: Find Map Object Pointer
    ///
    /// Steps:
    /// 1. Start game and login to a character
    /// 2. Note what map you're on
    /// 3. Run scan for map names
    /// 4. Find pointers to those map names
    /// 5. Test pointer stability across map changes
    /// </summary>
    public static void Example1_FindMapObjectPointer()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   SCENARIO 1: Finding Map Object Pointer                  ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

        // Get game process
        Process[] processes = Process.GetProcessesByName("game");
        if (processes.Length == 0)
        {
            Console.WriteLine("ERROR: Game process not found!");
            return;
        }

        int processId = processes[0].Id;
        Console.WriteLine($"Found game process: PID {processId}\n");

        var scanner = new MemoryScanner(processId);

        // Scan for map names
        var mapResults = scanner.ScanForMapNames();

        if (mapResults.Count == 0)
        {
            Console.WriteLine("No map names found. Make sure you're logged into a character.\n");
            return;
        }

        // Display all results
        Console.WriteLine("═══ FOUND MAP-RELATED ADDRESSES ═══\n");
        foreach (var result in mapResults)
        {
            Console.WriteLine(result);
        }

        // Find the most likely candidates (pointers in low memory range)
        var likelyCandidates = mapResults
            .Where(r => r.Type == "MapNamePointer")
            .Where(r => r.Address < 10000000) // Static addresses are usually low
            .OrderBy(r => r.Address)
            .ToList();

        Console.WriteLine("\n═══ MOST LIKELY STATIC POINTERS ═══\n");
        foreach (var candidate in likelyCandidates.Take(10))
        {
            Console.WriteLine($"0x{candidate.Address:X8} ({candidate.Address})");
            Console.WriteLine($"  Points to: 0x{(long)candidate.Value:X8}");

            // Try to follow the pointer
            long? targetAddr = scanner.ReadInt32(candidate.Address);
            if (targetAddr.HasValue)
            {
                string mapName = scanner.ReadString(targetAddr.Value);
                Console.WriteLine($"  Map name: {mapName}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("\n═══ NEXT STEPS ═══");
        Console.WriteLine("1. Write down these addresses");
        Console.WriteLine("2. Change maps in the game");
        Console.WriteLine("3. Run this scan again");
        Console.WriteLine("4. Compare - the address that changes to new map name is your Map Object Pointer!");
        Console.WriteLine();
    }

    /// <summary>
    /// SCENARIO 2: Find Attribute Offsets (STR, SPR, CON, INT, DEX)
    ///
    /// Steps:
    /// 1. Check your character's STR value in game (e.g., 150)
    /// 2. Scan for that value
    /// 3. Add 1 point to STR in game
    /// 4. Narrow scan to find addresses that changed to 151
    /// 5. Calculate offset from known stats base address
    /// </summary>
    public static void Example2_FindAttributeOffsets()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   SCENARIO 2: Finding Attribute Offsets (STR, INT, etc)   ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

        Process[] processes = Process.GetProcessesByName("game");
        if (processes.Length == 0)
        {
            Console.WriteLine("ERROR: Game process not found!");
            return;
        }

        int processId = processes[0].Id;
        var scanner = new MemoryScanner(processId);

        // STEP 1: Initial scan
        Console.WriteLine("═══ STEP 1: Initial Scan ═══");
        Console.WriteLine("What is your character's STRENGTH (STR) value?");
        Console.Write("Enter STR value: ");
        int strValue = int.Parse(Console.ReadLine());

        var results = scanner.ScanForInt32Value(strValue, "STR");

        Console.WriteLine($"Found {results.Count} addresses with value {strValue}\n");

        if (results.Count > 500)
        {
            Console.WriteLine("Too many results. We need to narrow it down...\n");
        }

        // STEP 2: Narrow down
        Console.WriteLine("═══ STEP 2: Narrow Down ═══");
        Console.WriteLine("In the game:");
        Console.WriteLine("  1. Add 1 point to STR (or use item/buff that changes STR)");
        Console.WriteLine("  2. Check your new STR value");
        Console.WriteLine();
        Console.Write("Enter NEW STR value: ");
        int newStrValue = int.Parse(Console.ReadLine());

        var narrowedResults = scanner.NarrowScan(results, newStrValue);

        Console.WriteLine($"Narrowed to {narrowedResults.Count} addresses\n");

        // STEP 3: Compare with known base
        Console.WriteLine("═══ STEP 3: Calculate Offsets from Stats Base ═══");
        Console.WriteLine("Enter the Stats Base Address you already know:");
        Console.Write("Stats Base (decimal): ");
        long statsBase = long.Parse(Console.ReadLine());

        Console.WriteLine("\nCalculating offsets from stats base...\n");

        foreach (var result in narrowedResults.Take(20))
        {
            long offset = result.Address - statsBase;

            Console.WriteLine($"Address: 0x{result.Address:X8} ({result.Address})");
            Console.WriteLine($"  Offset from stats base: {offset} (0x{offset:X})");

            if (offset > 0 && offset < 10000)
            {
                Console.WriteLine($"  *** LIKELY CANDIDATE: StatsBase + {offset} ***");
            }
            Console.WriteLine();
        }

        // STEP 4: Verify with memory dump
        Console.WriteLine("═══ STEP 4: Verify with Memory Dump ═══");
        if (narrowedResults.Count > 0)
        {
            long firstResult = narrowedResults[0].Address;
            Console.WriteLine($"Dumping memory around 0x{firstResult:X8}:\n");
            scanner.DumpMemory(firstResult - 100, 512);
        }
    }

    /// <summary>
    /// SCENARIO 3: Quick test of known player pointer chain
    /// Validates if your updated addresses are correct
    /// </summary>
    public static void Example3_TestPlayerPointerChain()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   SCENARIO 3: Test Player Pointer Chain                   ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

        Process[] processes = Process.GetProcessesByName("game");
        if (processes.Length == 0)
        {
            Console.WriteLine("ERROR: Game process not found!");
            return;
        }

        int processId = processes[0].Id;
        var scanner = new MemoryScanner(processId);

        // Test the updated player pointer chain
        const long PLAYER_BASE = 2381824; // 0x00245580

        Console.WriteLine($"Testing Player Base Pointer: 0x{PLAYER_BASE:X8} ({PLAYER_BASE})\n");

        // Level 1: Read entity object pointer
        int? entityPtr = scanner.ReadInt32(PLAYER_BASE);
        if (!entityPtr.HasValue || entityPtr.Value == 0)
        {
            Console.WriteLine("❌ Failed to read Entity Object Pointer");
            Console.WriteLine("   The base address might be incorrect!\n");
            return;
        }

        Console.WriteLine($"✓ Entity Object Pointer: 0x{entityPtr.Value:X8}");

        // Level 2: Entity base (+12)
        int? entityBase = scanner.ReadInt32(entityPtr.Value + 12);
        if (!entityBase.HasValue || entityBase.Value == 0)
        {
            Console.WriteLine("❌ Failed to read Entity Base");
            return;
        }

        Console.WriteLine($"✓ Entity Base Address: 0x{entityBase.Value:X8}");

        // Read coordinates
        Console.WriteLine("\n═══ Reading Coordinates ═══");
        Console.WriteLine($"Reading from EntityBase + 92 and EntityBase + 100...\n");

        // Note: We need to read as float, but our scanner reads int32
        // You would need to add ReadFloat method to scanner

        // Level 3: Stats object pointer (+340)
        int? statsPtr = scanner.ReadInt32(entityBase.Value + 340);
        if (!statsPtr.HasValue || statsPtr.Value == 0)
        {
            Console.WriteLine("❌ Failed to read Stats Object Pointer");
            Console.WriteLine("   The offset +340 might be incorrect!");
            return;
        }

        Console.WriteLine($"✓ Stats Object Pointer: 0x{statsPtr.Value:X8}");

        // Level 4: Stats base (+4)
        int? statsBase = scanner.ReadInt32(statsPtr.Value + 4);
        if (!statsBase.HasValue || statsBase.Value == 0)
        {
            Console.WriteLine("❌ Failed to read Stats Base");
            return;
        }

        Console.WriteLine($"✓ Stats Base Address: 0x{statsBase.Value:X8}");

        // Read character data
        Console.WriteLine("\n═══ Reading Character Data ═══");

        string charName = scanner.ReadString(statsBase.Value + 48);
        int? currentHP = scanner.ReadInt32(statsBase.Value + 1752);
        int? currentMP = scanner.ReadInt32(statsBase.Value + 1756);
        int? maxHP = scanner.ReadInt32(statsBase.Value + 1856);
        int? maxMP = scanner.ReadInt32(statsBase.Value + 1860);
        int? exp = scanner.ReadInt32(statsBase.Value + 92);

        Console.WriteLine($"Character Name: {charName}");
        Console.WriteLine($"HP: {currentHP}/{maxHP}");
        Console.WriteLine($"MP: {currentMP}/{maxMP}");
        Console.WriteLine($"Experience: {exp}");

        if (string.IsNullOrEmpty(charName))
        {
            Console.WriteLine("\n⚠ WARNING: Character name is empty!");
            Console.WriteLine("   The pointer chain might be incorrect.");
        }
        else
        {
            Console.WriteLine("\n✅ SUCCESS! Pointer chain is working correctly!");
        }

        // Dump stats structure
        Console.WriteLine("\n═══ Stats Structure Memory Dump ═══");
        scanner.DumpMemory(statsBase.Value, 2500, true);
    }

    /// <summary>
    /// SCENARIO 4: Find all attributes at once using pattern matching
    /// </summary>
    public static void Example4_FindAllAttributesPattern()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   SCENARIO 4: Find All Attributes (Pattern Method)        ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

        Process[] processes = Process.GetProcessesByName("game");
        if (processes.Length == 0)
        {
            Console.WriteLine("ERROR: Game process not found!");
            return;
        }

        int processId = processes[0].Id;
        var scanner = new MemoryScanner(processId);

        Console.WriteLine("Enter your character's attribute values:\n");

        Console.Write("Strength (STR): ");
        int str = int.Parse(Console.ReadLine());

        Console.Write("Spirit (SPR): ");
        int spr = int.Parse(Console.ReadLine());

        Console.Write("Constitution (CON): ");
        int con = int.Parse(Console.ReadLine());

        Console.Write("Intelligence (INT): ");
        int intel = int.Parse(Console.ReadLine());

        Console.Write("Dexterity (DEX): ");
        int dex = int.Parse(Console.ReadLine());

        Console.WriteLine("\nSearching for this pattern of 5 consecutive values...\n");

        // Scan for STR value
        var strResults = scanner.ScanForInt32Value(str, "STR");

        Console.WriteLine($"\nFound {strResults.Count} addresses with STR value {str}");
        Console.WriteLine("Checking if the next 4 int32s match SPR, CON, INT, DEX...\n");

        var matchingPatterns = new List<long>();

        foreach (var result in strResults)
        {
            // Read next 4 int32 values
            int? val1 = scanner.ReadInt32(result.Address + 4);
            int? val2 = scanner.ReadInt32(result.Address + 8);
            int? val3 = scanner.ReadInt32(result.Address + 12);
            int? val4 = scanner.ReadInt32(result.Address + 16);

            if (val1 == spr && val2 == con && val3 == intel && val4 == dex)
            {
                matchingPatterns.Add(result.Address);
                Console.WriteLine($"✓ FOUND PATTERN at 0x{result.Address:X8}:");
                Console.WriteLine($"  +0  STR = {str}");
                Console.WriteLine($"  +4  SPR = {spr}");
                Console.WriteLine($"  +8  CON = {con}");
                Console.WriteLine($"  +12 INT = {intel}");
                Console.WriteLine($"  +16 DEX = {dex}");
                Console.WriteLine();
            }
        }

        if (matchingPatterns.Count == 0)
        {
            Console.WriteLine("❌ No matching patterns found.");
            Console.WriteLine("   The attributes might not be stored consecutively,");
            Console.WriteLine("   or the values might be incorrect.\n");
        }
        else if (matchingPatterns.Count == 1)
        {
            Console.WriteLine($"✅ Found exactly ONE match! This is likely the correct location.\n");

            long baseAddr = matchingPatterns[0];
            Console.WriteLine("Enter your Stats Base Address:");
            Console.Write("Stats Base (decimal): ");
            long statsBase = long.Parse(Console.ReadLine());

            long offset = baseAddr - statsBase;
            Console.WriteLine($"\n STR Offset: +{offset} (0x{offset:X})");
            Console.WriteLine($" SPR Offset: +{offset + 4} (0x{offset + 4:X})");
            Console.WriteLine($" CON Offset: +{offset + 8} (0x{offset + 8:X})");
            Console.WriteLine($" INT Offset: +{offset + 12} (0x{offset + 12:X})");
            Console.WriteLine($" DEX Offset: +{offset + 16} (0x{offset + 16:X})");
            Console.WriteLine();

            // Dump surrounding memory
            Console.WriteLine("═══ Memory Dump Around Attributes ═══");
            scanner.DumpMemory(baseAddr - 40, 200);
        }
        else
        {
            Console.WriteLine($"⚠ Found {matchingPatterns.Count} matches. Need to narrow down.\n");
            foreach (var addr in matchingPatterns)
            {
                Console.WriteLine($"  0x{addr:X8}");
            }
        }
    }

    /// <summary>
    /// SCENARIO 5: Monitor memory changes during map change
    /// </summary>
    public static void Example5_MonitorMapChanges()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   SCENARIO 5: Monitor Memory During Map Change            ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

        Process[] processes = Process.GetProcessesByName("game");
        if (processes.Length == 0)
        {
            Console.WriteLine("ERROR: Game process not found!");
            return;
        }

        int processId = processes[0].Id;
        var scanner = new MemoryScanner(processId);

        Console.WriteLine("This will monitor candidate addresses during map change.\n");
        Console.WriteLine("Enter candidate Map Object Pointer addresses (one per line).");
        Console.WriteLine("Enter 'done' when finished:\n");

        var candidateAddresses = new List<long>();

        while (true)
        {
            Console.Write("Address (decimal or 0xHex): ");
            string input = Console.ReadLine();

            if (input.ToLower() == "done")
                break;

            try
            {
                long address;
                if (input.StartsWith("0x") || input.StartsWith("0X"))
                {
                    address = Convert.ToInt64(input, 16);
                }
                else
                {
                    address = long.Parse(input);
                }

                candidateAddresses.Add(address);
                Console.WriteLine($"  Added: 0x{address:X8}\n");
            }
            catch
            {
                Console.WriteLine("  Invalid address format\n");
            }
        }

        if (candidateAddresses.Count == 0)
        {
            Console.WriteLine("No addresses to monitor.");
            return;
        }

        // Initial reading
        Console.WriteLine("\n═══ INITIAL STATE ═══\n");
        var initialValues = new Dictionary<long, object>();

        foreach (var addr in candidateAddresses)
        {
            int? ptrValue = scanner.ReadInt32(addr);
            if (ptrValue.HasValue)
            {
                string mapName = scanner.ReadString(ptrValue.Value);
                initialValues[addr] = new { Pointer = ptrValue.Value, MapName = mapName };

                Console.WriteLine($"0x{addr:X8}:");
                Console.WriteLine($"  Pointer: 0x{ptrValue.Value:X8}");
                Console.WriteLine($"  Map: {mapName}");
                Console.WriteLine();
            }
        }

        Console.WriteLine("═══ NOW CHANGE MAPS IN GAME ═══");
        Console.WriteLine("Press ENTER when you've changed maps...");
        Console.ReadLine();

        // After map change
        Console.WriteLine("\n═══ AFTER MAP CHANGE ═══\n");

        foreach (var addr in candidateAddresses)
        {
            int? ptrValue = scanner.ReadInt32(addr);
            if (ptrValue.HasValue)
            {
                string mapName = scanner.ReadString(ptrValue.Value);

                Console.WriteLine($"0x{addr:X8}:");
                Console.WriteLine($"  Pointer: 0x{ptrValue.Value:X8}");
                Console.WriteLine($"  Map: {mapName}");

                var initial = (dynamic)initialValues[addr];
                if (mapName != initial.MapName)
                {
                    Console.WriteLine($"  ✓✓✓ CHANGED! (was '{initial.MapName}') ***");
                    Console.WriteLine($"  >>> THIS IS LIKELY YOUR MAP OBJECT POINTER! <<<");
                }
                else
                {
                    Console.WriteLine($"  (no change)");
                }
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// Main menu
    /// </summary>
    public static void Main(string[] args)
    {
        Console.Clear();
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     Dragon Oath Memory Scanner - Usage Guide              ║");
        Console.WriteLine("║     Find Map Object Pointer and Attribute Offsets         ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝\n");

        while (true)
        {
            Console.WriteLine("═══ CHOOSE A SCENARIO ═══\n");
            Console.WriteLine("1. Find Map Object Pointer (scan for map names)");
            Console.WriteLine("2. Find Attribute Offsets (STR, INT, etc) - Step by step");
            Console.WriteLine("3. Test Player Pointer Chain (verify updated addresses)");
            Console.WriteLine("4. Find All Attributes at Once (pattern matching)");
            Console.WriteLine("5. Monitor Map Changes (verify map pointer candidates)");
            Console.WriteLine("0. Exit");
            Console.WriteLine();
            Console.Write("Select option: ");

            string choice = Console.ReadLine();
            Console.WriteLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        Example1_FindMapObjectPointer();
                        break;
                    case "2":
                        Example2_FindAttributeOffsets();
                        break;
                    case "3":
                        Example3_TestPlayerPointerChain();
                        break;
                    case "4":
                        Example4_FindAllAttributesPattern();
                        break;
                    case "5":
                        Example5_MonitorMapChanges();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Invalid option\n");
                        continue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ERROR: {ex.Message}\n");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }
    }
}
