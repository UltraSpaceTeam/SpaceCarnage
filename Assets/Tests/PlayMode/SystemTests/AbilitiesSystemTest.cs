using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[Category("SystemTest")]
public class AbilitiesSystemTest
{
    private Player _hostPlayer;
    private PlayerController _controller;
    private ShipAssembler _assembler;
    private Rigidbody _rb;
    private Health _health;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 04] === SETUP ===");

        AggressiveCleanup();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.8f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm);

        nm.StartHost();

        float timeout = 10f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            _hostPlayer = Object.FindObjectsByType<Player>(FindObjectsSortMode.None)
                .FirstOrDefault(p => p.isLocalPlayer);

            if (_hostPlayer != null) break;

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        Assert.NotNull(_hostPlayer, $"Host player did not spawn within {timeout}s");

        _controller = _hostPlayer.GetComponent<PlayerController>();
        _assembler = _hostPlayer.GetComponent<ShipAssembler>();
        _rb = _hostPlayer.GetComponent<Rigidbody>();
        _health = _hostPlayer.GetComponent<Health>();

        Assert.NotNull(_controller);
        Assert.NotNull(_assembler);
        Assert.NotNull(_rb);

        Debug.Log("[System Test 04] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_AllAbilities_WorkCorrectly_InRealMatch()
    {
        Debug.Log("[System Test 04] === TEST START ===");

        var engines = GameResources.Instance?.partDatabase.engines;
        Assert.NotNull(engines);

        var shieldEngine = engines.FirstOrDefault(e => e.ability is ShieldAbility);
        Assert.NotNull(shieldEngine);
        _assembler.EquipEngine(shieldEngine);
        yield return new WaitForSeconds(1.0f);

        SetActivateAbility(true);
        yield return WaitUntilOrTimeout(() => IsShieldVisible(), 5f, "Shield VFX did not appear");

        float healthBefore = _health.GetHealthPercentage();
        _health.TakeDamage(50f, DamageContext.Weapon(0, "TestEnemy", "TestGun"));
        yield return new WaitForSeconds(0.5f);

        Assert.AreEqual(healthBefore, _health.GetHealthPercentage(), 0.005f, "Shield did not absorb damage");

        Debug.Log("[System Test 04] Shield passed");

        var invisEngine = engines.FirstOrDefault(e => e.ability is InvisAbility);
        Assert.NotNull(invisEngine);
        _assembler.EquipEngine(invisEngine);
        yield return new WaitForSeconds(1.0f);

        SetActivateAbility(true);
        yield return WaitUntilOrTimeout(() => !IsShipVisible(), 5f, "Ship should be invisible");

        _controller.ServerNotifyAttacked();
        yield return WaitUntilOrTimeout(() => IsShipVisible(), 5f, "Invisibility did not break on attack");

        Debug.Log("[System Test 04] Invisibility passed");

        var dashEngine = engines.FirstOrDefault(e => e.ability is DashAbility);
        Assert.NotNull(dashEngine);
        _assembler.EquipEngine(dashEngine);
        yield return new WaitForSeconds(1.5f);

        _rb.linearVelocity = Vector3.zero;
        ResetAbilityCooldown();
        yield return new WaitForFixedUpdate();

        Vector3 velocityBefore = _rb.linearVelocity;

        SetActivateAbility(true);
        yield return WaitUntilOrTimeout(
            () => (_rb.linearVelocity - velocityBefore).magnitude > 3.5f,
            5f,
            "Dash did not give expected speed boost"
        );

        Debug.Log("[System Test 04] Dash passed");

        Debug.Log("[System Test 04] === ALL ABILITIES PASSED ===");
    }

    private IEnumerator WaitUntilOrTimeout(System.Func<bool> condition, float timeout, string message)
    {
        float elapsed = 0f;
        while (!condition() && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        Assert.IsTrue(condition(), message);
    }

    private void SetActivateAbility(bool value)
    {
        var field = typeof(PlayerController).GetField("activateAbility",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(_controller, value);
    }

    private void ResetAbilityCooldown()
    {
        var field = typeof(PlayerController).GetField("abilityReadyTime",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(_controller, NetworkTime.time - 10.0);
    }

    private bool IsShieldVisible()
    {
        var field = typeof(Player).GetField("currentShieldInstance",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(_hostPlayer) is GameObject go && go.activeInHierarchy;
    }

    private bool IsShipVisible()
    {
        var invisManager = _hostPlayer.GetComponent<InvisManager>();
        if (invisManager == null) return true;

        var visibleField = typeof(InvisManager).GetField("isVisible",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return visibleField?.GetValue(invisManager) is bool visible && visible;
    }

    private void AggressiveCleanup()
    {
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        var managers = Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var m in managers) if (m != null) Object.DestroyImmediate(m.gameObject);

        var transports = Object.FindObjectsByType<kcp2k.KcpTransport>(FindObjectsSortMode.None);
        foreach (var t in transports) if (t != null) Object.DestroyImmediate(t.gameObject);

        ResetSingleton<UIManager>();
        ResetSingleton<GameResources>();
        ResetSingleton<SessionManager>();
        ResetSingleton<AudioManager>();

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