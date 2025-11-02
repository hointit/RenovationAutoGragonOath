# Map Scanner Window - Implementation Guide

**Created**: 2025-11-02
**Project**: AutoDragonOath (Modern WPF App)

---

## ✅ What Was Implemented

I've successfully added a **Map Scanner Window** to the AutoDragonOath WPF application. This specialized tool helps you find the Map Object Pointer (`CWorldManager::s_pMe->m_pActiveScene->m_pTheDefine`).

---

## 📁 Files Created

### 1. **Views/MapScannerWindow.xaml** ✅
- WPF window with dark theme UI
- Four scenario buttons for different scanning operations
- Terminal-style output display
- Auto-scrolling text output

### 2. **Views/MapScannerWindow.xaml.cs** ✅
- Code-behind file
- Initializes window with `MapScannerViewModel`

### 3. **ViewModels/MapScannerViewModel.cs** ✅
- MVVM ViewModel with business logic
- Win32 API integration for memory scanning
- Four scanning scenarios implemented

### 4. **MainWindow.xaml.cs** (Modified) ✅
- Updated `ButtonScanMap_Click` to open MapScannerWindow
- Existing "Scan Map" button now opens the new window

---

## 🎯 Features Implemented

### Scenario 1: Scan for Map Names 🔍
```csharp
ScanForMapNames()
```
**What it does:**
- Scans game memory for map name strings
- Searches for: "heaven", "nhon nam", "loc duong", "don hoang", etc.
- Shows addresses where map names are found

**Output Example:**
```
Searching for: "nhon nam"
  ✓ Found at: 0x15ABC000 (312345600)
  ✓ Found at: 0x16DEF123 (345678901)
```

### Scenario 2: Find Map Pointers 📍
```csharp
FindMapPointers()
```
**What it does:**
- After finding map names, scans for pointers pointing to them
- Identifies static pointers (low memory addresses)
- These pointers are likely the `szSceneMap` field in `_DBC_SCENE_DEFINE`

**Output Example:**
```
⭐ Pointer at: 0x00689ABC (6855360) [LOW MEMORY - LIKELY STATIC]
```

### Scenario 3: Test Player Chain ✓
```csharp
TestPlayerChain()
```
**What it does:**
- Validates the existing player pointer chain
- Follows: `[2381824, 12, 340, 4]`
- Reads character name, HP, MP, coordinates
- Confirms if addresses are correct

**Output Example:**
```
✓ Entity Object Pointer: 0x12345678
✓ Entity Base Address: 0x23456789
✓ Stats Base Address: 0x45678901

📊 Character Data:
   Name: MyCharacter
   HP: 4523/5000
   MP: 2100/3500

✅ SUCCESS! Pointer chain is working correctly!
```

### Scenario 4: Monitor Map Changes 🔄
```csharp
MonitorMapChanges()
```
**What it does:**
- Provides instructions for manual map change monitoring
- User should:
  1. Run Scenario 2 to get candidate addresses
  2. Change maps in game
  3. Run Scenario 2 again
  4. Compare which address now points to new map

---

## 🚀 How to Use

### Step 1: Build the Project

```bash
cd G:\microauto-6.9\AutoDragonOath
dotnet build --configuration Debug
```

Or open in Visual Studio and build.

### Step 2: Run AutoDragonOath

1. Start the Dragon Oath game
2. Login to your character
3. Run `AutoDragonOath.exe`

### Step 3: Open Map Scanner

1. Click the **"Scan Map"** button (purple button)
2. Confirm the dialog
3. Map Scanner window will open

### Step 4: Find Map Object Pointer

**Recommended Workflow:**

```
Day 1:
├─ Click "Scan for Map Names"
├─ Wait for scan to complete
├─ Click "Find Map Pointers"
└─ Write down candidate addresses (low memory ones)

Day 2:
├─ Change maps in game (teleport)
├─ Click "Find Map Pointers" again
├─ The address that now points to new map = Map Object Pointer!
└─ Update your code with found address
```

---

## 🔍 Understanding the Output

### What to Look For

1. **Map Name Addresses** (Scenario 1)
   ```
   Found at: 0x15ABC000
   ```
   → This is where the string "nhon nam" is stored in memory

2. **Static Pointers** (Scenario 2)
   ```
   ⭐ 0x00689ABC [LOW MEMORY - LIKELY STATIC]
   ```
   → Pointers in range 0x00400000 - 0x00800000 are likely static
   → These are good candidates for the base address

3. **Pointer Chain Validation** (Scenario 3)
   ```
   ✅ SUCCESS! Pointer chain is working correctly!
   ```
   → Your current player addresses are correct

---

## 🛠️ Integration with Memory Structure

### What You're Looking For

Based on source code analysis, you need to find:

```cpp
CWorldManager::s_pMe          // Static address (UNKNOWN)
    ↓
CWorldManager instance
    ↓ m_pActiveScene (offset UNKNOWN)
CScene instance
    ↓ m_pTheDefine (offset likely +0)
_DBC_SCENE_DEFINE structure
    ↓ +4: nServerID (Map ID)
    ↓ +44: szSceneMap (Map Name string)
```

