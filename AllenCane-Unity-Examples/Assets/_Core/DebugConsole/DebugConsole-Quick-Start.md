# Quick Start Guide

## 🚀 Get Started in 1 Minute

### Zero Setup Required!

Just add commands anywhere in your code. The system initializes automatically.

```csharp
using UnityEngine;
using Core.Utils;

public class MyPlayerScript : MonoBehaviour
{
    void Start()
    {
        // Add a simple command - No setup needed!
        DebugConsoleManager.AddCommand("Heal Player", () =>
        {
            health = 100;
            Debug.Log("Player Healed!");
        });

        // Add a toggle
        DebugConsoleManager.AddToggle("God Mode", false, (enabled) =>
        {
            isInvincible = enabled;
        });
    }
}
```

### How to Open

1. Run your game
2. Press **`[` + `]`** (both bracket keys) or **`` ` ``** (tilde)
3. That's it!

---

## 🎮 Input Controls

| Method          | Description                                                   |
| --------------- | ------------------------------------------------------------- |
| **`[` + `]`**   | Press both bracket keys simultaneously                        |
| **`` ` ``**     | Press tilde/backquote key (below Escape)                      |
| **4 Fingers**   | Touch screen with 4 fingers (mobile)                          |
| **Corner Hold** | Touch & hold bottom-left or top-left corner for 1.3s (mobile) |
| **`Esc`**       | Close the console                                             |

---

## 📚 Common Command Patterns

### Simple Button

```csharp
DebugConsoleManager.AddCommand("Restart Level", () =>
{
    SceneManager.LoadScene(0);
});
```

### Toggle (On/Off)

```csharp
DebugConsoleManager.AddToggle("Show FPS", false, (show) =>
{
    fpsCounter.SetActive(show);
});
```

### Custom Logging

```csharp
DebugConsoleManager.Log("Combat", "Player took 50 damage");
DebugConsoleManager.Log("Network", "Connected to server");
```

### Advanced: Folders & More

For complex setups (folders, sliders, etc.), access the instance directly:

```csharp
var commands = DebugConsoleManager.Instance.Commands;

commands.StartFolder("Player Settings");
{
    commands.AddSimpleCommand("Kill", KillPlayer);
    commands.AddInfo("Health", () => health.ToString());
}
commands.EndFolder();
```

---

## ⚠️ Requirements

1. **Unity Input System Package** must be installed
   - Window → Package Manager → Input System → Install

---

## 🎓 Next Steps

1. Check out `CoreDebugCommands.cs` for a full demo
2. Read `How-To-Use-DebugConsole.md` for deep customization details
3. **Press Play and start debugging! 🎉**
