using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mirror;
using System.Reflection;


public class BorderDamageTests
{
    private GameObject testGameObject;
    private BorderDamage borderDamage;
    private Health health;
    private NetworkIdentity networkIdentity;
    private Transform objectTransform;

    [SetUp]
    public void Setup()
    {
        testGameObject = new GameObject("TestPlayer");
        networkIdentity = testGameObject.AddComponent<NetworkIdentity>();
		var isLocalPlayerField = typeof(NetworkIdentity).GetField("_isLocalPlayer", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        isLocalPlayerField?.SetValue(networkIdentity, true);
        
        borderDamage = testGameObject.AddComponent<BorderDamage>();
        
        health = testGameObject.AddComponent<Health>();
        
        objectTransform = testGameObject.transform;
        
        SetPrivateField("_transform", objectTransform);
        SetPrivateField("_health", health);
		
        var isServerField = typeof(NetworkIdentity).GetField("_isServer", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        isServerField?.SetValue(networkIdentity, true);
        
        var isClientField = typeof(NetworkIdentity).GetField("_isClient", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        isClientField?.SetValue(networkIdentity, false);
    }	
	
	[TearDown]
    public void Teardown()
    {
        var instanceField = typeof(UIManager).GetField("Instance", 
            BindingFlags.Public | BindingFlags.Static);
        if (instanceField != null)
        {
            instanceField.SetValue(null, null);
        }
        
        if (testGameObject != null)
        {
            Object.DestroyImmediate(testGameObject);
        }
    }

    private void SetPrivateField(string fieldName, object value)
    {
        var field = typeof(BorderDamage).GetField(fieldName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(borderDamage, value);
    }

    private object GetPrivateField(string fieldName)
    {
        var field = typeof(BorderDamage).GetField(fieldName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field.GetValue(borderDamage);
    }
	
	private void CallPrivateMethod(string methodName, object[] parameters = null)
    {
        var method = typeof(BorderDamage).GetMethod(methodName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(borderDamage, parameters ?? new object[0]);
    }

    private T CallPrivateMethod<T>(string methodName, object[] parameters = null)
    {
        var method = typeof(BorderDamage).GetMethod(methodName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)method?.Invoke(borderDamage, parameters ?? new object[0]);
    }
	
	[Test]
	public void IsOutsideBorder_WhenInsideBorder_ReturnsFalse()
	{
		// Arrange
		objectTransform.position = new Vector3(5f, 0f, 5f);
		BorderConfiguration.borderRadius = 10f;
		
		// Act
		var result = CallPrivateMethod<bool>("IsOutsideBorder");
		
		// Assert
		Assert.IsFalse(result);
	}

	[Test]
	public void IsOutsideBorder_WhenOutsideBorder_ReturnsTrue()
	{
		// Arrange
		objectTransform.position = new Vector3(15f, 0f, 0f);
		BorderConfiguration.borderRadius = 10f;
		
		// Act
		var result = CallPrivateMethod<bool>("IsOutsideBorder");
		
		// Assert
		Assert.IsTrue(result);
	}

	[Test]
	public void IsOutsideBorder_WhenExactlyOnBorder_ReturnsFalse()
	{
		// Arrange
		objectTransform.position = new Vector3(10f, 0f, 0f);
		BorderConfiguration.borderRadius = 10f;
		
		// Act
		var result = CallPrivateMethod<bool>("IsOutsideBorder");
		
		// Assert
		Assert.IsFalse(result);
	}
	
	[Test]
    public void FixedUpdate_WhenEnteringBorderZone_StartsDamageCoroutine()
    {
        // Arrange
        objectTransform.position = new Vector3(15f, 0f, 0f);
		BorderConfiguration.borderRadius = 10f;
        
        //CallPrivateMethod("Awake");
        
        // Act
        CallPrivateMethod("FixedUpdate");
        
        // Assert
        bool isOutside = (bool)GetPrivateField("_isOutside");
        var damageCoroutine = GetPrivateField("_damageCoroutine");
        
        Assert.IsTrue(isOutside);
        Assert.IsNotNull(damageCoroutine);
    }
	
}