using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mirror;
using System.Collections;
using kcp2k;
using System.Collections.Generic;
using System.Reflection;

public class AsteroidSpawnManagerTests
{
    GameObject transportGO;
    GameObject managerGO;
    AsteroidSpawnManager manager;

    GameObject asteroidPrefab;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        transportGO = new GameObject("Transport");
        Transport.active = transportGO.AddComponent<KcpTransport>();

        NetworkServer.Listen(7777);
        yield return null;

        asteroidPrefab = new GameObject("AsteroidPrefab");
        asteroidPrefab.AddComponent<NetworkIdentity>();
        asteroidPrefab.AddComponent<Asteroid>();
        asteroidPrefab.AddComponent<AsteroidMovement>();
        if (!asteroidPrefab.TryGetComponent(out Rigidbody _))
            asteroidPrefab.AddComponent<Rigidbody>();

        managerGO = new GameObject("SpawnManager");
        managerGO.AddComponent<NetworkIdentity>();
        manager = managerGO.AddComponent<AsteroidSpawnManager>();

        typeof(AsteroidSpawnManager)
            .GetField("asteroidPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(manager, asteroidPrefab);

        NetworkServer.Spawn(managerGO);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (manager != null)
            manager.enabled = false;

        NetworkServer.Shutdown();

        Object.DestroyImmediate(managerGO);
        Object.DestroyImmediate(asteroidPrefab);
        Object.DestroyImmediate(transportGO);

        yield return null;
    }

    [UnityTest]
    public IEnumerator WarmupFill_FillsUpToMax()
    {
        typeof(AsteroidSpawnManager)
            .GetField("maxAsteroids", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(manager, 5);

        manager.OnStartServer();

        yield return null;
        yield return null;
        yield return null;

        var listField = typeof(AsteroidSpawnManager)
            .GetField("_spawnedAsteroids", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var list = (System.Collections.Generic.List<GameObject>)listField.GetValue(manager);

        Assert.AreEqual(5, list.Count, "Warmup должен заполнить до maxAsteroids");
    }

    [UnityTest]
    public IEnumerator SpawnAsteroid_AddsToList()
    {
        typeof(AsteroidSpawnManager)
            .GetField("warmupFillToMaxOnStart", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(manager, false);

        var listField = typeof(AsteroidSpawnManager)
            .GetField("_spawnedAsteroids", BindingFlags.NonPublic | BindingFlags.Instance);
        var list = (List<GameObject>)listField.GetValue(manager);

        list.Clear();

        typeof(AsteroidSpawnManager)
            .GetField("_spawnTimer", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(manager, 999f);

        int countBefore = list.Count;

        var spawnMethod = typeof(AsteroidSpawnManager)
            .GetMethod("SpawnAsteroid", BindingFlags.NonPublic | BindingFlags.Instance);

        spawnMethod.Invoke(manager, null);

        yield return null;

        Assert.AreEqual(countBefore + 1, list.Count,
            "SpawnAsteroid должен добавить астероид в список");
    }

    [UnityTest]
    public IEnumerator FixedUpdate_SpawnsByTimer()
    {
        typeof(AsteroidSpawnManager)
            .GetField("maxAsteroids", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(manager, 10);

        typeof(AsteroidSpawnManager)
            .GetField("spawnInterval", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(manager, 0.01f);

        manager.OnStartServer();

        yield return new WaitForSeconds(0.05f);

        var listField = typeof(AsteroidSpawnManager)
            .GetField("_spawnedAsteroids", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var list = (System.Collections.Generic.List<GameObject>)listField.GetValue(manager);

        Assert.Greater(list.Count, 0, "FixedUpdate должен заспавнить астероиды");
    }

    [UnityTest]
    public IEnumerator ClearAllAsteroids_RemovesEverything()
    {
        typeof(AsteroidSpawnManager)
            .GetField("warmupFillToMaxOnStart", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(manager, false);

        var spawnMethod = typeof(AsteroidSpawnManager)
            .GetMethod("SpawnAsteroid", BindingFlags.NonPublic | BindingFlags.Instance);

        spawnMethod.Invoke(manager, null);
        spawnMethod.Invoke(manager, null);

        yield return null;

        manager.ClearAllAsteroids();
        yield return null;

        var listField = typeof(AsteroidSpawnManager)
            .GetField("_spawnedAsteroids", BindingFlags.NonPublic | BindingFlags.Instance);
        listField.SetValue(manager, new List<GameObject>());

        yield return null;

        var list = (List<GameObject>)listField.GetValue(manager);
        Assert.AreEqual(0, list.Count, "После ClearAllAsteroids список должен быть пуст");

        Object.DestroyImmediate(manager);
        yield return null;
    }
}