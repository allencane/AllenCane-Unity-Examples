using UnityEngine;

namespace Core.Utils
{
    /// <summary>
    /// Console window for text input commands at the bottom of the debug UI.
    /// Allows typing commands and executing them on Enter or Escape to close.
    /// </summary>
    public class DebugConsoleWindow
    {
        private string consoleText = "";
        private bool keyboardWasOpen = false;
        private bool keyboardJustClosed = false;

        public delegate void CommandExecutedDelegate(string command);
        public event CommandExecutedDelegate OnCommandExecuted;

        /// <summary>
        /// Draw the console input window.
        /// Returns true if the console should be closed (escape was pressed).
        /// </summary>
        public bool DrawConsole(Rect region)
        {
            UpdateKeyboardDetection();

            // Draw dark background
            GUIHelpers.DrawBox(region, new Color(0.05f, 0.05f, 0.15f, 1.0f));

            // Check for Enter or Escape key
            bool pressedEnter = Event.current.Equals(Event.KeyboardEvent("return"));
            bool pressedEscape = Event.current.Equals(Event.KeyboardEvent("escape"));

            // Draw text input area
            GUI.SetNextControlName("ConsoleInput");
            string newText = GUIHelpers.DrawConsoleTextArea(region, consoleText);
            
            // Only update if text actually changed (prevents cursor reset)
            if (newText != consoleText)
            {
                consoleText = newText;
            }

            // Execute command on Enter or keyboard close
            if (!string.IsNullOrEmpty(consoleText) && (pressedEnter || keyboardJustClosed))
            {
                ExecuteCommand();
                keyboardJustClosed = false;
            }

            return pressedEscape;
        }

        /// <summary>
        /// Detect if the mobile keyboard was just closed.
        /// </summary>
        private void UpdateKeyboardDetection()
        {
            keyboardJustClosed = false;

            if (TouchScreenKeyboard.visible)
            {
                keyboardWasOpen = true;
            }
            else if (keyboardWasOpen)
            {
                keyboardWasOpen = false;
                keyboardJustClosed = true;
            }
        }

        /// <summary>
        /// Execute the current command text.
        /// </summary>
        private void ExecuteCommand()
        {
            string command = consoleText.Trim();
            
            if (!string.IsNullOrEmpty(command))
            {
                Debug.Log($"[DebugConsole] Command: {command}");
                OnCommandExecuted?.Invoke(command);
            }

            // Clear the input
            consoleText = "";
        }

        /// <summary>
        /// Clear the console input.
        /// </summary>
        public void Clear()
        {
            consoleText = "";
        }
    }
}

