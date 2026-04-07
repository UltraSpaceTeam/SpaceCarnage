using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ShieldFullFunctionalitySystemTest
{
    private Player _hostPlayer;
    private Health _health;
    private PlayerController _controller;
    private ShipAssembler _assembler;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 08] === SETUP ===");

        AggressiveCleanup();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.8f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        nm.StartHost();
        yield return new WaitForSeconds(1.8f);

        _hostPlayer = Object.FindObjectsByType<Player>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.isLocalPlayer);

        Assert.NotNull(_hostPlayer, "Host player not found");

        _health = _hostPlayer.GetComponent<Health>();
        _controller = _hostPlayer.GetComponent<PlayerController>();
        _assembler = _hostPlayer.GetComponent<ShipAssembler>();

        Assert.NotNull(_health);
        Assert.NotNull(_controller);
        Assert.NotNull(_assembler);

        var shieldEngine = GameResources.Instance?.partDatabase.engines
            .FirstOrDefault(e => e.ability is ShieldAbility);

        Assert.NotNull(shieldEngine, "Shield engine not found in database");

        _assembler.EquipEngine(shieldEngine);
        yield return new WaitForSeconds(1.0f);

        Debug.Log("[System Test 08] Setup OK - Shield equipped");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_Shield_FullCycle_Activation_Absorb_Break_Regenerate()
    {
        Debug.Log("[System Test 08] === TEST START ===");

        SetActivateAbility(true);
        yield return new WaitForSeconds(1.2f);

        Assert.IsTrue(IsShieldActive(), "Shield did not activate");

        float healthBefore = _health.GetHealthPercentage();
        _health.TakeDamage(50f, DamageContext.Weapon(0, "TestEnemy", "TestGun"));
        yield return new WaitForSeconds(0.6f);

        Assert.AreEqual(healthBefore, _health.GetHealthPercentage(), 0.005f,
            "Health changed while shield was active (should absorb 50 damage)");

        Debug.Log("[System Test 08] Shield absorbed 50 damage - PASSED");

        _health.TakeDamage(70f, DamageContext.Weapon(0, "TestEnemy", "TestGun"));
        yield return new WaitForSeconds(0.6f);

        Assert.IsFalse(IsShieldActive(), "Shield did not break after overload");

        Assert.Less(_health.GetHealthPercentage(), healthBefore,
            "Remaining 20 damage was not applied to health after shield broke");

        Debug.Log("[System Test 08] Shield broke and passed excess damage - PASSED");

        yield return new WaitForSeconds(21.0f);

        Assert.IsTrue(IsShieldFullyRegenerated(), "Shield did not fully regenerate after 20 seconds");

        Debug.Log("[System Test 08] Shield full regeneration - PASSED");

        Debug.Log("[System Test 08] === TEST PASSED ===");
    }

    private void SetActivateAbility(bool value)
    {
        var field = typeof(PlayerController).GetField("activateAbility",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(_controller, value);
    }

    private bool IsShieldActive()
    {
        var field = typeof(Player).GetField("currentShieldInstance",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(_hostPlayer) is GameObject go && go.activeInHierarchy;
    }

    private bool IsShieldFullyRegenerated()
    {
        return _controller.AbilityStatusValue > 0.98f;
    }

    private void AggressiveCleanup()
    {
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        var managers = Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var m in managers)
            if (m != null) Object.DestroyImmediate(m.gameObject);

        var transports = Object.FindObjectsByType<kcp2k.KcpTransport>(FindObjectsSortMode.None);
        foreach (var t in transports)
            if (t != null) Object.DestroyImmediate(t.gameObject);

        ResetSingleton<UIManager>();
        ResetSingleton<SessionManager>();
        ResetSingleton<GameResources>();

        Player.ActivePlayers.Clear();
        NetworkManager.startPositions.Clear();
    }

    private void ResetSingleton<T>() where T : MonoBehaviour
    {
        var field = typeof(T).GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }
}