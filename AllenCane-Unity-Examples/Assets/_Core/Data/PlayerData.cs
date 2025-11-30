using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Data
{
    /// <summary>
    /// Agnostic container for player data using a Dictionary backing store.
    /// Tracks changes to support optimized partial / incremental saves.
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
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("PlayerData: Attempted to set null or empty key.");
                return;
            }

            // Azure Table Storage Validation
            if (char.IsDigit(key[0]))
            {
                Debug.LogError($"PlayerData: Invalid key '{key}'. Keys cannot start with a number (Azure limitation).");
                return;
            }

            foreach (char c in key)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    Debug.LogError($"PlayerData: Invalid key '{key}'. Keys must contain only letters, numbers, or underscores.");
                    return;
                }
            }

            _data[key] = value;
        }

        // --- Optimization Logic ---

        /// <summary>
        /// Returns only the Key-Values that have changed since the last save.
        /// Automatically filters out keys that would be rejected by Azure Table Storage.
        /// </summary>
        public Dictionary<string, object> GetChanges()
        {
            var changes = new Dictionary<string, object>();

            foreach (var kvp in _data)
            {
                string key = kvp.Key;
                object currentVal = kvp.Value;

                // 1. Filter invalid Azure Table Storage keys (Prevents 400 Bad Request blocks)
                if (!IsValidAzureKey(key))
                {
                    Debug.LogWarning($"PlayerData: Skipping invalid key '{key}' during sync.");
                    continue;
                }

                // 2. Check for actual changes
                if (!_lastSavedState.ContainsKey(key) || !ValuesAreEqual(_lastSavedState[key], currentVal))
                {
                    changes[key] = currentVal;
                }
            }

            return changes;
        }

        private bool IsValidAzureKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (char.IsDigit(key[0])) return false; // Cannot start with number

            foreach (char c in key)
            {
                if (!char.IsLetterOrDigit(c) && c != '_') return false; // Only Alphanumeric + Underscore
            }
            return true;
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

        public List<string> GetAllKeys()
        {
            return _data.Keys.ToList();
        }

        public string ToDebugString()
        {
            return string.Join(", ", _data.Select(kv => $"{kv.Key}:{kv.Value}"));
        }
    }
}


