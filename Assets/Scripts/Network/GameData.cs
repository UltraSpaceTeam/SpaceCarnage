using Mirror;
using Network;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class GameData : MonoBehaviour
{
    [Serializable]
    public class GameServerRegisterRequest
    {
        public int port;
        public string ipAddress;
        public int maxPlayers;
    }

    [Serializable]
    public class GameServerRegisterResponse
    {
        public int sessionId;
        public string key;
    }

    public static GameData Instance { get; private set; }

    public int SessionId { get; private set; }
    public int PlayerId { get; private set; }
    public string Token { get; private set; }
    public string PlayerNickname { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetPlayerData(int playerId, string token, string nickname)
    {
        PlayerId = playerId;
        Token = token;
        PlayerNickname = nickname;

        Debug.Log($"Saved PlayerId = {playerId}, Nickname = {nickname}");
    }

    public void SetSessionId(int sessionId)
    {
        SessionId = sessionId;
        Debug.Log($"Saved SessionId = {sessionId}");
    }
}