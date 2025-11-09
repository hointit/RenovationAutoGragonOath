# Search Commands for HandleRecvTalkPacket Patterns

## Pattern 1: NULL Pointer Check
Looking for: `cmp [ebp+arg], 0` → `jnz` → `push` → `call Assert` → `mov eax, 0FFFFFFFFh` → `ret`

### PowerShell Command:
```powershell
# Search for NULL check pattern followed by return -1
Select-String -Path "g:\microauto-6.9\AutoDragonOath\Docs\result.txt" -Pattern "cmp.*\[ebp.*\], 0" -Context 0,10 | Where-Object { $_.Context.PostContext -match "jnz" -and $_.Context.PostContext -match "0FFFFFFFFh" }
```

### Alternative - Simple grep style:
```powershell
# Find all lines with "cmp [ebp+", then manually check next 10 lines
Select-String -Path "g:\microauto-6.9\AutoDragonOath\Docs\result.txt" -Pattern "cmp\s+\[ebp\+.*\],\s*0"
```

---

## Pattern 2: IsBlackName Check (Multiple Function Calls)
Looking for: Multiple `call` instructions → `test al, al` → `xor eax, eax` (return 0)

### PowerShell Command:
```powershell
# Search for test al, al (boolean check after function call)
Select-String -Path "g:\microauto-6.9\AutoDragonOath\Docs\result.txt" -Pattern "test\s+al,\s*al" -Context 5,5
```

### Find chain of calls:
```powershell
# Look for pattern: call → mov ecx, eax → call (method chaining)
Select-String -Path "g:\microauto-6.9\AutoDragonOath\Docs\result.txt" -Pattern "call.*\r?\n.*mov\s+ecx,\s*eax\r?\n.*call" -AllMatches
```

---

## Pattern 3: HistoryMsg Creation (Local Object)
Looking for: `lea ecx, [ebp-XX]` → `call` constructor → `push [ebp+arg]` → `lea ecx` again → `call SetByPacket`

### PowerShell Command:
```powershell
# Search for lea ecx with negative offset (local variable)
Select-String -Path "g:\microauto-6.9\AutoDragonOath\Docs\result.txt" -Pattern "lea\s+ecx,\s*\[ebp-" -Context 0,10
```

---

## Combined Search: Find ALL Candidates

### Method 1: Search for function prologues with specific patterns
```powershell
# Find all functions that start with standard prologue
$content = Get-Content "g:\microauto-6.9\AutoDragonOath\Docs\result.txt" -Raw
$pattern = '(?ms)(seg\d+:\w+).*?push\s+ebp.*?mov\s+ebp,\s*esp.*?sub\s+esp.*?cmp\s+\[ebp\+.*?\],\s*0.*?0FFFFFFFFh'
[regex]::Matches($content, $pattern) | ForEach-Object { 
    Write-Host "Found candidate at: $($_.Groups[1].Value)"
    Write-Host $_.Value
    Write-Host "`n---`n"
}
```

### Method 2: Search for specific instruction sequences
```powershell
# Find functions with NULL check AND return -1
$lines = Get-Content "g:\microauto-6.9\AutoDragonOath\Docs\result.txt"
$results = @()

for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match "cmp\s+\[ebp\+.*?\],\s*0") {
        $context = $lines[$i..($i+20)] -join "`n"
        if ($context -match "0FFFFFFFFh" -and $context -match "jnz") {
            $results += @{
                LineNumber = $i + 1
                Address = ($lines[$i] -split '\s+')[0]
                Context = $lines[($i-5)..($i+15)] -join "`n"
            }
        }
    }
}

