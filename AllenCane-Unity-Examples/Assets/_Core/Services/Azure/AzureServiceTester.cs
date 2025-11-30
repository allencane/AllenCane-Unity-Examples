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
    private Rect _windowRect;

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

            // --- UI TOGGLE ---
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
        if (!_showAuthUI) return;

        GUI.depth = -2000;

        // Spawn window in the middle-left of the screen,
        // roughly matching the width of the debug log panel.
        float logPanelWidthApprox = Screen.width * 0.55f;    // matches DebugConsoleManager ~55% log width
        float margin = 20f;
        float width = logPanelWidthApprox - margin * 2f;
        float height = Screen.height * 0.4f;
        float x = margin;                                    // small margin from the left edge
        float y = (Screen.height - height) / 2f;             // Vertically centered

        if (_windowRect.width < 1)
            _windowRect = new Rect(x, y, width, height);

        int fontSize = Mathf.Max(14, (int)(Screen.height * 0.025f));

        GUI.skin.window.fontSize = fontSize;
        GUI.skin.textField.fontSize = fontSize;
        GUI.skin.button.fontSize = fontSize;
        GUI.skin.label.fontSize = fontSize;

        _windowRect = GUI.Window(1001, _windowRect, DrawAuthWindow, "Azure Credentials");
    }

    private void DrawAuthWindow(int windowID)
    {
        float width = _windowRect.width;
        float height = _windowRect.height;
        int fontSize = Mathf.Max(18, (int)(height * 0.08f));

        GUI.skin.label.fontSize = fontSize;
        GUI.skin.textField.fontSize = fontSize;
        GUI.skin.button.fontSize = fontSize;

        float padding = 20f;
        float rowHeight = height * 0.2f;
        float startY = rowHeight * 0.8f;

        // Lay out label + field so label has enough width not to wrap.
        float labelWidth = width * 0.35f;
        float fieldX = padding + labelWidth + padding;
        float fieldWidth = width - fieldX - padding;

        GUI.Label(new Rect(padding, startY, labelWidth, rowHeight), "User:");
        testUsername = GUI.TextField(new Rect(fieldX, startY, fieldWidth, rowHeight), testUsername);

        startY += rowHeight + (padding / 2);

        GUI.Label(new Rect(padding, startY, labelWidth, rowHeight), "PW:");
        testPassword = GUI.TextField(new Rect(fieldX, startY, fieldWidth, rowHeight), testPassword);

        float buttonHeight = rowHeight * 1.2f;
        float buttonY = height - buttonHeight - padding;

        if (GUI.Button(new Rect(padding, buttonY, width - (padding * 2), buttonHeight), "Close & Save"))
        {
            _showAuthUI = false;
        }

        GUI.DragWindow();
    }
}

