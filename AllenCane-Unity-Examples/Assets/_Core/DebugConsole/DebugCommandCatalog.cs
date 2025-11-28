using UnityEngine;
using System.Collections.Generic;
using System;

namespace Core.Utils
{
    /// <summary>
    /// Folder that contains debug commands, allowing hierarchical organization.
    /// Can be collapsed/expanded in the UI.
    /// </summary>
    public class DebugCommandFolder : DebugCommandBase
    {
        protected List<DebugCommandBase> children = new List<DebugCommandBase>();
        public bool Opened = false;

        public DebugCommandFolder(string name) : base(name) { }

        public virtual DebugCommandBase Add(DebugCommandBase command)
        {
            children.Add(command);
            return command;
        }

        public List<DebugCommandBase> GetChildren() => children;

        public override void DoCommand()
        {
            Opened = !Opened;
        }

        public override string GetDisplayText()
        {
            return $"{(Opened ? "▼" : "►")} {Name}";
        }

        public override DebugCommandBase FindCommand(string id)
        {
            if (id == ID) return this;

            foreach (var child in children)
            {
                var found = child.FindCommand(id);
                if (found != null) return found;
            }

            return null;
        }
    }

    /// <summary>
    /// Main catalog that manages all debug commands.
    /// Provides methods for adding commands and organizing them into folders.
    /// </summary>
    public class DebugCommandCatalog : DebugCommandFolder
    {
        private Stack<DebugCommandFolder> folderStack = new Stack<DebugCommandFolder>();
        private Vector2 scrollPosition;

        public DebugCommandCatalog(string name) : base(name)
        {
            Opened = true; // Root is always open
        }

        /// <summary>
        /// Start a new folder. All subsequent commands will be added to this folder
        /// until EndFolder() is called.
        /// </summary>
        public DebugCommandFolder StartFolder(string name)
        {
            DebugCommandFolder folder = new DebugCommandFolder(name);
            Add(folder);
            folderStack.Push(folder);
            return folder;
        }

        /// <summary>
        /// End the current folder context.
        /// </summary>
        public void EndFolder()
        {
            if (folderStack.Count == 0)
            {
                Debug.LogError("Tried to end more debug folders than started.");
            }
            else
            {
                folderStack.Pop();
            }
        }

        /// <summary>
        /// Add a command to the current folder context (or root if no folder is open).
        /// </summary>
        public override DebugCommandBase Add(DebugCommandBase command)
        {
            if (folderStack.Count == 0)
            {
                base.Add(command);
            }
            else
            {
                // Build hierarchical ID
                command.SetID(BuildHierarchicalID(command.GetName()));
                folderStack.Peek().Add(command);
            }
            return command;
        }

        /// <summary>
        /// Build a hierarchical ID based on the current folder stack.
        /// </summary>
        private string BuildHierarchicalID(string name)
        {
            string id = "";
            foreach (var folder in folderStack)
            {
                if (!string.IsNullOrEmpty(folder.GetName()))
                {
                    id += folder.GetName() + ".";
                }
            }
            id += name;
            return id;
        }

        #region Convenience Methods

        /// <summary>
        /// Add a simple command that executes a callback.
        /// </summary>
        public DebugCommand AddSimpleCommand(string name, Action callback)
        {
            DebugCommand command = new DebugCommand(name);
            command.SetCallback(callback);
            Add(command);
            return command;
        }

        /// <summary>
        /// Add a toggle command with on/off states.
        /// </summary>
        public DebugCommandToggle AddToggle(string name, bool startValue, Action<bool> callback)
        {
            DebugCommandToggle command = new DebugCommandToggle(name);
            command.SetToggle(startValue, callback);
            Add(command);
            return command;
        }

        /// <summary>
        /// Add a value cycler command.
        /// </summary>
        public DebugCommandValue AddValueCycle(string name, int maxValue, Action<int> callback)
        {
            DebugCommandValue command = new DebugCommandValue(name);
            command.SetValueRange(maxValue, callback);
            Add(command);
            return command;
        }

