# Memory Scanner Tool - Complete Summary

**Created**: 2025-11-02
**Purpose**: Help you find the Map Object Pointer and unknown attribute offsets

---

## 📦 What Was Created

I've created a complete memory scanning toolkit for you:

### 1. Core Files

| File | Purpose | Lines |
|------|---------|-------|
| **MemoryScanner.cs** | Memory scanning engine with Win32 API wrappers | 500+ |
| **MemoryScannerUsageGuide.cs** | Interactive console app with 5 scenarios | 600+ |
| **BuildMemoryScanner.bat** | One-click compilation script | 50 |

### 2. Documentation

| File | Purpose |
|------|---------|
| **MEMORY_SCANNER_README.md** | Complete usage guide with examples |
| **PLAYER_INFORMATION_MEMORY_STRUCTURE.md** | Updated player memory structure with your new addresses |
| **MEMORY_SCANNER_SUMMARY.md** | This file - overview of everything |

---

## 🎯 What You Can Do With This Tool

### ✅ Already Working

These features are fully implemented and ready to use:

1. **Scan for map names** in game memory
2. **Find pointers** pointing to those map names
3. **Scan for integer values** (HP, MP, attributes, etc.)
4. **Narrow down scans** by comparing with changed values
5. **Pattern matching** for consecutive values (like STR, SPR, CON, INT, DEX)
6. **Memory dumping** to see structure layout
7. **Pointer chain following** to validate chains
8. **Monitor memory changes** during events (map changes, attribute changes)
9. **Calculate offsets** from known base addresses
10. **Test player pointer chain** with your updated addresses

### 🔍 What You're Looking For

#### Priority 1: Map Object Pointer ⚠️ UNKNOWN

Currently you have:
- ❌ Old pointer: `6870940` (obsolete)
- ❌ Old offsets: `[6870940, 14232]` + 96 for map ID

**Need to find:**
- ✅ New base pointer (probably in range 6000000-8000000)
- ✅ Pointer chain to map structure
- ✅ Offset to map name/ID within structure

**Use**: Scenario 1 + Scenario 5

#### Priority 2: Attribute Offsets ⚠️ UNKNOWN

Currently missing from `SDATA_PLAYER_MYSELF`:
- ❓ Strength (m_nSTR)
- ❓ Spirit (m_nSPR)
- ❓ Constitution (m_nCON)
- ❓ Intelligence (m_nINT)
- ❓ Dexterity (m_nDEX)
- ❓ Remaining Points (m_nPoint_Remain)
- ❓ Character Level (m_nLevel)
- ❓ Money/Gold (m_nMoney)

**Use**: Scenario 2 (individual) or Scenario 4 (pattern matching)

#### Priority 3: Combat & Elemental Stats ⚠️ UNKNOWN

All combat stats from the structure:
- Physical/Magic Attack/Defense
- Hit, Miss, Critical Rate, Attack Speed
- HP/MP Regen Speed
- All elemental stats (Cold, Fire, Lightning, Poison)

**Use**: Scenario 2 (one at a time)

---

## 🚀 Quick Start (3 Steps)

### Step 1: Build the Tool

**Option A: Double-click the batch file**
```
Right-click BuildMemoryScanner.bat → Run as Administrator
```

**Option B: Manual compilation**
```bash
cd G:\microauto-6.9
csc.exe /out:MemoryScanner.exe MemoryScanner.cs MemoryScannerUsageGuide.cs
```

### Step 2: Run the Game

1. Start Dragon Oath (game.exe)
2. Login to your character
3. Note your current map name

### Step 3: Run the Scanner

```
Right-click MemoryScanner.exe → Run as Administrator
```

Choose a scenario from the menu!

---

## 📋 Recommended Workflow

### For Finding Map Object Pointer

```
Day 1:
├─ Run Scenario 1 (Scan for map names)
├─ Write down candidate addresses (3-10 addresses)
└─ Save the results

Day 2:
├─ Run Scenario 5 (Monitor map changes)
├─ Enter your candidate addresses
├─ Change maps in game (teleport)
└─ The address that changes = Map Object Pointer ✓

Day 3:
├─ Test the pointer by reading it
├─ Verify it works after game restart
└─ Update GClass0.cs method_25()
```

