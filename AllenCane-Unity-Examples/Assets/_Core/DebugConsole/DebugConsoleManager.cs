using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Core.Utils
{
    /// <summary>
    /// Main debug console manager that coordinates all debug functionality.
    /// Handles input detection, UI rendering, and command management.
    /// </summary>
    public class DebugConsoleManager : MonoBehaviour
    {
        private static DebugConsoleManager instance;

        /// <summary>
        /// Public access to the singleton. Automatically creates the instance if it doesn't exist.
        /// </summary>
        public static DebugConsoleManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Check if it exists in scene but instance ref was lost
                    instance = FindObjectOfType<DebugConsoleManager>();

                    // If still null, create it
                    if (instance == null)
                    {
                        GameObject go = new GameObject("DebugConsole");
                        instance = go.AddComponent<DebugConsoleManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        // Core systems
        public DebugCommandCatalog Commands { get; private set; }
        public DebugLogViewer LogViewer { get; private set; }
        public DebugConsoleWindow ConsoleWindow { get; private set; }

        // UI state
        private bool isVisible = false;
        private bool commandsCollapsed = false;
        private bool consoleInputVisible = false; // Defaults to hidden
        private int commandsRegionWidth = 600;

        // Input actions
        private InputAction bracketLeftAction;
        private InputAction bracketRightAction;
        private InputAction tildeAction;
        private InputAction escapeAction;
        private InputAction fourFingerTouchAction;
        private bool fourFingersTouched = false;

        // Corner touch detection
        private bool lastTouched = false;
        private bool touchedInCheatSpot = false;
        private float touchedInCheatSpotStartTime = 0;
        private const int CheatSpotSize = 250;
        private const float CheatSpotTriggerSeconds = 1.3f;

        #region Initialization

        /// <summary>
        /// Automatically initializes the Debug Console when the game starts.
        /// This ensures the console is always available without manual setup.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            // Accessing Instance will force creation
            var _ = Instance;
        }

        /// <summary>
        /// Explicit initialization (optional, for backward compatibility)
        /// </summary>
        public static void Initialize(GameObject owner)
        {
            if (instance != null)
            {
                Debug.LogWarning("DebugConsoleManager already exists!");
                return;
            }

            var manager = owner.AddComponent<DebugConsoleManager>();
            DontDestroyOnLoad(owner);
            instance = manager;
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            // Initialize systems
            Commands = new DebugCommandCatalog("Root");
            LogViewer = new DebugLogViewer();
            ConsoleWindow = new DebugConsoleWindow();

            // Setup console command callback
            ConsoleWindow.OnCommandExecuted += HandleConsoleCommand;

            // Initialize input
            SetupInputSystem();
            SetupScreenScaling();

            // Hook into Unity's log system
            Application.logMessageReceived += HandleUnityLog;

            // Enable enhanced touch for mobile
            EnhancedTouchSupport.Enable();

            LogViewer.AddLine("Main", "Debug Console initialized. Press [ + ] or ~ to toggle.");
        }

        void Start()
        {
            // Cleanup hierarchy: Move [Debug Updater] to this object if it exists
            // We do this in Start because it might be created after Awake
            var inputDebugUpdater = GameObject.Find("[Debug Updater]");
            if (inputDebugUpdater != null)
            {
                inputDebugUpdater.transform.SetParent(this.transform);
            }
        }

        void OnDestroy()
        {
            // Cleanup input actions
            bracketLeftAction?.Disable();
            bracketRightAction?.Disable();
            tildeAction?.Disable();
            escapeAction?.Disable();
            fourFingerTouchAction?.Disable();

            Application.logMessageReceived -= HandleUnityLog;

            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Setup the Unity Input System actions.
        /// </summary>
        private void SetupInputSystem()
        {
            SetupKeyboardActions();
            SetupTouchActions();
        }

        private void SetupKeyboardActions()
        {
            // Left bracket
            bracketLeftAction = new InputAction();
            bracketLeftAction.AddBinding("<Keyboard>/leftBracket");
            bracketLeftAction.performed += OnBracketPressed;
            bracketLeftAction.Enable();

            // Right bracket
            bracketRightAction = new InputAction();
            bracketRightAction.AddBinding("<Keyboard>/rightBracket");
            bracketRightAction.performed += OnBracketPressed;
            bracketRightAction.Enable();

            // Tilde/backquote
            tildeAction = new InputAction();
            tildeAction.AddBinding("<Keyboard>/backquote");
            tildeAction.performed += _ => ToggleVisibility();
            tildeAction.Enable();

            // Escape to close
            escapeAction = new InputAction();
            escapeAction.AddBinding("<Keyboard>/escape");
            escapeAction.performed += _ => { if (isVisible) ToggleVisibility(); };
            escapeAction.Enable();
        }

        private void SetupTouchActions()
        {
            // Four finger touch
            fourFingerTouchAction = new InputAction(type: InputActionType.PassThrough);
            fourFingerTouchAction.AddBinding("<Touchscreen>/touch*/position");
            fourFingerTouchAction.performed += OnTouchPerformed;
            fourFingerTouchAction.Enable();
        }

        /// <summary>
        /// Setup responsive scaling based on screen size.
        /// </summary>
        private void SetupScreenScaling()
        {
            float commandsWidth = Screen.width * 0.45f;
            float commandsWidthMin = 300;

            if (commandsWidth < commandsWidthMin)
            {
                commandsWidth = commandsWidthMin;
            }

            commandsRegionWidth = (int)commandsWidth;
            LogViewer.AddLine("Main", $"Screen: {Screen.width}x{Screen.height}, Commands width: {commandsRegionWidth}");
        }

        #endregion

        #region Input Handling

        void Update()
        {
            UpdateCornerTouchDetection();
        }

        /// <summary>
        /// Handle bracket key combinations (both [ and ] must be pressed).
        /// </summary>
        private void OnBracketPressed(InputAction.CallbackContext context)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null &&
                keyboard.leftBracketKey.isPressed &&
                keyboard.rightBracketKey.isPressed)
            {
                ToggleVisibility();
            }
        }

        /// <summary>
        /// Handle four-finger touch detection.
        /// </summary>
        private void OnTouchPerformed(InputAction.CallbackContext context)
        {
            int activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count;

            if (activeTouches == 4 && !fourFingersTouched)
            {
                LogViewer.AddLine("Main", "Four fingers detected - toggling console");
                ToggleVisibility();
                fourFingersTouched = true;
            }
            else if (activeTouches != 4 && fourFingersTouched)
            {
                fourFingersTouched = false;
            }
        }

        /// <summary>
        /// Detect touch and hold in screen corners.
        /// </summary>
        private void UpdateCornerTouchDetection()
        {
            if (!TryGetValidTouch(out TouchControl touch))
            {
                ResetCornerTouch();
                return;
            }

            bool isInCheatSpot = IsTouchInCorner(touch.position.ReadValue());

            if (!lastTouched)
            {
                StartCornerTouch(isInCheatSpot);
            }
            else if (isInCheatSpot)
            {
                CheckCornerTouchDuration();
            }
            else
            {
                touchedInCheatSpot = false;
            }

            lastTouched = true;
        }

        private bool TryGetValidTouch(out TouchControl touch)
        {
            touch = Touchscreen.current?.primaryTouch;

            if (touch == null ||
                touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.None ||
                touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended ||
                touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                return false;
            }
            return true;
        }

        private void ResetCornerTouch()
        {
            lastTouched = false;
            touchedInCheatSpot = false;
        }

        private bool IsTouchInCorner(Vector2 touchPosition)
        {
            return (touchPosition.x < CheatSpotSize &&
                   (touchPosition.y < CheatSpotSize ||
                    Screen.height - touchPosition.y < CheatSpotSize));
        }

        private void StartCornerTouch(bool isInCheatSpot)
        {
            touchedInCheatSpot = isInCheatSpot;
            touchedInCheatSpotStartTime = Time.time;
        }

        private void CheckCornerTouchDuration()
        {
            if (touchedInCheatSpot &&
                (Time.time - touchedInCheatSpotStartTime >= CheatSpotTriggerSeconds))
            {
                touchedInCheatSpot = false;
                LogViewer.AddLine("Main", "Corner hold detected - toggling console");
                ToggleVisibility();
            }
        }

        #endregion

        #region UI Rendering

        void OnGUI()
        {
            if (!isVisible) return;

            // Full screen overlay setup
            GUI.enabled = true;
            GUI.color = Color.white;

            // Draw background overlay
            DrawBackground();

            // Layout calculations
            int horizontalPanelIntersection;
            CalculateLayout(out horizontalPanelIntersection, out int consoleHeight, out float safeAreaTop, out float safeAreaHeight);

            // Draw main components
            DrawConsoleInput(safeAreaTop, safeAreaHeight, consoleHeight);
            DrawLogViewer(safeAreaTop, safeAreaHeight, consoleHeight, horizontalPanelIntersection);
            DrawSideMenu(safeAreaTop, safeAreaHeight, consoleHeight, horizontalPanelIntersection);
        }

        private void DrawBackground()
        {
            GUIHelpers.DrawBox(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, 0.9f));
        }

        private void CalculateLayout(out int horizontalIntersection, out int consoleHeight, out float safeAreaTop, out float safeAreaHeight)
        {
            horizontalIntersection = commandsCollapsed
                ? Screen.width
                : (Screen.width - commandsRegionWidth);

            consoleHeight = consoleInputVisible ? Mathf.Max(100, (int)(Screen.height * 0.1f)) : 0;
            safeAreaTop = Screen.safeArea.y;
            safeAreaHeight = Screen.safeArea.height;
        }

        private void DrawConsoleInput(float safeAreaTop, float safeAreaHeight, int consoleHeight)
        {
            if (consoleInputVisible && consoleHeight > 0)
            {
                if (ConsoleWindow.DrawConsole(
                    new Rect(0, safeAreaTop + safeAreaHeight - consoleHeight, Screen.width, consoleHeight)))
                {
                    ToggleVisibility();
                }
            }
        }

        private void DrawLogViewer(float safeAreaTop, float safeAreaHeight, int consoleHeight, int horizontalIntersection)
        {
            LogViewer.DrawLog(new Rect(0, safeAreaTop, horizontalIntersection - 1,
                safeAreaHeight - consoleHeight));
        }

        private void DrawSideMenu(float safeAreaTop, float safeAreaHeight, int consoleHeight, int horizontalIntersection)
        {
            // Draw collapse/expand button
            DrawMenuButtons(horizontalIntersection, safeAreaTop);

            // Draw commands panel on the right (if not collapsed)
            if (!commandsCollapsed)
            {
                Commands.DrawCommands(new Rect(horizontalIntersection, safeAreaTop,
                    commandsRegionWidth, safeAreaHeight - consoleHeight));
            }
        }

        /// <summary>
        /// Draw utility buttons (collapse, copy, etc).
        /// </summary>
        private void DrawMenuButtons(int horizontalIntersection, float safeAreaTop)
        {
            Color buttonColor = new Color(0.53f, 0.62f, 0.46f);

            // Collapse/Expand button
            if (GUIHelpers.DrawButton(buttonColor,
                new Rect(horizontalIntersection - 70, safeAreaTop + 5, 67, 100),
                commandsCollapsed ? "<" : ">"))
            {
                commandsCollapsed = !commandsCollapsed;
            }

            // Copy button (when collapsed)
            if (commandsCollapsed)
            {
                if (GUIHelpers.DrawButton(buttonColor,
                    new Rect(horizontalIntersection - 70, safeAreaTop + 120, 67, 100),
                    "Copy"))
                {
                    LogViewer.CopyToClipboard();
                }
            }

            // Console Input Toggle (when expanded)
            if (!commandsCollapsed)
            {
                if (GUIHelpers.DrawButton(buttonColor,
                    new Rect(horizontalIntersection - 70, safeAreaTop + 120, 67, 100),
                    consoleInputVisible ? "Hide\nCmd" : "Show\nCmd"))
                {
                    consoleInputVisible = !consoleInputVisible;
                }

                // Copy Button (below Cmd toggle)
                if (GUIHelpers.DrawButton(buttonColor,
                    new Rect(horizontalIntersection - 70, safeAreaTop + 230, 67, 100),
                    "Copy"))
                {
                    LogViewer.CopyToClipboard();
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Toggle the debug console visibility.
        /// </summary>
        public static void Toggle()
        {
            Instance.ToggleVisibility();
        }

        /// <summary>
        /// Toggle the debug console visibility (instance method).
        /// </summary>
        public void ToggleVisibility()
        {
            isVisible = !isVisible;

            if (isVisible)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        /// <summary>
        /// Show the debug console.
        /// </summary>
        public void Show()
        {
            isVisible = true;
        }

        /// <summary>
        /// Hide the debug console.
        /// </summary>
        public void Hide()
        {
            isVisible = false;
        }

        /// <summary>
        /// Check if the console is currently visible.
        /// </summary>
        public bool IsVisible() => isVisible;

        #endregion

        #region Static Helper API

        // These static methods allow you to use the console without accessing Instance

        public static DebugCommand AddCommand(string name, System.Action callback)
        {
            return Instance.Commands.AddSimpleCommand(name, callback);
        }

        public static DebugCommandToggle AddToggle(string name, bool startValue, System.Action<bool> callback)
        {
            return Instance.Commands.AddToggle(name, startValue, callback);
        }

        public static void Log(string channel, string message)
        {
            Instance.LogViewer.AddLine(channel, message);
        }

        #endregion

        #region Command Handling

        /// <summary>
        /// Handle commands entered in the console window.
        /// </summary>
        private void HandleConsoleCommand(string command)
        {
            LogViewer.AddLine("Console", $"> {command}");

            // Try to find and execute a matching command
            DebugCommandBase foundCommand = Commands.FindCommand(command);
            if (foundCommand != null)
            {
                foundCommand.DoCommand();
                LogViewer.AddLine("Console", $"Executed: {command}");
            }
            else
            {
                LogViewer.AddLine("Console", $"Unknown command: {command}");
            }
        }

        /// <summary>
        /// Hook into Unity's log system to capture all logs.
        /// </summary>
        private void HandleUnityLog(string logText, string trace, LogType type)
        {
            string prefix = type switch
            {
                LogType.Error => "[ERROR]",
                LogType.Warning => "[WARN]",
                LogType.Exception => "[EXCEPTION]",
                _ => ""
            };

            LogViewer.AddLine("Unity", $"{prefix} {logText}");

            if (type == LogType.Exception)
            {
                LogViewer.AddLine("Unity", trace);
            }
        }

        #endregion
    }
}
