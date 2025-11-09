# Find HandleRecvTalkPacket - More flexible search
param(
    [string]$FilePath = "g:\microauto-6.9\AutoDragonOath\Docs\result.txt"
)

Write-Host "Searching for chat/talk handler patterns..." -ForegroundColor Cyan
Write-Host ""

$lines = Get-Content $FilePath

# First, let's find functions that look like packet handlers
# Look for functions with multiple characteristic patterns

$results = @()

Write-Host "Phase 1: Finding functions with 'cmp [ebp+X], 0' pattern..." -ForegroundColor Yellow
$cmpMatches = Select-String -Path $FilePath -Pattern "cmp\s+(dword ptr\s+)?\[ebp\+\w+\],\s*0" -AllMatches

Write-Host "Found $($cmpMatches.Count) NULL check patterns" -ForegroundColor Green
Write-Host ""

foreach ($match in $cmpMatches) {
    $lineNum = $match.LineNumber
    $startLine = [Math]::Max(0, $lineNum - 20)
    $endLine = [Math]::Min($lineNum + 80, $lines.Count - 1)
    
    $block = $lines[$startLine..$endLine] -join "`n"
    
    # Look for function characteristics
    $hasProlog = $block -match "push\s+ebp.*mov\s+ebp,\s*esp"
    $hasReturnNeg1 = $block -match "(mov|or)\s+eax,\s*0FFFFFFFFh"
    $hasFunctionCalls = ($block -match "call\s+" | Measure-Object).Count -ge 3
    $hasTest = $block -match "test\s+(al|eax)"
    $hasLea = $block -match "lea\s+ecx,\s*\[ebp"
    
    # Score the match
    $score = 0
    if ($hasProlog) { $score++ }
    if ($hasReturnNeg1) { $score++ }
    if ($hasFunctionCalls) { $score++ }
    if ($hasTest) { $score++ }
    if ($hasLea) { $score++ }
    
    # If score is high enough, it's a candidate
    if ($score -ge 3) {
        # Find actual function start
        $funcStart = $lineNum - 20
        for ($i = $lineNum; $i -gt [Math]::Max(0, $lineNum - 30); $i--) {
            if ($lines[$i-1] -match "push\s+ebp" -and $lines[$i] -match "mov\s+ebp,\s*esp") {
                $funcStart = $i - 1
                break
            }
        }
        
        # Get address
        $addr = "Unknown"
        if ($lines[$funcStart] -match "^([^\s]+)") {
            $addr = $matches[1]
        }
        
        $results += [PSCustomObject]@{
            Address = $addr
            Line = $funcStart + 1
            Score = $score
            HasProlog = $hasProlog
            HasReturnNeg1 = $hasReturnNeg1
            HasCalls = $hasFunctionCalls
            HasTest = $hasTest
            HasLea = $hasLea
        }
    }
}

Write-Host "Found $($results.Count) potential candidates" -ForegroundColor Green
Write-Host ""

# Show top candidates
$topCandidates = $results | Sort-Object -Property Score -Descending | Select-Object -First 10

if ($topCandidates) {
    Write-Host "Top Candidates:" -ForegroundColor Yellow
    Write-Host "===========================================" -ForegroundColor Yellow
    Write-Host ""
    
    $num = 1
    foreach ($cand in $topCandidates) {
        Write-Host "Candidate #$num (Score: $($cand.Score)/5)" -ForegroundColor Cyan
        Write-Host "  Address: $($cand.Address)" -ForegroundColor White
        Write-Host "  Line: $($cand.Line)" -ForegroundColor White
        Write-Host "  Patterns:" -ForegroundColor Gray
        Write-Host "    - Function Prolog: $(if($cand.HasProlog){'✓'}else{'✗'})" -ForegroundColor $(if($cand.HasProlog){'Green'}else{'Red'})
        Write-Host "    - Return -1: $(if($cand.HasReturnNeg1){'✓'}else{'✗'})" -ForegroundColor $(if($cand.HasReturnNeg1){'Green'}else{'Red'})
        Write-Host "    - Multiple Calls: $(if($cand.HasCalls){'✓'}else{'✗'})" -ForegroundColor $(if($cand.HasCalls){'Green'}else{'Red'})
        Write-Host "    - Test instruction: $(if($cand.HasTest){'✓'}else{'✗'})" -ForegroundColor $(if($cand.HasTest){'Green'}else{'Red'})
        Write-Host "    - Local var (lea): $(if($cand.HasLea){'✓'}else{'✗'})" -ForegroundColor $(if($cand.HasLea){'Green'}else{'Red'})
        Write-Host ""
        
        # Show code snippet
        Write-Host "  Code preview:" -ForegroundColor Gray
        $startIdx = $cand.Line - 1
        $endIdx = [Math]::Min($startIdx + 30, $lines.Count - 1)
        for ($i = $startIdx; $i -le $endIdx; $i++) {
            Write-Host "    $($lines[$i])" -ForegroundColor DarkGray
        }
        Write-Host ""
        Write-Host "-------------------------------------------" -ForegroundColor DarkGray
        Write-Host ""
        
        $num++
    }
} else {
    Write-Host "No strong candidates found" -ForegroundColor Red
}

Write-Host ""
Write-Host "Search complete!" -ForegroundColor Green
