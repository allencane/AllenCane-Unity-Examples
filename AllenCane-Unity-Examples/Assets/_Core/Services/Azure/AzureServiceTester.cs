using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Core.Data;
using Core.Services.Azure;
using Core.Services;
using Core.Utils; // For DebugConsoleManager

[DefaultExecutionOrder(1000)] // Ensure this runs last to draw on top
public class AzureServiceTester : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string serviceUrl = "https://allencane-coregame-gameapi-func.azurewebsites.net";
    [SerializeField] private string apiKey = "3TbsOrkSEWQGPfT3xZopob_3yt8PCkW37Ca71MJb8PsWAzFuY-sjHQ==";

    [Header("Test Data (Inspector View)")]
    [SerializeField] private string testUsername = "Playtester";
    [SerializeField] private string testPassword = "Password123!";
    [SerializeField] private int coins = 0;
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private int xp = 0;

    // Core data + services
    private PlayerData _playerData;
    private IPlayerAccountService _authService;
    private IPlayerDataSyncService _dataSyncService;

    // Runtime State
    private string _activePlayerId;
    private string _sessionToken;

    // UI State
    private bool _showAuthUI = false;
    private Rect _authWindowRect;

    // Dictionary test UI
    private bool _showDataUI = false;
    private Rect _dataWindowRect;

    private enum TestValueType { Int, Bool, String, Float }
    private TestValueType _testValueType = TestValueType.Int;
    private string _testKey = "TestKey";
    private string _testValue = "123";

    private void Awake()
    {
        _activePlayerId = PlayerPrefs.GetString("AzureTestPlayerId", "guest-" + System.Guid.NewGuid().ToString().Substring(0, 6));
        _authService = new AzurePlayerAccountService(serviceUrl, apiKey);
        _dataSyncService = new AzurePlayerDataSyncService(serviceUrl, apiKey);

        _playerData = new PlayerData();

        // Seed inspector view from PlayerData defaults
        coins = _playerData.Get<int>("Coins", 0);
        playerLevel = _playerData.Get<int>("PlayerLevel", 1);
        xp = _playerData.Get<int>("ExperiencePoints", 0);
    }

    private void Start()
    {
        RegisterDebugCommands();
    }

    private void RegisterDebugCommands()
    {
        if (DebugConsoleManager.Instance == null) return;
        var commands = DebugConsoleManager.Instance.Commands;

        commands.StartFolder("Azure Test");
        {
            commands.AddInfo("Status", () => string.IsNullOrEmpty(_sessionToken) ? "Guest (No Token)" : "LOGGED IN");
            commands.AddInfo("Player ID", () => _activePlayerId);
            commands.AddInfo("Username", () => testUsername);

            // --- UI TOGGLES ---
            commands.AddSimpleCommand(">> Edit User/Pass <<", () =>
            {
                _showAuthUI = !_showAuthUI;
            });

            // --- AUTH COMMANDS ---
            commands.AddSimpleCommand("1. Register User", async () =>
            {
                DebugConsoleManager.Log("Azure", $"Registering '{testUsername}'...");
                var (success, message, newId, token) = await _authService.RegisterUser(testUsername, testPassword);

                if (success)
                {
                    DebugConsoleManager.Log("Azure", $"<color=green>REGISTERED:</color> {message}");
                    _activePlayerId = newId;
                }
                else
                    DebugConsoleManager.Log("Azure", $"<color=red>ERROR:</color> {message}");
            });

            commands.AddSimpleCommand("2. Login User", async () =>
            {
                DebugConsoleManager.Log("Azure", $"Logging in '{testUsername}'...");
                var (success, message, id, token) = await _authService.LoginUser(testUsername, testPassword);

                if (success)
                {
                    _activePlayerId = id;
                    _sessionToken = token;
                    DebugConsoleManager.Log("Azure", $"<color=green>LOGIN SUCCESS!</color>");
                    DebugConsoleManager.Log("Azure", $"Token: {token.Substring(0, 8)}...");
                    _showAuthUI = false; // Auto-close window on success
                }
                else
                    DebugConsoleManager.Log("Azure", $"<color=red>LOGIN FAILED:</color> {message}");
            });

            // --- DATA FLOW COMMANDS (ordered as requested) ---
            commands.AddSimpleCommand("3. Load (PlayerData)", async () =>
            {
                DebugConsoleManager.Log("Azure", $"Loading dictionary for {_activePlayerId}...");
                var (success, data) = await _dataSyncService.LoadAsync(_activePlayerId, _sessionToken);

                if (success && data != null)
                {
                    var gameKeyCount = CountGameKeys(data.Keys);
                    DebugConsoleManager.Log("Azure", $"Loaded {data.Count} keys from cloud ({gameKeyCount} game keys).");
                    LogAzureKeys("Load", data.Keys);
                    LogAzureDict("Load", data);
                    _playerData.ApplyCloudLoad(data);

                    // Pull data back into inspector fields
                    coins = _playerData.Get("Coins", 0);
                    playerLevel = _playerData.Get("PlayerLevel", 1);
                    xp = _playerData.Get("ExperiencePoints", 0);

                    DebugConsoleManager.Log("Azure", "<color=green>LOADED (DICT)</color>");
                    DebugConsoleManager.Log("Data", "--- Full Dictionary Contents ---");
                    DebugConsoleManager.Log("Data", _playerData.ToDebugString(includeMetadata: false));
                    DebugConsoleManager.Log("Data", "--------------------------------");
                }
                else
                {
                    DebugConsoleManager.Log("Azure", "<color=red>FAILED to load dictionary.</color>");
                }
            });

            // Dictionary tester just under Load for convenience
            commands.AddSimpleCommand(">> Edit Dictionary Entry <<", () =>
            {
                _showDataUI = !_showDataUI;
            });

            commands.AddSimpleCommand("Save (PlayerData Changes)", async () =>
            {
                var changes = _playerData.GetChanges();
                DebugConsoleManager.Log("Azure", $"Preparing to save {changes.Count} changed keys...");
                LogAzureKeys("Save", changes.Keys);
                LogAzureDict("Save", changes);

                var (success, message) = await _dataSyncService.SaveAsync(_activePlayerId, changes, _sessionToken);

                if (success)
                {
                    _playerData.CommitChanges();
                    DebugConsoleManager.Log("Azure", $"<color=green>SAVED (DICT):</color> {message}");
                }
                else
                {
                    DebugConsoleManager.Log("Azure", $"<color=red>FAILED (DICT):</color> {message}");
                }
            });

            commands.AddSimpleCommand("Logout", () =>
            {
                _sessionToken = null;
                _activePlayerId = "guest-" + System.Guid.NewGuid().ToString().Substring(0, 6);
                DebugConsoleManager.Log("Azure", "Logged out. Switched to Guest ID.");
            });

            // Convenience command: full wipe + recreate default stats for the active player
            commands.AddSimpleCommand("Delete All Data (Cloud)", () =>
            {
                DeleteAllData();
            });

            commands.AddSimpleCommand("3. Logout", () =>
            {
                _sessionToken = null;
                _activePlayerId = "guest-" + System.Guid.NewGuid().ToString().Substring(0, 6);
                DebugConsoleManager.Log("Azure", "Logged out. Switched to Guest ID.");
            });

            // --- DATA COMMANDS (Incremental PlayerData sync) ---
            commands.AddSimpleCommand("Save (PlayerData Changes)", async () =>
            {
                var changes = _playerData.GetChanges();
                DebugConsoleManager.Log("Azure", $"Preparing to save {changes.Count} changed keys...");
                LogAzureKeys("Save", changes.Keys);
                LogAzureDict("Save", changes);

                var (success, message) = await _dataSyncService.SaveAsync(_activePlayerId, changes, _sessionToken);

                if (success)
                {
                    _playerData.CommitChanges();
                    DebugConsoleManager.Log("Azure", $"<color=green>SAVED (DICT):</color> {message}");
                }
                else
                {
                    DebugConsoleManager.Log("Azure", $"<color=red>FAILED (DICT):</color> {message}");
                }
            });

            commands.AddSimpleCommand("Load (PlayerData)", async () =>
            {
                DebugConsoleManager.Log("Azure", $"Loading dictionary for {_activePlayerId}...");
                var (success, data) = await _dataSyncService.LoadAsync(_activePlayerId, _sessionToken);

                if (success && data != null)
                {
                    var gameKeyCount = CountGameKeys(data.Keys);
                    DebugConsoleManager.Log("Azure", $"Loaded {data.Count} keys from cloud ({gameKeyCount} game keys).");
                    LogAzureKeys("Load", data.Keys);
                    LogAzureDict("Load", data);
                    _playerData.ApplyCloudLoad(data);

                    // Pull data back into inspector fields
                    coins = _playerData.Get("Coins", 0);
                    playerLevel = _playerData.Get("PlayerLevel", 1);
                    xp = _playerData.Get("ExperiencePoints", 0);

                    DebugConsoleManager.Log("Azure", "<color=green>LOADED (DICT)</color>");
                    DebugConsoleManager.Log("Data", "--- Full Dictionary Contents ---");
                    DebugConsoleManager.Log("Data", _playerData.ToDebugString(includeMetadata: false));
                    DebugConsoleManager.Log("Data", "--------------------------------");
                }
                else
                {
                    DebugConsoleManager.Log("Azure", "<color=red>FAILED to load dictionary.</color>");
                }
            });

            // Convenience command: full wipe + recreate default stats for the active player
            commands.AddSimpleCommand("Delete All Data (Cloud)", () =>
            {
                DeleteAllData();
            });
        }
        commands.EndFolder();
    }

    private void OnGUI()
    {
        if (!_showAuthUI && !_showDataUI) return;

        GUI.depth = -2000;

        float logPanelWidthApprox = Screen.width * 0.55f;
        float margin = 20f;
        float width = logPanelWidthApprox - margin * 2f;
        float height = Screen.height * 0.4f;

        int fontSize = Mathf.Max(14, (int)(Screen.height * 0.025f));
        GUI.skin.window.fontSize = fontSize;
        GUI.skin.textField.fontSize = fontSize;
        GUI.skin.button.fontSize = fontSize;
        GUI.skin.label.fontSize = fontSize;

        if (_showAuthUI)
        {
            float authX = margin;
            float authY = (Screen.height - height) / 2f;
            if (_authWindowRect.width < 1)
                _authWindowRect = new Rect(authX, authY, width, height);

            _authWindowRect = GUI.Window(1001, _authWindowRect, DrawAuthWindow, "Azure Credentials");
        }

        if (_showDataUI)
        {
            // Place data window slightly lower to avoid overlapping header text
            // UPDATE: Increased height to 1.3x to fit stacked buttons and prevent cutoff
            float dataHeight = height * 1.3f;
            float dataX = margin;
            // Center vertically to ensure it stays on screen
            float dataY = (Screen.height - dataHeight) / 2f;

            if (_dataWindowRect.width < 1)
                _dataWindowRect = new Rect(dataX, dataY, width, dataHeight);

            _dataWindowRect = GUI.Window(1002, _dataWindowRect, DrawDataWindow, "Dictionary Tester");
        }
    }

    private void DrawAuthWindow(int windowID)
    {
        // 1. Calculate and enforce font size inside the window callback
        int fontSize = Mathf.Max(14, (int)(Screen.height * 0.025f));

        GUI.skin.label.fontSize = fontSize;
        GUI.skin.textField.fontSize = fontSize;
        GUI.skin.button.fontSize = fontSize;

        // 2. Calculate layout dimensions based on this font size
        float labelWidth = fontSize * 3.5f; // Reduced width to save space
        float fieldHeight = fontSize * 2.0f;
        float titlePadding = fontSize * 1.5f;

        GUILayout.BeginVertical();
        GUILayout.Space(titlePadding);

        // User Row
        GUILayout.BeginHorizontal();
        GUILayout.Label("User:", GUILayout.Width(labelWidth));
        testUsername = GUILayout.TextField(testUsername, GUILayout.Height(fieldHeight));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Password Row
        GUILayout.BeginHorizontal();
        GUILayout.Label("PW:", GUILayout.Width(labelWidth));
        testPassword = GUILayout.TextField(testPassword, GUILayout.Height(fieldHeight));
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Close & Save", GUILayout.Height(fieldHeight * 1.2f)))
        {
            _showAuthUI = false;
        }

        GUILayout.Space(10);
        GUILayout.EndVertical();

        GUI.DragWindow();
    }

    private void DrawDataWindow(int windowID)
    {
        // 1. Calculate and enforce font size inside the window callback
        int fontSize = Mathf.Max(14, (int)(Screen.height * 0.025f));

        GUI.skin.label.fontSize = fontSize;
        GUI.skin.textField.fontSize = fontSize;
        GUI.skin.button.fontSize = fontSize;

        // 2. Calculate layout dimensions based on this font size
        float labelWidth = fontSize * 3.5f; // Reduced width to save space
        float fieldHeight = fontSize * 2.0f;
        float titlePadding = fontSize * 1.5f;

        GUILayout.BeginVertical();
        GUILayout.Space(titlePadding);

        // Key Row
        GUILayout.BeginHorizontal();
        GUILayout.Label("Key:", GUILayout.Width(labelWidth));
        _testKey = GUILayout.TextField(_testKey, GUILayout.Height(fieldHeight));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Type Row
        GUILayout.BeginHorizontal();
        GUILayout.Label("Type:", GUILayout.Width(labelWidth));
        var types = new[] { "Int", "Bool", "String", "Float" };
        // Note: Toolbar uses 'button' style by default in some Unity versions, but let's be safe
        _testValueType = (TestValueType)GUILayout.Toolbar((int)_testValueType, types, GUILayout.Height(fieldHeight));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Value Row
        GUILayout.BeginHorizontal();
        GUILayout.Label("Value:", GUILayout.Width(labelWidth));
        _testValue = GUILayout.TextField(_testValue, GUILayout.Height(fieldHeight));
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        // Buttons - Stacked Vertically
        if (GUILayout.Button("Set Local", GUILayout.Height(fieldHeight * 1.2f)))
        {
            TryApplyTestValue(setOnly: true);
        }

        GUILayout.Space(5); // Small gap between buttons

        if (GUILayout.Button("Set & Dirty", GUILayout.Height(fieldHeight * 1.2f)))
        {
            TryApplyTestValue(setOnly: false);
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Delete Key (Cloud)", GUILayout.Height(fieldHeight * 1.2f)))
        {
            // Trigger deletion of the currently entered key
            DeleteCurrentKey();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Delete All Data (Cloud)", GUILayout.Height(fieldHeight * 1.2f)))
        {
            DeleteAllData();
        }

        GUILayout.Space(10);
        GUILayout.EndVertical();

        GUI.DragWindow();
    }

    private async void DeleteAllData()
    {
        DebugConsoleManager.Log("Azure", "Syncing before wipe...");

        // 1. Load first to ensure we know about ALL cloud keys
        var (loadSuccess, data) = await _dataSyncService.LoadAsync(_activePlayerId, _sessionToken);
        if (loadSuccess && data != null)
        {
            _playerData.ApplyCloudLoad(data);
        }
        else
        {
            DebugConsoleManager.Log("Azure", "<color=yellow>Load failed/empty. Attempting delete of local-only keys...</color>");
        }

        DebugConsoleManager.Log("Azure", "Attempting to delete ALL keys from Cloud...");

        var allKeys = _playerData.GetAllKeys();
        if (allKeys.Count == 0)
        {
            DebugConsoleManager.Log("Azure", "Dictionary is empty. Nothing to delete.");
            return;
        }

        var (success, message) = await _dataSyncService.DeleteKeysAsync(_activePlayerId, allKeys, _sessionToken);

        if (success)
        {
            DebugConsoleManager.Log("Azure", $"<color=green>WIPED:</color> {message}");

            // Reset local to defaults
            _playerData = new PlayerData();
            coins = 0;
            playerLevel = 1;
            xp = 0;
            DebugConsoleManager.Log("Azure", "Local data reset to defaults.");

            // Immediately push default stats back to the cloud so a Load sees 0/1/0 instead of missing row
            var defaults = new Dictionary<string, object>
            {
                // Canonical dictionary-based keys only (no legacy fixed-field mirrors)
                { "Coins", coins },
                { "PlayerLevel", playerLevel },
                { "ExperiencePoints", xp }
            };

            DebugConsoleManager.Log("Azure", "Recreating default stats on cloud after wipe...");
            var (saveSuccess, saveMessage) = await _dataSyncService.SaveAsync(_activePlayerId, defaults, _sessionToken);

            if (saveSuccess)
            {
                _playerData.CommitChanges();
                DebugConsoleManager.Log("Azure", "<color=green>DEFAULT STATS SAVED:</color> " + saveMessage);
            }
            else
            {
                DebugConsoleManager.Log("Azure", "<color=red>FAILED to save default stats after wipe:</color> " + saveMessage);
            }
        }
        else
        {
            DebugConsoleManager.Log("Azure", $"<color=red>WIPE FAILED:</color> {message}");
        }
    }

    private async void DeleteCurrentKey()
    {
        if (string.IsNullOrEmpty(_testKey))
        {
            DebugConsoleManager.Log("Azure", "<color=red>Error:</color> Key cannot be empty.");
            return;
        }

        DebugConsoleManager.Log("Azure", $"Attempting to delete key '{_testKey}' from Cloud...");

        var keysToDelete = new List<string> { _testKey };
        var (success, message) = await _dataSyncService.DeleteKeysAsync(_activePlayerId, keysToDelete, _sessionToken);

        if (success)
        {
            DebugConsoleManager.Log("Azure", $"<color=green>DELETED:</color> {message}");
            // Optionally clear it locally too? 
            // For now, we just delete from cloud. To sync, user should Load.
        }
        else
        {
            DebugConsoleManager.Log("Azure", $"<color=red>DELETE FAILED:</color> {message}");
        }
    }

    private void TryApplyTestValue(bool setOnly)
    {
        try
        {
            object parsed = _testValue;
            switch (_testValueType)
            {
                case TestValueType.Int:
                    parsed = int.Parse(_testValue);
                    break;
                case TestValueType.Bool:
                    parsed = bool.Parse(_testValue);
                    break;
                case TestValueType.String:
                    parsed = _testValue;
                    break;
                case TestValueType.Float:
                    parsed = float.Parse(_testValue);
                    break;
            }

            _playerData.Set(_testKey, parsed);
            DebugConsoleManager.Log("Azure", $"Set key '{_testKey}' = {parsed} ({_testValueType})");

            if (!setOnly)
            {
                // Mimic a change that will be picked up by GetChanges on next save
                // (PlayerData already tracks differences internally)
                DebugConsoleManager.Log("Azure", "Marked for next Save (PlayerData Changes).");
            }
        }
        catch (Exception e)
        {
            DebugConsoleManager.Log("Azure", $"<color=red>Parse error:</color> {e.Message}");
        }
    }

    private bool IsMetadataKey(string key)
    {
        if (key == null) return false;
        var k = key.ToLowerInvariant();
        if (k.StartsWith("odata.")) return true;
        if (k.StartsWith("timestamp")) return true; // Timestamp & Timestamp@odata.type
        if (k == "partitionkey" || k == "rowkey") return true;
        return false;
    }

    private int CountGameKeys(IEnumerable<string> keys)
    {
        if (keys == null) return 0;
        return keys.Count(k => !IsMetadataKey(k));
    }

    private void LogAzureKeys(string context, IEnumerable<string> keys)
    {
        if (keys == null) return;
        var gameKeys = keys
            .Where(k => !IsMetadataKey(k))
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (gameKeys.Count == 0) return;

        var body = string.Join("\n", gameKeys);
        DebugConsoleManager.Log(
            "Azure",
            $"\n▶ {context} keys:\n----------------\n{body}\n----------------"
        );
    }

    private void LogAzureDict(string context, IDictionary<string, object> dict)
    {
        if (dict == null || dict.Count == 0) return;

        var lines = dict
            .Where(kv => !IsMetadataKey(kv.Key))
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"{kv.Key}:{kv.Value}")
            .ToList();

        if (lines.Count == 0)
            return;

        // Single log entry: timestamp once, then a visual header + separators + values
        var body = string.Join("\n", lines);
        DebugConsoleManager.Log(
            "Azure",
            $"\n▶ {context} values:\n----------------\n{body}\n----------------"
        );
    }
}

