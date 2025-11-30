using System;

namespace Core.Services.Azure
{
    [Serializable]
    public class PlayerAccountRequest
    {
    // Use standard, backend-agnostic naming conventions for auth models
        public int PlayerLevel;
        public int ExperiencePoints;
        public int Coins; // Or WalletJson if we want to be advanced later
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
        public string token;
    }
}
