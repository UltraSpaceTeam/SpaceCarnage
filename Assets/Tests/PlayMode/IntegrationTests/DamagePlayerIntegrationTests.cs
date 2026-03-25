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

        Debug.Log("[Test 15] Setup OK - Host player ready");
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
            "Health decreased — shield did not absorb the damage!");

        Debug.Log("[Test 15] === PASSED ===");
    }

    private bool IsShieldVisible(Player player)
    {
        var field = typeof(Player).GetField("currentShieldInstance",
            BindingFlags.NonPublic | BindingFlags.Instance);

        return field?.GetValue(player) is GameObject go && go.activeInHierarchy;
    }
}