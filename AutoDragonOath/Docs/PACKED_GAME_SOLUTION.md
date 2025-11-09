# Finding HandleRecvTalkPacket in Packed Game.exe

## Problem: The Game is Packed! 🔒

Your IDA `result.txt` analysis shows that **Game.exe is packed with ASPack**. This means:

- ✗ The actual game code is encrypted/compressed
- ✗ IDA cannot disassemble the packed code
- ✗ Pattern searching in result.txt will NOT work
- ✓ The code only exists in memory when the game is running

## Evidence from result.txt:

```
Sections found:
- seg000-seg004: Data only (dd declarations)
- .data: More data
- .aspack: Packed/encrypted code (cannot be analyzed)

Only 1 function found in entire 467K lines!
```

---

## Solution 1: Runtime Memory Scanning (RECOMMENDED) ✅

Since your C# code already attaches to the running game process, you can search for the pattern **in memory** instead of in the file.

### Step 1: Create Pattern from Source Code Logic

Based on the source code patterns, create a signature:

```csharp
// Pattern for HandleRecvTalkPacket
// Looking for: push ebp; mov ebp, esp; sub esp, XX; push ebx; push esi; push edi
private static readonly byte[] PATTERN = new byte[]
{
    0x55,                    // push ebp
    0x8B, 0xEC,              // mov ebp, esp
    0x83, 0xEC, 0xFF,        // sub esp, ?? (wildcard)
    0x53,                    // push ebx
    0x56,                    // push esi
    0x57                     // push edi
};

private static readonly string MASK = "xxxxx?xxx";
```

### Step 2: Scan Game Memory

Use your existing `GameMemory` or `AddressFinder` class:

```csharp
public IntPtr FindHandleRecvTalkPacket()
{
    // Common address ranges for game code after unpacking
    IntPtr baseAddress = Process.MainModule.BaseAddress;
    
    // Scan from base + 0x1000 to base + 0x500000 (typical code section)
    IntPtr startAddress = IntPtr.Add(baseAddress, 0x1000);
    int scanSize = 0x500000;
    
    // Your existing pattern scan function
    IntPtr address = ScanPattern(startAddress, scanSize, PATTERN, MASK);
    
    if (address != IntPtr.Zero)
    {
        Debug.WriteLine($"Found HandleRecvTalkPacket at: 0x{address.ToInt64():X}");
        return address;
    }
    
    return IntPtr.Zero;
}
```

### Step 3: Verify Multiple Patterns

To be more confident, scan for multiple characteristic patterns:

```csharp
private IntPtr FindWithMultiplePatterns()
{
    // Pattern 1: Function prologue
    var pattern1 = new byte[] { 0x55, 0x8B, 0xEC, 0x83, 0xEC };
    
    // Pattern 2: NULL check (cmp [ebp+8], 0)
    var pattern2 = new byte[] { 0x83, 0x7D, 0x08, 0x00 };
    
    // Pattern 3: return -1
    var pattern3 = new byte[] { 0xB8, 0xFF, 0xFF, 0xFF, 0xFF, 0xC3 };
    
    // Find candidates with pattern1
    var candidates = FindAllMatches(pattern1);
    
    // Check each candidate for patterns 2 and 3 nearby
    foreach (var candidate in candidates)
    {
        byte[] block = ReadMemory(candidate, 200);
        
        if (ContainsPattern(block, pattern2) && 
            ContainsPattern(block, pattern3))
        {
            return candidate;
        }
    }
    
    return IntPtr.Zero;
}
```

---

## Solution 2: Unpack the Game.exe (ADVANCED) 🔓

If you want to analyze the unpacked code in IDA:

### Tools Needed:
- **ASPack Unpacker** or generic unpackers like:
  - UPX
  - PEiD with plugins
  - Unipacker
  - Manual unpacking with OllyDbg/x64dbg

### Steps:

1. **Detect Packer:**
   ```bash
   # Use PEiD or DIE (Detect It Easy)
   DIE Game.exe
   ```

2. **Unpack with appropriate tool:**
   ```bash
   # If it's ASPack
   aspack-unpacker.exe Game.exe Game_unpacked.exe
   
   # Or use OllyDbg:
   # 1. Load Game.exe in OllyDbg
   # 2. Let it run to OEP (Original Entry Point)
   # 3. Dump the unpacked process
   # 4. Fix imports with ImpREC
   ```

3. **Load unpacked exe in IDA:**
   ```
   Now the code will be visible and you can search for patterns!
   ```

---

## Solution 3: Dynamic Analysis in Debugger 🐛

### Using x64dbg or OllyDbg:

1. **Attach to running game:**
   ```
   x64dbg.exe
   File → Attach → Game.exe
   ```

2. **Search for string references:**
   ```
   Right-click → Search for → All modules → String references
   Search for: "Talk", "Chat", "Packet", "Black", "Name"
   ```

3. **Set breakpoints:**
   ```
   Find functions that reference these strings
   Set breakpoint (F2)
   Continue execution (F9)
   When chat message is sent → breakpoint hits
   ```

4. **Note the address:**
   ```
   When breakpoint hits, you're in HandleRecvTalkPacket!
   Note the function address
   ```

