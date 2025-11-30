using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Data
{
    /// <summary>
    /// Agnostic container for player data using a Dictionary backing store.
    /// Tracks changes to support optimized partial saves (Match_GO style).
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        // The master state
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        // Snapshot of the state at the last successful save
        private Dictionary<string, object> _lastSavedState = new Dictionary<string, object>();

        public PlayerData()
        {
            // Initialize default values
            _data["Coins"] = 0;
            _data["PlayerLevel"] = 1;
            _data["ExperiencePoints"] = 0;

            // Assume these defaults are 'saved'
            _lastSavedState = new Dictionary<string, object>(_data);
        }

        // --- Public Accessors ---

        public T Get<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out object value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    Debug.LogWarning($"PlayerData: Could not convert key '{key}' to {typeof(T)}");
                }
            }
            return defaultValue;
        }

        public void Set<T>(string key, T value)
        {
            _data[key] = value;
        }

        // --- Optimization Logic ---

        /// <summary>
        /// Returns only the Key-Values that have changed since the last save.
        /// </summary>
        public Dictionary<string, object> GetChanges()
        {
            var changes = new Dictionary<string, object>();

            foreach (var kvp in _data)
            {
                string key = kvp.Key;
                object currentVal = kvp.Value;

                if (!_lastSavedState.ContainsKey(key) || !ValuesAreEqual(_lastSavedState[key], currentVal))
                {
                    changes[key] = currentVal;
                }
            }

            return changes;
        }

        /// <summary>
        /// Call this after a successful Cloud Save to verify the data is synced.
        /// </summary>
        public void CommitChanges()
        {
            _lastSavedState = new Dictionary<string, object>(_data);
        }

        /// <summary>
        /// Update local data from a full Cloud Load.
        /// </summary>
        public void ApplyCloudLoad(Dictionary<string, object> cloudData)
        {
            foreach (var kvp in cloudData)
            {
                _data[kvp.Key] = kvp.Value;
            }
            CommitChanges();
        }

        private bool ValuesAreEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.ToString() == b.ToString();
        }

        public string ToDebugString()
        {
            return string.Join(", ", _data.Select(kv => $"{kv.Key}:{kv.Value}"));
        }
    }
}


