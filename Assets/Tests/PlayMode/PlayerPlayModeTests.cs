using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mirror;
using kcp2k;
using Object = UnityEngine.Object;

public class PlayerNoServerTests
{
    private T GetField<T>(object obj, string name)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {obj.GetType().Name}");
        return (T)f.GetValue(obj);
    }

    private void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {obj.GetType().Name}");
        f.SetValue(obj, value);
    }

    private void Invoke(object obj, string name, params object[] args)
    {
        var m = obj.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{name}' not found on {obj.GetType().Name}");
        m.Invoke(obj, args);
    }

    private GameObject CreatePlayerGO()
    {
        var go = new GameObject("Player");
        go.AddComponent<NetworkIdentity>();
        go.AddComponent<Rigidbody>();
        go.AddComponent<Health>();
        go.AddComponent<PlayerController>().enabled = false;
        go.AddComponent<ShipShooting>().enabled = false;
        go.AddComponent<ShipAssembler>().enabled = false;
        go.AddComponent<NetworkAudio>().enabled = false;
        go.AddComponent<Player>();
        return go;
    }

    [SetUp]
    public void SetUp()
    {
        LogAssert.ignoreFailingMessages = true;
    }

    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;
        Player.ActivePlayers.Clear();
    }

    [Test]
    public void SyncVars_DefaultValues()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();

        Assert.IsTrue(p.IsActive);
        Assert.AreEqual(0, p.Kills);
        Assert.AreEqual(0, p.Deaths);
        Assert.AreEqual("Player", p.Nickname);
        Assert.AreEqual(0, p.ServerPlayerId);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void SyncVars_CanBeSetDirectly()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();

        p.Kills = 5;
        p.Deaths = 3;
        p.Nickname = "TestPlayer";
        p.IsActive = false;

        Assert.AreEqual(5, p.Kills);
        Assert.AreEqual(3, p.Deaths);
        Assert.AreEqual("TestPlayer", p.Nickname);
        Assert.IsFalse(p.IsActive);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Awake_NetworkAudio_IsSet()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Assert.IsNotNull(p.networkAudio);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Awake_HealthField_IsSet()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Assert.IsNotNull(GetField<Health>(p, "health"));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Awake_ControllerField_IsSet()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Assert.IsNotNull(GetField<PlayerController>(p, "controller"));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Awake_AssemblerField_IsSet()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Assert.IsNotNull(GetField<ShipAssembler>(p, "assembler"));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Test_SetMatchTimer_SetsAllStatics()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        p.Test_SetMatchTimer(2, 10, 20);
        Assert.AreEqual(2, Player.ClientTimerState);
        Assert.AreEqual(10, Player.ClientMatchStartTime, 0.001);
        Assert.AreEqual(20, Player.ClientEndingStartTime, 0.001);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Test_SetMatchTimer_ZeroValues()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        p.Test_SetMatchTimer(0, 0.0, 0.0);
        Assert.AreEqual(0, Player.ClientTimerState);
        Assert.AreEqual(0.0, Player.ClientMatchStartTime, 0.001);
        Assert.AreEqual(0.0, Player.ClientEndingStartTime, 0.001);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Test_SetMatchTimer_NegativeValues()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        p.Test_SetMatchTimer(-1, -100.0, -200.0);
        Assert.AreEqual(-1, Player.ClientTimerState);
        Assert.AreEqual(-100.0, Player.ClientMatchStartTime, 0.001);
        Assert.AreEqual(-200.0, Player.ClientEndingStartTime, 0.001);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Test_SetMatchTimer_OverwrittenByNewCall()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        p.Test_SetMatchTimer(1, 10, 20);
        p.Test_SetMatchTimer(5, 50, 100);
        Assert.AreEqual(5, Player.ClientTimerState);
        Assert.AreEqual(50, Player.ClientMatchStartTime, 0.001);
        Assert.AreEqual(100, Player.ClientEndingStartTime, 0.001);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ShowShield_NoPrefab_LogsWarning()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        LogAssert.Expect(LogType.Warning, "Shield prefab not assigned in Player!");
        Invoke(p, "ShowShield", true, 1f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ShowShield_CreatesInstance()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.shieldBubblePrefab = prefab;

        Invoke(p, "ShowShield", true, 1f);

        var instance = GetField<GameObject>(p, "currentShieldInstance");
        Assert.IsNotNull(instance);
        Assert.IsTrue(instance.activeSelf);

        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ShowShield_Hide_DeactivatesInstance()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.shieldBubblePrefab = prefab;

        Invoke(p, "ShowShield", true, 1f);
        Invoke(p, "ShowShield", false, 0f);

        Assert.IsFalse(GetField<GameObject>(p, "currentShieldInstance").activeSelf);

        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ShowShield_Hide_WhenNoInstance_DoesNotThrow()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.shieldBubblePrefab = prefab;

        Assert.DoesNotThrow(() => Invoke(p, "ShowShield", false, 0f));

        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ShowShield_ShowTwice_ReusesSameInstance()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.shieldBubblePrefab = prefab;

        Invoke(p, "ShowShield", true, 0.5f);
        var first = GetField<GameObject>(p, "currentShieldInstance");
        Invoke(p, "ShowShield", true, 0.8f);
        var second = GetField<GameObject>(p, "currentShieldInstance");

        Assert.AreSame(first, second);

        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ShowShield_HealthRatio_AffectsAlpha()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.shieldBubblePrefab = prefab;

        Invoke(p, "ShowShield", true, 0f);
        var renderer = GetField<Renderer>(p, "currentShieldRenderer");
        if (renderer != null)
            Assert.AreEqual(0.1f, renderer.material.color.a, 0.01f);

        Invoke(p, "ShowShield", true, 1f);
        if (renderer != null)
            Assert.AreEqual(0.4f, renderer.material.color.a, 0.01f);

        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator HandleDeath_IncrementsDeaths()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Invoke(p, "HandleDeath", DamageContext.Suicide("test"));
        yield return null;
        Assert.AreEqual(1, p.Deaths);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HandleDeath_Suicide_DoesNotIncrementKills()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Player.ActivePlayers.Clear();
        Invoke(p, "HandleDeath", DamageContext.Suicide("self"));
        Assert.AreEqual(1, p.Deaths);
        foreach (var kv in Player.ActivePlayers)
            Assert.AreEqual(0, kv.Value?.Kills ?? 0);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HandleDeath_WeaponKill_IncrementsKillerKills()
    {
        var victimGO = CreatePlayerGO();
        var victim = victimGO.GetComponent<Player>();
        var killerGO = CreatePlayerGO();
        var killer = killerGO.GetComponent<Player>();
        uint killerId = 1234u;
        Player.ActivePlayers[killerId] = killer;

        Invoke(victim, "HandleDeath", DamageContext.Weapon(killerId, "Killer", "Gun"));

        Assert.AreEqual(1, victim.Deaths);
        Assert.AreEqual(1, killer.Kills);

        Player.ActivePlayers.Remove(killerId);
        Object.DestroyImmediate(victimGO);
        Object.DestroyImmediate(killerGO);
    }

    [Test]
    public void HandleDeath_CollisionKill_IncrementsKillerKills()
    {
        var victimGO = CreatePlayerGO();
        var victim = victimGO.GetComponent<Player>();
        var killerGO = CreatePlayerGO();
        var killer = killerGO.GetComponent<Player>();
        uint killerId = 5555u;
        Player.ActivePlayers[killerId] = killer;

        Invoke(victim, "HandleDeath", DamageContext.Collision(killerId, "Killer", "Collision"));

        Assert.AreEqual(1, killer.Kills);

        Player.ActivePlayers.Remove(killerId);
        Object.DestroyImmediate(victimGO);
        Object.DestroyImmediate(killerGO);
    }

    [Test]
    public void HandleDeath_AttackerIdZero_NoKillIncrement()
    {
        var killerGO = CreatePlayerGO();
        var killer = killerGO.GetComponent<Player>();
        uint killerId = 6666u;
        Player.ActivePlayers[killerId] = killer;

        var victimGO = CreatePlayerGO();
        Invoke(victimGO.GetComponent<Player>(), "HandleDeath", DamageContext.Weapon(0u, "", ""));

        Assert.AreEqual(0, killer.Kills);

        Player.ActivePlayers.Remove(killerId);
        Object.DestroyImmediate(victimGO);
        Object.DestroyImmediate(killerGO);
    }

    [Test]
    public void HandleDeath_KillerNotInActivePlayers_NoException()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Assert.DoesNotThrow(() =>
            Invoke(p, "HandleDeath", DamageContext.Weapon(8888u, "Ghost", "Laser")));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HandleDeath_CalledTwice_DeathsEqualsTwo()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Invoke(p, "HandleDeath", DamageContext.Suicide("a"));
        Invoke(p, "HandleDeath", DamageContext.Suicide("b"));
        Assert.AreEqual(2, p.Deaths);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void OnStopLocalPlayer_SetsIsActiveFalse()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        p.IsActive = true;
        p.OnStopLocalPlayer();
        Assert.IsFalse(p.IsActive);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void OnStopLocalPlayer_CalledTwice_NoException()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Assert.DoesNotThrow(() => { p.OnStopLocalPlayer(); p.OnStopLocalPlayer(); });
        Object.DestroyImmediate(go);
    }

    [Test]
    public void OnStopLocalPlayer_NullHealth_DoesNotThrow()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        SetField(p, "health", null);
        Assert.DoesNotThrow(() => p.OnStopLocalPlayer());
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HandleHealthUpdate_WithoutUIManager_DoesNotThrow()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Assert.DoesNotThrow(() => Invoke(p, "HandleHealthUpdate", 50f, 100f));
        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator SpawnDebris_DoesNotCrash()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Assert.DoesNotThrow(() => Invoke(p, "SpawnDebris"));
        yield return null;
        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator SpawnDebris_WithChildren_CreatesDebrisCopies()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        var child = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        child.transform.SetParent(go.transform);

        int before = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
        Invoke(p, "SpawnDebris");
        yield return null;
        int after = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;

        Assert.Greater(after, before);
        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator SetupDebrisRecursive_AddsRigidbody()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        var debrisGO = GameObject.CreatePrimitive(PrimitiveType.Cube);

        Invoke(p, "SetupDebrisRecursive", debrisGO.transform);
        yield return null;

        var rb = debrisGO.GetComponent<Rigidbody>();
        Assert.IsNotNull(rb);
        Assert.IsFalse(rb.useGravity);

        Object.DestroyImmediate(debrisGO);
        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator SetupDebrisRecursive_SetsRendererGray()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        var debrisGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var renderer = debrisGO.GetComponent<Renderer>();

        Invoke(p, "SetupDebrisRecursive", debrisGO.transform);
        yield return null;

        Assert.AreEqual(Color.gray, renderer.material.color);

        Object.DestroyImmediate(debrisGO);
        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator SetupDebrisRecursive_ParticleSystem_DoesNotThrow()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        var debrisGO = new GameObject("Debris");
        var childPS = new GameObject("Particles");
        childPS.transform.SetParent(debrisGO.transform);
        childPS.AddComponent<ParticleSystem>();

        Assert.DoesNotThrow(() => Invoke(p, "SetupDebrisRecursive", debrisGO.transform));
        yield return null;

        Object.DestroyImmediate(debrisGO);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void TestTriggerOnDie_NullHitVFX_DoesNotThrow()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        SetField(p, "HitVFX", null);
        Assert.DoesNotThrow(() => p.TestTriggerOnDie(DamageContext.Suicide("test")));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void OnDestroy_DoesNotThrow()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Assert.DoesNotThrow(() => Invoke(p, "OnDestroy"));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ActivePlayers_IsInitialized()
    {
        Assert.IsNotNull(Player.ActivePlayers);
    }

    [Test]
    public void ActivePlayers_AddAndRemove()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Player.ActivePlayers[7777u] = p;
        Assert.IsTrue(Player.ActivePlayers.ContainsKey(7777u));
        Player.ActivePlayers.Remove(7777u);
        Assert.IsFalse(Player.ActivePlayers.ContainsKey(7777u));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ActivePlayers_TryGetValue_MissingKey_ReturnsFalse()
    {
        Player.ActivePlayers.Clear();
        bool found = Player.ActivePlayers.TryGetValue(9999u, out var result);
        Assert.IsFalse(found);
        Assert.IsNull(result);
    }

    [Test]
    public void HandleHullChange_NullHull_DoesNotThrow()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Assert.DoesNotThrow(() => Invoke(p, "HandleHullChange", new object[] { null }));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void KickForDuplicateAccount_NegativeId_DoesNotThrow()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Assert.DoesNotThrow(() => Invoke(p, "KickForDuplicateAccount", -1));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void KickForDuplicateAccount_ZeroId_DoesNotThrow()
    {
        var go = CreatePlayerGO();
        var p = go.GetComponent<Player>();
        Assert.DoesNotThrow(() => Invoke(p, "KickForDuplicateAccount", 0));
        Object.DestroyImmediate(go);
    }
}

