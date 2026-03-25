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

        // Полная агрессивная очистка перед запуском теста
        AggressiveCleanup();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.6f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm, "NetworkManager not found in scene");

        nm.StartHost();
        yield return new WaitForSeconds(1.5f);

        _hostPlayer = Object.FindObjectsByType<Player>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.isLocalPlayer);

        Assert.NotNull(_hostPlayer, "Host player not found");

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
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Shield_Activation_RpcShownToAllClients_And_AbsorbsDamage()
    {
        Debug.Log("[Test 08] === TEST START ===");

        var controller = _hostPlayer.GetComponent<PlayerController>();
        var activateField = controller.GetType().GetField("activateAbility",
            BindingFlags.NonPublic | BindingFlags.Instance);
        activateField?.SetValue(controller, true);

        yield return new WaitForSeconds(1.0f);

        Assert.IsTrue(IsShieldVisible(_hostPlayer), "Shield VFX did not appear!");

        Debug.Log("[Test 08] Shield visual effect shown ?");

        var health = _hostPlayer.GetComponent<Health>();
        float healthBefore = health.GetHealthPercentage();

        health.TakeDamage(50f, DamageContext.Weapon(0, "TestEnemy", "TestGun"));

        yield return new WaitForSeconds(0.5f);

        float healthAfter = health.GetHealthPercentage();
        Assert.AreEqual(healthBefore, healthAfter, 0.001f,
            "Health decreased - shield did not absorb the damage!");

        Debug.Log("[Test 08] Shield successfully absorbed damage ?");
        Debug.Log("[Test 08] === PASSED ===");
    }

    private bool IsShieldVisible(Player player)
    {
        var field = typeof(Player).GetField("currentShieldInstance",
            BindingFlags.NonPublic | BindingFlags.Instance);

        return field?.GetValue(player) is GameObject go && go.activeInHierarchy;
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