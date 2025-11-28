using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Utils
{
    /// <summary>
    /// Utility class for rendering debug UI elements using Unity's immediate mode GUI (OnGUI).
    /// Provides helper methods for drawing buttons, boxes, and handling touch/scroll interactions.
    /// </summary>
    public static class GUIHelpers
    {
        private static Vector2 lastTouchPosition;
        private static bool wasTouching;
        private static float lastButtonClickTime = 0f;
        private const float buttonClickCooldown = 0.1f;

        /// <summary>
        /// Draws a colored box at the specified region.
        /// </summary>
        public static void DrawBox(Rect region, Color color)
        {
            Color originalColor = GUI.color;
            GUI.color = color;
            GUI.Box(region, "", GUI.skin.box);
            GUI.color = originalColor;
        }

        /// <summary>
        /// Draws a button with custom color and returns true if pressed.
        /// Handles both legacy GUI events and New Input System checks.
        /// </summary>
        public static bool DrawButton(Color color, Rect region, string text, System.Action onLongPress = null)
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = color;

            ConfigureButtonText(region, text);

            // Check for button press using multiple methods
            bool pressed = CheckButtonInput(region, text);

            if (pressed)
            {
                // Debug.Log($"[DebugConsole] Button pressed: {text}");
            }

            RestoreButtonSkin(originalColor);

            return pressed;
        }

        /// <summary>
        /// Configures button font size and word wrap based on region height and text width.
        /// </summary>
        private static void ConfigureButtonText(Rect region, string text)
        {
            int dynamicSize = Mathf.Max(12, (int)(region.height * 0.3f));
            GUI.skin.button.fontSize = dynamicSize;

            GUIContent content = new GUIContent(text);
            Vector2 size = GUI.skin.button.CalcSize(content);
            if (size.x > region.width)
            {
                GUI.skin.button.fontSize = Mathf.Max(10, (int)(dynamicSize * 0.7f));
                GUI.skin.button.wordWrap = true;
            }
            else
            {
                GUI.skin.button.wordWrap = false;
            }
        }

        /// <summary>
        /// Restores original button skin settings.
        /// </summary>
        private static void RestoreButtonSkin(Color originalColor)
        {
            // Note: In immediate mode GUI, we often don't need to restore fontSize if we set it every frame,
            // but it's good practice if other GUI calls rely on defaults. 
            // However, since we don't store the original fontSize in a static, we rely on the next Draw call setting it again.
            // Or we can assume standard Unity default is roughly 13-14.
            GUI.skin.button.fontSize = 14; // Reset to sensible default
            GUI.backgroundColor = originalColor;
        }

        /// <summary>
        /// Checks for button press using Legacy GUI, Mouse (New Input), and Touch (New Input).
        /// </summary>
        private static bool CheckButtonInput(Rect region, string text)
        {
            bool pressed = false;
            float currentTime = Time.time;

            // Method 1: Standard GUI.Button
            bool guiPressed = GUI.Button(region, text);
            if (guiPressed && currentTime - lastButtonClickTime >= buttonClickCooldown)
            {
                pressed = true;
                lastButtonClickTime = currentTime;
            }

#if UNITY_EDITOR
            // Method 2: New Input System Mouse (Game View / Editor)
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                mousePos.y = Screen.height - mousePos.y; // Flip Y for GUI coordinates

                Rect screenRect = GUIUtility.GUIToScreenRect(region);
                
                if (screenRect.Contains(mousePos) && currentTime - lastButtonClickTime >= buttonClickCooldown)
                {
                    pressed = true;
                    lastButtonClickTime = currentTime;
                }
            }

            // Method 3: New Input System Touch (Simulator / Device)
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
                touchPos.y = Screen.height - touchPos.y; // Flip Y for GUI coordinates

                Rect screenRect = GUIUtility.GUIToScreenRect(region);
                
                if (screenRect.Contains(touchPos) && currentTime - lastButtonClickTime >= buttonClickCooldown)
                {
                    pressed = true;
                    lastButtonClickTime = currentTime;
                }
            }
#endif

            return pressed;
        }

        // Redoing DrawButton to be cleaner without helper splitting that complicates drawing
        // (GUI.Button must be called exactly once per layout/repaint event)

        /// <summary>
        /// Draws a label with custom styling.
        /// </summary>
        public static void DrawLabel(Rect region, string text)
        {
            int originalFontSize = GUI.skin.label.fontSize;
            int dynamicSize = Mathf.Max(12, (int)(region.height * 0.4f));
            GUI.skin.label.fontSize = dynamicSize;

            Color originalColor = GUI.skin.label.normal.textColor;
            GUI.skin.label.normal.textColor = Color.yellow;

            GUI.Label(region, text);

            GUI.skin.label.fontSize = originalFontSize;
            GUI.skin.label.normal.textColor = originalColor;
        }

        /// <summary>
        /// Updates scroll position based on touch drag input.
        /// This allows smooth scrolling on mobile devices.
        /// </summary>
        public static Vector2 UpdateScrollForTouchDrag(Rect region, Vector2 currentScrollPosition)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
                touchPos.y = Screen.height - touchPos.y; // Flip Y

                Rect screenRect = GUIUtility.GUIToScreenRect(region);

                if (screenRect.Contains(touchPos))
                {
                    Vector2 delta = Touchscreen.current.primaryTouch.delta.ReadValue();
                    currentScrollPosition.y -= delta.y;
                    currentScrollPosition.y = Mathf.Max(0, currentScrollPosition.y);
                }
            }
            return currentScrollPosition;
        }

        /// <summary>
        /// Draws a text area with custom styling for console input.
        /// </summary>
        public static string DrawConsoleTextArea(Rect region, string text)
        {
            int originalFontSize = GUI.skin.textArea.fontSize;
            Color originalTextColor = GUI.skin.textArea.normal.textColor;

            GUI.skin.textArea.fontSize = Mathf.Max(16, (int)(region.height * 0.5f));
            GUI.skin.textArea.normal.textColor = new Color(0.6f, 0.75f, 0.95f, 1);
            GUI.skin.textArea.focused.textColor = new Color(0.95f, 0.95f, 1, 1);

            string result = GUI.TextArea(region, text);

            GUI.skin.textArea.fontSize = originalFontSize;
            GUI.skin.textArea.normal.textColor = originalTextColor;

            return result;
        }

        /// <summary>
        /// Draws a scrollable log view with custom styling.
        /// </summary>
        public static void DrawLogText(Rect region, string text, ref Vector2 scrollPosition)
        {
            int originalFontSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 14;
            GUI.skin.label.wordWrap = true;

            GUIStyle logStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                fontSize = 14
            };

            GUIContent content = new GUIContent(text);
            float height = logStyle.CalcHeight(content, region.width - 20);

            scrollPosition = GUI.BeginScrollView(region, scrollPosition,
                new Rect(0, 0, region.width - 20, height));

            GUI.Label(new Rect(0, 0, region.width - 20, height), text, logStyle);

            GUI.EndScrollView();

            GUI.skin.label.fontSize = originalFontSize;
        }
    }
}
