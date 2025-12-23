# 🌱 Command Console for PVZ Fusion  
### _A powerful developer console & script running engine for Plants vs. Zombies: Fusion_  

---

## 🌟 Overview

**CommandConsole** is a fully‑featured in‑game developer console and script running framework designed for **PVZ Fusion**.  
It gives modders, testers, and power‑users the tools to:

- Run cheat commands  
- Spawn plants, zombies  
- Execute custom scripts
- Modapi to extend the console with your own commands  
- Build automation workflows for testing or mod development  

Whether you're debugging a new plant, stress‑testing zombie waves, or building a full toolkit: CommandConsole is your foundation.

---

## 🧩 Supported Mod Frameworks

CommandConsole is designed to integrate cleanly with other PVZ Fusion modding systems:

### 🔹 (Required) MelonLoader
Easy drag/drop install: drop into MelonLoader's "\Mods" directory

### 🔹 Fusion Mod Manager (FMM)  
Compatible with PVZ Fusion Mod Manager

---

## 🔧 ModApi Features

### ✔ Cheat Command System
Register custom commands with:
- Name  
- Usage  
- Description  
- Delegate callback  

Example:

```csharp
CheatAPI.Register(
    "search",
    "search <term> — search plants and zombies",
    "Searches both PlantType and ZombieType enums.",
    delegate (string[] args)
    {
        if (args.Length < 1)
        {
            CheatConsoleLog.Warn("Usage: search <term>");
            return;
        }

        string term = args[0].ToLower();

        foreach (PlantType p in System.Enum.GetValues(typeof(PlantType)))
        {
            string name = p.ToString();
            if (name.ToLower().Contains(term))
            {
                int id = (int)p;
                CheatConsoleLog.Msg($"[{id}] {name}");
            }
        }

        foreach (ZombieType z in System.Enum.GetValues(typeof(ZombieType)))
        {
            string name = z.ToString();
            if (name.ToLower().Contains(term))
            {
                int id = (int)z;
                CheatConsoleLog.Msg($"[{id}] {name}");
            }
        }
    }
);
```

---

## 📜 Scripting Engine

CommandConsole includes a lightweight scripting language supporting:

### ✔ Variables  
```
set row = 3
set type = 1
```

### ✔ Loops  
```
repeat 5
{
    spawnzombie 1 3
    wait 0.5
}
```

### ✔ Conditionals  
```
if $row > 2
{
    spawnzombie 5 $row
}
```

### ✔ Functions  
```
function summon()
{
    spawnzombie 2 3
    wait 1
}
```

### ✔ Built‑in commands  
- `set`
- `wait`
- `return`
- `break`
- `continue`

### ✔ Script execution  
Run scripts directly:
via command:

```
runscript myscript.txt
```
---

## 🚀 Getting Started

1. Drop the CommandConsole mod into your PVZ Fusion Mods folder.  
2. Launch the game.  
3. Press the console hotkey (default: **~**/**`**) to open the console.  
4. Type commands or run scripts.  
5. Extend the console by registering your own commands.

---

## ❤️ Credits

- **PVZ Fusion** Created by **LanPiaoPiao**
