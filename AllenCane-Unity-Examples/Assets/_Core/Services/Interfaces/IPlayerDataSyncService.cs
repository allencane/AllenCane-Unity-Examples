using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Services
{
    /// <summary>
    /// Backend-agnostic interface for syncing player data as a dictionary.
    /// Mirrors the Match_GO pattern of sending key/value changes to a cloud backend.
    /// </summary>
    public interface IPlayerDataSyncService
    {
        Task<(bool success, string message)> SaveAsync(string playerId, Dictionary<string, object> changes, string token = null);
        Task<(bool success, Dictionary<string, object> data)> LoadAsync(string playerId, string token = null);
    }
}


