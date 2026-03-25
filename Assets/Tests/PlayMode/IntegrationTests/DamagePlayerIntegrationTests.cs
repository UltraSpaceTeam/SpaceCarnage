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

        // Полная агрессивная очистка перед запуском теста
        AggressiveCleanup();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Additive);
        yield return new WaitForSeconds(0.6f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm, "NetworkManager not found in scene");

		nm.offlineScene = SceneManager.GetActiveScene().name;
		nm.onlineScene = SceneManager.GetActiveScene().name;

        nm.StartHost();
        yield return new WaitForSeconds(1.5f);

        _hostPlayer = Object.FindObjectsByType<Player>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.isLocalPlayer);

        Assert.NotNull(_hostPlayer, "Host player not found");

        yield return new WaitForSeconds(0.8f);

        Debug.Log("[DamagePlayerIntegrationTests] Setup OK - Host player ready");
    }

    [UnityTearDown]
    public IEnumerator TearDown()

    {	
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();
        Player.ActivePlayers.Clear();
        yield return SceneManager.UnloadSceneAsync("TestMultiplayerScene");
        yield return new WaitForSeconds(1.5f);

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

        Debug.Log("[Test 15] Shield successfully absorbed damage ✓");
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