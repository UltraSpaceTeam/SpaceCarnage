using System.Collections;
using System.Linq;
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

        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.6f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm, "NetworkManager not found");

        nm.StartHost();
        yield return new WaitForSeconds(1.5f);

        _hostPlayer = Object.FindObjectsByType<Player>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.isLocalPlayer);

        Assert.NotNull(_hostPlayer, "Host player not found");

        // Даём хосту нормальную сборку
        EquipBasicShip(_hostPlayer);

        yield return new WaitForSeconds(0.8f);

        Debug.Log("[Test 05] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var nm = Object.FindAnyObjectByType<NetworkManager>();
        if (nm != null) nm.StopHost();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Client_Connect_Ship_Assembly_Is_Synchronized_Correctly()
    {
        Debug.Log("[Test 05] === TEST START ===");

        var hostAssembler = _hostPlayer.GetComponent<ShipAssembler>();
        var database = GameResources.Instance.partDatabase;

        // Выбираем конкретные части
        var selectedHull = database.hulls.FirstOrDefault();
        var selectedWeapon = database.weapons.FirstOrDefault();
        var selectedEngine = database.engines.FirstOrDefault();

        Assert.NotNull(selectedHull, "Hull not found in database");
        Assert.NotNull(selectedWeapon, "Weapon not found in database");
        Assert.NotNull(selectedEngine, "Engine not found in database");

        // Устанавливаем сборку на хосте
        hostAssembler.EquipHull(selectedHull);
        hostAssembler.EquipWeapon(selectedWeapon);
        hostAssembler.EquipEngine(selectedEngine);

        yield return new WaitForSeconds(1.2f);

        // Проверяем, что ShipNetworkSync применил те же части
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
}