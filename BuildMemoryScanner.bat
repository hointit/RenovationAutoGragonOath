@echo off
echo ====================================
echo Memory Scanner Builder
echo ====================================
echo.

REM Find csc.exe (C# compiler)
SET CSC_PATH=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe

IF NOT EXIST "%CSC_PATH%" (
    echo ERROR: C# compiler not found at %CSC_PATH%
    echo.
    echo Please install .NET Framework 4.0 or later
    echo Or update CSC_PATH in this batch file
    echo.
    pause
    exit /b 1
)

echo Found C# compiler: %CSC_PATH%
echo.

echo Compiling MemoryScanner...
"%CSC_PATH%" /out:MemoryScanner.exe /platform:x86 MemoryScanner.cs MemoryScannerUsageGuide.cs

IF %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Compilation failed!
    echo.
    pause
    exit /b 1
)

echo.
echo ====================================
echo SUCCESS! MemoryScanner.exe created
echo ====================================
echo.
echo To run the scanner:
echo   1. Make sure Dragon Oath game is running
echo   2. Right-click MemoryScanner.exe
echo   3. Select "Run as Administrator"
echo.
echo Or run this batch file as admin to launch it now:
echo.

REM Check if running as admin
net session >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    echo Running as Administrator - Launching scanner...
    echo.
    MemoryScanner.exe
) ELSE (
    echo Not running as Administrator.
    echo Please run this batch file as admin to launch the scanner.
    echo.
    pause
)