        /// <summary>
        /// Add a value cycler with custom labels.
        /// </summary>
        public DebugCommandValue AddValueCycleLabeled(string name, string[] labels, Action<int> callback)
        {
            DebugCommandValue command = new DebugCommandValue(name);
            command.SetValueLabels(labels, callback);
            Add(command);
            return command;
        }

        /// <summary>
        /// Add an info display command.
        /// </summary>
        public DebugCommandInfo AddInfo(string name, Func<string> textProvider)
        {
            DebugCommandInfo command = new DebugCommandInfo(name);
            command.SetTextProvider(textProvider);
            Add(command);
            return command;
        }

        #endregion

        #region Drawing

        private struct DrawNode
        {
            public DebugCommandBase Command;
            public int Depth;

            public DrawNode(DebugCommandBase command, int depth)
            {
                Command = command;
                Depth = depth;
            }
        }

        /// <summary>
        /// Draw all commands in a scrollable region.
        /// </summary>
        public void DrawCommands(Rect region)
        {
            GUIHelpers.DrawBox(region, new Color(0, 0, 0, 1.0f));

            // Build flat list of visible nodes
            List<DrawNode> nodes = new List<DrawNode>();
            BuildDrawNodes(this, 0, ref nodes);

            // Layout calculations
            int buttonHeight = Mathf.Max(70, (int)(Screen.height * 0.08f));
            int buttonSpacingY = 5;
            float contentHeight = (buttonHeight + buttonSpacingY) * nodes.Count;

            // Handle scrolling
            scrollPosition = GUIHelpers.UpdateScrollForTouchDrag(region, scrollPosition);

            // Draw scrollable content
            DrawScrollableCommandList(region, nodes, buttonHeight, buttonSpacingY, contentHeight);
        }

        private void DrawScrollableCommandList(Rect region, List<DrawNode> nodes, int buttonHeight, int buttonSpacingY, float contentHeight)
        {
            int baseIndent = 10;

            scrollPosition = GUI.BeginScrollView(region, scrollPosition,
                new Rect(0, 0, region.width - baseIndent, contentHeight),
                GUIStyle.none, GUI.skin.verticalScrollbar);

            int yPos = 0;
            foreach (DrawNode node in nodes)
            {
                DrawSingleCommand(node, region.width, yPos, buttonHeight, baseIndent);
                yPos += buttonHeight + buttonSpacingY;
            }

            GUI.EndScrollView();
        }

        private void DrawSingleCommand(DrawNode node, float availableWidth, int yPos, int height, int baseIndent)
        {
            int folderIndent = 25;
            int currentIndent = node.Depth * folderIndent + baseIndent;

            Color drawColor = GetCommandColor(node);
            Rect buttonRect = new Rect(currentIndent, yPos, availableWidth - baseIndent - currentIndent, height);

            if (GUIHelpers.DrawButton(drawColor, buttonRect, node.Command.GetDisplayText()))
            {
                node.Command.DoCommand();
            }
        }

        private Color GetCommandColor(DrawNode node)
        {
            Color color = node.Command.GetDrawColor();

            // Override color for nested items (Depth > 0) to distinct Blue
            if (node.Depth > 0 && (node.Command is DebugCommandFolder || node.Command is DebugCommand))
            {
                return new Color(0.2f, 0.6f, 0.9f, 1.0f);
            }
            return color;
        }

        /// <summary>
        /// Recursively build a flat list of visible commands for drawing.
        /// </summary>
        private void BuildDrawNodes(DebugCommandFolder folder, int depth, ref List<DrawNode> drawNodes)
        {
            foreach (DebugCommandBase command in folder.GetChildren())
            {
                if (command.IsVisible())
                {
                    drawNodes.Add(new DrawNode(command, depth));

                    // If it's a folder and it's open, add its children
                    if (command is DebugCommandFolder childFolder && childFolder.Opened)
                    {
                        BuildDrawNodes(childFolder, depth + 1, ref drawNodes);
                    }
                }
            }
        }

        #endregion
    }
}

