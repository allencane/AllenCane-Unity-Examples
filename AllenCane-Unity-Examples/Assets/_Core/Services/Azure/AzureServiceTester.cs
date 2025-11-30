using UnityEngine;
using System;
using System.Collections.Generic;
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
    [SerializeField] private int level = 1;
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

    private enum TestValueType { Int, Bool, String }
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
        level = _playerData.Get<int>("PlayerLevel", 1);
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

            commands.AddSimpleCommand(">> Edit Dictionary Entry <<", () =>
            {
                _showDataUI = !_showDataUI;
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

            commands.AddSimpleCommand("3. Logout", () =>
            {
                _sessionToken = null;
                _activePlayerId = "guest-" + System.Guid.NewGuid().ToString().Substring(0, 6);
                DebugConsoleManager.Log("Azure", "Logged out. Switched to Guest ID.");
            });

            // --- DATA COMMANDS (Match_GO style) ---
            commands.AddSimpleCommand("Save (PlayerData Changes)", async () =>
            {
                // Push inspector values into PlayerData
                _playerData.Set("Coins", coins);
                _playerData.Set("PlayerLevel", level);
                _playerData.Set("ExperiencePoints", xp);

                var changes = _playerData.GetChanges();
                DebugConsoleManager.Log("Azure", $"Preparing to save {changes.Count} changed keys...");

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
                    _playerData.ApplyCloudLoad(data);

                    // Pull data back into inspector fields
                    coins = _playerData.Get("Coins", 0);
                    level = _playerData.Get("PlayerLevel", 1);
                    xp = _playerData.Get("ExperiencePoints", 0);

                    DebugConsoleManager.Log("Azure", "<color=green>LOADED (DICT)</color>");
                    foreach (var kvp in data)
                    {
                        DebugConsoleManager.Log("Data", $"{kvp.Key}: {kvp.Value}");
                    }
                }
                else
                {
                    DebugConsoleManager.Log("Azure", "<color=red>FAILED to load dictionary.</color>");
                }
            });

            commands.AddSimpleCommand("Reset Stats (PlayerData)", () =>
            {
                coins = 0;
                level = 1;
                xp = 0;

                _playerData.Set("Coins", 0);
                _playerData.Set("PlayerLevel", 1);
                _playerData.Set("ExperiencePoints", 0);

                DebugConsoleManager.Log("Azure", "Reset local PlayerData stats to 0/1/0. Use Save to persist.");
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
            float dataX = margin;
            float dataY = (Screen.height - height) / 2f + height * 0.55f;
            if (_dataWindowRect.width < 1)
                _dataWindowRect = new Rect(dataX, dataY, width, height * 0.75f);

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
        float labelWidth = fontSize * 5f; // Increased width allowance
        float fieldHeight = fontSize * 2.0f; // Increased height for touch friendliness

        GUILayout.BeginVertical();
        GUILayout.Space(15);

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
        float labelWidth = fontSize * 5f; // Increased width allowance
        float fieldHeight = fontSize * 2.0f; // Increased height for touch friendliness

        GUILayout.BeginVertical();
        GUILayout.Space(15);

        // Key Row
        GUILayout.BeginHorizontal();
        GUILayout.Label("Key:", GUILayout.Width(labelWidth));
        _testKey = GUILayout.TextField(_testKey, GUILayout.Height(fieldHeight));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Type Row
        GUILayout.BeginHorizontal();
        GUILayout.Label("Type:", GUILayout.Width(labelWidth));
        var types = new[] { "Int", "Bool", "String" };
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

        // Buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Local", GUILayout.Height(fieldHeight * 1.2f)))
        {
            TryApplyTestValue(setOnly: true);
        }
        if (GUILayout.Button("Set & Dirty", GUILayout.Height(fieldHeight * 1.2f)))
        {
            TryApplyTestValue(setOnly: false);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.EndVertical();

        GUI.DragWindow();
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
}

