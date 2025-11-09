@echo off
REM Test Pattern 1 - Quick test script
echo ===============================================
echo   Testing ChatHookDLL Pattern 1
echo   (Address: 0x0048D6F0, Offset: +0x8D6F0)
echo ===============================================
echo.

REM Check if DLL exists
if not exist "ChatHookDLL_Pattern1.dll" (
    echo ERROR: ChatHookDLL_Pattern1.dll not found!
    echo Please run compile_all.bat first.
    pause
    exit /b 1
)

REM Check if game is running
tasklist | findstr /i "Game.exe" >nul
if errorlevel 1 (
    echo WARNING: Game.exe is not running!
    echo Please start the game first.
    pause
    exit /b 1
)

echo Game.exe is running...
echo.
echo Injecting Pattern 1...
ChatInjector.exe ChatHookDLL_Pattern1.dll

echo.
echo ===============================================
echo   Injection Complete
echo ===============================================
echo.
echo Next steps:
echo 1. Send a chat message in the game
echo 2. Check the log file:
echo.

REM Wait a moment for injection to complete
timeout /t 2 /nobreak >nul

echo Showing log file contents:
echo -----------------------------------------------
type C:\DragonOath_ChatLog_Pattern1.txt 2>nul

if errorlevel 1 (
    echo Log file not created yet. Wait a moment and check manually:
    echo    type C:\DragonOath_ChatLog_Pattern1.txt
)

echo.
echo -----------------------------------------------
echo.
echo If you see "SUCCESS: Hook installed" above,
echo send a chat message and check the log again.
echo.
echo If Pattern 1 doesn't work, run test_pattern2.bat
echo.
pause
