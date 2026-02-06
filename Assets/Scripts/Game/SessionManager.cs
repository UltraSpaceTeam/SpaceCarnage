using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.SceneManagement;
using kcp2k;
using System;
using UnityEngine.Networking;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    private List<PlayerMatchStats> matchStats = new List<PlayerMatchStats>();

    [SerializeField] private string nextSceneName = "TestMultiplayerScene";

    private enum MatchState { Waiting, Playing, Finished }
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
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        bool isHeadless = Application.isBatchMode;
        bool forceAuto = GetArg("-autoStart", "false") == "true";

        if (isHeadless || forceAuto)
        {
            _ = StartDedicatedServer();
        }
    }

    private async Task StartDedicatedServer()
    {
        Debug.Log(">>> STARTING DEDICATED SERVER <<<");
        int port = GetArgInt("-port", 7777);
        string ip = GetArg("-ip", "auto");
        int maxPlayers = GetArgInt("-maxPlayers", 20);

        if (Transport.active is KcpTransport kcp)
        {
            kcp.Port = (ushort)port;
            Debug.Log($"[SessionManager] Port set to {port}");
        }

        NetworkManager.singleton.StartServer();
        Debug.Log("[SessionManager] NetworkManager Server Started.");

        NetworkManager.singleton.ServerChangeScene(nextSceneName);

        await Task.Delay(1000);
        await RegisterGameServerAsync(port, ip, maxPlayers);
    }

    public async Task<int> RegisterGameServerAsync(int port = 7777, string ipAddress = "auto", int maxPlayers = 20)
    {
        string ip = ipAddress;
        if (string.IsNullOrEmpty(ip) || ip == "auto") ip = "127.0.0.1";

        var payload = new GameData.GameServerRegisterRequest
        {
            Port = port,
            IpAddress = ip,
            MaxPlayers = maxPlayers
        };

        Debug.Log($"[API] Registering: Port={port}, IP={ip}");

        var response = await APINetworkManager.Instance.PostRequestAsync<GameData.GameServerRegisterResponse>("/games/register", payload);

        if (response != null)
        {
            Debug.Log($"[API] Registered! SessionId = {response.sessionId}");

            if (GameData.Instance != null)
            {
                GameData.Instance.SetSessionData(response.sessionId, response.key);
            }

            StartCoroutine(WaitingForPlayersCoroutine());

            return response.sessionId;
        }

        Debug.LogError("Registration failed.");
        return -1;
    }

    private IEnumerator WaitingForPlayersCoroutine()
    {
        Debug.Log("[SessionManager] Waiting for players... Sending Heartbeats.");
        currentState = MatchState.Waiting;

        while (currentState == MatchState.Waiting)
        {
            Debug.Log("[SessionManager] Waiting for players... Sending Heartbeats. 2");

            SendHealthcheck();
            yield return new WaitForSeconds(10f);
        }
    }

    [Server]
    public void ConnectPlayer(Player player)
    {
        if (player == null) return;
        int pId = player.ServerPlayerId;

        var existing = matchStats.Find(s => s.PlayerId == player.ServerPlayerId);
        if (existing == null) matchStats.Add(new PlayerMatchStats { PlayerId = pId, Kills = player.Kills, Deaths = player.Deaths });
        else { existing.Kills = player.Kills; existing.Deaths = player.Deaths; }

        _ = NotifyPlayerJoined(pId);

        if (Player.ActivePlayers.Count == 1 && currentState == MatchState.Waiting)
        {
            StartCoroutine(MatchTimerCoroutine());
            Debug.Log("[SessionManager] Match Started!");
        }

        Debug.Log($"[SessionManager] Player connected: {player.Nickname} (ID: {pId})");
    }

    private async Task NotifyPlayerJoined(int playerId)
    {
        if (GameData.Instance == null || GameData.Instance.SessionId == 0) return;

        var payload = new GameData.PlayerJoinedRequest
        {
            SessionId = GameData.Instance.SessionId,
            PlayerId = playerId
        };

        try
        {
            await APINetworkManager.Instance.PostRequestAsync<object>("/games/player_joined", payload);
        }
        catch (System.Exception ex) { Debug.LogError($"[API] PlayerJoined Error: {ex.Message}"); }
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
            if ((int)(MatchDuration - stateTimer) % 10 == 0) SendHealthcheck();
        }

        currentState = MatchState.Finished;
        stateTimer = EndingDuration;

        Debug.Log("[SessionManager] Match Ending...");
        foreach (var player in Player.ActivePlayers.Values) player.RpcShowEndMatchLeaderboard();

        while (stateTimer > 0)
        {
            yield return new WaitForSeconds(1f);
            stateTimer -= 1f;
            if ((int)(EndingDuration - stateTimer) % 10 == 0) SendHealthcheck();
        }

        Debug.Log("[SessionManager] Match Over. Restarting.");

        foreach (var kvp in Player.ActivePlayers)
        {
            var p = kvp.Value;
            var s = matchStats.Find(x => x.PlayerId == p.ServerPlayerId);
            if (s != null) { s.Kills = p.Kills; s.Deaths = p.Deaths; }
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
        }
    }

    [Server]
    private async void SendResultsToServer()
    {
        if (matchStats.Count == 0 || GameData.Instance == null) return;

        var payload = new GameData.GameResultRequest
        {
            SessionId = GameData.Instance.SessionId,
            Leaderboard = matchStats.Select(s => new GameData.PlayerResult
            {
                PlayerId = s.PlayerId,
                Kills = s.Kills,
                Deaths = s.Deaths
            }).ToArray()
        };

        Debug.Log("Sending Results...");
        try
        {
            await APINetworkManager.Instance.PostRequestAsync<object>("/games/result", payload);
        }
        catch (Exception ex) { Debug.LogError($"[API] Result Error: {ex.Message}"); }

        matchStats.Clear();
    }

    [Server]
    private async void SendHealthcheck()
    {
        Debug.Log($"[Heartbeat] tick. GameData={(GameData.Instance == null ? "NULL" : "OK")} SessionId={(GameData.Instance?.SessionId ?? -1)}");

        if (GameData.Instance == null || GameData.Instance.SessionId == 0)
        {
            Debug.LogWarning("[Heartbeat] early return (no GameData or SessionId==0)");
            return;
        }

        Debug.Log("[Heartbeat] sending /games/healthcheck ...");

        string gameTime = FormatTime(MatchDuration - stateTimer);
        if (currentState == MatchState.Finished) gameTime = FormatTime(EndingDuration - stateTimer + MatchDuration);

        int[] playerIds = (Player.ActivePlayers != null)
            ? Player.ActivePlayers.Values.Select(p => p.ServerPlayerId).ToArray()
            : new int[0];

        string stateString = currentState.ToString().ToLower();

        var payload = new GameData.HealthcheckRequest
        {
            SessionId = GameData.Instance.SessionId,
            State = stateString,
            Time = gameTime,
            Players = playerIds
        };

        try
        {
            await APINetworkManager.Instance.PostRequestAsync<object>("/games/healthcheck", payload);
            Debug.Log("[Heartbeat] SUCCESS!");
        }
        catch (System.Exception ex) { Debug.LogError($"[Heartbeat] FAILED: {ex.Message}"); }
    }

    private string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60);
        int seconds = Mathf.FloorToInt(totalSeconds % 60);
        return $"{minutes:D2}:{seconds:D2}";
    }

    [Server]
    private void RestartServer()
    {
        foreach (var player in Player.ActivePlayers.Values) player.RpcHideEndMatchLeaderboard();
        matchStats.Clear();
        currentState = MatchState.Waiting;
        SceneManager.LoadScene(nextSceneName);
    }

    private string GetArg(string name, string defaultValue)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
            if (args[i] == name && args.Length > i + 1) return args[i + 1];
        return defaultValue;
    }

    private int GetArgInt(string name, int defaultValue)
    {
        string str = GetArg(name, defaultValue.ToString());
        if (int.TryParse(str, out int val)) return val;
        return defaultValue;
    }
}