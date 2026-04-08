using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

[Category("SystemTest")]
public class RespawnSystemTest
{
    private Player _hostPlayer;
    private Health _health;
    private Button _respawnButton;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 06] === SETUP ===");

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
        Assert.NotNull(_health, "Health component not found");

        _respawnButton = FindRespawnButton();
        Assert.NotNull(_respawnButton, "Respawn button not found in DeathScreen");

        Debug.Log("[System Test 06] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_Respawn_AfterDeath_WithInvulnerability()
    {
        Debug.Log("[System Test 06] === TEST START ===");

        _health.TakeDamage(9999f, DamageContext.Suicide("Test"));
        yield return new WaitForSeconds(1.2f);

        Assert.IsTrue(_health.IsDead, "Player did not die");

        _respawnButton.onClick.Invoke();
        yield return new WaitForSeconds(1.5f);

        Assert.IsFalse(_health.IsDead, "Player did not respawn");
        Assert.AreEqual(1.0f, _health.GetHealthPercentage(), 0.02f, "Health was not restored to 100%");

        Debug.Log("[System Test 06] Respawn + full health - PASSED");

        float healthBefore = _health.GetHealthPercentage();

        _health.TakeDamage(40f, DamageContext.Weapon(0, "TestEnemy", "TestGun"));
        yield return new WaitForSeconds(0.5f);

        Assert.AreEqual(healthBefore, _health.GetHealthPercentage(), 0.02f,
            "Damage passed during invulnerability period");

        Debug.Log("[System Test 06] Invulnerability (first ~3s) - PASSED");

        yield return new WaitForSeconds(4.0f);

        healthBefore = _health.GetHealthPercentage();
        _health.TakeDamage(40f, DamageContext.Weapon(0, "TestEnemy", "TestGun"));
        yield return new WaitForSeconds(0.5f);

        Assert.Less(_health.GetHealthPercentage(), healthBefore,
            "Damage was not applied after invulnerability ended");

        Debug.Log("[System Test 06] Damage after invulnerability - PASSED");

        Debug.Log("[System Test 06] === TEST PASSED ===");
    }

    private Button FindRespawnButton()
    {
        var deathScreenPanel = GameObject.Find("DeathScreenPanel");
        if (deathScreenPanel == null) return null;

        var respawnGo = deathScreenPanel.transform.Find("DeathScreen/Respawn");
        if (respawnGo == null) return null;

        return respawnGo.GetComponent<Button>();
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