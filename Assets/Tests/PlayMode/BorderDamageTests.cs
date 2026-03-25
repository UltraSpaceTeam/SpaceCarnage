using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Reflection;

public class BorderDamageTests
{
    private GameObject testGameObject;
    private BorderDamage borderDamage;
    private Health health;
    private Transform objectTransform;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        testGameObject = new GameObject("TestBorderDamageObject");
        objectTransform = testGameObject.transform;

        health = testGameObject.AddComponent<Health>();
        borderDamage = testGameObject.AddComponent<BorderDamage>();

        // Заполняем приватные поля
        SetPrivateField("_transform", objectTransform);
        SetPrivateField("_health", health);
        SetPrivateField("_isOutside", false);
        SetPrivateField("_damageCoroutine", null);

        health.SetMaxHealth(100f);
        BorderConfiguration.borderRadius = 10f;

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        if (testGameObject != null)
            Object.DestroyImmediate(testGameObject);

        BorderConfiguration.borderRadius = 100f; // возвращаем дефолт

        yield return null;
    }

    // ====================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ======================

    private void SetPrivateField(string fieldName, object value)
    {
        var field = typeof(BorderDamage).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(borderDamage, value);
    }

    private object GetPrivateField(string fieldName)
    {
        var field = typeof(BorderDamage).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(borderDamage);
    }

    // Явно указываем тип T
    private T CallPrivateMethod<T>(string methodName, object[] parameters = null)
    {
        var method = typeof(BorderDamage).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
            Assert.Fail($"Method '{methodName}' not found in BorderDamage");

        return (T)method.Invoke(borderDamage, parameters ?? new object[0]);
    }

    // Для методов без возвращаемого значения
    private void CallPrivateMethod(string methodName, object[] parameters = null)
    {
        var method = typeof(BorderDamage).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
            Assert.Fail($"Method '{methodName}' not found in BorderDamage");

        method.Invoke(borderDamage, parameters ?? new object[0]);
    }

    // ====================== ТЕСТЫ ======================

    [Test]
    public void IsOutsideBorder_WhenInsideBorder_ReturnsFalse()
    {
        objectTransform.position = new Vector3(5f, 0f, 5f);
        BorderConfiguration.borderRadius = 10f;

        var result = CallPrivateMethod<bool>("IsOutsideBorder");

        Assert.IsFalse(result);
    }

    [Test]
    public void IsOutsideBorder_WhenOutsideBorder_ReturnsTrue()
    {
        objectTransform.position = new Vector3(15f, 0f, 0f);
        BorderConfiguration.borderRadius = 10f;

        var result = CallPrivateMethod<bool>("IsOutsideBorder");

        Assert.IsTrue(result);
    }

    [Test]
    public void IsOutsideBorder_WhenExactlyOnBorder_ReturnsFalse()
    {
        objectTransform.position = new Vector3(10f, 0f, 0f);
        BorderConfiguration.borderRadius = 10f;

        var result = CallPrivateMethod<bool>("IsOutsideBorder");

        Assert.IsFalse(result);
    }

    [UnityTest]
    public IEnumerator FixedUpdate_WhenEnteringBorderZone_StartsDamageCoroutine()
    {
        objectTransform.position = new Vector3(15f, 0f, 0f); // за границей
        BorderConfiguration.borderRadius = 10f;

        // Вызываем FixedUpdate
        CallPrivateMethod("FixedUpdate");

        yield return null;

        bool isOutside = (bool)GetPrivateField("_isOutside");
        var damageCoroutine = GetPrivateField("_damageCoroutine");

        Assert.IsTrue(isOutside, "Should be marked as outside the border");
        Assert.IsNotNull(damageCoroutine, "Damage coroutine was not started");
    }

    [UnityTest]
    public IEnumerator FixedUpdate_WhenExitingBorderZone_StopsDamageCoroutine()
    {
        SetPrivateField("_isOutside", true); // имитируем, что раньше был снаружи
        objectTransform.position = new Vector3(5f, 0f, 0f); // внутри границы
        BorderConfiguration.borderRadius = 10f;

        CallPrivateMethod("FixedUpdate");

        yield return null;

        bool isOutside = (bool)GetPrivateField("_isOutside");
        var damageCoroutine = GetPrivateField("_damageCoroutine");

        Assert.IsFalse(isOutside, "Should be marked as inside the border");
        Assert.IsNull(damageCoroutine, "Damage coroutine was not stopped");
    }
}