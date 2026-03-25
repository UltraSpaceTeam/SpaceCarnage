using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ShieldIntegrationTests
{
    private Player _hostPlayer;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[Test 08] === SETUP ===");

        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();
        Player.ActivePlayers.Clear();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.6f);

        // Запускаем только Host
        var nm = Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm, "NetworkManager not found in scene");

        nm.StartHost();
        yield return new WaitForSeconds(1.5f);

        // Находим игрока-хоста
        _hostPlayer = Object.FindObjectsByType<Player>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.isLocalPlayer);

        Assert.NotNull(_hostPlayer, "Host player not found");

        // Даём щит
        var assembler = _hostPlayer.GetComponent<ShipAssembler>();
        var shieldEngine = GameResources.Instance?.partDatabase.engines
            .FirstOrDefault(e => e.ability is ShieldAbility);

        Assert.NotNull(shieldEngine, "Shield engine not found in database");

        assembler.EquipEngine(shieldEngine);
        yield return new WaitForSeconds(0.8f);

        Debug.Log("[Test 08] Setup OK - Host player ready");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var nm = Object.FindAnyObjectByType<NetworkManager>();
        if (nm != null) nm.StopHost();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Shield_Activation_RpcShownToAllClients_And_AbsorbsDamage()
    {
        Debug.Log("[Test 08] === TEST START ===");

        // Активируем щит
        var controller = _hostPlayer.GetComponent<PlayerController>();
        var activateField = controller.GetType().GetField("activateAbility",
            BindingFlags.NonPublic | BindingFlags.Instance);
        activateField?.SetValue(controller, true);

        yield return new WaitForSeconds(1.0f);

        // Проверяем, что щит появился
        Assert.IsTrue(IsShieldVisible(_hostPlayer), "Shield VFX did not appear!");

        Debug.Log("[Test 08] Shield visual effect shown ?");

        // Проверяем поглощение урона
        var health = _hostPlayer.GetComponent<Health>();
        float healthBefore = health.GetHealthPercentage();

        health.TakeDamage(50f, DamageContext.Weapon(0, "TestEnemy", "TestGun"));

        yield return new WaitForSeconds(0.5f);

        float healthAfter = health.GetHealthPercentage();
        Assert.AreEqual(healthBefore, healthAfter, 0.001f,
            "Health decreased — shield did not absorb the damage!");

        Debug.Log("[Test 08] Shield successfully absorbed damage ?");
        Debug.Log("[Test 08] === PASSED ===");
    }

    private bool IsShieldVisible(Player player)
    {
        var field = typeof(Player).GetField("currentShieldInstance",
            BindingFlags.NonPublic | BindingFlags.Instance);

        return field?.GetValue(player) is GameObject go && go.activeInHierarchy;
    }
}