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
}

