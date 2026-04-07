using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CollisionDamageSystemTest
{
    private Player _hostPlayer;
    private Health _playerHealth;
    private AsteroidSpawnManager _spawnManager;
    private GameObject _asteroidPrefab;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 10] === SETUP ===");

        AggressiveCleanup();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(1.0f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        nm.StartHost();
        yield return new WaitForSeconds(2.0f);

        _hostPlayer = Object.FindObjectsByType<Player>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.isLocalPlayer);

        Assert.NotNull(_hostPlayer);

        _playerHealth = _hostPlayer.GetComponent<Health>();
        _spawnManager = Object.FindAnyObjectByType<AsteroidSpawnManager>();

        Assert.NotNull(_playerHealth);
        Assert.NotNull(_spawnManager);

        _asteroidPrefab = GetAsteroidPrefab();
        Assert.NotNull(_asteroidPrefab);

        Debug.Log("[System Test 10] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_CollisionDamage_PlayerAndAsteroids()
    {
        Debug.Log("[System Test 10] === TEST START ===");

        float initialHealth = _playerHealth.GetHealthPercentage();

        Vector3 spawnPos = _hostPlayer.transform.position + new Vector3(0f, 2f, 15f);
        GameObject asteroid = SpawnAsteroid(spawnPos);

        var rb = asteroid.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dir = (_hostPlayer.transform.position - spawnPos).normalized;
            rb.AddForce(dir * 80f, ForceMode.Impulse);
            Debug.Log($"[Test 10] Asteroid launched with force 80 from {spawnPos}");
        }

        yield return new WaitForSeconds(4.0f);

        float afterHealth = _playerHealth.GetHealthPercentage();

        Debug.Log($"[Test 10] Player health before: {initialHealth:F3}, after: {afterHealth:F3}");

        Assert.Less(afterHealth, initialHealth,
            "Player did not take damage from asteroid collision");

        Debug.Log("[System Test 10] Player collision damage - PASSED");

        Debug.Log("[System Test 10] === TEST PASSED ===");
    }

    private GameObject GetAsteroidPrefab()
    {
        var field = typeof(AsteroidSpawnManager).GetField("asteroidPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(_spawnManager) as GameObject;
    }

    private GameObject SpawnAsteroid(Vector3 position)
    {
        GameObject go = Object.Instantiate(_asteroidPrefab, position, Quaternion.identity);
        NetworkServer.Spawn(go);
        return go;
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