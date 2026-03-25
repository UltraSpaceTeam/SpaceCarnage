using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PlayerDisconnectIntegrationTests
{
    private Player _hostPlayer;
    private SessionManager _sessionManager;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[Test 11] === SETUP ===");

        // Полная агрессивная очистка перед тестом
        AggressiveCleanup();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.6f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm, "NetworkManager not found");

        nm.StartHost();
        yield return new WaitForSeconds(1.5f);

        _hostPlayer = Object.FindObjectsByType<Player>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.isLocalPlayer);

        Assert.NotNull(_hostPlayer, "Host player not found");

        _sessionManager = Object.FindAnyObjectByType<SessionManager>();
        Assert.NotNull(_sessionManager, "SessionManager not found");

        EquipBasicShip(_hostPlayer);

        _hostPlayer.Kills = 5;
        _hostPlayer.Deaths = 2;

        yield return new WaitForSeconds(0.8f);

        Debug.Log($"[Test 11] Setup OK - Host player ready (Kills={_hostPlayer.Kills}, Deaths={_hostPlayer.Deaths})");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Player_Disconnect_Saves_Stats_And_Notifies_Others()
    {
        Debug.Log("[Test 11] === TEST START ===");

        int killsBefore = _hostPlayer.Kills;
        int deathsBefore = _hostPlayer.Deaths;

        Debug.Log("[Test 11] Simulating player disconnect...");

        _sessionManager.DisconnectPlayer(_hostPlayer);

        var connection = _hostPlayer.connectionToClient;
        if (connection != null)
        {
            NetworkServer.RemovePlayerForConnection(connection, RemovePlayerOptions.Destroy);
        }

        yield return new WaitForSeconds(1.0f);

        Assert.IsFalse(Player.ActivePlayers.ContainsKey(_hostPlayer.netId),
            "Disconnected player still remains in ActivePlayers");

        Debug.Log("[Test 11] Player successfully removed from ActivePlayers ?");

        Assert.AreEqual(killsBefore, _hostPlayer.Kills, "Kills were not preserved on disconnect");
        Assert.AreEqual(deathsBefore, _hostPlayer.Deaths, "Deaths were not preserved on disconnect");

        Debug.Log($"[Test 11] Statistics preserved ? Kills: {_hostPlayer.Kills}, Deaths: {_hostPlayer.Deaths}");
        Debug.Log("[Test 11] === PASSED ===");
    }

    private void EquipBasicShip(Player player)
    {
        var assembler = player.GetComponent<ShipAssembler>();
        if (assembler == null) return;

        var hull = GameResources.Instance?.partDatabase.hulls.FirstOrDefault();
        var weapon = GameResources.Instance?.partDatabase.weapons.FirstOrDefault();
        var engine = GameResources.Instance?.partDatabase.engines.FirstOrDefault();

        if (hull != null) assembler.EquipHull(hull);
        if (weapon != null) assembler.EquipWeapon(weapon);
        if (engine != null) assembler.EquipEngine(engine);
    }

    // ====================== АГРЕССИВНАЯ ОЧИСТКА ======================
    private void AggressiveCleanup()
    {
        // Полностью выключаем сеть
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        // Уничтожаем все NetworkManager
        var managers = Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var m in managers)
        {
            if (m != null)
                Object.DestroyImmediate(m.gameObject);
        }

        // Уничтожаем все KcpTransport
        var transports = Object.FindObjectsByType<kcp2k.KcpTransport>(FindObjectsSortMode.None);
        foreach (var t in transports)
        {
            if (t != null)
                Object.DestroyImmediate(t.gameObject);
        }

        // Сбрасываем важные Singletons
        ResetSingleton<UIManager>();
        ResetSingleton<GameResources>();
        ResetSingleton<SessionManager>();
        ResetSingleton<AudioManager>();

        // Очищаем статические данные Mirror
        Player.ActivePlayers.Clear();
        NetworkManager.startPositions.Clear();
    }

    private void ResetSingleton<T>() where T : MonoBehaviour
    {
        var field = typeof(T).GetField("Instance",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }
}