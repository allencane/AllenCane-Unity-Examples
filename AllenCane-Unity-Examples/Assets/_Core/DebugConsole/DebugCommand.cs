using UnityEngine;
using System;

namespace Core.Utils
{
    /// <summary>
    /// Command execution status for visual feedback.
    /// </summary>
    public enum DebugCommandStatus
    {
        Normal,
        Active,
        ToggleOn,
        ToggleOff
    }

    /// <summary>
    /// Abstract base class for all debug commands.
    /// Provides core functionality for command identification, display, and execution.
    /// </summary>
    public abstract class DebugCommandBase
    {
        // Default color palette for commands
        protected static Color DefaultCommandColor = new Color(0.2f, 0.93f, 0.7f, 1.0f); // Green
        protected static Color InfoCommandColor = new Color(0.5f, 0.6f, 0.7f); // Grey
        protected static Color TrueCommandColor = new Color(0.48f, 0.56f, 0.29f); // Grey-Green
        protected static Color FalseCommandColor = new Color(0.76f, 0.33f, 0.03f); // Orange

        protected string Name;
        protected string ID;

        public DebugCommandBase(string name)
        {
            Name = name;
            ID = name; // Default ID is the name, can be overridden
        }

        public string GetName() => Name;
        public string GetID() => ID;
        public void SetID(string id) => ID = id;

        /// <summary>
        /// Execute the command's action.
        /// </summary>
        public abstract void DoCommand();

        /// <summary>
        /// Get the color to display this command with.
        /// </summary>
        public virtual Color GetDrawColor() => DefaultCommandColor;

        /// <summary>
        /// Get the display text for this command.
        /// </summary>
        public virtual string GetDisplayText() => Name;

        /// <summary>
        /// Determine if this command should be visible.
        /// </summary>
        public virtual bool IsVisible() => true;

        /// <summary>
        /// Find a command by ID (used for hierarchical searches).
        /// </summary>
        public virtual DebugCommandBase FindCommand(string id)
        {
            return id == ID ? this : null;
        }
    }

    /// <summary>
    /// Simple command that executes a callback action when pressed.
    /// </summary>
    public class DebugCommand : DebugCommandBase
    {
        private Action callback;

        public DebugCommand(string name) : base(name) { }

        public void SetCallback(Action action)
        {
            callback = action;
        }

        public override void DoCommand()
        {
            callback?.Invoke();
        }
    }

    /// <summary>
    /// Command that cycles through a set of integer values.
    /// Useful for switching between different states or difficulty levels.
    /// </summary>
    public class DebugCommandValue : DebugCommandBase
    {
        private Action<int> callback;
        private int currentValue;
        private int maxValue;
        private string[] valueLabels;

        public DebugCommandValue(string name) : base(name) { }

        /// <summary>
        /// Setup the value cycler with a maximum count.
        /// </summary>
        public void SetValueRange(int max, Action<int> action)
        {
            maxValue = max;
            currentValue = 0;
            callback = action;
        }

        /// <summary>
        /// Setup the value cycler with custom labels.
        /// </summary>
        public void SetValueLabels(string[] labels, Action<int> action)
        {
            valueLabels = labels;
            maxValue = labels.Length;
            currentValue = 0;
            callback = action;
        }

        public override void DoCommand()
        {
            currentValue = (currentValue + 1) % maxValue;
            callback?.Invoke(currentValue);
        }

        public override string GetDisplayText()
        {
            if (valueLabels != null && currentValue < valueLabels.Length)
            {
                return $"{Name}: {valueLabels[currentValue]}";
            }
            return $"{Name}: {currentValue}";
        }

        public override Color GetDrawColor()
        {
            // Cycle through colors based on value
            float hue = (float)currentValue / maxValue;
            return Color.HSVToRGB(hue * 0.3f, 0.6f, 0.8f);
        }
    }

    /// <summary>
    /// Command that toggles between on/off states.
    /// </summary>
    public class DebugCommandToggle : DebugCommandBase
    {
        private Action<bool> callback;
        private bool isOn;

        public DebugCommandToggle(string name) : base(name) { }

        public void SetToggle(bool startValue, Action<bool> action)
        {
            isOn = startValue;
            callback = action;
        }

        public override void DoCommand()
        {
            isOn = !isOn;
            callback?.Invoke(isOn);
        }

        public override string GetDisplayText()
        {
            return $"{Name}: {(isOn ? "ON" : "OFF")}";
        }

        public override Color GetDrawColor()
        {
            return isOn ? TrueCommandColor : FalseCommandColor;
        }
    }

    /// <summary>
    /// Command that displays a text value without any action.
    /// Useful for showing debug information or stats.
    /// </summary>
    public class DebugCommandInfo : DebugCommandBase
    {
        private Func<string> textProvider;

        public DebugCommandInfo(string name) : base(name) { }

        public void SetTextProvider(Func<string> provider)
        {
            textProvider = provider;
        }

        public override void DoCommand()
        {
            // Info commands don't do anything when clicked
        }

        public override string GetDisplayText()
        {
            if (textProvider != null)
            {
                return $"{Name}: {textProvider()}";
            }
            return Name;
        }

        public override Color GetDrawColor()
        {
            return InfoCommandColor;
        }
    }

    /// <summary>
    /// Command that accepts a string parameter before execution.
    /// </summary>
    public class DebugCommandString : DebugCommandBase
    {
        private Action<string> callback;
        private string parameter;

        public DebugCommandString(string name) : base(name) { }

        public void SetCallback(Action<string> action, string defaultParam = "")
        {
            callback = action;
            parameter = defaultParam;
        }

        public void SetParameter(string param)
        {
            parameter = param;
        }

        public override void DoCommand()
        {
            callback?.Invoke(parameter);
        }

        public override string GetDisplayText()
        {
            return string.IsNullOrEmpty(parameter) ? Name : $"{Name} ({parameter})";
        }
    }
}

