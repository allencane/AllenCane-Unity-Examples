using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace Core.Utils
{
    /// <summary>
    /// Manages and displays multiple log channels in a scrollable view.
    /// Supports filtering, multiple tabs, and copying logs to clipboard.
    /// </summary>
    public class DebugLogViewer
    {
        private Dictionary<string, List<string>> logs = new Dictionary<string, List<string>>();
        private string selectedLog = "Main";
        private Vector2 scrollPosition;
        private int maxLinesPerLog = 500;

        public DebugLogViewer()
        {
            // Create default log channel
            logs["Main"] = new List<string>();
        }

        /// <summary>
        /// Add a line to a specific log channel.
        /// </summary>
        public void AddLine(string logName, string line)
        {
            if (!logs.ContainsKey(logName))
            {
                logs[logName] = new List<string>();
            }

            logs[logName].Add($"[{System.DateTime.Now:HH:mm:ss}] {line}");

            // Trim old lines if we exceed max
            if (logs[logName].Count > maxLinesPerLog)
            {
                logs[logName].RemoveAt(0);
            }
        }

        /// <summary>
        /// Select which log channel to display.
        /// </summary>
        public void SelectLog(string logName)
        {
            if (logs.ContainsKey(logName))
            {
                selectedLog = logName;
                // Auto-scroll to bottom when switching logs
                scrollPosition.y = float.MaxValue;
            }
        }

        /// <summary>
        /// Clear a specific log channel.
        /// </summary>
        public void ClearLog(string logName)
        {
            if (logs.ContainsKey(logName))
            {
                logs[logName].Clear();
            }
        }

        /// <summary>
        /// Clear all log channels.
        /// </summary>
        public void ClearAll()
        {
            foreach (var log in logs.Values)
            {
                log.Clear();
            }
        }

        /// <summary>
        /// Get the full text of a log channel.
        /// </summary>
        public string GetLogText(string logName)
        {
            if (!logs.ContainsKey(logName))
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            foreach (string line in logs[logName])
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Copy the current log to clipboard.
        /// </summary>
        public void CopyToClipboard()
        {
            string text = GetLogText(selectedLog);
            GUIUtility.systemCopyBuffer = text;
            AddLine("Main", $"Copied {selectedLog} log to clipboard ({text.Length} chars)");
        }

        /// <summary>
        /// Draw the log viewer UI.
        /// </summary>
        public void DrawLog(Rect region)
        {
            // Draw background
            GUIHelpers.DrawBox(region, new Color(0.05f, 0.05f, 0.1f, 1.0f));

            // Calculate layout
            int tabHeight = Mathf.Max(60, (int)(Screen.height * 0.06f));
            int buttonWidth = Mathf.Max(120, (int)(region.width * 0.25f));
            int buttonSpacing = 5;

            // Draw tabs at the top
            Rect tabsRegion = new Rect(region.x, region.y, region.width, tabHeight);
            DrawTabs(tabsRegion, buttonWidth, buttonSpacing);

            // Draw the log content below tabs
            Rect logRegion = new Rect(
                region.x + 5,
                region.y + tabHeight + 5,
                region.width - 10,
                region.height - tabHeight - 10
            );
            DrawLogContent(logRegion);
        }

        /// <summary>
        /// Draw tab buttons for switching between log channels.
        /// </summary>
        private void DrawTabs(Rect region, int buttonWidth, int spacing)
        {
            int xPos = (int)region.x + spacing;
            int yPos = (int)region.y + spacing;
            int buttonHeight = (int)region.height - (spacing * 2);

            foreach (string logName in logs.Keys)
            {
                // Check if this tab would go off screen
                if (xPos + buttonWidth > region.xMax)
                {
                    break; // Stop drawing tabs if we run out of space
                }

                Color tabColor = (logName == selectedLog)
                    ? new Color(0.3f, 0.6f, 0.9f, 1.0f)  // Highlighted
                    : new Color(0.2f, 0.2f, 0.3f, 1.0f);  // Dim

                bool pressed = GUIHelpers.DrawButton(
                    tabColor,
                    new Rect(xPos, yPos, buttonWidth, buttonHeight),
                    $"{logName} ({logs[logName].Count})");

                if (pressed)
                {
                    SelectLog(logName);
                }

                xPos += buttonWidth + spacing;
            }
        }

        /// <summary>
        /// Draw the actual log text content.
        /// </summary>
        private void DrawLogContent(Rect region)
        {
            string logText = GetLogText(selectedLog);

            if (string.IsNullOrEmpty(logText))
            {
                // Show placeholder text
                GUI.Label(region, "No log entries yet.", new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    normal = { textColor = Color.gray }
                });
                return;
            }

            // Calculate responsive font size (approx 2% of screen height, min 14)
            int fontSize = Mathf.Max(14, (int)(Screen.height * 0.02f));

            // Use custom log drawing helper
            GUIStyle logStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true, // Enable wrapping to prevent clipping
                fontSize = fontSize,
                normal = { textColor = new Color(0.8f, 0.9f, 1.0f, 1.0f) },
                richText = true
            };

            // Constrain text width to avoid overlapping side buttons (approx 90px)
            float textWidth = region.width - 90;
            GUIContent content = new GUIContent(logText);
            float contentHeight = logStyle.CalcHeight(content, textWidth);

            // Handle touch scrolling
            scrollPosition = GUIHelpers.UpdateScrollForTouchDrag(region, scrollPosition);

            scrollPosition = GUI.BeginScrollView(region, scrollPosition,
                new Rect(0, 0, textWidth, contentHeight));

            GUI.Label(new Rect(0, 0, textWidth, contentHeight), logText, logStyle);

            GUI.EndScrollView();
        }

        /// <summary>
        /// Get all available log channel names.
        /// </summary>
        public List<string> GetLogNames()
        {
            return new List<string>(logs.Keys);
        }
    }
}

