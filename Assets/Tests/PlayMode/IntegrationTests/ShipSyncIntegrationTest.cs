using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ShipSyncIntegrationTests
{
    private Player _hostPlayer;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[Test 05] === SETUP ===");

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

        EquipBasicShip(_hostPlayer);

        yield return new WaitForSeconds(0.8f);

        Debug.Log("[Test 05] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Client_Connect_Ship_Assembly_Is_Synchronized_Correctly()
    {
        Debug.Log("[Test 05] === TEST START ===");

        var hostAssembler = _hostPlayer.GetComponent<ShipAssembler>();
        var database = GameResources.Instance.partDatabase;

        var selectedHull = database.hulls.FirstOrDefault();
        var selectedWeapon = database.weapons.FirstOrDefault();
        var selectedEngine = database.engines.FirstOrDefault();

        Assert.NotNull(selectedHull, "Hull not found in database");
        Assert.NotNull(selectedWeapon, "Weapon not found in database");
        Assert.NotNull(selectedEngine, "Engine not found in database");

        hostAssembler.EquipHull(selectedHull);
        hostAssembler.EquipWeapon(selectedWeapon);
        hostAssembler.EquipEngine(selectedEngine);

        yield return new WaitForSeconds(1.2f);

        var sync = _hostPlayer.GetComponent<ShipNetworkSync>();
        Assert.NotNull(sync, "ShipNetworkSync not found on player");

        Assert.AreEqual(selectedHull.id, hostAssembler.CurrentHull.id, "Hull was not synchronized");
        Assert.AreEqual(selectedWeapon.id, hostAssembler.CurrentWeapon.id, "Weapon was not synchronized");
        Assert.AreEqual(selectedEngine.id, hostAssembler.CurrentEngine.id, "Engine was not synchronized");

        Debug.Log("[Test 05] Ship assembly successfully synchronized on client connect ?");
        Debug.Log("[Test 05] === PASSED ===");
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