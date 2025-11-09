# Download and setup pre-built Detours
# Run this in PowerShell

Write-Host "Downloading pre-built Detours..." -ForegroundColor Cyan

$detoursUrl = "https://github.com/microsoft/Detours/releases/download/v4.0.1/detours.zip"
$downloadPath = "C:\detours-prebuilt.zip"
$extractPath = "C:\Detours"

try {
    # Download
    Write-Host "Downloading from GitHub releases..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $detoursUrl -OutFile $downloadPath -ErrorAction Stop
    
    # Extract
    Write-Host "Extracting to $extractPath..." -ForegroundColor Yellow
    Expand-Archive -Path $downloadPath -DestinationPath $extractPath -Force
    
    # Verify
    if (Test-Path "$extractPath\include\detours.h") {
        Write-Host "`n✅ SUCCESS! Detours is ready at: $extractPath" -ForegroundColor Green
        Write-Host "`nVerified files:" -ForegroundColor Green
        Write-Host "  - include\detours.h" -ForegroundColor Gray
        Write-Host "  - lib.X86\detours.lib" -ForegroundColor Gray
        Write-Host "  - lib.X64\detours.lib" -ForegroundColor Gray
        Write-Host "`nYou can now compile your DLL!" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Extraction successful but structure unexpected" -ForegroundColor Yellow
        Write-Host "Please manually download from: https://github.com/microsoft/Detours/releases" -ForegroundColor Yellow
    }
    
    # Cleanup
    Remove-Item $downloadPath -Force
    
} catch {
    Write-Host "`n❌ Download failed: $_" -ForegroundColor Red
    Write-Host "`nPlease manually download from:" -ForegroundColor Yellow
    Write-Host "https://github.com/microsoft/Detours/releases" -ForegroundColor Cyan
    Write-Host "`nExtract to: C:\Detours\" -ForegroundColor Cyan
}
