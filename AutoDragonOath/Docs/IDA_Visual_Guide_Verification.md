# IDA Free 9.2 - Visual Guide to Verify HandleRecvTalkPacket

## Where to See Function Verification Patterns

This guide shows you **exactly where to look** in IDA Free to verify you found the correct function.

---

## Overview: What You're Looking At

```
┌─────────────────────────────────────────────────────────────────┐
│ IDA Free 9.2 Window Layout                                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌───────────────┬──────────────────────────────────────────┐  │
│  │ Functions (F3)│  IDA View-A (Disassembly Window)         │  │
│  │               │                                           │  │
│  │ List of all   │  ← THIS IS WHERE YOU VERIFY THE FUNCTION │  │
│  │ functions     │                                           │  │
│  │               │  Shows assembly code with addresses      │  │
│  │               │                                           │  │
│  └───────────────┴──────────────────────────────────────────┘  │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ Hex View (Alt+T)                                         │  │
│  │                                                           │  │
│  │ Shows raw bytes - use this to copy pattern              │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Step-by-Step: Where to Look

### 1. Position Your Cursor

**In the main disassembly window (IDA View-A):**

1. Navigate to a function (double-click from Functions list, or press `G` to go to address)
2. Click at the **START of the function** (the first line)

```
Address   Assembly Code                  ← Click here
────────────────────────────────────────────────────────
.text:00789A20  push    ebp              ← Function start
.text:00789A21  mov     ebp, esp
.text:00789A23  sub     esp, 4Ch
.text:00789A26  push    ebx
.text:00789A27  push    esi
.text:00789A28  push    edi
```

**You're looking at the right place when:**
- You see the address in the left column (e.g., `.text:00789A20`)
- You see assembly instructions in the middle (e.g., `push ebp`)

---

### 2. Pattern 1: NULL Pointer Check

**What to look for:**
From source code line 1729-1732:
```cpp
if( NULL == pPacket)
{
    TDAssert(FALSE);
    return -1;
}
```

**Where to see it in IDA:**

Look for this pattern in the disassembly (scroll down a few lines from function start):

```
.text:00789A2E  cmp     [ebp+pPacket], 0      ← Checking if pPacket == NULL
.text:00789A32  jnz     short loc_789A45      ← If NOT null, jump ahead
.text:00789A34  push    offset aGcchathandler  ← Prepare error message
.text:00789A39  push    1Ch                    ← Line number
.text:00789A3B  push    offset aGminterface   ← File name
.text:00789A40  call    TDAssert               ← Call assert
.text:00789A45  mov     eax, 0FFFFFFFFh       ← return -1
.text:00789A4A  ret
```

**HOW TO READ THIS:**

1. **Look at the instruction column** (middle): Find `cmp [ebp+something], 0`
2. **Next line should be:** `jnz` (jump if not zero)
3. **A few lines later:** `call` to something like `TDAssert` or `Assert`
4. **Then:** `mov eax, 0FFFFFFFFh` (which is -1 in hex)
5. **Finally:** `ret` (return from function)

**Visual location:**
```
┌────────────────────────────────────────────────┐
│ IDA View-A Window                              │
├────────────────────────────────────────────────┤
│                                                 │
│  .text:00789A20  push    ebp        ← Start   │
│  .text:00789A21  mov     ebp, esp            │
│  .text:00789A23  sub     esp, 4Ch            │
│  ... (more lines)                              │
│  .text:00789A2E  cmp [ebp+arg_0], 0  ← HERE!  │  ← Look here!
│  .text:00789A32  jnz short loc_xxx           │
│  .text:00789A34  push offset aString         │
│                                                 │
└────────────────────────────────────────────────┘
```

---

### 3. Pattern 2: IsBlackName Check

**What to look for:**
From source code line 1736-1738:
```cpp
if( CDataPool::GetMe()->GetRelation()->IsBlackName( pPacket->GetSourName() ) )
{
    return 0;
}
```

**Where to see it in IDA:**

Scroll down more in the same function, look for this pattern:

```
.text:00789A50  call    CDataPool__GetMe           ← Get DataPool singleton
.text:00789A55  mov     ecx, eax                   ← Store result in ECX
.text:00789A57  call    GetRelation                ← Call GetRelation()
.text:00789A5C  mov     ecx, eax                   ← Store Relation object
.text:00789A5E  push    [ebp+pPacket]              ← Push packet
.text:00789A61  call    GetSourName                ← Get sender name
.text:00789A66  push    eax                        ← Push name as parameter
.text:00789A67  call    IsBlackName                ← Call IsBlackName()
.text:00789A6C  test    al, al                     ← Test if result is true
.text:00789A6E  jz      short loc_789A78           ← If false (not blacklisted), continue
.text:00789A70  xor     eax, eax                   ← return 0
.text:00789A72  ret
```

**HOW TO READ THIS:**

1. **Look for multiple `call` instructions** in sequence
2. **Specifically:** `call` something with "DataPool", "Relation", or "Black"
3. **After the calls:** Look for `test al, al` (testing boolean result)
4. **Then:** `jz` (jump if zero) or `jnz` (jump if not zero)
5. **If condition true:** `xor eax, eax` (set to 0) then `ret`

**Visual location:**
```
┌────────────────────────────────────────────────┐
│ IDA View-A Window (scrolled down)              │
├────────────────────────────────────────────────┤
│                                                 │
│  ... (previous code)                            │
│  .text:00789A50  call sub_XXXXX      ← HERE!   │  ← Chain of calls
│  .text:00789A55  mov  ecx, eax                │
│  .text:00789A57  call sub_YYYYY      ← HERE!   │  ← Look for this pattern
│  .text:00789A5C  mov  ecx, eax                │
│  .text:00789A61  call sub_ZZZZZ      ← HERE!   │
│  .text:00789A67  call IsBlackName    ← HERE!   │
│  .text:00789A6C  test al, al         ← HERE!   │  ← Boolean test
│  .text:00789A6E  jz   short loc_xxx           │
│  .text:00789A70  xor  eax, eax       ← HERE!   │  ← return 0
│  .text:00789A72  ret                          │
│                                                 │
└────────────────────────────────────────────────┘
```

---

### 4. Pattern 3: HistoryMsg Creation

**What to look for:**
From source code line 1764-1765:
```cpp
HistoryMsg msg;
if( 0 == msg.SetByPacket(pPacket))
```

**Where to see it in IDA:**

Continue scrolling down, look for:

```
.text:00789A80  lea     ecx, [ebp+historyMsg]     ← Load address of local var
.text:00789A83  call    HistoryMsg__ctor          ← Constructor call
.text:00789A88  push    [ebp+pPacket]             ← Push packet
.text:00789A8B  lea     ecx, [ebp+historyMsg]     ← Load msg object
.text:00789A8E  call    HistoryMsg__SetByPacket   ← Call SetByPacket
.text:00789A93  test    eax, eax                  ← Test if result == 0
.text:00789A95  jnz     short loc_789B00          ← If not 0, skip
```

**HOW TO READ THIS:**

1. **Look for:** `lea ecx, [ebp+something]` (loading local variable address)
2. **Followed by:** `call` to constructor
3. **Then:** Another `lea ecx, [ebp+same_thing]`
4. **Then:** `call` with a name like "SetByPacket" or similar
5. **After:** `test eax, eax` (testing return value)

**Visual location:**
```
┌────────────────────────────────────────────────┐
│ IDA View-A Window (scrolled down more)         │
├────────────────────────────────────────────────┤
│                                                 │
│  ... (previous code)                            │
│  .text:00789A80  lea  ecx, [ebp-14h] ← HERE!   │  ← Local variable
│  .text:00789A83  call sub_XXXXX      ← HERE!   │  ← Constructor
│  .text:00789A88  push [ebp+arg_0]             │
│  .text:00789A8B  lea  ecx, [ebp-14h]          │
│  .text:00789A8E  call sub_YYYYY      ← HERE!   │  ← SetByPacket
│  .text:00789A93  test eax, eax       ← HERE!   │  ← Check result
│  .text:00789A95  jnz  short loc_xxx           │
│                                                 │
└────────────────────────────────────────────────┘
```

---

### 5. Pattern 4: Return Values

**What to look for:**
From source code:
- Returns `0` on success
- Returns `-1` on error
- Returns `PACKET_EXE_CONTINUE` (likely 0 or 1)

**Where to see it in IDA:**

Look at the **END of the function** (scroll to bottom):

```
.text:00789B20  xor     eax, eax                  ← Set EAX to 0
.text:00789B22  pop     edi                       ← Restore registers
.text:00789B23  pop     esi
.text:00789B24  pop     ebx
.text:00789B25  mov     esp, ebp                  ← Restore stack
.text:00789B27  pop     ebp
.text:00789B28  ret                               ← Return 0
```

Or for error:
```
.text:00789A45  mov     eax, 0FFFFFFFFh           ← -1 in hex
.text:00789A4A  pop     edi
.text:00789A4B  pop     esi
.text:00789A4C  pop     ebx
.text:00789A4D  mov     esp, ebp
.text:00789A4F  pop     ebp
.text:00789A50  ret                               ← Return -1
```

**HOW TO READ THIS:**

1. **Look for:** `xor eax, eax` (sets to 0) OR `mov eax, 0FFFFFFFFh` (sets to -1)
2. **Followed by:** Series of `pop` instructions (cleaning up)
3. **Finally:** `ret` (return from function)

---

## Using IDA's Graph View

**Alternative way to see structure:**

1. **Switch to Graph View:**
   - Press `Spacebar` while in the function
   - Or: `View` menu → `Open subviews` → `Graph overview`

2. **What you'll see:**

```
┌─────────────────────────────────────────────────────────┐
│                     Graph View                          │
├─────────────────────────────────────────────────────────┤
│                                                          │
│            ┌─────────────┐                              │
│            │ Function    │                              │
│            │ Start       │                              │
│            └──────┬──────┘                              │
│                   │                                      │
│            ┌──────▼──────┐                              │
│            │ NULL check  │  ← Pattern 1                │
│            └──────┬──────┘                              │
│                   │                                      │
│        ┌──────────┴──────────┐                          │
│        │                     │                          │
│   ┌────▼────┐         ┌─────▼─────┐                   │
│   │ Error   │         │ Continue  │                    │
│   │ return  │         │ execution │                    │
│   └─────────┘         └─────┬─────┘                    │
│                             │                           │
│                      ┌──────▼──────┐                   │
│                      │ IsBlackName │  ← Pattern 2      │
│                      │ check       │                   │
│                      └──────┬──────┘                   │
│                             │                           │
│                      ┌──────▼──────┐                   │
│                      │ HistoryMsg  │  ← Pattern 3      │
│                      │ creation    │                   │
│                      └──────┬──────┘                   │
│                             │                           │
│                      ┌──────▼──────┐                   │
│                      │ Return 0    │  ← Pattern 4      │
│                      └─────────────┘                   │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

