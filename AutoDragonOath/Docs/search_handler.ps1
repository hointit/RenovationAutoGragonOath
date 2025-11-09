# Search for HandleRecvTalkPacket in IDA result.txt
# This script looks for the 3 key patterns

param(
    [string]$FilePath = "g:\microauto-6.9\AutoDragonOath\Docs\result.txt"
)

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  HandleRecvTalkPacket Pattern Search" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $FilePath)) {
    Write-Host "Error: File not found at $FilePath" -ForegroundColor Red
    exit
}

Write-Host "Loading file: $FilePath" -ForegroundColor Yellow
$lines = Get-Content $FilePath
Write-Host "Total lines: $($lines.Count)" -ForegroundColor Green
Write-Host ""

$candidates = @()
$candidateCount = 0

Write-Host "Searching for patterns..." -ForegroundColor Yellow
Write-Host ""

# Progress tracking
$totalLines = $lines.Count
$progressInterval = [Math]::Floor($totalLines / 100)
$lastProgress = 0

for ($i = 0; $i -lt $lines.Count; $i++) {
    # Show progress
    if ($i % $progressInterval -eq 0) {
        $percent = [Math]::Floor(($i / $totalLines) * 100)
        if ($percent -ne $lastProgress) {
            Write-Progress -Activity "Scanning file" -Status "$percent% Complete" -PercentComplete $percent
            $lastProgress = $percent
        }
    }
    
    # Pattern 1: Look for NULL check (cmp [ebp+X], 0)
    if ($lines[$i] -match "cmp\s+\[ebp\+.*?\],\s*0") {
        # Get next 100 lines for analysis
        $endLine = [Math]::Min($i + 100, $lines.Count - 1)
        $block = $lines[$i..$endLine] -join "`n"
        
        # Check for all three patterns
        $hasPattern1 = $block -match "0FFFFFFFFh"  # return -1
        $hasPattern2 = $block -match "test\s+(al|eax),\s*(al|eax)"  # boolean check
        $hasPattern3 = $block -match "lea\s+ecx,\s*\[ebp-"  # local var creation
        
        if ($hasPattern1 -and $hasPattern2 -and $hasPattern3) {
            $candidateCount++
            
            # Extract address from the line
            $address = ""
            if ($lines[$i] -match "^(seg\d+:\w+)") {
                $address = $matches[1]
            } elseif ($lines[$i] -match "^(\.\w+:\w+)") {
                $address = $matches[1]
            }
            
            # Look back to find function start (push ebp; mov ebp, esp)
            $functionStart = $i
            for ($j = $i; $j -gt [Math]::Max(0, $i - 50); $j--) {
                if ($lines[$j] -match "push\s+ebp" -and $lines[$j+1] -match "mov\s+ebp,\s*esp") {
                    $functionStart = $j
                    break
                }
            }
            
            # Extract function address
            $funcAddress = ""
            if ($lines[$functionStart] -match "^(seg\d+:\w+)") {
                $funcAddress = $matches[1]
            } elseif ($lines[$functionStart] -match "^(\.\w+:\w+)") {
                $funcAddress = $matches[1]
            }
            
            Write-Host ""
            Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Green
            Write-Host "║  CANDIDATE #$candidateCount FOUND                              " -ForegroundColor Green
            Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Green
            Write-Host ""
            Write-Host "Function Start: $funcAddress (Line: $($functionStart+1))" -ForegroundColor Yellow
            Write-Host "NULL Check at:  $address (Line: $($i+1))" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "Pattern Verification:" -ForegroundColor Cyan
            Write-Host "  ✓ Pattern 1: NULL check + return -1" -ForegroundColor Green
            Write-Host "  ✓ Pattern 2: Boolean test (test al, al)" -ForegroundColor Green
            Write-Host "  ✓ Pattern 3: Local object (lea ecx, [ebp-X])" -ForegroundColor Green
            Write-Host ""
            Write-Host "Context (showing 60 lines from function start):" -ForegroundColor Cyan
            Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
            
            # Show context
            $contextStart = [Math]::Max(0, $functionStart - 5)
            $contextEnd = [Math]::Min($functionStart + 60, $lines.Count - 1)
            
            for ($k = $contextStart; $k -le $contextEnd; $k++) {
                $line = $lines[$k]
                
                # Highlight key patterns
                if ($k -eq $functionStart) {
                    Write-Host $line -ForegroundColor Yellow  # Function start
                } elseif ($line -match "cmp\s+\[ebp\+.*?\],\s*0") {
                    Write-Host $line -ForegroundColor Magenta  # Pattern 1
                } elseif ($line -match "0FFFFFFFFh") {
                    Write-Host $line -ForegroundColor Red  # return -1
                } elseif ($line -match "test\s+(al|eax),\s*(al|eax)") {
                    Write-Host $line -ForegroundColor Cyan  # Pattern 2
                } elseif ($line -match "lea\s+ecx,\s*\[ebp-") {
                    Write-Host $line -ForegroundColor Green  # Pattern 3
                } else {
                    Write-Host $line -ForegroundColor Gray
                }
            }
            
            Write-Host "─────────────────────────────────────────────────────────────" -ForegroundColor DarkGray
            Write-Host ""
            
            # Store candidate info
            $candidates += @{
                Number = $candidateCount
                FunctionLine = $functionStart + 1
                FunctionAddress = $funcAddress
                NullCheckLine = $i + 1
                NullCheckAddress = $address
            }
            
            # Pause after each candidate (optional)
            # Read-Host "Press Enter to continue..."
        }
    }
}

Write-Progress -Activity "Scanning file" -Completed

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  SEARCH COMPLETE" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total candidates found: $candidateCount" -ForegroundColor Green
Write-Host ""

if ($candidateCount -gt 0) {
    Write-Host "Summary of all candidates:" -ForegroundColor Yellow
    Write-Host ""
    $candidates | ForEach-Object {
        Write-Host "  Candidate #$($_.Number):" -ForegroundColor Cyan
        Write-Host "    Address: $($_.FunctionAddress)" -ForegroundColor White
        Write-Host "    Line:    $($_.FunctionLine)" -ForegroundColor White
        Write-Host ""
    }
    
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Review each candidate above" -ForegroundColor White
    Write-Host "  2. Verify the patterns match the source code logic" -ForegroundColor White
    Write-Host "  3. Extract the hex pattern from the function start" -ForegroundColor White
    Write-Host "  4. Use the address in your C# code" -ForegroundColor White
} else {
    Write-Host "No candidates found." -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  - Verify result.txt contains disassembly (not hex dump)" -ForegroundColor White
    Write-Host "  - Try manual search with: Select-String -Path result.txt -Pattern cmp" -ForegroundColor White
    Write-Host "  - Check if the patterns are written differently in this version" -ForegroundColor White
}

Write-Host ""
