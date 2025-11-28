# How To Use Debug Console

<div align="center">

**A Production-Ready In-Game Debug Console for Unity**

_Multi-platform • Touch-Friendly • Hierarchical Commands • Real-time Logging_

</div>

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Quick Start](#-quick-start)
- [Input Controls](#-input-controls)
- [Features](#-features)
- [API Reference](#-api-reference)
- [Troubleshooting](#-troubleshooting)

---

## 🎯 Overview

The **Debug Console** is a comprehensive in-game debugging system designed for Unity projects. It provides developers with a powerful command interface accessible through multiple input methods, perfect for both desktop and mobile development.

---

## 🚀 Quick Start

### Installation

**Step 1:** Create a bootstrap script or add to an existing one:

```csharp
using UnityEngine;
using Core.Utils;

public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        // Initialize debug console
        DebugConsoleManager.Initialize(this.gameObject);

        // Optional: Add example commands to see it in action
        gameObject.AddComponent<Core.Examples.CoreDebugCommands>();
    }
}
```

**Step 2:** Run the game and press **`[` + `]`** or **`` ` ``** to open!

### Adding Commands

```csharp
void Start()
{
    var commands = DebugConsoleManager.Instance.Commands;

    // Simple button
    commands.AddSimpleCommand("Heal Player", () =>
    {
        Debug.Log("Player Healed!");
    });

    // Toggle switch
    commands.AddToggle("God Mode", false, (enabled) =>
    {
        isInvincible = enabled;
    });
}
```

---

## 🎮 Input Controls

### Desktop

| Input         | Action                        |
| ------------- | ----------------------------- |
| **`[` + `]`** | Toggle Console (Bracket keys) |
| **`` ` ``**   | Toggle Console (Tilde key)    |
| **`Esc`**     | Close Console                 |

### Mobile / Simulator

| Input            | Action                                       |
| ---------------- | -------------------------------------------- |
| **Four Fingers** | Touch screen with 4 fingers to toggle        |
| **Corner Hold**  | Hold bottom-left or top-left corner for 1.3s |

---

## ✨ Features

### Command Types

| Type       | Description    | Code Example                                         |
| ---------- | -------------- | ---------------------------------------------------- |
| **Simple** | Basic button   | `commands.AddSimpleCommand("Name", () => Action());` |
| **Toggle** | On/Off switch  | `commands.AddToggle("Name", false, (val) => {});`    |
| **Value**  | Cycle options  | `commands.AddValueCycle("Name", 5, (val) => {});`    |
| **Info**   | Read-only text | `commands.AddInfo("FPS", () => fps.ToString());`     |

### Folders (Hierarchy)

Organize commands into folders to keep the menu clean.

```csharp
commands.StartFolder("Player");
{
    commands.AddSimpleCommand("Kill", () => ...);
    commands.AddSimpleCommand("Revive", () => ...);
}
commands.EndFolder();
```

### Logging

View logs in real-time within the console.

```csharp
// Log to specific channel
DebugConsoleManager.Log("Network", "Connected to server");

// Unity logs (Debug.Log) are automatically captured in the "Unity" tab.
```

---

## 📚 API Reference

### DebugConsoleManager

```csharp
// Static Access
DebugConsoleManager.Instance
DebugConsoleManager.Toggle()
DebugConsoleManager.Log(channel, message)
DebugConsoleManager.AddCommand(name, callback)
```

### DebugCommandCatalog

```csharp
// Folder Management
StartFolder(string name)
EndFolder()

// Command Registration
AddSimpleCommand(string name, Action callback)
AddToggle(string name, bool startValue, Action<bool> callback)
AddValueCycle(string name, int max, Action<int> callback)
AddInfo(string name, Func<string> provider)
```

---

## 🐛 Troubleshooting

### Console Won't Open?

1. Check if `DebugConsoleManager.Initialize()` is called.
2. Ensure **Input System** package is installed.
3. Try both **Bracket keys** (`[`+`]`) and **Tilde** (`~`).

### Commands Not Showing?

1. Ensure commands are registered **after** initialization.
2. Check that every `StartFolder()` has a matching `EndFolder()`.

### Taps Not Working in Simulator?

The console uses robust input handling for Simulator. Ensure you are simulating **Touch** input if testing mobile features, or use the Mouse in Game View.

---

<div align="center">

**Happy Debugging! 🎮🐛**

</div>