**Graph View is easier to understand the flow!**

---

## Hex View - Where to See Bytes

**To see the raw bytes for pattern creation:**

1. **Open Hex View:**
   - Press `Alt + T` while in disassembly
   - Or: `View` menu → `Open subviews` → `Hex dump`

2. **What you'll see:**

```
┌─────────────────────────────────────────────────────────┐
│ Hex View Window                                         │
├─────────────────────────────────────────────────────────┤
│                                                          │
│ Address   Hex Bytes                      ASCII          │
│ ──────────────────────────────────────────────────────  │
│ 00789A20  55 8B EC 83 EC 4C 53 56 57 8B  U..Ì.LVW.    │ ← Copy these!
│ 00789A2A  F9 89 7D F4 83 7D 08 00 75 0C  ù.}ô.}..u.    │
│ 00789A34  68 1C 00 00 00 68 00 00 00 00  h....h....    │
│ 00789A3E  E8 XX XX XX XX 83 C4 0C B8 FF  è....Ä.¸ÿ    │
│                                                          │
│ ▲ Position cursor here to see function start bytes     │
└─────────────────────────────────────────────────────────┘
```

3. **Sync with disassembly:**
   - Click in disassembly → Hex view follows
   - Click in Hex view → Disassembly follows
   - Both show the same location

