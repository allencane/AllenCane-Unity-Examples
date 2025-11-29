using System;

namespace Core.Services.Azure
{
    [Serializable]
    public class PlayerAccountRequest
    {
        public int coins;
        public int level;
        public int xp;
    }

    [Serializable]
    public class PlayerAccountResponse
    {
        public bool success;
        public string message;
    }

    // --- AUTH MODELS ---

    [Serializable]
    public class AuthRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class AuthResponse
    {
        public bool success;
        public string message;
        public string playerId;
        public string token; // Optional, for Login
    }
}
