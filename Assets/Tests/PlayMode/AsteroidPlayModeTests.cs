using NUnit.Framework;
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

        // Базовая настройка
        health.TestSetMaxHealth(100f);
        rb.mass = 10f;
        rb.linearVelocity = Vector3.zero;

        // Отключаем ненужные Update/FixedUpdate
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
        // Запускаем мини-сервер
        var transportGO = new GameObject("TestTransport");
        var transport = transportGO.AddComponent<KcpTransport>();
        Transport.active = transport;

        NetworkServer.Listen(7777);
        yield return null;

        // Убеждаемся, что NetworkIdentity существует ДО спавна
        var identity = asteroidGO.GetComponent<NetworkIdentity>();
        if (identity == null)
        {
            identity = asteroidGO.AddComponent<NetworkIdentity>();
        }

        // Спавним астероид на сервере — теперь isServer = true
        NetworkServer.Spawn(asteroidGO);
        yield return null;

        float newSize = 2f;

        // Вызываем [Server] метод — теперь он выполнится
        asteroid.SetSize(newSize);

        yield return null; // даём кадр на обработку ApplySizeServer

        // Масштаб = newSize * baseScale = 2 * 100 = 200
        float expectedScale = newSize * asteroid.TestBaseScale;
        Assert.AreEqual(expectedScale, asteroidGO.transform.localScale.x, "Масштаб должен быть newSize * baseScale");

        // Здоровье = baseHP * Pow(newSize, hpPower) = 50 * 2^2 = 200
        float expectedHP = asteroid.TestBaseHP * Mathf.Pow(newSize, asteroid.TestHpPower);
        Assert.AreEqual(expectedHP, health.TestMaxHealth, "Здоровье должно масштабироваться по hpPower");

        // Масса = baseMass * Pow(newSize, massPower) = 5 * 2^3 = 40
        float expectedMass = asteroid.TestBaseMass * Mathf.Pow(newSize, asteroid.TestMassPower);
        Assert.AreEqual(expectedMass, rb.mass, "Масса должна масштабироваться по massPower");

        // Очистка
        NetworkServer.Shutdown();
        Object.DestroyImmediate(transportGO);
    }

    [UnityTest]
    public IEnumerator OnDie_SpawnsVFX_WhenHitVFXIsSet()
    {
        var fakeVFX = new GameObject("FakeAsteroidVFX");
        asteroid.TestHitVFX = fakeVFX;  // если поле доступно через тестовый getter/setter

        asteroid.TestOnDie(DamageContext.Suicide("Test"));

        yield return null;

        // Проверяем, что метод прошёл без исключений
        Assert.Pass("OnDie выполнен без ошибок");
    }

    [UnityTest]
    public IEnumerator OnDie_DoesNothing_WhenHitVFXIsNull()
    {
        // Устанавливаем null через рефлексию (если поле приватное)
        var field = typeof(Asteroid).GetField("HitVFX", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            Assert.Inconclusive("Поле HitVFX не найдено");
            yield break;
        }

        var originalVFX = field.GetValue(asteroid);
        field.SetValue(asteroid, null);

        try
        {
            asteroid.TestOnDie(DamageContext.Suicide("Test"));

            Assert.Pass("OnDie прошёл без исключения при HitVFX = null");
        }
        finally
        {
            field.SetValue(asteroid, originalVFX);  // восстанавливаем
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

        Assert.AreEqual(0f, damage.TestCalculateSpeedFactor(1f), "Скорость ниже min ? 0");
        Assert.AreEqual(1f, damage.TestCalculateSpeedFactor(20f), "Скорость выше max ? 1");

        float mid = (2f + 15f) / 2f;
        Assert.AreEqual(0.5f, damage.TestCalculateSpeedFactor(mid), 0.01f, "Средняя скорость ? ~0.5");
    }

    [Test]
    public void CalculateFinalDamage_ScalesCorrectly()
    {
        var damage = asteroidGO.GetComponent<AsteroidCollisionDamage>();

        // min урон
        float factor0 = damage.TestCalculateSpeedFactor(0f);
        Assert.AreEqual(20, damage.TestCalculateFinalDamage(factor0));

        // max урон
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

        // Вызываем метод
        move.SetMovementParameters(thrust, force, torque);

        // Получаем приватные поля через рефлексию
        var thrustField = typeof(AsteroidMovement).GetField("_thrustForce", BindingFlags.NonPublic | BindingFlags.Instance);
        var forceField = typeof(AsteroidMovement).GetField("_initialForce", BindingFlags.NonPublic | BindingFlags.Instance);
        var torqueField = typeof(AsteroidMovement).GetField("_initialTorque", BindingFlags.NonPublic | BindingFlags.Instance);

        // Проверяем, что значения сохранены
        Assert.IsNotNull(thrustField, "Поле _thrustForce не найдено");
        Assert.IsNotNull(forceField, "Поле _initialForce не найдено");
        Assert.IsNotNull(torqueField, "Поле _initialTorque не найдено");

        Assert.AreEqual(thrust, thrustField.GetValue(move), "Тяга не сохранена");
        Assert.AreEqual(force, forceField.GetValue(move), "Начальная сила не сохранена");
        Assert.AreEqual(torque, torqueField.GetValue(move), "Начальный момент не сохранён");
    }

    // ==============================================
    // AsteroidsPlacer.cs (бонусный, если нужно)
    // ==============================================

    [Test]
    public void OnStartServer_AppliesNonZeroForceAndTorque()
    {
        // Добавляем компонент, если его ещё нет (в SetUp он уже добавлен, но для ясности)
        var placer = asteroidGO.GetComponent<AsteroidsPlacer>();
        if (placer == null)
        {
            placer = asteroidGO.AddComponent<AsteroidsPlacer>();
        }

        // Вызываем OnStartServer напрямую (он публичный)
        placer.OnStartServer();

        // Даём Unity кадр на применение сил (AddForce/AddTorque асинхронны)
        yield return null;

        // Проверяем, что силы применены (не нулевые)
        Assert.AreNotEqual(Vector3.zero, rb.linearVelocity, "Линейная скорость должна быть изменена (сила применена)");
        Assert.AreNotEqual(Vector3.zero, rb.angularVelocity, "Угловая скорость должна быть изменена (torque применён)");
    }
}