5. **Extract pattern from memory:**
   ```
   Copy first 20 bytes of the function
   Use as your pattern in C# code
   ```

---

## Recommended Approach for You:

Since you already have C# code that attaches to the game, **use Solution 1 (Runtime Memory Scanning)**:

### Complete Implementation:

```csharp
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class ChatAddressFinder
{
    [DllImport("kernel32.dll")]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, 
        byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);
    
    private Process _process;
    
    public ChatAddressFinder(Process process)
    {
        _process = process;
    }
    
    public IntPtr FindHandleRecvTalkPacket()
    {
        // Multiple possible patterns (game might be updated)
        var patterns = new[]
        {
            // Pattern 1: Standard prologue with push edi
            new { 
                Bytes = new byte[] { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0xFF, 0x53, 0x56, 0x57 },
                Mask = "xxxxx?xxx"
            },
            // Pattern 2: Shorter prologue
            new { 
                Bytes = new byte[] { 0x55, 0x8B, 0xEC, 0x53, 0x56, 0x57 },
                Mask = "xxxxxx"
            },
            // Pattern 3: With sub esp, larger value
            new { 
                Bytes = new byte[] { 0x55, 0x8B, 0xEC, 0x81, 0xEC },
                Mask = "xxxxx"
            }
        };
        
        IntPtr baseAddress = _process.MainModule.BaseAddress;
        int moduleSize = _process.MainModule.ModuleMemorySize;
        
        // Scan from start of code section (usually after PE header)
        IntPtr searchStart = IntPtr.Add(baseAddress, 0x1000);
        int searchSize = Math.Min(moduleSize - 0x1000, 0x800000); // Search up to 8MB
        
        Debug.WriteLine($"Scanning memory from 0x{searchStart.ToInt64():X} size: 0x{searchSize:X}");
        
        foreach (var pattern in patterns)
        {
            Debug.WriteLine($"Trying pattern: {BitConverter.ToString(pattern.Bytes)}");
            
            var candidates = FindAllPatterns(searchStart, searchSize, pattern.Bytes, pattern.Mask);
            
            Debug.WriteLine($"Found {candidates.Count} candidates");
            
            // Verify each candidate
            foreach (var candidate in candidates)
            {
                if (VerifyFunctionPattern(candidate))
                {
                    Debug.WriteLine($"✓ Verified: HandleRecvTalkPacket at 0x{candidate.ToInt64():X}");
                    return candidate;
                }
            }
        }
        
        Debug.WriteLine("× HandleRecvTalkPacket not found");
        return IntPtr.Zero;
    }
    
    private List<IntPtr> FindAllPatterns(IntPtr start, int size, byte[] pattern, string mask)
    {
        var results = new List<IntPtr>();
        byte[] buffer = new byte[size];
        
        if (!ReadProcessMemory(_process.Handle, start, buffer, size, out int bytesRead))
            return results;
        
        for (int i = 0; i < bytesRead - pattern.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (mask[j] == '?' || buffer[i + j] == pattern[j])
                    continue;
                    
                found = false;
                break;
            }
            
            if (found)
            {
                results.Add(IntPtr.Add(start, i));
            }
        }
        
        return results;
    }
    
    private bool VerifyFunctionPattern(IntPtr address)
    {
        // Read 200 bytes from the function
        byte[] buffer = new byte[200];
        if (!ReadProcessMemory(_process.Handle, address, buffer, 200, out int bytesRead))
            return false;
        
        // Check for characteristic patterns:
        // 1. NULL check: 83 7D ?? 00 (cmp [ebp+X], 0)
        // 2. return -1: B8 FF FF FF FF or 83 C8 FF (mov eax, -1)
        // 3. Function calls: E8 (call rel32)
        
        bool hasNullCheck = false;
        bool hasReturnNeg1 = false;
        int callCount = 0;
        
        for (int i = 0; i < bytesRead - 5; i++)
        {
            // Check for: cmp [ebp+X], 0
            if (buffer[i] == 0x83 && buffer[i+1] == 0x7D && buffer[i+3] == 0x00)
                hasNullCheck = true;
            
            // Check for: mov eax, -1
            if (buffer[i] == 0xB8 && buffer[i+1] == 0xFF && buffer[i+2] == 0xFF)
                hasReturnNeg1 = true;
            
            // Count function calls
            if (buffer[i] == 0xE8)
                callCount++;
        }
        
        // Valid HandleRecvTalkPacket should have:
        // - NULL check
        // - return -1 path
        // - Multiple function calls (IsBlackName, SetByPacket, etc.)
        return hasNullCheck && hasReturnNeg1 && callCount >= 3;
    }
}
```

### Usage:

```csharp
var process = Process.GetProcessesByName("Game")[0];
var finder = new ChatAddressFinder(process);

IntPtr handleRecvTalkPacket = finder.FindHandleRecvTalkPacket();

if (handleRecvTalkPacket != IntPtr.Zero)
{
    Console.WriteLine($"Found at: 0x{handleRecvTalkPacket.ToInt64():X}");
    // Now you can hook it!
}
```

---

## Summary:

❌ **Don't use result.txt** - it only has packed data
✅ **Use runtime memory scanning** - search while game is running  
✅ **Pattern is in memory** - not in the file on disk

Your C# code should scan the **running game's memory** to find the function!

