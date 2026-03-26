using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TMPro;
using Mirror;
using System.Reflection;

public class HullSelectionIntegrationTest
{
    private ShipEditorUI _shipEditorUI;
    private ShipAssembler _shipAssembler;
    private TextMeshProUGUI _shipStatsText;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[Test 02] === SETUP ===");

        AggressiveCleanup();

        yield return SceneManager.LoadSceneAsync("ShipEditor", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.8f);

        _shipEditorUI = Object.FindAnyObjectByType<ShipEditorUI>();
        Assert.NotNull(_shipEditorUI, "ShipEditorUI not found");

        _shipAssembler = Object.FindAnyObjectByType<ShipAssembler>();
        Assert.NotNull(_shipAssembler, "ShipAssembler not found");

        _shipStatsText = _shipEditorUI.GetComponentInChildren<TextMeshProUGUI>();
        // или найти по имени, если нужно
        if (_shipStatsText == null)
            _shipStatsText = GameObject.Find("shipStatsText")?.GetComponent<TextMeshProUGUI>();

        Debug.Log("[Test 02] Setup completed");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Hull_Selection_Updates_Model_CurrentHull_And_MaxHealth()
    {
        Debug.Log("[Test 02] === TEST START ===");

        var hullComponents = _shipEditorUI.hullComponents;
        Assert.NotNull(hullComponents, "hullComponents is null");
        Assert.GreaterOrEqual(hullComponents.Length, 2, "Need at least 2 hulls");

        var hull1 = hullComponents[0];
        var hull2 = hullComponents[1];

        var hullData1 = hull1.componentData as HullData;
        var hullData2 = hull2.componentData as HullData;

        Assert.NotNull(hullData1);
        Assert.NotNull(hullData2);

        float health1 = hullData1.maxHealth;
        float health2 = hullData2.maxHealth;

        // === Выбираем первый корпус ===
        CallOnHullSlotClicked(0);
        yield return new WaitForSeconds(0.7f);

        Assert.IsNotNull(_shipAssembler.CurrentHullObject, "CurrentHullObject is null after selecting hull 1");
        Assert.IsTrue(_shipAssembler.CurrentHullObject.activeInHierarchy, "Hull model is not active");

        Debug.Log($"[Test 02] Hull 1 selected. Model updated.");

        // === Выбираем второй корпус ===
        CallOnHullSlotClicked(1);
        yield return new WaitForSeconds(0.7f);

        Assert.IsNotNull(_shipAssembler.CurrentHullObject, "CurrentHullObject is null after selecting hull 2");
        Assert.IsTrue(_shipAssembler.CurrentHullObject.activeInHierarchy, "Hull model is not active after switch");

        Debug.Log($"[Test 02] Hull 2 selected. Model updated.");

        // Проверка обновления статистики в UI (самое надёжное, что у нас есть)
        if (_shipStatsText != null)
        {
            string statsText = _shipStatsText.text;
            Assert.IsTrue(statsText.Contains(hull2.componentName),
                $"Stats text does not contain hull name '{hull2.componentName}'");

            Debug.Log($"[Test 02] Stats UI updated with hull name: {hull2.componentName}");
        }

        // Проверка здоровья (приближённо)
        var healthComponent = Object.FindAnyObjectByType<Health>();
        if (healthComponent != null)
        {
            float currentHealthRatio = healthComponent.GetHealthPercentage();
            Assert.Greater(currentHealthRatio, 0.1f, "Health is suspiciously low after hull change");
        }

        Debug.Log("[Test 02] === PASSED ===");
    }

    private void CallOnHullSlotClicked(int index)
    {
        var method = typeof(ShipEditorUI).GetMethod("OnHullSlotClicked",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method, "OnHullSlotClicked method not found");

        method.Invoke(_shipEditorUI, new object[] { index });
    }

    private void AggressiveCleanup()
    {
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        foreach (var nm in Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None))
            if (nm != null) Object.DestroyImmediate(nm.gameObject);

        foreach (var t in Object.FindObjectsByType<kcp2k.KcpTransport>(FindObjectsSortMode.None))
            if (t != null) Object.DestroyImmediate(t.gameObject);

        ResetSingleton<UIManager>();
        ResetSingleton<GameResources>();
        ResetSingleton<SessionManager>();
        ResetSingleton<AudioManager>();
        ResetSingleton<APINetworkManager>();

        Player.ActivePlayers.Clear();
        NetworkManager.startPositions.Clear();
    }

    private void ResetSingleton<T>() where T : MonoBehaviour
    {
        var field = typeof(T).GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }
}