public class PlayerWithServerTests
{
    private GameObject _transportGO;
    private GameObject _serverPlayerGO;
    private Player _serverPlayer;

    private T GetField<T>(object obj, string name)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {obj.GetType().Name}");
        return (T)f.GetValue(obj);
    }

    private void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {obj.GetType().Name}");
        f.SetValue(obj, value);
    }

    private void Invoke(object obj, string name, params object[] args)
    {
        var m = obj.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{name}' not found on {obj.GetType().Name}");
        m.Invoke(obj, args);
    }

    private GameObject CreatePlayerGO()
    {
        var go = new GameObject("Player");
        go.AddComponent<NetworkIdentity>();
        go.AddComponent<Rigidbody>();
        go.AddComponent<Health>();
        go.AddComponent<PlayerController>().enabled = false;
        go.AddComponent<ShipShooting>().enabled = false;
        go.AddComponent<ShipAssembler>().enabled = false;
        go.AddComponent<NetworkAudio>().enabled = false;
        go.AddComponent<Player>();
        return go;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        if (NetworkServer.active)
        {
            NetworkServer.DisconnectAll();
            NetworkServer.Shutdown();
        }
        if (NetworkClient.active)
            NetworkClient.Shutdown();

        yield return null;

        var resourcesGO = new GameObject("FakeGameResources");
        var resources = resourcesGO.AddComponent<GameResources>();
        resources.partDatabase = ScriptableObject.CreateInstance<ShipPartDatabase>();

        _transportGO = new GameObject("Transport");
        _transportGO.AddComponent<KcpTransport>();
        Transport.active = _transportGO.GetComponent<KcpTransport>();

        NetworkServer.Listen(1);
        yield return null;

        _serverPlayerGO = CreatePlayerGO();
        _serverPlayer = _serverPlayerGO.GetComponent<Player>();

        LogAssert.ignoreFailingMessages = true;
        NetworkServer.Spawn(_serverPlayerGO);
        yield return null;
        LogAssert.ignoreFailingMessages = false;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Player.ActivePlayers.Clear();

        if (_serverPlayerGO != null)
        {
            LogAssert.ignoreFailingMessages = true;
            NetworkServer.Destroy(_serverPlayerGO);
            _serverPlayerGO = null;
            yield return null;
            LogAssert.ignoreFailingMessages = false;
        }

        NetworkServer.DisconnectAll();
        NetworkServer.Shutdown();
        NetworkClient.Shutdown();
        yield return null;

        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            Object.DestroyImmediate(go);

        yield return null;
    }

    [UnityTest]
    public IEnumerator HandleHullChange_SetsMaxHealth_DirectCall()
    {
        var health = _serverPlayerGO.GetComponent<Health>();
        var hull = ScriptableObject.CreateInstance<HullData>();
        hull.maxHealth = 300f;

        Invoke(_serverPlayer, "HandleHullChange", hull);
        yield return null;

        Assert.AreEqual(300f, GetField<float>(health, "maxHealth"), 0.01f);

        Object.DestroyImmediate(hull);
    }

    [UnityTest]
    public IEnumerator KickForDuplicateAccount_NoDuplicates_DoesNotThrow()
    {
        _serverPlayer.ServerPlayerId = 42;
        Assert.DoesNotThrow(() => Invoke(_serverPlayer, "KickForDuplicateAccount", 42));
        yield return null;
    }

    [UnityTest]
    public IEnumerator SpawnDebris_WithServer_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Invoke(_serverPlayer, "SpawnDebris"));
        yield return null;
    }

}