using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.Services;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Core.Services.Azure
{
    /// <summary>
    /// Azure-backed implementation of IPlayerDataSyncService.
    /// Sends and receives Dictionary&lt;string, object&gt; payloads as JSON.
    /// </summary>
    public class AzurePlayerDataSyncService : IPlayerDataSyncService
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;

        public AzurePlayerDataSyncService(string baseUrl, string apiKey)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _apiKey = apiKey;
        }

        public async Task<(bool success, string message)> SaveAsync(string playerId, Dictionary<string, object> changes, string token = null)
        {
            if (changes == null || changes.Count == 0)
            {
                return (true, "No changes to save.");
            }

            string url = $"{_baseUrl}/api/v1/players/{playerId}/account";
            string jsonBody = JsonConvert.SerializeObject(changes);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(_apiKey))
                    request.SetRequestHeader("x-functions-key", _apiKey);

                if (!string.IsNullOrEmpty(token))
                    request.SetRequestHeader("X-Session-Token", token);

                var op = request.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return (true, "Saved dictionary successfully.");
                }

                Debug.LogError($"[AzurePlayerDataSyncService] Save failed: {request.error} (Code: {request.responseCode})");
                return (false, $"Network Error: {request.error} (Code: {request.responseCode})");
            }
        }

        public async Task<(bool success, Dictionary<string, object> data)> LoadAsync(string playerId, string token = null)
        {
            string url = $"{_baseUrl}/api/v1/players/{playerId}/account";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(_apiKey))
                    request.SetRequestHeader("x-functions-key", _apiKey);

                if (!string.IsNullOrEmpty(token))
                    request.SetRequestHeader("X-Session-Token", token);

                var op = request.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);
                        return (true, dict ?? new Dictionary<string, object>());
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[AzurePlayerDataSyncService] JSON parse error: {e.Message}");
                        return (false, null);
                    }
                }

                Debug.LogError($"[AzurePlayerDataSyncService] Load failed: {request.error} (Code: {request.responseCode})");
                return (false, null);
            }
        }
        public async Task<(bool success, string message)> DeleteKeysAsync(string playerId, List<string> keysToDelete, string token = null)
        {
            if (keysToDelete == null || keysToDelete.Count == 0)
            {
                return (true, "No keys to delete.");
            }

            string url = $"{_baseUrl}/api/v1/players/{playerId}/account/delete";
            string jsonBody = JsonConvert.SerializeObject(keysToDelete);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(_apiKey))
                    request.SetRequestHeader("x-functions-key", _apiKey);

                if (!string.IsNullOrEmpty(token))
                    request.SetRequestHeader("X-Session-Token", token);

                var op = request.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return (true, "Deleted keys successfully.");
                }

                Debug.LogError($"[AzurePlayerDataSyncService] Delete failed: {request.error} (Code: {request.responseCode})");
                return (false, $"Network Error: {request.error} (Code: {request.responseCode})");
            }
        }
    }
}


