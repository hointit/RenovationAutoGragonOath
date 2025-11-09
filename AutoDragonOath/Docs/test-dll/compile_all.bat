@echo off
REM Batch file to compile both ChatHookDLL patterns
REM Run this in Visual Studio Developer Command Prompt (x86)

echo ===============================================
echo   Compiling ChatHookDLL Test Patterns
echo ===============================================
echo.

REM Check if we're in the right directory
if not exist "ChatHookDLL_Pattern1.cpp" (
    echo ERROR: ChatHookDLL_Pattern1.cpp not found!
    echo Please run this from the test-dll directory.
    pause
    exit /b 1
)

REM Check if Detours is installed
if not exist "C:\Detours\include\detours.h" (
    echo ERROR: Microsoft Detours not found at C:\Detours\
    echo Please install Detours first:
    echo   cd C:\
    echo   git clone https://github.com/microsoft/Detours.git
    echo   cd Detours\src
    echo   nmake
    pause
    exit /b 1
)

echo [1/3] Compiling Pattern 1...
cl /LD /MT /O2 ChatHookDLL_Pattern1.cpp ^
   /I"C:\Detours\include" ^
   /link /LIBPATH:"C:\Detours\lib.X86" detours.lib Psapi.lib ^
   /OUT:ChatHookDLL_Pattern1.dll

if errorlevel 1 (
    echo ERROR: Pattern 1 compilation failed!
    pause
    exit /b 1
)

echo.
echo [2/3] Compiling Pattern 2...
cl /LD /MT /O2 ChatHookDLL_Pattern2.cpp ^
   /I"C:\Detours\include" ^
   /link /LIBPATH:"C:\Detours\lib.X86" detours.lib Psapi.lib ^
   /OUT:ChatHookDLL_Pattern2.dll

if errorlevel 1 (
    echo ERROR: Pattern 2 compilation failed!
    pause
    exit /b 1
)

REM Compile injector if it doesn't exist
if not exist "ChatInjector.exe" (
    echo.
    echo [3/3] Compiling ChatInjector...

    if not exist "ChatInjector.cpp" (
        echo Copying ChatInjector.cpp from parent directory...
        copy ..\ChatInjector.cpp .
    )

    cl /MT /O2 ChatInjector.cpp /OUT:ChatInjector.exe

    if errorlevel 1 (
        echo ERROR: ChatInjector compilation failed!
        pause
        exit /b 1
    )
) else (
    echo.
    echo [3/3] ChatInjector.exe already exists, skipping...
)

echo.
echo ===============================================
echo   Compilation Successful!
echo ===============================================
echo.
echo Created files:
dir /b ChatHookDLL*.dll 2>nul
dir /b ChatInjector.exe 2>nul
echo.
echo Next steps:
echo 1. Start Game.exe
echo 2. Run: ChatInjector.exe ChatHookDLL_Pattern1.dll
echo 3. Send a chat message in game
echo 4. Check: type C:\DragonOath_ChatLog_Pattern1.txt
echo.
echo If Pattern 1 doesn't work:
echo    Run: ChatInjector.exe ChatHookDLL_Pattern2.dll
echo    Check: type C:\DragonOath_ChatLog_Pattern2.txt
echo.
pause
