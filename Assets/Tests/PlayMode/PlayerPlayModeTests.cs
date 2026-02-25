using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mirror;
using kcp2k;
using System.Collections;

public class PlayerPlayModeTests
{
    private GameObject playerGO;
    private Player player;
    private Health health;
    private PlayerController controller;
    private ShipShooting shooting;
    private ShipAssembler assembler;
    private NetworkAudio networkAudio;

    private GameObject transportGO;
    private KcpTransport transport;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        transportGO = new GameObject("TestTransport");
        transport = transportGO.AddComponent<KcpTransport>();
        Transport.active = transport;

        if (!NetworkServer.active)
        {
            NetworkServer.Listen(7777);
        }
        yield return null;

        var resGO = new GameObject("FakeResources");
        var res = resGO.AddComponent<GameResources>();

        playerGO = new GameObject("TestPlayer");
        playerGO.SetActive(false);

        playerGO.AddComponent<NetworkIdentity>();

        health = playerGO.AddComponent<Health>();
        controller = playerGO.AddComponent<PlayerController>();
        shooting = playerGO.AddComponent<ShipShooting>();
        assembler = playerGO.AddComponent<ShipAssembler>();
        networkAudio = playerGO.AddComponent<NetworkAudio>();

        player = playerGO.AddComponent<Player>();

        player.enabled = false;
        controller.enabled = false;
        shooting.enabled = false;
        assembler.enabled = false;
        networkAudio.enabled = false;

        health.SetMaxHealth(100f);

        playerGO.SetActive(true);
        yield return null;

        NetworkServer.Spawn(playerGO);
        yield return null;

        var conn = new NetworkConnectionToClient(999);
        NetworkServer.AddPlayerForConnection(conn, playerGO);
        yield return null;

        Debug.Log($"[SetUp] «авершено | netId = {player.netId} | isServer = {player.isServer}");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (NetworkServer.active)
            NetworkServer.Shutdown();

        if (playerGO != null)
            Object.DestroyImmediate(playerGO);

        if (transportGO != null)
            Object.DestroyImmediate(transportGO);

        var fakeRes = GameObject.Find("FakeGameResources");
        if (fakeRes != null)
            Object.DestroyImmediate(fakeRes);

        yield return null;
    }

    [Test]
    public void Awake_DidNotThrow_AndObjectSpawned()
    {
        Assert.IsTrue(player.isServer, "ѕосле Spawn должен быть серверным");
        Assert.IsNotNull(player.netIdentity, "NetworkIdentity должен быть назначен");
        Assert.Greater(player.netId, 0u, "netId должен быть присвоен");
    }

    [Test]
    public void IsActive_DefaultValue_IsTrue()
    {
        Assert.IsTrue(player.IsActive);
    }

    [Test]
    public void ServerPlayerId_CanBeSetAndRead()
    {
        player.ServerPlayerId = 12345;
        Assert.AreEqual(12345, player.ServerPlayerId);
    }

    [UnityTest]
    public IEnumerator OnDie_CanBeTriggeredViaTestHelper()
    {
        DamageContext ctx = DamageContext.Suicide("Test Suicide");

        Assert.DoesNotThrow(() =>
        {
            player.TestTriggerOnDie(ctx);
        });

        yield return null;

        Assert.Pass("OnDie успешно выполнен без исключений");
    }

    [UnityTest]
    public IEnumerator DeathViaHealth_TriggersServerLogic()
    {
        health.TakeDamage(2000f, DamageContext.Suicide("Lethal"));

        yield return null;

        Assert.IsTrue(health.IsDead);

        yield return null;
    }
}