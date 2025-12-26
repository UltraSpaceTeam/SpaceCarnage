using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    public int PlayerId { get; private set; }
    public int SessionId { get; private set; }
    public string Token { get; private set; }

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

    // Вызывай после успешного логина
    public void SetPlayerData(int playerId, string token, string nickname)
    {
        PlayerId = playerId;
        Token = token;
        Debug.Log($"Сохранён PlayerId = {playerId}");
    }

    // Вызывай после получения SessionId (из /games/register)
    public void SetSessionId(int sessionId)
    {
        SessionId = sessionId;
        Debug.Log($"Сохранён SessionId = {sessionId}");
    }
}