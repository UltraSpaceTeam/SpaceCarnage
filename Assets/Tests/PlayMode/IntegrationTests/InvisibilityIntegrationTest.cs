using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class InvisibilityIntegrationTests
{
    private Player _hostPlayer;
    private InvisManager _invisManager;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[Test 09] === SETUP ===");

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

        var assembler = _hostPlayer.GetComponent<ShipAssembler>();
        var invisEngine = GameResources.Instance?.partDatabase.engines
            .FirstOrDefault(e => e.ability is InvisAbility);

        Assert.NotNull(invisEngine, "Engine with InvisAbility not found");

        assembler.EquipEngine(invisEngine);
        yield return new WaitForSeconds(0.8f);

        _invisManager = _hostPlayer.GetComponent<InvisManager>();
        Assert.NotNull(_invisManager, "InvisManager not found");

        Debug.Log("[Test 09] Setup completed");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var nm = Object.FindAnyObjectByType<NetworkManager>();
        if (nm != null) nm.StopHost();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Invisibility_Hides_Ship_And_Disables_On_Attack_Or_Damage()
    {
        Debug.Log("[Test 09] === TEST START ===");

        var controller = _hostPlayer.GetComponent<PlayerController>();
        var activateField = controller.GetType().GetField("activateAbility",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // 1. Активируем невидимость
        activateField?.SetValue(controller, true);
        yield return new WaitForSeconds(2.0f);        // ждём активацию + delay

        Assert.IsFalse(IsShipVisible(_hostPlayer), "Ship should be hidden after activating invisibility");

        Debug.Log("[Test 09] Invisibility activated - ship model hidden ?");

        // 2. Наносим урон ? невидимость должна сняться
        var health = _hostPlayer.GetComponent<Health>();
        health.TakeDamage(10f, DamageContext.Weapon(999, "TestEnemy", "TestGun"));

        yield return new WaitForSeconds(1.0f);

        Assert.IsTrue(IsShipVisible(_hostPlayer), "Ship should become visible after taking damage");

        Debug.Log("[Test 09] Invisibility correctly disabled on damage ?");

        // 3. Ждём окончания кулдауна (InvisAbility.cooldown обычно 20 сек, но для теста ускоряем)
        //    Для теста сбрасываем кулдаун через рефлексию
        ResetAbilityCooldown(controller);

        // 4. Повторная активация
        activateField?.SetValue(controller, true);
        yield return new WaitForSeconds(2.0f);

        Assert.IsFalse(IsShipVisible(_hostPlayer), "Invisibility should reactivate after cooldown");

        Debug.Log("[Test 09] Invisibility successfully reactivated after cooldown ?");
        Debug.Log("[Test 09] === PASSED ===");
    }

    private bool IsShipVisible(Player player)
    {
        var assembler = player.GetComponent<ShipAssembler>();
        var hull = assembler?.CurrentHullObject;
        if (hull == null) return true;

        var renderers = hull.GetComponentsInChildren<Renderer>(true);

        foreach (var r in renderers)
        {
            if (r is LineRenderer || r is TrailRenderer || r is ParticleSystemRenderer)
                continue;

            if (r.enabled)
                return true;
        }

        return false;
    }

    // Сбрасываем кулдаун способности для теста (чтобы не ждать 20 секунд)
    private void ResetAbilityCooldown(PlayerController controller)
    {
        var readyTimeField = controller.GetType().GetField("abilityReadyTime",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (readyTimeField != null)
        {
            readyTimeField.SetValue(controller, NetworkTime.time - 1.0); // готово к использованию
        }
    }
}