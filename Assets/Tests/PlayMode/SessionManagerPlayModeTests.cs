using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mirror;
using Object = UnityEngine.Object;
using kcp2k;

public class SessionManagerPlayModeTests
{
    private GameObject sessionGO;
    private SessionManager manager;

    private T GetField<T>(object obj, string name)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found");
        return (T)f.GetValue(obj);
    }

    private void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found");
        f.SetValue(obj, value);
    }

    private void Invoke(object obj, string name, params object[] args)
    {
        var m = obj.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{name}' not found");
        m.Invoke(obj, args);
    }

    private Dictionary<uint, object> GetStatsDict()
    {
        return GetField<Dictionary<uint, object>>(manager, "matchStats");
    }

    [UnitySetUp]
    public IEnumerator Setup()
    {
        LogAssert.ignoreFailingMessages = true;

        sessionGO = new GameObject("SessionManager");
        manager = sessionGO.AddComponent<SessionManager>();

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        LogAssert.ignoreFailingMessages = false;

        if (sessionGO != null)
            Object.DestroyImmediate(sessionGO);

        Player.ActivePlayers.Clear();

        yield return null;
    }

    [Test]
    public void Awake_SetsSingleton()
    {
        Assert.AreEqual(manager, SessionManager.Instance);
    }

    [Test]
    public void FormatTime_Works()
    {
        string result = (string)typeof(SessionManager)
            .GetMethod("FormatTime", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(manager, new object[] { 125f });

        Assert.AreEqual("02:05", result);
    }

    [Test]
    public void ConnectPlayer_Null_DoesNothing()
    {
        Assert.DoesNotThrow(() => manager.ConnectPlayer(null));
    }

    [Test]
    public void DisconnectPlayer_Null_DoesNothing()
    {
        Assert.DoesNotThrow(() => manager.DisconnectPlayer(null));
    }

    [Test]
    public void SendTimerTo_NoServer_NoCrash()
    {
        var go = CreateFakePlayer();
        var player = go.GetComponent<Player>();

        Assert.DoesNotThrow(() => manager.SendTimerTo(player));

        Object.DestroyImmediate(go);
    }

    [Test]
    public void BroadcastTimer_NoPlayers_NoCrash()
    {
        Assert.DoesNotThrow(() => Invoke(manager, "BroadcastTimer"));
    }

    [UnityTest]
    public IEnumerator MatchTimerCoroutine_StartsCorrectState()
    {
        LogAssert.ignoreFailingMessages = true;

        var transportGO = new GameObject("Transport");
        Transport.active = transportGO.AddComponent<KcpTransport>();

        NetworkServer.Listen(1);
        yield return null;

        Assert.IsTrue(NetworkServer.active, "Server not active");

        var go = new GameObject("SessionManager");
        var manager = go.AddComponent<SessionManager>();

        var coroutine = typeof(SessionManager)
            .GetMethod("MatchTimerCoroutine", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(manager, null);

        Assert.IsNotNull(coroutine);

        NetworkServer.Shutdown();
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(transportGO);

        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void SendHealthcheck_NoGameData_NoCrash()
    {
        Assert.DoesNotThrow(() => Invoke(manager, "SendHealthcheck"));
    }

    private GameObject CreateFakePlayer()
    {
        var go = new GameObject("Player");

        go.AddComponent<NetworkIdentity>();
        go.AddComponent<Rigidbody>();
        go.AddComponent<Health>();
        go.AddComponent<PlayerController>().enabled = false;
        go.AddComponent<ShipAssembler>().enabled = false;
        go.AddComponent<ShipShooting>().enabled = false;
        go.AddComponent<NetworkAudio>().enabled = false;

        go.AddComponent<Player>();

        return go;
    }
}