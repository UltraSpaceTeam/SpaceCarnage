using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.SceneManagement;
using kcp2k;
using System;

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

    private Coroutine matchCoroutine;

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
        if (Application.isBatchMode || GetArg("-autoStart", "false") == "true")
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

        await Task.Yield();
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

        var response = await APINetworkManager.Instance.PostRequestAsync<GameData.GameServerRegisterResponse>("/games/register", payload);

        if (response != null)
        {
            Debug.Log($"[API] Registered! SessionId = {response.sessionId}");
            GameData.Instance?.SetSessionData(response.sessionId, response.key);

            StartCoroutine(HeartbeatLoop());

            return response.sessionId;
        }

        Debug.LogError("Registration failed.");
        return -1;
    }

    private IEnumerator HeartbeatLoop()
    {
        Debug.Log("[SessionManager] Heartbeat loop started.");
        while (true)
        {
            SendHealthcheck();
            yield return new WaitForSeconds(10f);
        }
    }

    [Server]
    public void ConnectPlayer(Player player)
    {
        if (player == null) return;
        int pId = player.ServerPlayerId;

        var existing = matchStats.Find(s => s.PlayerId == pId);
        if (existing == null) matchStats.Add(new PlayerMatchStats { PlayerId = pId, Kills = player.Kills, Deaths = player.Deaths });

        _ = NotifyPlayerJoined(pId);

        if (Player.ActivePlayers.Count == 1 && currentState == MatchState.Waiting)
        {
            matchCoroutine = StartCoroutine(MatchTimerCoroutine());
            Debug.Log("[SessionManager] Match Started!");
        }

        Debug.Log($"[SessionManager] Player connected: {player.Nickname} (ID: {pId})");
    }

    [Server]
    public void DisconnectPlayer(Player player)
    {
        if (player == null) return;
        var stats = matchStats.Find(s => s.PlayerId == player.ServerPlayerId);
        if (stats != null) { stats.Kills = player.Kills; stats.Deaths = player.Deaths; }

        if (Player.ActivePlayers.Count <= 1 && currentState != MatchState.Waiting)
        {
            Debug.Log("[SessionManager] Room is empty or last player left. Resetting session.");
            if (matchCoroutine != null) StopCoroutine(matchCoroutine);
            _ = RestartServer();
        }
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
        }

        currentState = MatchState.Finished;
        stateTimer = EndingDuration;

        Debug.Log("[SessionManager] Match Ending...");
        foreach (var p in Player.ActivePlayers.Values) p.RpcShowEndMatchLeaderboard();

        while (stateTimer > 0)
        {
            yield return new WaitForSeconds(1f);
            stateTimer -= 1f;
        }

        Debug.Log("[SessionManager] Match Over. Sending results and restarting.");

        foreach (var kvp in Player.ActivePlayers)
        {
            var p = kvp.Value;
            var s = matchStats.Find(x => x.PlayerId == p.ServerPlayerId);
            if (s != null) { s.Kills = p.Kills; s.Deaths = p.Deaths; }
        }

        RestartServer();
    }

    [Server]
    private void SendHealthcheck()
    {
        if (GameData.Instance == null || GameData.Instance.SessionId == 0) return;

        float elapsed = (currentState == MatchState.Waiting) ? 0 :
                        (currentState == MatchState.Playing) ? (MatchDuration - stateTimer) :
                        (MatchDuration + (EndingDuration - stateTimer));

        string gameTime = FormatTime(elapsed);
        int[] playerIds = Player.ActivePlayers.Values.Select(p => p.ServerPlayerId).ToArray();

        var payload = new GameData.HealthcheckRequest
        {
            SessionId = GameData.Instance.SessionId,
            State = currentState.ToString().ToLower(),
            Time = gameTime,
            Players = playerIds
        };

        _ = APINetworkManager.Instance.PostRequestAsync<object>("/games/healthcheck", payload);
    }

    [Server]
    private async Task RestartServer()
    {
        await SendResultsToServer();

        foreach (var player in Player.ActivePlayers.Values)
            player.RpcHideEndMatchLeaderboard();

        matchStats.Clear();
        currentState = MatchState.Waiting;
        stateTimer = 0;

        Debug.Log("[SessionManager] Restarting scene now...");
        SceneManager.LoadScene(nextSceneName);
    }


    private async Task NotifyPlayerJoined(int playerId)
    {
        if (GameData.Instance == null || GameData.Instance.SessionId == 0) return;
        var payload = new GameData.PlayerJoinedRequest { SessionId = GameData.Instance.SessionId, PlayerId = playerId };
        try { await APINetworkManager.Instance.PostRequestAsync<object>("/games/player_joined", payload); }
        catch (Exception ex) { Debug.LogError($"[API] PlayerJoined Error: {ex.Message}"); }
    }

    [Server]
    private async Task SendResultsToServer()
    {
        foreach (var kvp in Player.ActivePlayers)
        {
            var playerObj = kvp.Value;
            var stats = matchStats.Find(s => s.PlayerId == playerObj.ServerPlayerId);

            if (stats != null)
            {
                stats.Kills = playerObj.Kills;
                stats.Deaths = playerObj.Deaths;
            }
        }

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

        Debug.Log($"[API] Sending results to server. Players: {matchStats.Count}");

        try
        {
            await APINetworkManager.Instance.PostRequestAsync<object>("/games/result", payload);
            Debug.Log("[API] Results submitted successfully!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[API] Result Error: {ex.Message}");
        }

        matchStats.Clear();
    }

    private string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60);
        int seconds = Mathf.FloorToInt(totalSeconds % 60);
        return $"{minutes:D2}:{seconds:D2}";
    }

    private string GetArg(string name, string defaultValue)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++) if (args[i] == name && args.Length > i + 1) return args[i + 1];
        return defaultValue;
    }

    private int GetArgInt(string name, int defaultValue)
    {
        string str = GetArg(name, defaultValue.ToString());
        return int.TryParse(str, out int val) ? val : defaultValue;
    }
}