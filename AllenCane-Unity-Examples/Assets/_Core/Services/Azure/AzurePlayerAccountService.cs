using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Core.Services.Azure
{
    public class AzurePlayerAccountService : IPlayerAccountService
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;

        public AzurePlayerAccountService(string baseUrl, string apiKey)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _apiKey = apiKey;
        }

        // --- AUTH IMPLEMENTATION ---

        public async Task<(bool success, string message, string playerId, string token)> RegisterUser(string username, string password)
        {
            string url = $"{_baseUrl}/api/v1/auth/register";
            return await SendAuthRequest(url, username, password);
        }

        public async Task<(bool success, string message, string playerId, string token)> LoginUser(string username, string password)
        {
            string url = $"{_baseUrl}/api/v1/auth/login";
            return await SendAuthRequest(url, username, password);
        }

        private async Task<(bool success, string message, string playerId, string token)> SendAuthRequest(string url, string username, string password)
        {
            var requestData = new AuthRequest { username = username, password = password };
            string jsonBody = JsonUtility.ToJson(requestData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(_apiKey))
                    request.SetRequestHeader("x-functions-key", _apiKey);

                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                        return (response.success, response.message, response.playerId, response.token);
                    }
                    catch (Exception e)
                    {
                        return (false, $"JSON Error: {e.Message}", null, null);
                    }
                }
                else
                {
                    return (false, $"Network Error: {request.error}", null, null);
                }
            }
        }

        // --- DATA IMPLEMENTATION ---

        public async Task<(bool success, string message)> SavePlayerAccount(string playerId, int coins, int level, int xp, string token = null)
        {
            string url = $"{_baseUrl}/api/v1/players/{playerId}/account";

            var requestData = new PlayerAccountRequest
            {
                coins = coins,
                level = level,
                xp = xp
            };

            string jsonBody = JsonUtility.ToJson(requestData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(_apiKey))
                    request.SetRequestHeader("x-functions-key", _apiKey);

                // Pass the session token if we have one (Future-proofing)
                if (!string.IsNullOrEmpty(token))
                    request.SetRequestHeader("X-Session-Token", token);

                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<PlayerAccountResponse>(request.downloadHandler.text);
                        return (response.success, response.message);
                    }
                    catch (Exception e)
                    {
                        return (false, $"JSON Parse Error: {e.Message}");
                    }
                }
                else
                {
                    return (false, $"Network Error: {request.error} (Code: {request.responseCode})");
                }
            }
        }

        public async Task<(bool success, string data)> GetPlayerAccount(string playerId, string token = null)
        {
            string url = $"{_baseUrl}/api/v1/players/{playerId}/account";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(_apiKey))
                    request.SetRequestHeader("x-functions-key", _apiKey);

                if (!string.IsNullOrEmpty(token))
                    request.SetRequestHeader("X-Session-Token", token);

                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return (true, request.downloadHandler.text);
                }
                else
                {
                    return (false, $"Network Error: {request.error} (Code: {request.responseCode})");
                }
            }
        }
    }
}
