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

        public async Task<(bool success, string message)> SavePlayerAccount(string playerId, int coins, int level, int xp)
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
                {
                    request.SetRequestHeader("x-functions-key", _apiKey);
                }

                // Send request and wait
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

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

        public async Task<(bool success, string data)> GetPlayerAccount(string playerId)
        {
            string url = $"{_baseUrl}/api/v1/players/{playerId}/account";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    request.SetRequestHeader("x-functions-key", _apiKey);
                }

                // Send request and wait
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

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