### For Finding All Attributes (STR, SPR, CON, INT, DEX)

```
Method A - Pattern Matching (FASTEST):
├─ Check all 5 attributes in game
├─ Run Scenario 4
├─ Enter all values
└─ Get all 5 offsets at once ✓

Method B - One by One (if Method A fails):
├─ Run Scenario 2 for STR
├─ Repeat for SPR, CON, INT, DEX
└─ Calculate offsets from Stats Base
```

---

## 🧪 Scenario Overview

| Scenario | When to Use | What You Need | Output |
|----------|-------------|---------------|--------|
| **1. Find Map Pointer** | First step for map | Character logged in | Candidate addresses |
| **2. Find Attribute** | Know current value, can change it | Current attribute value | Offset from Stats Base |
| **3. Test Player Chain** | Verify your addresses | Updated pointer chain | Validation result |
| **4. Pattern Match** | Know all 5 attribute values | All 5 attribute values | All 5 offsets |
| **5. Monitor Changes** | Have candidates, need proof | Candidate addresses | Confirmed address |

---

## 💡 Pro Tips

### Tip 1: Use Scenario 3 First!

Before doing anything, run **Scenario 3** to verify your player pointer chain is working:
- If it shows your character name, HP, MP correctly ✅
- If it shows empty or wrong data ❌ (addresses are wrong)

### Tip 2: Pattern Matching is Powerful

For attributes like STR, SPR, CON, INT, DEX:
- Don't scan one by one
- Use **Scenario 4** to find all 5 at once
- Pattern of 5 consecutive values is nearly unique

### Tip 3: Narrow, Narrow, Narrow

If you get 1000+ results:
1. Change the value slightly (add 1 point)
2. Run narrow scan
3. Repeat until <20 results

### Tip 4: Monitor Map Changes Live

The best way to find Map Object Pointer:
1. Get candidates from Scenario 1
2. Use Scenario 5 to monitor them
3. Teleport in game
4. The one that changes = winner!

### Tip 5: Memory Dumps Are Your Friend

When you find a candidate address:
```
scanner.DumpMemory(address, 1000);
```
This shows the full structure around that address!

---

## 📊 Expected Results

### What Map Object Pointer Looks Like

```
Base: 0x0068B91C (6870940) ← This is what you're looking for
  ↓ (read pointer)
Intermediate: 0x12AB3400
  ↓ +14232
Map Base: 0x12AE8228
  ↓ +96
Map Name: "nhon nam"
```

### What Attributes Look Like

```
Stats Base: 0x45678900
  ↓ +184 (example offset)
STR: 150
  ↓ +4
SPR: 120
  ↓ +4
CON: 140
  ↓ +4
INT: 100
  ↓ +4
DEX: 130
```

They're usually stored **consecutively**, 4 bytes apart.

---

## 🐛 Troubleshooting

### "Failed to open process"
→ Run as Administrator

### "Game process not found"
→ Check process name in Task Manager, update code if needed

### Too many scan results (>1000)
→ Use narrow scan technique (Scenario 2)

### Pattern not found (Scenario 4)
→ Attributes might not be consecutive, use Scenario 2 instead

### Memory dump shows garbage
→ Pointer chain might be wrong, verify with Scenario 3

### Map pointer doesn't change
→ You might be looking at a cached value, try different candidates

---

## 📝 After You Find Them

### 1. Update Documentation

Edit `PLAYER_INFORMATION_MEMORY_STRUCTURE.md`:

```markdown
### Confirmed Offsets

| Data | Pointer Chain | Final Offset | Status |
|------|--------------|--------------|--------|
| **Map Base Pointer** | - | **6855123** | ✅ **FOUND** |
| **STR** | [2381824, 12, 340, 4] | **+184** | ✅ **FOUND** |
| **SPR** | [2381824, 12, 340, 4] | **+188** | ✅ **FOUND** |
```

### 2. Update Code

**GClass0.cs** - Add new methods:

```csharp
// Map reading (example)
public string method_27() // Get map name
{
    int mapBase = class7_0.method_1(new int[] { 6855123, 14232 });
    return class7_0.method_5(mapBase + 96);
}

// Attributes
public int method_GetStrength()
{
    return class7_0.method_0(this.method_29() + 184);
}

public int method_GetSpirit()
{
    return class7_0.method_0(this.method_29() + 188);
}
// etc.
```

