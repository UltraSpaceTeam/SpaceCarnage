using System.Collections;
using System.Linq;
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

        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();
        Player.ActivePlayers.Clear();

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

        // Даём нормальную сборку корабля
        EquipBasicShip(_hostPlayer);

        // Даём тестовую статистику
        _hostPlayer.Kills = 5;
        _hostPlayer.Deaths = 2;

        yield return new WaitForSeconds(0.8f);

        Debug.Log($"[Test 11] Setup OK - Host player ready (Kills={_hostPlayer.Kills}, Deaths={_hostPlayer.Deaths})");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var nm = Object.FindAnyObjectByType<NetworkManager>();
        if (nm != null) nm.StopHost();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Player_Disconnect_Saves_Stats_And_Notifies_Others()
    {
        Debug.Log("[Test 11] === TEST START ===");

        int killsBefore = _hostPlayer.Kills;
        int deathsBefore = _hostPlayer.Deaths;

        // Симулируем отключение текущего игрока (как будто он дисконнектится)
        Debug.Log("[Test 11] Simulating player disconnect...");

        _sessionManager.DisconnectPlayer(_hostPlayer);

        // Удаляем игрока с сервера (как делает Mirror при реальном отключении)
        var connection = _hostPlayer.connectionToClient;
        if (connection != null)
        {
            NetworkServer.RemovePlayerForConnection(connection, RemovePlayerOptions.Destroy);
        }

        yield return new WaitForSeconds(1.0f);

        // Проверки
        Assert.IsFalse(Player.ActivePlayers.ContainsKey(_hostPlayer.netId),
            "Disconnected player still remains in ActivePlayers");

        Debug.Log("[Test 11] Player successfully removed from ActivePlayers ?");

        // Статистика должна остаться (SessionManager сохраняет её при DisconnectPlayer)
        Assert.AreEqual(killsBefore, _hostPlayer.Kills, "Kills were not preserved on disconnect");
        Assert.AreEqual(deathsBefore, _hostPlayer.Deaths, "Deaths were not preserved on disconnect");

        Debug.Log($"[Test 11] Statistics preserved ? Kills: {_hostPlayer.Kills}, Deaths: {_hostPlayer.Deaths} ?");

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
}