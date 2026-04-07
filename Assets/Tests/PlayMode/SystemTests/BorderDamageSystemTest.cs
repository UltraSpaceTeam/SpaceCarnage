using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class BorderDamageSystemTest
{
    private Player _hostPlayer;
    private Health _health;
    private BorderDamage _borderDamage;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 07] === SETUP ===");

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
        _borderDamage = _hostPlayer.GetComponent<BorderDamage>();

        Assert.NotNull(_health, "Health component not found");
        Assert.NotNull(_borderDamage, "BorderDamage component not found");

        Debug.Log("[System Test 07] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_BorderDamage_DealsPeriodicDamage_And_CausesDeath()
    {
        Debug.Log("[System Test 07] === TEST START ===");

        Vector3 outsidePosition = new Vector3(150f, 0f, 0f);
        _hostPlayer.transform.position = outsidePosition;

        yield return new WaitForSeconds(1.5f);

        float healthBefore = _health.GetHealthPercentage();

        yield return new WaitForSeconds(3.0f);

        float healthAfter = _health.GetHealthPercentage();

        Assert.Less(healthAfter, healthBefore, "Border damage was not applied");

        Debug.Log("[System Test 07] Periodic border damage - PASSED");

        float startHealth = _health.GetHealthPercentage();
        float elapsed = 0f;

        while (!_health.IsDead && elapsed < 30f)
        {
            yield return new WaitForSeconds(1.0f);
            elapsed += 1.0f;
        }

        Assert.IsTrue(_health.IsDead, "Player did not die from border damage");

        Debug.Log("[System Test 07] Death from border zone - PASSED");

        Assert.IsTrue(_health.IsDead, "Player should remain dead after border death");

        Debug.Log("[System Test 07] === TEST PASSED ===");
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