using System.Threading.Tasks;

namespace Core.Services
{
    public interface IPlayerAccountService
    {
        // Auth
        Task<(bool success, string message, string playerId, string token)> RegisterUser(string username, string password);
        Task<(bool success, string message, string playerId, string token)> LoginUser(string username, string password);

        // Data
        Task<(bool success, string message)> SavePlayerAccount(string playerId, int coins, int level, int xp, string token = null);
        Task<(bool success, string data)> GetPlayerAccount(string playerId, string token = null);
    }
}