4. **Copy bytes:**
   - Select bytes in Hex view
   - `Ctrl + C` to copy
   - You get: `55 8B EC 83 EC 4C 53 56 57`

---

## Practical Example: Step-by-Step

Let me show you a real example walkthrough:

### Start Here:

```
You opened Game.exe in IDA
Auto-analysis completed
Now you're looking at the main IDA window
```

### Step 1: Find a suspicious function

```
1. Press Shift+F3 to open Functions window
2. Scroll through the list
3. Look for names with "Chat", "Talk", "Recv", or sub_XXXXXXXX

Functions window:
┌──────────────────────┐
│ Functions List       │
├──────────────────────┤
│ sub_00401000        │
│ sub_00401234        │
│ ...                  │
│ sub_00789A20        │ ← Double-click this
│ ...                  │
└──────────────────────┘
```

### Step 2: You jump to the function

```
IDA View-A now shows:

.text:00789A20  push    ebp              ← You're here now
.text:00789A21  mov     ebp, esp
.text:00789A23  sub     esp, 4Ch
.text:00789A26  push    ebx
.text:00789A27  push    esi
.text:00789A28  push    edi
.text:00789A29  mov     edi, ecx         ← "this" pointer (thiscall)
.text:00789A2B  mov     [ebp-0Ch], edi
```

