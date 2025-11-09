# Fix: 'nmake' is not recognized

## Problem

When running:
```batch
cd C:\Detours\src
nmake
```

You get:
```
'nmake' is not recognized as an internal or external command,
operable program or batch file.
```

---

## ✅ Quick Solution (Pick One)

### Solution 1: Use Developer Command Prompt (RECOMMENDED)

1. **Close your current terminal/PowerShell**

2. **Open Developer Command Prompt:**
   - Press `Windows Key`
   - Type: `developer command`
   - Click: **"Developer Command Prompt for VS 2022"**
   
   OR
   
   - Start Menu → **Visual Studio 2022** → **Developer Command Prompt for VS 2022**

3. **Verify nmake works:**
   ```batch
   nmake /?
   ```
   
   Should show:
   ```
   Microsoft (R) Program Maintenance Utility Version 14.XX.XXXXX
   ...
   ```

4. **Build Detours:**
   ```batch
   cd C:\Detours\src
   nmake
   ```

5. **Done!** ✅

---

### Solution 2: Download Pre-built Detours (FASTEST - No building needed!)

**This is the easiest if you just want to get started quickly:**

1. **Visit:** https://github.com/microsoft/Detours/releases

2. **Download the latest release:**
   - Look for: `Detours-4.0.1.zip` or similar
   - Click to download

3. **Extract the ZIP:**
   ```batch
   # Extract to: C:\Detours\
   # You should have:
   # C:\Detours\include\
   # C:\Detours\lib.X86\
   # C:\Detours\lib.X64\
   ```

4. **Skip nmake completely!** The files are already built.

5. **Continue with compiling your DLL:**
   ```batch
   cd C:\ChatHook
   
   # Open Developer Command Prompt (still needed for cl.exe)
   cl /LD /MT /O2 ChatHookDLL.cpp ^
      /I"C:\Detours\include" ^
      /link /LIBPATH:"C:\Detours\lib.X86" detours.lib ^
      /OUT:ChatHookDLL.dll
   ```

---

### Solution 3: Install Visual Studio Build Tools

**If you don't have Visual Studio or Developer Command Prompt:**

1. **Download Build Tools:**
   - Visit: https://visualstudio.microsoft.com/downloads/
   - Scroll to: **"Tools for Visual Studio"**
   - Click: **"Build Tools for Visual Studio 2022"**

2. **Install with C++ workload:**
   - Run the installer
   - Select: **"Desktop development with C++"**
   - Click Install (takes ~5-10 minutes)

3. **After installation:**
   - Search for: **"Developer Command Prompt for VS 2022"**
   - Open it
   - Now `nmake` will work!

4. **Build Detours:**
   ```batch
   cd C:\Detours\src
   nmake
   ```

---

## Verification Checklist

### Check if you have Visual Studio installed:

```batch
# Try to find cl.exe (C++ compiler):
where cl

# If found, you have Visual Studio or Build Tools installed
# If not found, install Build Tools (Solution 3 above)
```

### Check if you have nmake:

```batch
where nmake

# If found: You're in Developer Command Prompt ✅
# If not found: Open Developer Command Prompt
```

### Check if Detours is built:

```batch
# Check if these files exist:
dir C:\Detours\lib.X86\detours.lib
dir C:\Detours\include\detours.h

# If both exist: Detours is ready! ✅
# If not: Either build with nmake OR download pre-built
```

---

## Current Situation Fix (Step-by-Step)

Based on your message, you:
- ✅ Successfully cloned Detours
- ❌ Can't run nmake

**Do this right now:**

1. **Close your current PowerShell**

2. **Press Windows Key + R**

3. **Type:** `cmd`

4. **Press Enter**

5. **In the CMD window, type:**
   ```batch
   "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"
   ```
   
   *If that doesn't work, try:*
   ```batch
   "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat"
   ```

6. **If the VsDevCmd.bat runs successfully, you'll see:**
   ```
   **********************************************************************
   ** Visual Studio 2022 Developer Command Prompt v17.X.X
   **********************************************************************
   ```

7. **Now run:**
   ```batch
   cd C:\Detours\src
   nmake
   ```

8. **Done!** ✅

---

## Alternative: Just Use cl.exe Without Detours Build

You actually **don't need to build Detours from source** if you download the release:

```batch
# 1. Download pre-built: https://github.com/microsoft/Detours/releases
# 2. Extract to C:\Detours\
# 3. Open Developer Command Prompt
# 4. Go straight to compiling your DLL

cd C:\ChatHook
cl /LD /MT /O2 ChatHookDLL.cpp ^
   /I"C:\Detours\include" ^
   /link /LIBPATH:"C:\Detours\lib.X86" detours.lib ^
   /OUT:ChatHookDLL.dll
```

**This is faster and easier!**

---

## Summary

**Problem:** `nmake` not in PATH

**Root cause:** Not using Developer Command Prompt

**Best solution:** 
1. Download pre-built Detours (no nmake needed!)
2. OR use Developer Command Prompt for VS 2022

**Time to fix:** 2 minutes (download pre-built) or 5 minutes (use Dev CMD)

---

Need more help? The pre-built Detours approach is recommended - it's what most developers use anyway!
