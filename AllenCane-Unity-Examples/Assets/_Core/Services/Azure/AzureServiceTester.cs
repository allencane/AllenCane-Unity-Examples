using UnityEngine;
using Core.Services.Azure;
using Core.Services;
using Core.Utils; // For DebugConsoleManager

public class AzureServiceTester : MonoBehaviour
{
    [SerializeField] private string serviceUrl = "https://allencane-coregame-gameapi-func.azurewebsites.net";
    [SerializeField] private string apiKey = "3TbsOrkSEWQGPfT3xZopob_3yt8PCkW37Ca71MJb8PsWAzFuY-sjHQ==";
    [SerializeField] private string playerId = "unity-test-user";
    [SerializeField] private int coins = 500;
    [SerializeField] private int level = 10;
    [SerializeField] private int xp = 1500;

    private IPlayerAccountService _accountService;

    private void Awake()
    {
        // DI MOCK: In a real game, this would be injected by Zenject/VContainer
        _accountService = new AzurePlayerAccountService(serviceUrl, apiKey);
    }

    private void Start()
    {
        RegisterDebugCommands();
    }

    private void RegisterDebugCommands()
    {
        // Check if DebugConsole exists
        if (DebugConsoleManager.Instance == null) return;

        var commands = DebugConsoleManager.Instance.Commands;

        commands.StartFolder("Azure Test");
        {
            commands.AddSimpleCommand("Save Current Config", async () =>
            {
                DebugConsoleManager.Log("Azure", $"Saving account for {playerId}...");

                var (success, message) = await _accountService.SavePlayerAccount(playerId, coins, level, xp);

                if (success)
                    DebugConsoleManager.Log("Azure", $"<color=green>SUCCESS:</color> {message}");
                else
                    DebugConsoleManager.Log("Azure", $"<color=red>FAILED:</color> {message}");
            });

            commands.AddSimpleCommand("Load Current Config", async () =>
            {
                DebugConsoleManager.Log("Azure", $"Loading account for {playerId}...");

                var (success, data) = await _accountService.GetPlayerAccount(playerId);

                if (success)
                    DebugConsoleManager.Log("Azure", $"<color=green>LOADED:</color> {data}");
                else
                    DebugConsoleManager.Log("Azure", $"<color=red>FAILED:</color> {data}");
            });

            commands.AddSimpleCommand("Ping Localhost", async () =>
            {
                // Quick sanity check command
                DebugConsoleManager.Log("Azure", "Sending test request...");
                var (success, message) = await _accountService.SavePlayerAccount("ping_user", 0, 0, 0);
                DebugConsoleManager.Log("Azure", success ? "Pong! (Connection OK)" : "No Ping (Connection Failed)");
            });

            // Add info display to show current target
            commands.AddInfo("Target URL", () => serviceUrl);
        }
        commands.EndFolder();
    }

    [ContextMenu("Test Save Account")]
    public async void TestSaveAccount()
    {
        Debug.Log($"[AzureServiceTester] Saving account for {playerId}...");

        var (success, message) = await _accountService.SavePlayerAccount(playerId, coins, level, xp);

        if (success)
        {
            Debug.Log($"[AzureServiceTester] SUCCESS: {message}");
        }
        else
        {
            Debug.LogError($"[AzureServiceTester] FAILED: {message}");
        }
    }
}
