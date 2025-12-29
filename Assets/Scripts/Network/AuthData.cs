using System;

[Serializable]
public class AuthRequest
{
    public string username;
    public string password;
}

[Serializable]
public class AuthResponse
{
    public string token;
    public int playerId;
    public string username;
}

[Serializable]
public class ErrorResponse
{
    public string error;
}
