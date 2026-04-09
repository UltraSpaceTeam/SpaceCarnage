using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[Category("SystemTest")]
public class InvisibilitySystemTest
{
    private Player _hostPlayer;
    private PlayerController _controller;
    private ShipAssembler _assembler;
    private Health _health;
    private InvisManager _invisManager;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 12] === SETUP ===");

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
        _invisManager = _hostPlayer.GetComponent<InvisManager>();

        Assert.NotNull(_health);
        Assert.NotNull(_controller);
        Assert.NotNull(_assembler);

        // Экипируем корпус
        var hulls = GameResources.Instance?.partDatabase.hulls;
        if (hulls != null && hulls.Count > 0)
        {
            var firstHull = hulls.FirstOrDefault();
            if (firstHull != null)
            {
                _assembler.EquipHull(firstHull);
                yield return new WaitForSeconds(0.5f);
            }
        }

        var invisEngine = GameResources.Instance?.partDatabase.engines
            .FirstOrDefault(e => e.ability is InvisAbility);

        Assert.NotNull(invisEngine, "Engine with InvisAbility not found in database!");

        _assembler.EquipEngine(invisEngine);
        yield return new WaitForSeconds(1.0f);

        Debug.Log("[System Test 12] Setup OK - Invisibility equipped");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_Invisibility_CombatBehavior()
    {
        Debug.Log("[System Test 12] === TEST: Invisibility Combat Behavior ===\n");

        // ========== Шаг 1: Нажатие инвиза - проверка, что ты в инвизе ==========
        Debug.Log(">>> Step 1: Activate invisibility");
        
        SetActivateAbility(true);
        yield return new WaitForSeconds(1.5f);

        Assert.IsTrue(IsShipInvisible(), "[Step 1 FAILED] Player should be invisible after activation");
        Debug.Log("[OK] Step 1: Invisibility activated, player is invisible\n");

        // ========== Шаг 2: Урон игрокам - спадение инвиза ==========
        Debug.Log(">>> Step 2: Deal damage to enemy - invisibility should drop");
        
        _controller.ServerNotifyAttacked();
        yield return new WaitForSeconds(0.6f);

        Assert.IsFalse(IsShipInvisible(), "[Step 2 FAILED] Invisibility should drop after dealing damage");
        Debug.Log("[OK] Step 2: Invisibility dropped after dealing damage, player is visible\n");

        // ========== Шаг 3: Проверка, что игрок всё ещё жив и видим ==========
        Debug.Log(">>> Step 3: Verify player is still visible and alive");
        
        Assert.IsNotNull(_hostPlayer, "[Step 3 FAILED] Player should still exist");
        Assert.IsFalse(IsShipInvisible(), "[Step 3 FAILED] Player should remain visible");
        Debug.Log("[OK] Step 3: Player is visible and alive\n");

        // ========== Шаг 4: Проверка получения урона (без инвиза) ==========
        Debug.Log(">>> Step 4: Take damage (baseline test - player should take damage when visible)");
        
        float healthBefore = _health.GetHealthPercentage();
        _health.TakeDamage(10f, DamageContext.Weapon(0, "TestEnemy", "TestGun"));
        yield return new WaitForSeconds(0.6f);

        Assert.Less(_health.GetHealthPercentage(), healthBefore, "[Step 4 FAILED] Health should decrease when not invisible");
        Debug.Log("[OK] Step 4: Damage taken correctly when visible\n");

        // ========== Шаг 5: Финальная проверка - игрок всё ещё существует ==========
        Debug.Log(">>> Step 5: Final verification - player still exists");
        Assert.IsNotNull(_hostPlayer, "[Step 5 FAILED] Player should exist at the end of test");
        Debug.Log("[OK] Step 5: Player exists\n");

        Debug.Log("=========================================");
        Debug.Log("[System Test 12] === ALL TESTS PASSED ===");
    }

    // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

    private void SetActivateAbility(bool value)
    {
        var field = typeof(PlayerController).GetField("activateAbility",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(_controller, value);
    }

    private bool IsShipInvisible()
    {
        if (_invisManager == null) return false;

        var visibleField = typeof(InvisManager).GetField("isVisible",
            BindingFlags.NonPublic | BindingFlags.Instance);
        bool isVisible = visibleField?.GetValue(_invisManager) is bool visible && visible;
        
        return !isVisible;
    }

    private void AggressiveCleanup()
    {
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        var session = UnityEngine.Object.FindAnyObjectByType<SessionManager>();
        if (session != null)
        {
            session.StopAllCoroutines();
            UnityEngine.Object.DestroyImmediate(session.gameObject);
        }

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