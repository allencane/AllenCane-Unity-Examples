# Debug Menu, DI, Azure & Java

---

## Project Overview

This repository contains a set of Unity examples and tooling that showcase a production‑style debug menu, Azure‑backed player data, input/debug workflows suitable for mobile and desktop projects, and a little bit of dependency injection & service work.

[Watch the demo video on YouTube](https://youtu.be/YAfE67S2xgU)

---

## Project Setup

- **Unity Version:** 6000.2.6f2 with the Legacy IMGUI still available (tested in Editor + Simulator mode).
- **Input System:** Project uses the **new Input System** for toggling the debug console, but the Dictionary Tester GUI is IMGUI.
- **Opening the Example:**
  Open SampleScene and play in Unity Editor: open debug menu by pressing double bracket keys "][", or if on phone/tablet, a four-finger tap.
 
---

## Debug Console & In‑Game Debug Menu

The project ships with a full-screen **Debug Console** that can be opened at runtime and extended with custom commands.

When open, you’ll see:

- **Log View (left):** All Unity and custom logs, grouped by channel (Main, Azure, Data).
- **Command Panel (right):** Hierarchical folders of debug commands.
- **Optional Command Input Bar:** For typed commands, if enabled.

---

## Azure Test Folder (Runtime Cloud Debugging)

The **AzureServiceTester** component registers a folder in the debug console called **“Azure Test”**. This is your main entry point for testing login and cloud‑backed player data.

### Commands in `Azure Test`

- **Status, Player ID, Username**
  - Read‑only info panels showing your current auth/session state.
- **>> Edit User/Pass <<**
  - Opens a small overlay window to edit username and password.
- **>> Edit Dictionary Entry <<**
  - Opens the **Dictionary Tester** window (see below).
- **1. Register User**
  - Calls the RegisterUser Azure Function (auth endpoint).
- **2. Login User**
  - Calls the LoginUser Azure Function and stores the session token.
- **3. Logout**
  - Clears the token and swaps back to a guest ID.
- **Save (PlayerData Changes)**
  - Pushes only **changed keys** from the local PlayerData dictionary to Azure (SavePlayerAccount).
- **Load (PlayerData)**
  - Calls GetPlayerAccount, applies the cloud payload into PlayerData, and logs the full dictionary on the Data tab.
- **Reset Stats (PlayerData)**
  - Locally sets:
    - Coins = 0
    - PlayerLevel = 1
    - ExperiencePoints = 0
  - Press **Save** to persist those values.

---

## Dictionary Tester Window

The **Dictionary Tester** is an in‑game GUI for editing arbitrary keys in the PlayerData dictionary.

Open it via:

- Debug Console → **Azure Test** → >> Edit Dictionary Entry <<

### Fields & Controls

- **Key**
  - Text field for the dictionary key.  
  - **Key rules (Azure Table Storage safe):**
    - Must not be empty.
    - Cannot start with a number.
    - May only contain letters, digits, and `_` (underscore).
- **Type**
  - Toolbar with value type:
    - Int
    - Bool
    - String
    - Float
- **Value**
  - Text field parsed according to the selected type.

### Buttons

- **Set Local**
  - Parses the value and updates only the local PlayerData dictionary.
  - Good for quick tests without touching the cloud.
- **Set & Dirty**
  - Same as **Set Local**, plus marks the entry as “dirty” so it will be picked up by the next **Save (PlayerData Changes)**.
- **Delete Key (Cloud)**
  - Calls the DeletePlayerAccountKeys Azure Function with a **single key** list.
  - Used for surgically removing a specific key/column from the current player row.
- **Delete All Data (Cloud)**
  - Flow:
    1. Calls Load (PlayerData) internally to sync the latest cloud state.
    2. Sends **all known keys** for the active player to DeletePlayerAccountKeys.
    3. On success:
       - Deletes the entire entity for that playerId on the server.
       - Re‑creates default stats on the server:
         - Coins = 0, PlayerLevel = 1, ExperiencePoints = 0.
         - Also writes legacy testing columns coins`, level, xp for compatibility.

---

## PlayerData & Incremental Sync

_Core/Data/PlayerData.cs implements a lightweight, dictionary‑backed player state.

Key features:

- **Dictionary storage:** Dictionary<string, object> internally.
- **Type‑safe access:**
 sharp
  int coins   = _playerData.Get<int>("Coins", 0);
  float speed = _playerData.Get<float>("RunSpeed", 5f);
  - **Validation on Set:**
  - Azure rejects keys that:
    - Are empty or null.
    - Start with a digit.
    - Contain anything other than letters, digits, or underscores.
- **Incremental Sync:**
  - GetChanges() compares current data to the last committed snapshot and returns **only changed keys**.
  - It also skips invalid/Azure metadata keys so they don’t break saves.
- **Commit & Load:**
  - CommitChanges() snapshots the current state after a successful save.
  - ApplyCloudLoad(Dictionary<string, object> cloudData) merges a full payload from Azure and then calls CommitChanges().

---

## Azure Backend Overview

The Java backend lives on my local machine and exposes these HTTP-triggered functions:

- SavePlayerAccount – POST /api/v1/players/{playerId}/account
  - Upserts a dynamic dictionary into Azure Table Storage (`PlayerAccounts` table).
- GetPlayerAccount – GET /api/v1/players/{playerId}/account
  - Reads the player row and returns all properties as JSON.
- DeletePlayerAccountKeys – POST /api/v1/players/{playerId}/account/delete
  - Currently implemented to delete the **entire entity** for the given playerId (used by “Delete All Data”).
- RegisterUser / LoginUser – /api/v1/auth/register and /api/v1/auth/login
  - Handle simple auth & token issuance used by the AzureServiceTester.

Unity talks to these via:

- _Core/Services/Azure/AzurePlayerAccountService.cs (auth)
- _Core/Services/Azure/AzurePlayerDataSyncService.cs (dictionary save/load/delete)

---

## Notes & Known Behaviors

- **Table Storage Columns:**  
  Azure Table Storage is schema‑less, so columns like coins, level, xp, and any test keys you add become properties on the row. “Delete All Data” now deletes the row and recreates default stats to keep table columns consistent for the active player.

---
