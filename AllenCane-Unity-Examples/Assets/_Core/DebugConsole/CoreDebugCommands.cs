using UnityEngine;
using UnityEngine.SceneManagement;
using Core.Utils;

namespace Core.Examples
{
    /// <summary>
    /// Core debug commands demonstrating how to use the debug console system.
    /// Add this component to your bootstrap/startup GameObject to register example commands.
    /// </summary>
    public class CoreDebugCommands : MonoBehaviour
    {
        [Header("Example Settings")]
        [Tooltip("Enable FPS display on startup")]
        public bool showFPSOnStart = false;

        private bool showFPS = false;
        private float deltaTime = 0.0f;
        private bool godModeEnabled = false;

        /// <summary>
        /// Automatically add example commands on game start.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            // Ensure DebugConsoleManager is ready, then attach this component to it
            // to keep the hierarchy clean (one GameObject).
            if (DebugConsoleManager.Instance != null)
            {
                var managerGO = DebugConsoleManager.Instance.gameObject;
                if (managerGO.GetComponent<CoreDebugCommands>() == null)
                {
                    managerGO.AddComponent<CoreDebugCommands>();
                }
            }
        }

        void Start()
        {
            // Wait a frame for DebugConsoleManager to be ready
            StartCoroutine(RegisterCommandsNextFrame());
        }

        System.Collections.IEnumerator RegisterCommandsNextFrame()
        {
            yield return null;

            if (DebugConsoleManager.Instance == null)
            {
                Debug.LogWarning("CoreDebugCommands: DebugConsoleManager not initialized!");
                yield break;
            }

            RegisterCommands();
            showFPS = showFPSOnStart;
        }

        /// <summary>
        /// Register all example commands with the debug console.
        /// </summary>
        void RegisterCommands()
        {
            var commands = DebugConsoleManager.Instance.Commands;

            // ===== Account Folder (First Entry!) =====
            commands.StartFolder("Account");
            {
                commands.AddSimpleCommand("Reset Player", () =>
                {
                    DebugConsoleManager.Log("Account", "Reset Player requested (Not implemented in vanilla version)");
                });

                commands.StartFolder("Reset");
                {
                    commands.AddSimpleCommand("Clear Analytics ID", () =>
                    {
                        DebugConsoleManager.Log("Account", "Cleared Analytics ID");
                    });

                    commands.AddSimpleCommand("Clear First Install Pref", () =>
                    {
                        PlayerPrefs.DeleteKey("FirstInstall");
                        DebugConsoleManager.Log("Account", "Cleared First Install Pref");
                    });
                }
                commands.EndFolder();

                commands.AddSimpleCommand("Clear Login Provider", () =>
                {
                    DebugConsoleManager.Log("Account", "Cleared Login Provider");
                });
            }
            commands.EndFolder();

            // ===== Core Folder (Renamed from System) =====
            commands.StartFolder("Core");
            {
                // Logs sub-folder
                commands.StartFolder("Logs");
                {
                    commands.AddSimpleCommand("View Main Log", () => DebugConsoleManager.Instance.LogViewer.SelectLog("Main"));
                    commands.AddSimpleCommand("View Unity Log", () => DebugConsoleManager.Instance.LogViewer.SelectLog("Unity"));
                    commands.AddSimpleCommand("Clear All Logs", () => DebugConsoleManager.Instance.LogViewer.ClearAll());
                    commands.AddSimpleCommand("Copy All Logs", () => DebugConsoleManager.Instance.LogViewer.CopyToClipboard());
                }
                commands.EndFolder();

                // Audio sub-folder
                commands.StartFolder("Audio");
                {
                    commands.AddToggle("Mute All", AudioListener.pause, (val) => AudioListener.pause = val);
                    commands.AddValueCycleLabeled("Volume",
                        new string[] { "0%", "25%", "50%", "75%", "100%" },
                        (i) => AudioListener.volume = i * 0.25f);
                }
                commands.EndFolder();

                // Azure sub-folder
                commands.StartFolder("Azure");
                {
                    commands.AddSimpleCommand("Test Connection", () => DebugConsoleManager.Log("Azure", "Testing connection..."));
                    commands.AddSimpleCommand("Sync Data", () => DebugConsoleManager.Log("Azure", "Sync started..."));
                    commands.AddInfo("Status", () => "Disconnected");
                }
                commands.EndFolder();

                // System commands
                commands.AddInfo("FPS", () => $"{(1.0f / deltaTime):0.0}");
                commands.AddToggle("Show FPS Overlay", showFPS, (val) => showFPS = val);

                commands.AddSimpleCommand("Reload Scene", () =>
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                });
            }
            commands.EndFolder();

            // ===== Graphics Folder =====
            commands.StartFolder("Graphics");
            {
                commands.AddValueCycleLabeled("Quality Level", QualitySettings.names, (i) => QualitySettings.SetQualityLevel(i));
                commands.AddValueCycleLabeled("VSync", new string[] { "Off", "Every V Blank", "Every Second V Blank" }, (i) => QualitySettings.vSyncCount = i);
                commands.AddValueCycleLabeled("Target FPS", new string[] { "30", "60", "120", "Unlimited" },
                    (i) => Application.targetFrameRate = (i == 3 ? -1 : int.Parse(new string[] { "30", "60", "120" }[i])));
            }
            commands.EndFolder();

            // ===== Gameplay Folder =====
            commands.StartFolder("Gameplay");
            {
                // commands.AddToggle("God Mode", godModeEnabled, (val) => godModeEnabled = val);
                // commands.AddSimpleCommand("Add 1000 Coins", () => DebugConsoleManager.Log("Gameplay", "Added 1000 coins"));
                // commands.AddSimpleCommand("Unlock All Levels", () => DebugConsoleManager.Log("Gameplay", "Unlocked all levels"));
                // commands.AddSimpleCommand("Win Current Level", () => DebugConsoleManager.Log("Gameplay", "Level won!"));
            }
            commands.EndFolder();

            // Log registration complete
            DebugConsoleManager.Instance.LogViewer.AddLine("Main", "Example debug commands registered!");
        }

        void Update()
        {
            // Update FPS calculation
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }

        void OnGUI()
        {
            // Draw FPS overlay if enabled
            if (showFPS)
            {
                int w = Screen.width;
                int h = Screen.height;

                GUIStyle style = new GUIStyle();
                Rect rect = new Rect(10, 10, w, h * 2 / 100);
                style.alignment = TextAnchor.UpperLeft;
                style.fontSize = h * 2 / 50;
                style.normal.textColor = Color.yellow;

                float fps = 1.0f / deltaTime;
                string text = $"FPS: {fps:0.}";

                // Draw shadow
                GUI.color = Color.black;
                GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);

                // Draw text
                GUI.color = fps < 30 ? Color.red : fps < 60 ? Color.yellow : Color.green;
                GUI.Label(rect, text, style);
                GUI.color = Color.white;
            }
        }

        /// <summary>
        /// Example: Check if god mode is enabled.
        /// Use this in your game code to check cheat states.
        /// </summary>
        public bool IsGodModeEnabled() => godModeEnabled;
    }
}

