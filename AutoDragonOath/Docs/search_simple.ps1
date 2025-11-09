# Search for HandleRecvTalkPacket - Simple Version
param(
    [string]$FilePath = "g:\microauto-6.9\AutoDragonOath\Docs\result.txt"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "HandleRecvTalkPacket Pattern Search" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $FilePath)) {
    Write-Host "Error: File not found" -ForegroundColor Red
    exit
}

Write-Host "Loading file..." -ForegroundColor Yellow
$lines = Get-Content $FilePath
Write-Host "Total lines: $($lines.Count)" -ForegroundColor Green
Write-Host ""

$candidateCount = 0
$candidates = @()

Write-Host "Searching..." -ForegroundColor Yellow

for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($i % 10000 -eq 0) {
        $percent = [Math]::Floor(($i / $lines.Count) * 100)
        Write-Host "  Progress: $percent%" -ForegroundColor DarkGray
    }
    
    # Look for NULL check pattern
    if ($lines[$i] -match "cmp\s+\[ebp\+.*?\],\s*0") {
        $endLine = [Math]::Min($i + 100, $lines.Count - 1)
        $block = $lines[$i..$endLine] -join "`n"
        
        # Check for all patterns
        $hasPattern1 = $block -match "0FFFFFFFFh"
        $hasPattern2 = $block -match "test\s+(al|eax),\s*(al|eax)"
        $hasPattern3 = $block -match "lea\s+ecx,\s*\[ebp-"
        
        if ($hasPattern1 -and $hasPattern2 -and $hasPattern3) {
            $candidateCount++
            
            # Find function start
            $functionStart = $i
            for ($j = $i; $j -gt [Math]::Max(0, $i - 50); $j--) {
                if ($lines[$j] -match "push\s+ebp" -and $lines[$j+1] -match "mov\s+ebp,\s*esp") {
                    $functionStart = $j
                    break
                }
            }
            
            # Get address
            $funcAddress = "Unknown"
            if ($lines[$functionStart] -match "^([^\s]+)") {
                $funcAddress = $matches[1]
            }
            
            Write-Host ""
            Write-Host "=== CANDIDATE #$candidateCount ===" -ForegroundColor Green
            Write-Host "Address: $funcAddress" -ForegroundColor Yellow
            Write-Host "Line: $($functionStart + 1)" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "Patterns found:" -ForegroundColor Cyan
            Write-Host "  [OK] NULL check + return -1" -ForegroundColor Green
            Write-Host "  [OK] Boolean test" -ForegroundColor Green
            Write-Host "  [OK] Local object creation" -ForegroundColor Green
            Write-Host ""
            Write-Host "Code snippet:" -ForegroundColor Cyan
            
            $contextStart = [Math]::Max(0, $functionStart)
            $contextEnd = [Math]::Min($functionStart + 40, $lines.Count - 1)
            
            for ($k = $contextStart; $k -le $contextEnd; $k++) {
                Write-Host $lines[$k] -ForegroundColor Gray
            }
            
            Write-Host ""
            Write-Host "-----------------------------------"
            Write-Host ""
            
            $candidates += @{
                Number = $candidateCount
                Address = $funcAddress
                Line = $functionStart + 1
            }
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SEARCH COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Found $candidateCount candidate(s)" -ForegroundColor Green
Write-Host ""

if ($candidateCount -gt 0) {
    Write-Host "Summary:" -ForegroundColor Yellow
    $candidates | ForEach-Object {
        Write-Host "  #$($_.Number): $($_.Address) at line $($_.Line)" -ForegroundColor White
    }
} else {
    Write-Host "No candidates found" -ForegroundColor Red
}

Write-Host ""