### Expected Results

When you find the correct pointer:
- It will point to a structure at offset +44 that contains map name
- At offset +4 from structure base = Map ID (integer)
- The pointer should be stable across game sessions
- The pointer should change when you change maps

---

## 📊 Example Workflow

### Finding Map Pointer - Complete Example

**Initial State: Character in "nhon nam"**

1. **Run Scenario 1**:
   ```
   Found "nhon nam" at: 0x15ABC000
   ```

2. **Run Scenario 2**:
   ```
   Pointer at: 0x00689ABC [LOW MEMORY - LIKELY STATIC]
   Pointer at: 0x14567DEF
   ```
   → **Candidate 1**: 0x00689ABC (static)
   → **Candidate 2**: 0x14567DEF (dynamic)

3. **Change to "heaven" map in game**

4. **Run Scenario 2 again**:
   ```
   Pointer at: 0x00689ABC [LOW MEMORY - LIKELY STATIC]
   Pointer at: 0x14789XYZ (different!)
   ```

5. **Analysis**:
   - 0x00689ABC stayed the same → Not the map pointer
   - 0x14567DEF changed to 0x14789XYZ → This is dynamic memory
   - Need to find what **points to** 0x14567DEF

6. **Calculate Structure Base**:
   ```
   If szSceneMap is at offset +44:
   Structure base = 0x14567DEF - 44 = 0x14567DC3

   Read Map ID at: 0x14567DC3 + 4 = 0x14567DC7
   ```

---

## 🐛 Troubleshooting

### Error: "Failed to open process"
**Solution**: Run AutoDragonOath as Administrator

### Error: "No game process found"
**Solution**:
- Make sure game.exe is running
- Character must be logged in

### No map names found
**Possible reasons**:
- Character is at login screen (not in game world)
- Map name strings use different encoding
- Scan didn't reach that memory region

### Too many results
**Solution**:
- Results are limited to 100 per search
- Focus on LOW MEMORY addresses (< 10,000,000)

---

## 💻 Code Structure

### ViewModel Pattern

```csharp
public class MapScannerViewModel : INotifyPropertyChanged
{
    // Properties
    public string OutputText { get; set; }
    public bool IsScanning { get; set; }
    public string StatusMessage { get; set; }

    // Commands
    public ICommand ScanMapNamesCommand { get; }
    public ICommand FindMapPointersCommand { get; }
    public ICommand TestPlayerChainCommand { get; }
    public ICommand MonitorMapChangesCommand { get; }

    // Methods
    private void ScanForMapNames() { ... }
    private void FindMapPointers() { ... }
    private void TestPlayerChain() { ... }

    // Win32 API
    private List<long> ScanForString(IntPtr handle, string text) { ... }
    private List<long> FindPointersTo(IntPtr handle, long address) { ... }
}
```

### Key Technologies Used

- **WPF** - Windows Presentation Foundation UI
- **MVVM** - Model-View-ViewModel pattern
- **Win32 API** - `ReadProcessMemory`, `VirtualQueryEx`
- **ICommand** - RelayCommand for button bindings
- **INotifyPropertyChanged** - Property change notifications

---

## 📚 Related Documentation

Read these files for more context:

1. **COMPLETE_MAP_SCENE_ANALYSIS.md** - Detailed map structure analysis
2. **MAP_FINDING_SUMMARY.txt** - Quick reference guide
3. **PLAYER_INFORMATION_MEMORY_STRUCTURE.md** - Player memory structure
4. **MEMORY_SCANNER_README.md** - Standalone scanner tool guide

---

## ✅ Checklist

Use this to track your progress:

- [ ] Build AutoDragonOath project successfully
- [ ] Run the application
- [ ] Open Map Scanner window
- [ ] Run Scenario 1 (Scan for Map Names)
- [ ] Run Scenario 2 (Find Map Pointers)
- [ ] Write down candidate addresses
- [ ] Change maps in game
- [ ] Run Scenario 2 again
- [ ] Identify which address changed
- [ ] Calculate structure base address
- [ ] Update GameProcessMonitor.cs with new address
- [ ] Run Scenario 3 to verify

---

## 🎉 Next Steps

After finding the Map Object Pointer:

1. **Document Your Findings**:
   ```
   Map Object Pointer: 0x00XXXXXX
   Map Base Chain: [base, offset1, offset2]
   Map ID Offset: +4
   Map Name Offset: +44
   ```

2. **Update Code**:
   - Edit `Services/GameProcessMonitor.cs`
   - Add new MAP_BASE_POINTER constant
   - Implement GetMapName() method

3. **Test Thoroughly**:
   - Restart game and verify
   - Test across multiple characters
   - Check after game updates

---

**Good luck finding the Map Object Pointer!** 🗺️✨

The MapScannerWindow is now fully integrated into AutoDragonOath.
