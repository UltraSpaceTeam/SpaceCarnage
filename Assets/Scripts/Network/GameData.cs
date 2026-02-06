using Mirror;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameData : MonoBehaviour
{
    [Serializable]
    public class GameServerRegisterRequest
    {
        public int Port;
        public string IpAddress;
        public int MaxPlayers;
    }

    [Serializable]
    public class GameServerRegisterResponse
    {
        public int sessionId;
        public string key;
    }
    [System.Serializable]
    public class PlayerJoinedRequest
    {
        public int SessionId;
        public int PlayerId;
    }
    [Serializable]
    public class GameResultRequest
    {
        public int SessionId;
        public PlayerResult[] Leaderboard;
    }

    [Serializable]
    public class PlayerResult
    {
        public int PlayerId;
        public int Kills;
        public int Deaths;
    }

    [Serializable]
    public class HealthcheckRequest
    {
        public int SessionId;
        public string State;
        public string Time;
        public int[] Players;
    }

    public static GameData Instance { get; private set; }

    public int SessionId { get; private set; }
    public string SessionKey { get; private set; }
    public int PlayerId { get; private set; }
    public string Token { get; private set; }
    public string PlayerNickname { get; private set; }

    private void Awake()
    {
        Debug.Log($"[GameData] Awake InstanceId={GetInstanceID()} scene={gameObject.scene.name}");
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

    public void SetSessionData(int sessionId, string key)
    {
        SessionId = sessionId;
        SessionKey = key;
        Debug.Log($"[GameData] Saved SessionId = {sessionId}, Key={key}, InstanceId={GetInstanceID()}");
    }
}