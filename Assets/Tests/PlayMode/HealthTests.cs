using Mirror;
using kcp2k;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class HealthPlayModeTests
{
    private GameObject testObject;
    private Health health;
    private GameObject transportGO;
    private KcpTransport transport;
    private bool deathEventTriggered;
    private System.Action<DamageContext> deathHandler;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        var resourcesGO = new GameObject("FakeGameResources");
        var resources = resourcesGO.AddComponent<GameResources>();
        resources.partDatabase = ScriptableObject.CreateInstance<ShipPartDatabase>();

        transportGO = new GameObject("Transport");
        transport = transportGO.AddComponent<KcpTransport>();
        Transport.active = transport;
        NetworkServer.Listen(1);
        yield return null;

        testObject = new GameObject("TestHealthObject");
        testObject.AddComponent<NetworkIdentity>();

        health = testObject.AddComponent<Health>();
        health.SetMaxHealth(100f);

        deathEventTriggered = false;
        deathHandler = ctx => deathEventTriggered = true;
        health.OnDeath += deathHandler;

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        if (health != null && deathHandler != null)
        {
            health.OnDeath -= deathHandler;
            deathHandler = null;
        }

        NetworkServer.Shutdown();

        if (testObject != null) Object.DestroyImmediate(testObject);
        if (transportGO != null) Object.DestroyImmediate(transportGO);

        var fakeRes = GameObject.Find("FakeGameResources");
        if (fakeRes != null) Object.DestroyImmediate(fakeRes);

        yield return null;
    }

    [UnityTest]
    public IEnumerator TestSetMaxHealth()
    {
        health.SetMaxHealth(100f);
        Assert.AreEqual(100f, health.GetHealthPercentage() * 100f, "Health percentage should be 100%");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TestTakeDamageWeaponKillsPlayer()
    {
        health.SetMaxHealth(50f);

        var damage = DamageContext.Weapon(1, "Attacker", "Laser");
        health.TakeDamage(50f, damage);

        Assert.IsTrue(health.IsDead, "Player should be dead after lethal damage");
        Assert.IsTrue(deathEventTriggered, "OnDeath event should have been triggered");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TestTakeDamagePartial()
    {
        health.SetMaxHealth(100f);

        var damage = DamageContext.Weapon(1, "Attacker", "Laser");
        health.TakeDamage(30f, damage);

        Assert.IsFalse(health.IsDead, "Player should be alive after partial damage");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TestInvincibilityPreventsDamage()
    {
        health.SetMaxHealth(50f);
        health.SetInvincibility(true);

        var damage = DamageContext.Weapon(1, "Attacker", "Laser");
        health.TakeDamage(50f, damage);

        Assert.IsFalse(health.IsDead, "Player should be alive while invincible");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TestMultipleDamageEvents()
    {
        health.SetMaxHealth(100f);

        var damage1 = DamageContext.Weapon(1, "Attacker1", "Laser");
        var damage2 = DamageContext.Collision(0, "Asteroid", "Rock");
        var damage3 = DamageContext.Suicide("Player");

        health.TakeDamage(30f, damage1);
        Assert.IsFalse(health.IsDead, "After 30 damage player should be alive");

        health.TakeDamage(50f, damage2);
        Assert.IsFalse(health.IsDead, "After 50 more damage player should still be alive");

        health.TakeDamage(30f, damage3);
        Assert.IsTrue(health.IsDead, "After fatal suicide damage player should be dead");

        yield return null;
    }
}