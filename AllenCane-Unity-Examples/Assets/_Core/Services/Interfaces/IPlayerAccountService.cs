using System.Threading.Tasks;

namespace Core.Services
{
    public interface IPlayerAccountService
    {
        Task<(bool success, string message)> SavePlayerAccount(string playerId, int coins, int level, int xp);
        Task<(bool success, string data)> GetPlayerAccount(string playerId);
    }
}

