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

        public AzurePlayerAccountService(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
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
                    return (false, $"Network Error: {request.error}");
                }
            }
        }
    }
}