$results | ForEach-Object {
    Write-Host "=== Found at Line $($_.LineNumber) - Address $($_.Address) ===`n"
    Write-Host $_.Context
    Write-Host "`n"
}
```

---

## Simplified Manual Search

### Step 1: Open result.txt in VS Code
```powershell
code "g:\microauto-6.9\AutoDragonOath\Docs\result.txt"
```

### Step 2: Use VS Code Search (Ctrl+F)

**Search 1: Find NULL check pattern**
- Search: `cmp.*\[ebp\+.*\], 0`
- Enable: Regex (Alt+R)
- Look through results for ones followed by `jnz` and `0FFFFFFFFh`

**Search 2: Find test al, al (boolean checks)**
- Search: `test\s+al,\s*al`
- Enable: Regex
- Look for ones near multiple `call` instructions

**Search 3: Find local variable creation**
- Search: `lea\s+ecx,\s*\[ebp-`
- Enable: Regex
- Look for ones followed by `call` and `push`

---

## Quick Grep-Style Search (if you have grep installed)

```bash
# Pattern 1: NULL check
grep -n "cmp.*\[ebp.*\], 0" result.txt | head -20

# Pattern 2: Boolean test
grep -n "test.*al.*al" result.txt | head -20

# Pattern 3: Local object
grep -n "lea.*ecx.*\[ebp-" result.txt | head -20
```

---

## Best Approach: Combined Pattern Search

```powershell
# This searches for all three patterns near each other
$file = "g:\microauto-6.9\AutoDragonOath\Docs\result.txt"
$lines = Get-Content $file

Write-Host "Searching for HandleRecvTalkPacket patterns...`n"

$candidates = @()

for ($i = 0; $i -lt $lines.Count; $i++) {
    # Check for Pattern 1: NULL check
    if ($lines[$i] -match "cmp\s+\[ebp\+.*?\],\s*0") {
        $block = $lines[$i..($i+100)] -join "`n"
        
        # Check if this block has all 3 patterns
        $hasPattern1 = $block -match "0FFFFFFFFh"  # return -1
        $hasPattern2 = $block -match "test\s+al,\s*al"  # boolean check
        $hasPattern3 = $block -match "lea\s+ecx,\s*\[ebp-"  # local var
        
        if ($hasPattern1 -and $hasPattern2 -and $hasPattern3) {
            Write-Host "=== STRONG CANDIDATE at line $($i+1) ===`n"
            Write-Host "Address: $($lines[$i] -replace '\s+.*', '')`n"
            Write-Host "Context:`n"
            Write-Host ($lines[($i-5)..($i+50)] -join "`n")
            Write-Host "`n`n"
            
            $candidates += $i + 1
        }
    }
}

Write-Host "`nFound $($candidates.Count) strong candidates at lines: $($candidates -join ', ')"
```

---

## Expected Output Format

When you find the function, you should see something like:

```
seg000:00789A20  push    ebp
seg000:00789A21  mov     ebp, esp
seg000:00789A23  sub     esp, 4Ch
seg000:00789A26  push    ebx
seg000:00789A27  push    esi
seg000:00789A28  push    edi
seg000:00789A2E  cmp     [ebp+8], 0          ← Pattern 1
seg000:00789A32  jnz     short loc_789A45
seg000:00789A40  call    TDAssert
seg000:00789A45  mov     eax, 0FFFFFFFFh     ← return -1
...
seg000:00789A6C  test    al, al              ← Pattern 2
seg000:00789A70  xor     eax, eax            ← return 0
...
seg000:00789A80  lea     ecx, [ebp-14h]      ← Pattern 3
seg000:00789A83  call    sub_6C8910
```

The address you want is the first one: **seg000:00789A20**

---

## Run This First:

Save this PowerShell script and run it:

```powershell
# Save as: search_handler.ps1
$file = "g:\microauto-6.9\AutoDragonOath\Docs\result.txt"

Write-Host "Searching for HandleRecvTalkPacket function...`n" -ForegroundColor Green

# Quick search for functions with all key patterns
Select-String -Path $file -Pattern "cmp\s+\[ebp\+.*?\],\s*0" | ForEach-Object {
    $lineNum = $_.LineNumber
    $content = Get-Content $file
    $block = ($content[($lineNum-5)..($lineNum+100)]) -join "`n"
    
    if ($block -match "0FFFFFFFFh" -and 
        $block -match "test\s+al,\s*al" -and 
        $block -match "lea\s+ecx,\s*\[ebp-") {
        
        Write-Host "Found candidate at line: $lineNum" -ForegroundColor Yellow
        Write-Host ($content[($lineNum-10)..($lineNum+30)] -join "`n")
        Write-Host "`n================================`n"
    }
}
```

Then run:
```powershell
.\search_handler.ps1
```
