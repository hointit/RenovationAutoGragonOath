@echo off
REM Test Pattern 2 - Quick test script
echo ===============================================
echo   Testing ChatHookDLL Pattern 2
echo   (Address: 0x0048D790, Offset: +0x8D790)
echo ===============================================
echo.

REM Check if DLL exists
if not exist "ChatHookDLL_Pattern2.dll" (
    echo ERROR: ChatHookDLL_Pattern2.dll not found!
    echo Please run compile_all.bat first.
    pause
    exit /b 1
)

REM Check if game is running
tasklist | findstr /i "Game.exe" >nul
if errorlevel 1 (
    echo WARNING: Game.exe is not running!
    echo Please start the game first.
    echo.
    echo NOTE: If you tested Pattern 1, restart the game
    echo       before testing Pattern 2.
    pause
    exit /b 1
)

echo Game.exe is running...
echo.
echo Injecting Pattern 2...
ChatInjector.exe ChatHookDLL_Pattern2.dll

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
type C:\DragonOath_ChatLog_Pattern2.txt 2>nul

if errorlevel 1 (
    echo Log file not created yet. Wait a moment and check manually:
    echo    type C:\DragonOath_ChatLog_Pattern2.txt
)

echo.
echo -----------------------------------------------
echo.
echo If you see "SUCCESS: Hook installed" above,
echo send a chat message and check the log again.
echo.
echo If Pattern 2 also doesn't work:
echo 1. Re-run Memory Scanner in AutoDragonOath
echo 2. Check if patterns changed (game update?)
echo 3. Try using x64dbg for manual verification
echo.
pause
