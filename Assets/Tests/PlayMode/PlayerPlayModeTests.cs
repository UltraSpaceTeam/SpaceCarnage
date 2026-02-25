using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mirror;
using System.Text.RegularExpressions;
using kcp2k;

public class PlayerPlayModeTests
{
    T GetField<T>(object obj, string name)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field {name} not found");
        return (T)f.GetValue(obj);
    }

    void Invoke(object obj, string name, params object[] args)
    {
        var m = obj.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(m, $"Method {name} not found");
        m.Invoke(obj, args);
    }

    GameObject CreatePlayer()
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

    [Test]
    public void SyncVars_DefaultValues()
    {
        var go = CreatePlayer();
        var p = go.GetComponent<Player>();

        Assert.IsTrue(p.IsActive);
        Assert.AreEqual(0, p.Kills);
        Assert.AreEqual(0, p.Deaths);
        Assert.AreEqual("Player", p.Nickname);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void RpcSetMatchTimer_SetsStatics_WithoutMirrorRuntime()
    {
        var go = CreatePlayer();
        var p = go.GetComponent<Player>();

        p.Test_SetMatchTimer(2, 10, 20);

        Assert.AreEqual(2, Player.ClientTimerState);
        Assert.AreEqual(10, Player.ClientMatchStartTime);
        Assert.AreEqual(20, Player.ClientEndingStartTime);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void RpcShowShield_NoPrefab_LogsWarning()
    {
        var go = CreatePlayer();
        var p = go.GetComponent<Player>();

        LogAssert.Expect(LogType.Warning, "Shield prefab not assigned in Player!");

        Invoke(p, "ShowShield", true, 1f);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void RpcShowShield_CreatesInstance()
    {
        var go = CreatePlayer();
        var p = go.GetComponent<Player>();

        var shieldPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.shieldBubblePrefab = shieldPrefab;

        Invoke(p, "ShowShield", true, 1f);

        var instance = GetField<GameObject>(p, "currentShieldInstance");

        Assert.IsNotNull(instance);
        Assert.IsTrue(instance.activeSelf);

        Object.DestroyImmediate(shieldPrefab);
        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator ServerHandleDeath_IncrementsDeaths()
    {
        var go = CreatePlayer();
        var p = go.GetComponent<Player>();

        var ctx = DamageContext.Suicide("test");

        Invoke(p, "HandleDeath", ctx);

        yield return null;

        Assert.AreEqual(1, p.Deaths);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void OnStopLocalPlayer_DisablesPlayer()
    {
        var go = CreatePlayer();
        var p = go.GetComponent<Player>();

        p.OnStopLocalPlayer();   // ? напрямую

        Assert.IsFalse(p.IsActive);

        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator RpcSpawnDebris_DoesNotCrash()
    {
        var go = CreatePlayer();
        var p = go.GetComponent<Player>();

        Assert.DoesNotThrow(() => Invoke(p, "SpawnDebris"));

        yield return null;

        Object.DestroyImmediate(go);
    }

    [Test]
    public void TestTriggerOnDie_NoCrash()
    {
        var go = CreatePlayer();
        var p = go.GetComponent<Player>();

        Assert.DoesNotThrow(() =>
            p.TestTriggerOnDie(DamageContext.Suicide("test")));

        Object.DestroyImmediate(go);
    }
}