### Step 3: Scroll down with arrow keys

```
Press ↓ key to scroll down:

.text:00789A2E  cmp     [ebp+8], 0       ← PATTERN 1 FOUND! NULL check
.text:00789A32  jnz     short loc_789A45
.text:00789A34  push    offset aGcchatha
.text:00789A39  push    1Ch
.text:00789A3B  push    offset aGminterf
.text:00789A40  call    TDAssert         ← Assert function
.text:00789A45  mov     eax, 0FFFFFFFFh  ← return -1
```

✅ Pattern 1 verified!

### Step 4: Continue scrolling

```
.text:00789A50  call    sub_4D2E10       ← Chain of calls
.text:00789A55  mov     ecx, eax
.text:00789A57  call    sub_4D3A20
.text:00789A5C  mov     ecx, eax
.text:00789A61  call    sub_5A6B30
.text:00789A66  push    eax
.text:00789A67  call    sub_4D4C80       ← Likely IsBlackName
.text:00789A6C  test    al, al           ← PATTERN 2 FOUND!
.text:00789A6E  jz      short loc_789A78
.text:00789A70  xor     eax, eax         ← return 0
```

✅ Pattern 2 verified!

### Step 5: Keep scrolling

```
.text:00789A80  lea     ecx, [ebp-14h]   ← Local variable
.text:00789A83  call    sub_6C8910       ← Constructor
.text:00789A88  push    [ebp+8]          ← Push packet
.text:00789A8B  lea     ecx, [ebp-14h]
.text:00789A8E  call    sub_6C8A20       ← PATTERN 3 FOUND! SetByPacket
.text:00789A93  test    eax, eax
```

✅ Pattern 3 verified!

### Step 6: Press `Alt+T` for hex bytes

```
Hex View:

Address   Bytes
00789A20  55 8B EC 83 EC 4C 53 56 57 8B F9 89 7D F4
          ↑  ↑  ↑  ↑  ↑  ↑  ↑  ↑  ↑
          └──┴──┴──┴──┴──┴──┴──┴──┴─ Copy these for pattern!
```

✅ Pattern obtained!

### Result:

```
You found HandleRecvTalkPacket at address 0x00789A20
Pattern: 55 8B EC 83 EC 4C 53 56 57 8B F9
With wildcard: 55 8B EC 83 EC ?? 53 56 57 8B F9
Mask: xxxxx?xxxxx
```

---

## Quick Verification Checklist

When you think you found the function, check:

- [ ] Function starts with `push ebp; mov ebp, esp` (standard prologue)
- [ ] Has NULL pointer check (`cmp [ebp+X], 0`)
- [ ] Has error return (`mov eax, 0FFFFFFFFh`)
- [ ] Has multiple function calls in sequence
- [ ] Has boolean test (`test al, al` or `test eax, eax`)
- [ ] Creates local object (`lea ecx, [ebp-XX]`)
- [ ] Returns 0 at the end (`xor eax, eax`)

**If all checked: You found it! ✅**

---

## Troubleshooting

### "I don't see any of these patterns!"

**Solution:** You're looking at the wrong function
- Go back to Functions list
- Try a different function
- Search for strings first ("Talk", "Chat")
- Follow cross-references (press X)

### "The assembly looks different!"

**Possible causes:**
1. **Compiler optimization:** Code might be reorganized
2. **Different game version:** Pattern changed
3. **Inlining:** Function was inlined into another

**What to do:**
- Look for the **general pattern**, not exact match
- Focus on key operations: NULL check, function calls, returns
- Use Graph View to see the overall structure

### "Function names are all sub_XXXXXXXX"

**This is normal!** IDA doesn't know the real names.
- Use patterns to identify functions
- Rename them yourself (press `N`)
- Your names will persist in the IDA database

---

## Summary

**Where to look:**

1. **Main disassembly (IDA View-A):** See assembly code
2. **Hex view (Alt+T):** See raw bytes for patterns
3. **Graph view (Spacebar):** See function structure
4. **Functions list (Shift+F3):** Navigate between functions

**What to verify:**

1. NULL pointer check → `cmp [ebp+X], 0`
2. IsBlackName check → Chain of calls + `test al, al`
3. HistoryMsg creation → `lea ecx` + constructor
4. Return values → `xor eax, eax` or `mov eax, -1`

**When you see all 4 patterns: You found HandleRecvTalkPacket! ✅**

---

Now you know exactly where to look and what to look for! 🎯
