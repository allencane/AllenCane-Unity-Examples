using UnityEngine;
using Core.Services.Azure;
using Core.Services;
using Core.Utils; // For DebugConsoleManager

public class AzureServiceTester : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string serviceUrl = "https://allencane-coregame-gameapi-func.azurewebsites.net";
    [SerializeField] private string apiKey = "3TbsOrkSEWQGPfT3xZopob_3yt8PCkW37Ca71MJb8PsWAzFuY-sjHQ==";

    [Header("Test Data")]
    [SerializeField] private string testUsername = "Allen";
    [SerializeField] private string testPassword = "Password123!";
    [SerializeField] private int coins = 500;
    [SerializeField] private int level = 10;
    [SerializeField] private int xp = 1500;

    // Runtime State
    private string _activePlayerId;
    private string _sessionToken;
    private IPlayerAccountService _accountService;

    private void Awake()
    {
        // 1. Default to Guest ID if not logged in
        _activePlayerId = PlayerPrefs.GetString("AzureTestPlayerId", "guest-" + System.Guid.NewGuid().ToString().Substring(0, 6));

        // DI MOCK
        _accountService = new AzurePlayerAccountService(serviceUrl, apiKey);
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

            // --- AUTH COMMANDS ---
            commands.AddSimpleCommand("1. Register User", async () =>
            {
                DebugConsoleManager.Log("Azure", $"Registering '{testUsername}'...");
                var (success, message, newId, token) = await _accountService.RegisterUser(testUsername, testPassword);

                if (success)
                {
                    DebugConsoleManager.Log("Azure", $"<color=green>REGISTERED:</color> {message}");
                    // Auto-login logic
                    _activePlayerId = newId;
                }
                else
                    DebugConsoleManager.Log("Azure", $"<color=red>ERROR:</color> {message}");
            });

            commands.AddSimpleCommand("2. Login User", async () =>
            {
                DebugConsoleManager.Log("Azure", $"Logging in '{testUsername}'...");
                var (success, message, id, token) = await _accountService.LoginUser(testUsername, testPassword);

                if (success)
                {
                    _activePlayerId = id;
                    _sessionToken = token;
                    DebugConsoleManager.Log("Azure", $"<color=green>LOGIN SUCCESS!</color>");
                    DebugConsoleManager.Log("Azure", $"Token: {token.Substring(0, 8)}...");
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

            // --- DATA COMMANDS ---
            commands.AddSimpleCommand("Save Config", async () =>
            {
                DebugConsoleManager.Log("Azure", $"Saving for {_activePlayerId}...");
                // Pass token (if any)
                var (success, message) = await _accountService.SavePlayerAccount(_activePlayerId, coins, level, xp, _sessionToken);

                if (success)
                    DebugConsoleManager.Log("Azure", $"<color=green>SAVED:</color> {message}");
                else
                    DebugConsoleManager.Log("Azure", $"<color=red>FAILED:</color> {message}");
            });

            commands.AddSimpleCommand("Load Config", async () =>
            {
                DebugConsoleManager.Log("Azure", $"Loading for {_activePlayerId}...");
                var (success, data) = await _accountService.GetPlayerAccount(_activePlayerId, _sessionToken);

                if (success)
                    DebugConsoleManager.Log("Azure", $"<color=green>LOADED:</color> {data}");
                else
                    DebugConsoleManager.Log("Azure", $"<color=red>FAILED:</color> {data}");
            });

            commands.AddSimpleCommand("Ping", async () =>
            {
                var (success, message) = await _accountService.SavePlayerAccount("ping", 0, 0, 0);
                DebugConsoleManager.Log("Azure", success ? "Pong!" : "Ping Failed");
            });

            commands.AddSimpleCommand("Reset & Save Stats", async () =>
            {
                coins = 0;
                level = 1;
                xp = 0;
                DebugConsoleManager.Log("Azure", $"Resetting stats for {_activePlayerId}...");

                var (success, message) = await _accountService.SavePlayerAccount(_activePlayerId, coins, level, xp, _sessionToken);

                if (success)
                    DebugConsoleManager.Log("Azure", $"<color=green>RESET SAVED:</color> Stats set to 0/1/0.");
                else
                    DebugConsoleManager.Log("Azure", $"<color=red>RESET FAILED:</color> {message}");
            });
        }
        commands.EndFolder();
    }
}
