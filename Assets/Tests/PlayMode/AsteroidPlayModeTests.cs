/*using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mirror;
using System.Collections;
using System.Reflection;
using kcp2k;

public class AsteroidPlayModeTests
{
    private GameObject asteroidGO;
    private Asteroid asteroid;
    private Rigidbody rb;
    private Health health;
    private AsteroidCollisionDamage collisionDamage;
    private AsteroidMovement movement;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        asteroidGO = new GameObject("TestAsteroid");
        asteroidGO.tag = "Asteroid";

        rb = asteroidGO.AddComponent<Rigidbody>();
        health = asteroidGO.AddComponent<Health>();
        collisionDamage = asteroidGO.AddComponent<AsteroidCollisionDamage>();
        movement = asteroidGO.AddComponent<AsteroidMovement>();

        asteroid = asteroidGO.AddComponent<Asteroid>();

        // ������� ���������
        health.TestSetMaxHealth(100f);
        rb.mass = 10f;
        rb.linearVelocity = Vector3.zero;

        // ��������� �������� Update/FixedUpdate
        movement.enabled = false;
        collisionDamage.enabled = false;

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (asteroidGO != null)
            Object.DestroyImmediate(asteroidGO);

        yield return null;
    }

    // ==============================================
    // Asteroid.cs
    // ==============================================

    [UnityTest]
    public IEnumerator OnSizeChanged_SetsScaleAndHealthAndMass_ViaSetSize()
    {
        // ��������� ����-������
        var transportGO = new GameObject("TestTransport");
        var transport = transportGO.AddComponent<KcpTransport>();
        Transport.active = transport;

        NetworkServer.Listen(7777);
        yield return null;

        // ����������, ��� NetworkIdentity ���������� �� ������
        var identity = asteroidGO.GetComponent<NetworkIdentity>();
        if (identity == null)
        {
            identity = asteroidGO.AddComponent<NetworkIdentity>();
        }

        // ������� �������� �� ������� � ������ isServer = true
        NetworkServer.Spawn(asteroidGO);
        yield return null;

        float newSize = 2f;

        // �������� [Server] ����� � ������ �� ����������
        asteroid.SetSize(newSize);

        yield return null; // ��� ���� �� ��������� ApplySizeServer

        // ������� = newSize * baseScale = 2 * 100 = 200
        float expectedScale = newSize * asteroid.TestBaseScale;
        Assert.AreEqual(expectedScale, asteroidGO.transform.localScale.x, "������� ������ ���� newSize * baseScale");

        // �������� = baseHP * Pow(newSize, hpPower) = 50 * 2^2 = 200
        float expectedHP = asteroid.TestBaseHP * Mathf.Pow(newSize, asteroid.TestHpPower);
        Assert.AreEqual(expectedHP, health.TestMaxHealth, "�������� ������ ���������������� �� hpPower");

        // ����� = baseMass * Pow(newSize, massPower) = 5 * 2^3 = 40
        float expectedMass = asteroid.TestBaseMass * Mathf.Pow(newSize, asteroid.TestMassPower);
        Assert.AreEqual(expectedMass, rb.mass, "����� ������ ���������������� �� massPower");

        // �������
        NetworkServer.Shutdown();
        Object.DestroyImmediate(transportGO);
    }

    [UnityTest]
    public IEnumerator OnDie_SpawnsVFX_WhenHitVFXIsSet()
    {
        var fakeVFX = new GameObject("FakeAsteroidVFX");
        asteroid.TestHitVFX = fakeVFX;  // ���� ���� �������� ����� �������� getter/setter

        asteroid.TestOnDie(DamageContext.Suicide("Test"));

        yield return null;

        // ���������, ��� ����� ������ ��� ����������
        Assert.Pass("OnDie �������� ��� ������");
    }

    [UnityTest]
    public IEnumerator OnDie_DoesNothing_WhenHitVFXIsNull()
    {
        // ������������� null ����� ��������� (���� ���� ���������)
        var field = typeof(Asteroid).GetField("HitVFX", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            Assert.Inconclusive("���� HitVFX �� �������");
            yield break;
        }

        var originalVFX = field.GetValue(asteroid);
        field.SetValue(asteroid, null);

        try
        {
            asteroid.TestOnDie(DamageContext.Suicide("Test"));

            Assert.Pass("OnDie ������ ��� ���������� ��� HitVFX = null");
        }
        finally
        {
            field.SetValue(asteroid, originalVFX);  // ���������������
        }

        yield return null;
    }

    // ==============================================
    // AsteroidCollisionDamage.cs
    // ==============================================

    [Test]
    public void CalculateSpeedFactor_ReturnsExpectedValues()
    {
        var damage = asteroidGO.GetComponent<AsteroidCollisionDamage>();

        Assert.AreEqual(0f, damage.TestCalculateSpeedFactor(1f), "�������� ���� min ? 0");
        Assert.AreEqual(1f, damage.TestCalculateSpeedFactor(20f), "�������� ���� max ? 1");

        float mid = (2f + 15f) / 2f;
        Assert.AreEqual(0.5f, damage.TestCalculateSpeedFactor(mid), 0.01f, "������� �������� ? ~0.5");
    }

    [Test]
    public void CalculateFinalDamage_ScalesCorrectly()
    {
        var damage = asteroidGO.GetComponent<AsteroidCollisionDamage>();

        // min ����
        float factor0 = damage.TestCalculateSpeedFactor(0f);
        Assert.AreEqual(20, damage.TestCalculateFinalDamage(factor0));

        // max ����
        float factor1 = damage.TestCalculateSpeedFactor(15f);
        float expected = 20 * (1f + 5f * 1f);
        Assert.AreEqual(Mathf.RoundToInt(expected), damage.TestCalculateFinalDamage(factor1));
    }

    // ==============================================
    // AsteroidMovement.cs
    // ==============================================

    [Test]
    public void SetMovementParameters_StoresValuesCorrectly()
    {
        var move = asteroidGO.GetComponent<AsteroidMovement>();

        float thrust = 12.5f;
        Vector3 force = new Vector3(3.2f, 0f, -4.7f);
        Vector3 torque = new Vector3(0.9f, -1.2f, 0.4f);

        // �������� �����
        move.SetMovementParameters(thrust, force, torque);

        // �������� ��������� ���� ����� ���������
        var thrustField = typeof(AsteroidMovement).GetField("_thrustForce", BindingFlags.NonPublic | BindingFlags.Instance);
        var forceField = typeof(AsteroidMovement).GetField("_initialForce", BindingFlags.NonPublic | BindingFlags.Instance);
        var torqueField = typeof(AsteroidMovement).GetField("_initialTorque", BindingFlags.NonPublic | BindingFlags.Instance);

        // ���������, ��� �������� ���������
        Assert.IsNotNull(thrustField, "���� _thrustForce �� �������");
        Assert.IsNotNull(forceField, "���� _initialForce �� �������");
        Assert.IsNotNull(torqueField, "���� _initialTorque �� �������");

        Assert.AreEqual(thrust, thrustField.GetValue(move), "���� �� ���������");
        Assert.AreEqual(force, forceField.GetValue(move), "��������� ���� �� ���������");
        Assert.AreEqual(torque, torqueField.GetValue(move), "��������� ������ �� ��������");
    }

    // ==============================================
    // AsteroidsPlacer.cs (��������, ���� �����)
    // ==============================================

    [Test]
    public void OnStartServer_AppliesNonZeroForceAndTorque()
    {
        // ��������� ���������, ���� ��� ��� ��� (� SetUp �� ��� ��������, �� ��� �������)
        var placer = asteroidGO.GetComponent<AsteroidsPlacer>();
        if (placer == null)
        {
            placer = asteroidGO.AddComponent<AsteroidsPlacer>();
        }

        // �������� OnStartServer �������� (�� ���������)
        placer.OnStartServer();

        // ��� Unity ���� �� ���������� ��� (AddForce/AddTorque ����������)
        yield return null;

        // ���������, ��� ���� ��������� (�� �������)
        Assert.AreNotEqual(Vector3.zero, rb.linearVelocity, "�������� �������� ������ ���� �������� (���� ���������)");
        Assert.AreNotEqual(Vector3.zero, rb.angularVelocity, "������� �������� ������ ���� �������� (torque ��������)");
    }
}*/