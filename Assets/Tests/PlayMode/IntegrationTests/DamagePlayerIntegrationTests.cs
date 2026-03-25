using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class DamagePlayerIntegrationTests
{
    private Player _hostPlayer;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[DamagePlayerIntegrationTests] === SETUP ===");

        // Полная очистка Mirror
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

        yield return new WaitForSeconds(0.8f);

        Debug.Log("[DamagePlayerIntegrationTests] Setup OK - Host player ready");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var nm = Object.FindAnyObjectByType<NetworkManager>();
        if (nm != null) nm.StopHost();
        yield return null;
    }

    [UnityTest]
    public IEnumerator DamageAfterRespawn_NoDamageDealt_RespawnSuccess()
    {
        Debug.Log("[Test 15] === TEST START ===");
		var controller = _hostPlayer.GetComponent<PlayerController>();
		var health = _hostPlayer.GetComponent<Health>();
		
        Debug.Log("[Test 15] Deal 9999 damage");
        health.TakeDamage(9999f, DamageContext.Weapon(0, "TestEnemy", "TestGun"));

        yield return new WaitForSeconds(0.5f);
		
		
        Debug.Log("[Test 15] Respawn");
		
		_hostPlayer.CmdRequestRespawn();
		
        yield return new WaitForSeconds(0.5f);
		
        Debug.Log("[Test 15] Deal 9999 damage");
		
        float healthBefore = health.GetHealthPercentage();
        health.TakeDamage(9999f, DamageContext.Weapon(0, "TestEnemy", "TestGun"));

        yield return new WaitForSeconds(0.5f);
		
        Debug.Log("[Test 15] Assert no damage taken");

        float healthAfter = health.GetHealthPercentage();
        Assert.AreEqual(healthBefore, healthAfter, 0.001f,
            "Health decreased — respawn damage cooldown is not working!");

        Debug.Log("[Test 15] === PASSED ===");
    }

    [UnityTest]
    public IEnumerator DamageMoreThanShield_HealthDamaged()
    {
        Debug.Log("[Test 14] === TEST START ===");
		var controller = _hostPlayer.GetComponent<PlayerController>();
		var health = _hostPlayer.GetComponent<Health>();
		
        var assembler = _hostPlayer.GetComponent<ShipAssembler>();
        var shieldEngine = GameResources.Instance?.partDatabase.engines
            .FirstOrDefault(e => e.ability is ShieldAbility);

        Assert.NotNull(shieldEngine, "Shield engine not found in database");

        assembler.EquipEngine(shieldEngine);
        yield return new WaitForSeconds(0.8f);	
		
        var activateField = controller.GetType().GetField("activateAbility",
            BindingFlags.NonPublic | BindingFlags.Instance);
        activateField?.SetValue(controller, true);

        yield return new WaitForSeconds(1.0f);		
		
		ShieldAbility shield = ScriptableObject.CreateInstance<ShieldAbility>();
		
        float healthBefore = health.GetHealthPercentage();
        health.TakeDamage(shield.maxShieldHealth + 20, DamageContext.Weapon(0, "TestEnemy", "TestGun"));

        yield return new WaitForSeconds(0.5f);

        float healthAfter = health.GetHealthPercentage();
        Assert.Greater(healthBefore, healthAfter,
            "Health decreased — shield did absorb too many the damage!");

        Debug.Log("[Test 14] === PASSED ===");
    }

}