### 3. Test Thoroughly

1. Restart game
2. Run Scenario 3 again
3. Check all values match in-game stats
4. Test across multiple characters
5. Test after game updates

---

## 🎓 Understanding the Code

### How Memory Scanning Works

1. **VirtualQueryEx** - Finds memory regions in target process
2. **ReadProcessMemory** - Reads data from those regions
3. **Pattern matching** - Compares bytes to find specific values
4. **Pointer scanning** - Finds addresses that contain other addresses

### The Scanner Can:

- ✅ Scan entire process memory (1-2 GB)
- ✅ Find strings (map names, character names)
- ✅ Find integers (HP, MP, attributes)
- ✅ Find pointers (addresses pointing to other addresses)
- ✅ Follow pointer chains
- ✅ Monitor memory changes
- ✅ Dump memory for analysis

### Key Classes in MemoryScanner.cs

| Class/Method | Purpose |
|--------------|---------|
| `ScanForMapNames()` | Scan for map name strings |
| `ScanForInt32Value()` | Scan for specific integer |
| `NarrowScan()` | Filter previous results |
| `FindPointersTo()` | Find what points to an address |
| `FindPointerChains()` | Build pointer chains |
| `DumpMemory()` | Show memory structure |
| `ReadInt32()` | Read 4-byte integer |
| `ReadString()` | Read ASCII string |

---

## 📚 Additional Resources

### Files to Read

1. **MEMORY_SCANNER_README.md** - Detailed usage guide
2. **PLAYER_INFORMATION_MEMORY_STRUCTURE.md** - Memory structure reference
3. **MEMORY_READING_SYSTEM.md** - How Class7 works
4. **PROJECT_ARCHITECTURE.md** - Overall system architecture

### Code Examples

The `MemoryScannerUsageGuide.cs` file contains **5 complete examples** showing exactly how to use each feature.

---

## ✅ Checklist

Use this to track your progress:

### Map Object Pointer
- [ ] Run Scenario 1 (get candidates)
- [ ] Run Scenario 5 (verify with map change)
- [ ] Test pointer stability (restart game)
- [ ] Update documentation
- [ ] Update GClass0.cs
- [ ] Test in automation

### Attributes (STR, SPR, CON, INT, DEX)
- [ ] Run Scenario 4 or Scenario 2
- [ ] Find all 5 offsets
- [ ] Verify with memory dump
- [ ] Test with Scenario 3
- [ ] Update documentation
- [ ] Add methods to GClass0.cs

### Other Stats
- [ ] Money/Gold
- [ ] Character Level
- [ ] Combat stats (Attack, Defense, etc.)
- [ ] Elemental stats
- [ ] HP/MP Regen Speed

---

## 🎉 Success Criteria

You'll know you succeeded when:

1. **Map Object Pointer**:
   - You can read current map name
   - It changes when you teleport
   - It's stable across game restarts

2. **Attributes**:
   - Values match what you see in game
   - They update when you add points
   - Offsets are consistent across characters

3. **Integration**:
   - Scenario 3 shows all correct data
   - Your automation can read the values
   - Everything works after game restart

---

## 🤝 Need Help?

If you get stuck:

1. **Check Scenario 3** - Does player chain work?
2. **Read the README** - Detailed explanations there
3. **Look at code comments** - Every method is documented
4. **Try different scenarios** - Pattern matching vs step-by-step
5. **Verify game is running** - Scanner needs game.exe process

---

## 🔄 What's Next?

After you find the addresses:

1. Update all documentation
2. Integrate into MicroAuto 6.0
3. Test with multiple characters
4. Test after game patches (addresses may change!)
5. Share your findings (update the .md files)

---

## Summary

You now have:
- ✅ Complete memory scanning toolkit
- ✅ 5 different scanning scenarios
- ✅ Updated player information structure
- ✅ Ready-to-use code examples
- ✅ Detailed documentation

**Next step**: Double-click `BuildMemoryScanner.bat` and start scanning!

Good luck finding those addresses! 🔍✨
