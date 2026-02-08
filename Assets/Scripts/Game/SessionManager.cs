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

    private readonly Dictionary<uint, PlayerMatchStats> matchStats = new();

    [SerializeField] private string nextSceneName = "TestMultiplayerScene";

    private enum MatchState { Waiting, Playing, Finished }
    private MatchState currentState = MatchState.Waiting;

    private const float MatchDuration = 20f;
    private const float EndingDuration = 30f;
    private float stateTimer = 0f;

    private double matchStartTime;
    private double endingStartTime;
    private int syncedState;

    private const int STATE_WAITING = 0;
    private const int STATE_PLAYING = 1;
    private const int STATE_ENDING = 2;

    private int _port;
    private string _ip;
    private int _maxPlayers;
    private bool _endingPhase;

    private class PlayerMatchStats
    {
        public uint NetId;
        public string Nickname;
        public int PlayerId;
        public int Kills;
        public int Deaths;
        public bool Left;
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
        _port = GetArgInt("-port", 7777);
        _ip = GetArg("-ip", "auto");
        _maxPlayers = GetArgInt("-maxPlayers", 20);

        if (Transport.active is KcpTransport kcp)
        {
            kcp.Port = (ushort)_port;
            Debug.Log($"[SessionManager] Port set to {_port}");
        }

        NetworkManager.singleton.StartServer();
        Debug.Log("[SessionManager] NetworkManager Server Started.");

        NetworkManager.singleton.ServerChangeScene(nextSceneName);

        await Task.Delay(1000);
        await RegisterGameServerAsync(_port, _ip, _maxPlayers);
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
        if (!matchStats.TryGetValue(player.netId, out var s))
        {
            s = new PlayerMatchStats
            {
                NetId = player.netId,
                PlayerId = player.ServerPlayerId,
                Nickname = player.Nickname,
                Kills = player.Kills,
                Deaths = player.Deaths,
                Left = false
            };
            matchStats[player.netId] = s;
        }
        else
        {
            s.Kills = player.Kills;
            s.Deaths = player.Deaths;
            s.Left = false;
        }

        Debug.Log($"[SessionManager] Player connected netId={player.netId} id={player.ServerPlayerId} nick={player.Nickname}");
        player.TargetSetMatchTimer(player.connectionToClient, syncedState, matchStartTime, endingStartTime);
        if (Player.ActivePlayers.Count == 1 && currentState == MatchState.Waiting)
        {
            StartCoroutine(MatchTimerCoroutine());
            Debug.Log("[SessionManager] Match Started!");
        }
    }

    [Server]
    public void BindIdentity(Player player)
    {
        if (player == null) return;

        if (!matchStats.TryGetValue(player.netId, out var s))
        {
            s = new PlayerMatchStats { NetId = player.netId };
            matchStats[player.netId] = s;
        }

        s.PlayerId = player.ServerPlayerId;
        s.Nickname = player.Nickname;

        if (s.PlayerId > 0)
            _ = NotifyPlayerJoined(s.PlayerId);
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
        syncedState = STATE_PLAYING;
        matchStartTime = NetworkTime.time;
        stateTimer = MatchDuration;
        endingStartTime = 0;
        BroadcastTimer();

        while (stateTimer > 0)
        {
            yield return new WaitForSeconds(1f);
            stateTimer -= 1f;
            if ((int)(MatchDuration - stateTimer) % 10 == 0) SendHealthcheck();
        }

        _endingPhase = true;
        syncedState = STATE_ENDING;
        endingStartTime = NetworkTime.time;
        stateTimer = EndingDuration;
        BroadcastTimer();

        Debug.Log("[SessionManager] Match Ending...");
        foreach (var player in Player.ActivePlayers.Values) player.RpcShowEndMatchLeaderboard();

        while (stateTimer > 0)
        {
            yield return new WaitForSeconds(1f);
            stateTimer -= 1f;
            if ((int)(EndingDuration - stateTimer) % 10 == 0) SendHealthcheck();
        }

        _endingPhase = false;

        Debug.Log("[SessionManager] Match Over. Restarting.");


        foreach (var p in Player.ActivePlayers.Values)
        {
            if (matchStats.TryGetValue(p.netId, out var s))
            {
                s.Kills = p.Kills;
                s.Deaths = p.Deaths;
                s.PlayerId = p.ServerPlayerId;
                s.Nickname = p.Nickname;
                s.Left = false;
            }
        }
      

        Debug.Log("[Result] BEFORE SEND: " + string.Join(", ",
            matchStats.Values.Select(s => $"{s.PlayerId}:{s.Kills}/{s.Deaths}")));
        yield return SendResultsCoroutine();
        currentState = MatchState.Finished;
        SendHealthcheck();

        RestartServer();
        yield return new WaitForSeconds(1f);
        _ = RegisterGameServerAsync(_port, _ip, _maxPlayers);
    }

    [Server]
    private IEnumerator SendResultsCoroutine()
    {
        var task = SendResultsToServer();
        while (!task.IsCompleted) yield return null;
    }

    [Server]
    public void DisconnectPlayer(Player player)
    {
        if (player == null) return;
        if (matchStats.TryGetValue(player.netId, out var s))
        {
            s.Kills = player.Kills;
            s.Deaths = player.Deaths;
            s.Left = true;
        }
    }

    [Server]
    private async Task SendResultsToServer()
    {
        if (matchStats.Count == 0 || GameData.Instance == null)
        {
            Debug.LogWarning("[SessionManager] No match stats to send or GameData is null. Skipping results submission.");
            return;
        }

        var snapshot = matchStats.Values
            .Where(s => s.PlayerId > 0) 
            .Select(s => new GameData.PlayerResult
            {
                PlayerId = s.PlayerId,
                Kills = s.Kills,
                Deaths = s.Deaths
            })
            .ToArray();

        Debug.Log($"[Result] Sending Results... session={GameData.Instance.SessionId} players={snapshot.Length}");


        var payload = new GameData.GameResultRequest
        {
            SessionId = GameData.Instance.SessionId,
            Leaderboard = snapshot
        };

        Debug.Log("Sending Results...");
        try
        {
            await APINetworkManager.Instance.PostRequestAsync<object>("/games/result", payload);
        }
        catch (Exception ex) { Debug.LogError($"[API] Result Error: {ex.Message}"); }
        finally
        {
            matchStats.Clear();
        }
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

        string gameTime;
        if (_endingPhase)
            gameTime = FormatTime(EndingDuration - stateTimer + MatchDuration);
        else
            gameTime = FormatTime(MatchDuration - stateTimer);

        int[] playerIds = (Player.ActivePlayers != null)
            ? Player.ActivePlayers.Values.Select(p => p.ServerPlayerId).ToArray()
            : new int[0];

        string stateString = _endingPhase ? "playing" : currentState.ToString().ToLower();

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

        foreach (var kv in NetworkServer.connections)
        {
            var conn = kv.Value;
            if (conn != null && conn.identity != null)
            {
                NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.Destroy);
            }
        }

        currentState = MatchState.Waiting;
        stateTimer = 0f;
        _endingPhase = false;
        syncedState = STATE_WAITING;
        matchStartTime = 0;
        endingStartTime = 0;
        BroadcastTimer();
        NetworkManager.singleton.ServerChangeScene(nextSceneName);
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


    private void BroadcastTimer()
    {
        foreach (var p in Player.ActivePlayers.Values)
        {
            if (p != null)
                p.TargetSetMatchTimer(p.connectionToClient, syncedState, matchStartTime, endingStartTime);
        }
    }

    public void SendTimerTo(Player player)
    {
        if (!NetworkServer.active) return;
        if (player == null || player.connectionToClient == null) return;

        player.TargetSetMatchTimer(player.connectionToClient, syncedState, matchStartTime, endingStartTime);
    }


}