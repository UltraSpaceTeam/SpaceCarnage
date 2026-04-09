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

        // Устанавливаем здоровье напрямую через рефлексию (обходим Server-атрибут)
        var maxHealthField = typeof(Health).GetField("_maxHealth", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (maxHealthField != null)
            maxHealthField.SetValue(health, 100f);
        
        var currentHealthField = typeof(Health).GetField("_currentHealth", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (currentHealthField != null)
            currentHealthField.SetValue(health, 100f);

        // Заполняем приватные поля BorderDamage
        var transformField = typeof(BorderDamage).GetField("_transform", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        transformField?.SetValue(borderDamage, objectTransform);
        
        var healthField = typeof(BorderDamage).GetField("_health", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        healthField?.SetValue(borderDamage, health);
        
        var isOutsideField = typeof(BorderDamage).GetField("_isOutside", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        isOutsideField?.SetValue(borderDamage, false);
        
        var damageCoroutineField = typeof(BorderDamage).GetField("_damageCoroutine", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        damageCoroutineField?.SetValue(borderDamage, null);

        BorderConfiguration.borderRadius = 10f;

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        if (testGameObject != null)
            Object.DestroyImmediate(testGameObject);

        BorderConfiguration.borderRadius = 100f;

        yield return null;
    }

    // ====================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ======================

    private void SetPrivateField(string fieldName, object value)
    {
        var field = typeof(BorderDamage).GetField(fieldName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(borderDamage, value);
    }

    private object GetPrivateField(string fieldName)
    {
        var field = typeof(BorderDamage).GetField(fieldName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(borderDamage);
    }

    private T CallPrivateMethod<T>(string methodName, object[] parameters = null)
    {
        var method = typeof(BorderDamage).GetMethod(methodName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
            Assert.Fail($"Method '{methodName}' not found in BorderDamage");

        return (T)method.Invoke(borderDamage, parameters ?? new object[0]);
    }

    private void CallPrivateMethod(string methodName, object[] parameters = null)
    {
        var method = typeof(BorderDamage).GetMethod(methodName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
            Assert.Fail($"Method '{methodName}' not found in BorderDamage");

        method.Invoke(borderDamage, parameters ?? new object[0]);
    }

    private float GetCurrentHealth()
    {
        var currentHealthField = typeof(Health).GetField("_currentHealth", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (currentHealthField != null)
            return (float)currentHealthField.GetValue(health);
        
        return 100f;
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
        // Arrange
        objectTransform.position = new Vector3(15f, 0f, 0f);
        BorderConfiguration.borderRadius = 10f;

        // Act
        CallPrivateMethod("FixedUpdate");
        yield return null;

        // Assert
        bool isOutside = (bool)GetPrivateField("_isOutside");
        Assert.IsTrue(isOutside, "Should be marked as outside the border");
        
        // Проверяем, что урон начал наноситься
        float initialHealth = GetCurrentHealth();
        yield return new WaitForSeconds(0.3f);
        
        Assert.Less(GetCurrentHealth(), initialHealth, 
            "Health should decrease, confirming damage coroutine started");
    }

    [UnityTest]
    public IEnumerator FixedUpdate_WhenExitingBorderZone_StopsDamageCoroutine()
    {
        // Arrange - сначала устанавливаем объект СНАРУЖИ
        objectTransform.position = new Vector3(15f, 0f, 0f);
        BorderConfiguration.borderRadius = 10f;
        
        // Вызываем FixedUpdate, чтобы объект отметился как "снаружи"
        CallPrivateMethod("FixedUpdate");
        yield return null;
        
        // Проверяем, что объект действительно снаружи
        bool isOutsideBefore = (bool)GetPrivateField("_isOutside");
        Assert.IsTrue(isOutsideBefore, "Object should be marked as outside before moving");
        
        // Act - перемещаем объект ВНУТРЬ границы
        objectTransform.position = new Vector3(5f, 0f, 0f);
        
        // Вызываем FixedUpdate снова, чтобы обновить статус
        CallPrivateMethod("FixedUpdate");
        yield return null;
        
        // Assert
        bool isOutsideAfter = (bool)GetPrivateField("_isOutside");
        var damageCoroutine = GetPrivateField("_damageCoroutine");
        
        Assert.IsFalse(isOutsideAfter, "Should be marked as inside the border");
        Assert.IsNull(damageCoroutine, "Damage coroutine should be stopped");
    }

    [Test]
public void QuickDiagnostic()
{
    objectTransform.position = new Vector3(15f, 0f, 0f);
    BorderConfiguration.borderRadius = 10f;
    
    // Проверяем, активен ли компонент
    Assert.IsTrue(borderDamage.enabled, "BorderDamage component should be enabled");
    Assert.IsTrue(borderDamage.gameObject.activeInHierarchy, "GameObject should be active");
    
    // Проверяем transform
    Assert.IsNotNull(borderDamage.transform, "Transform should not be null");
    Debug.Log($"Transform position: {borderDamage.transform.position}");
    Debug.Log($"Border radius: {BorderConfiguration.borderRadius}");
    Debug.Log($"Distance from center: {borderDamage.transform.position.magnitude}");
    
    // Проверяем, что IsOutsideBorder вычисляется правильно
    var isOutsideMethod = typeof(BorderDamage).GetMethod("IsOutsideBorder", 
        BindingFlags.NonPublic | BindingFlags.Instance);
    var result = (bool)isOutsideMethod.Invoke(borderDamage, null);
    Debug.Log($"IsOutsideBorder result: {result}");
    
    Assert.IsTrue(result, "IsOutsideBorder should return true for position outside border");
}
}

