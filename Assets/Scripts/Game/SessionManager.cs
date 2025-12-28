using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Network;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    private List<PlayerMatchStats> matchStats = new List<PlayerMatchStats>();

    [SerializeField] private string nextSceneName = "TestMultiplayerScene";

    private enum MatchState
    {
        Waiting,
        Playing,
        Ending
    }

    private MatchState currentState = MatchState.Waiting;

    private const float MatchDuration = 600f;
    private const float EndingDuration = 30f;
    private float stateTimer = 0f;

    private class PlayerMatchStats
    {
        public int PlayerId;
        public int Kills;
        public int Deaths;
    }

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

    [Server]
    public async Task<int> RegisterGameServerAsync(int port = 7777, string ipAddress = "auto", int maxPlayers = 20)
    {
        string ip = ipAddress;
        if (string.IsNullOrEmpty(ip) || ip == "auto")
        {
            ip = "127.0.0.1";
            Debug.LogWarning("[APINetworkManager] ipAddress = 'auto' - using 127.0.0.1.");
        }

        var payload = new GameData.GameServerRegisterRequest
        {
            port = port,
            ipAddress = ip,
            maxPlayers = maxPlayers
        };

        Debug.Log($"[APINetworkManager] Register server: port={port}, ip={ip}, maxPlayers={maxPlayers}");

        var response = await APINetworkManager.Instance.PostRequestAsync<GameData.GameServerRegisterResponse>("/games/register", payload);

        Debug.Log($"[APINetworkManager] Server registered successfully! SessionId = {response.sessionId}, Key = {response.key}");

        if (GameData.Instance != null)
        {
            GameData.Instance.SetSessionId(response.sessionId);
        }
        else
        {
            Debug.LogError("[APINetworkManager] GameData.Instance == null! SessionId not saved, but returning from response.");
        }

        return response.sessionId;
    }

    [Server]
    public void ConnectPlayer(Player player)
    {
        if (player == null) return;

        // If reconnect
        var existing = matchStats.Find(s => s.PlayerId == player.ServerPlayerId);
        if (existing == null)
        {
            matchStats.Add(new PlayerMatchStats
            {
                PlayerId = player.ServerPlayerId,
                Kills = player.Kills,
                Deaths = player.Deaths
            });
        }
        else
        {
            existing.Kills = player.Kills;
            existing.Deaths = player.Deaths;
        }

        if (Player.ActivePlayers.Count == 1)
        {
            StartCoroutine(MatchTimerCoroutine());
            Debug.Log("[SessionManager] First player connected - match started! Duration: 10 minutes.");
        }

        Debug.Log($"[SessionManager] Player connected: {player.Nickname} (ID: {player.ServerPlayerId})");
    }

    [Server]
    private IEnumerator MatchTimerCoroutine()
    {
        currentState = MatchState.Playing;
        stateTimer = MatchDuration;

        while (stateTimer > 0)
        {
            yield return new WaitForSeconds(1f);
            stateTimer -= 1f;

            if ((int)(MatchDuration - stateTimer) % 10 == 0)
            {
                SendHealthcheck();
            }
        }

        currentState = MatchState.Ending;
        stateTimer = EndingDuration;

        Debug.Log("[SessionManager] Match is ending. Showing tab for 30 seconds...");

        // Forced showing the leaderboard to ALL players
        foreach (var player in Player.ActivePlayers.Values)
        {
            player.RpcShowEndMatchLeaderboard();
        }

        while (stateTimer > 0)
        {
            yield return new WaitForSeconds(1f);
            stateTimer -= 1f;

            if ((int)(EndingDuration - stateTimer) % 10 == 0)
            {
                SendHealthcheck();
            }
        }

        Debug.Log("[SessionManager] Time is up. Sending final results and restarting...");

        // Updating stats before sending
        foreach (var kvp in Player.ActivePlayers)
        {
            var player = kvp.Value;
            var stats = matchStats.Find(s => s.PlayerId == player.ServerPlayerId);
            if (stats != null)
            {
                stats.Kills = player.Kills;
                stats.Deaths = player.Deaths;
            }
        }

        SendResultsToServer();

        RestartServer();
    }

    [Server]
    public void DisconnectPlayer(Player player)
    {
        if (player == null) return;

        var stats = matchStats.Find(s => s.PlayerId == player.ServerPlayerId);
        if (stats != null)
        {
            stats.Kills = player.Kills;
            stats.Deaths = player.Deaths;
            Debug.Log($"[SessionManager] Updated {player.Nickname} stats: Kills {stats.Kills}, Deaths {stats.Deaths}");
        }
    }

    [Server]
    private async void SendResultsToServer()
    {
        if (matchStats.Count == 0)
        {
            Debug.LogWarning("[SessionManager] No data to send.");
            return;
        }

        if (GameData.Instance == null)
        {
            Debug.LogError("[SessionManager] GameData not found!");
            return;
        }

        string requestData = JsonUtility.ToJson(new
        {
            sessionId = GameData.Instance.SessionId,
            leaderboard = matchStats.Select(s => new
            {
                playerId = s.PlayerId,
                kills = s.Kills,
                deaths = s.Deaths
            }).ToArray()
        });

        Debug.Log("Sending Results Request...");

        AuthResponse response = null;
        response = await APINetworkManager.Instance.PostRequestAsync<AuthResponse>("/games/result", requestData);
        matchStats.Clear();
    }

    [Server]
    private async void SendHealthcheck()
    {
        if (GameData.Instance == null || GameData.Instance.SessionId == 0)
        {
            Debug.LogWarning("[SessionManager] SessionID is not set - healthcheck has not been sent.");
            return;
        }

        string gameTime = FormatTime(MatchDuration - stateTimer);

        if (currentState == MatchState.Ending)
        {
            gameTime = FormatTime(EndingDuration - stateTimer + MatchDuration);
        }

        var playerIds = Player.ActivePlayers.Values
            .Select(p => p.ServerPlayerId)
            .ToList();

        string requestData = JsonUtility.ToJson(new
        {
            sessionId = GameData.Instance.SessionId,
            state = currentState.ToString(),
            time = gameTime,
            players = playerIds.ToArray()
        });

        Debug.Log($"[Healthcheck] Sending: {requestData}");

        try
        {
            var response = await APINetworkManager.Instance.PostRequestAsync<AuthResponse>("/games/healthcheck", requestData);
            Debug.Log("[Healthcheck] Sended successfully.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Healthcheck] Sending error: {ex.Message}");
        }
    }

    private string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60);
        int seconds = Mathf.FloorToInt(totalSeconds % 60);
        return $"{minutes:D2}:{seconds:D2}:00";
    }

    [Server]
    private void RestartServer()
    {
        Debug.Log("[SessionManager] Match ended. Returning all players to lobby...");

        foreach (var player in Player.ActivePlayers.Values)
        {
            player.RpcHideEndMatchLeaderboard();
        }

        matchStats.Clear();
        currentState = MatchState.Waiting;

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}