using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SaveConfigurationTests
{
    private Player _hostPlayer;
	

	private void CallPrivateMethod(object _object, string methodName, object[] parameters = null)
    {
        var method = _object.GetType().GetMethod(methodName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(_object, parameters ?? new object[0]);
    }
	
	private void SetPrivateField(object _object, string fieldName, object value)
    {
        var field = _object.GetType().GetField(fieldName, 
            BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(_object, value);
    }

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[SaveConfigurationTests] === SETUP ===");

        // Полная очистка Mirror
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();
        Player.ActivePlayers.Clear();

        yield return SceneManager.LoadSceneAsync("ShipEditor", LoadSceneMode.Single);
        yield return new WaitForSeconds(1.5f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        yield return null;
    }

    [UnityTest]
    public IEnumerator ConfigurationChange_Saved_And_Persist()
    {
        Debug.Log("[Test 17] === TEST START ===");
		var shipEditor = Object.FindAnyObjectByType<ShipEditorUI>();
		
		CallPrivateMethod(shipEditor, "SelectComponent",  
			new object[] { shipEditor.hullComponents[3], ShipComponentType.Hull });
			
		yield return new WaitForSeconds(0.1f);
		
		CallPrivateMethod(shipEditor, "SelectComponent",  
			new object[] { shipEditor.hullComponents[0], ShipComponentType.Hull });
			
		yield return new WaitForSeconds(0.1f);
		
        yield return SceneManager.UnloadSceneAsync("ShipEditor");
        yield return new WaitForSeconds(1.5f);
        yield return SceneManager.LoadSceneAsync("ShipEditor", LoadSceneMode.Single);
        yield return new WaitForSeconds(1.5f);
		shipEditor = Object.FindAnyObjectByType<ShipEditorUI>();
		var selectedComponentFields = typeof(ShipEditorUI).GetField("selectedComponents", 
			BindingFlags.NonPublic | 
			BindingFlags.Instance);
		
		var selectedComponent = selectedComponentFields.GetValue(shipEditor) as Dictionary<ShipComponentType, ShipComponent>;
		
		Assert.AreEqual(shipEditor.hullComponents[0], selectedComponent[ShipComponentType.Hull], "Configuration is not saved properly");

        yield return new WaitForSeconds(1.0f);
    }
	
	[UnityTest]
    public IEnumerator ConfigurationMisspeled_LoadDefaultData()
    {
        Debug.Log("[Test 16] === TEST START ===");
		var shipEditor = Object.FindAnyObjectByType<ShipEditorUI>();
		
		var field = typeof(ShipConfigManager).GetField("filePath", 
            BindingFlags.NonPublic | BindingFlags.Static);
        field.SetValue(null, Path.Combine(Application.dataPath, "Tests", "Configs", "misspelled_config.cfg"));
							
		CallPrivateMethod(shipEditor, "LoadSavedConfiguration");
        Debug.Log("[Test 16] Saved configuration");
		
		LogAssert.Expect(LogType.Error, "Failed to load config: JSON parse error: Missing a name for object member.");
		
		yield return new WaitForSeconds(0.5f);
		
		var selectedComponentFields = typeof(ShipEditorUI).GetField("selectedComponents", 
			BindingFlags.NonPublic | 
			BindingFlags.Instance);
		
		var selectedComponent = selectedComponentFields.GetValue(shipEditor) as Dictionary<ShipComponentType, ShipComponent>;
		
		Assert.AreEqual(0, selectedComponent[ShipComponentType.Hull].componentId, "Configuration is not read properly");
		Assert.AreEqual(0, selectedComponent[ShipComponentType.Engine].componentId, "Configuration is not read properly");
		Assert.AreEqual(0, selectedComponent[ShipComponentType.Weapon].componentId, "Configuration is not read properly");
    }

}

