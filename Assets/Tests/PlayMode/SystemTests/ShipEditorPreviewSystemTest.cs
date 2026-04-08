using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Linq;

[Category("SystemTest")]
public class ShipEditorPreviewSystemTest
{
    private ShipEditorUI _shipEditorUI;
    private TextMeshProUGUI _shipStatsText;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 05] === SETUP ===");

        AggressiveCleanup();

        yield return SceneManager.LoadSceneAsync("ShipEditor", LoadSceneMode.Single);
        yield return new WaitForSeconds(1.2f);

        _shipEditorUI = Object.FindAnyObjectByType<ShipEditorUI>();
        Assert.NotNull(_shipEditorUI, "ShipEditorUI not found");

        _shipStatsText = FindStatsText();
        Assert.NotNull(_shipStatsText, "Ship stats TextMeshProUGUI not found");

        Debug.Log("[System Test 05] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_ComponentHover_ShowsPreview_And_Selection_UpdatesStats()
    {
        Debug.Log("[System Test 05] === TEST START ===");

        var hullSlot0 = FindSlotButton("hullSlots", 0);
        var hullSlot1 = FindSlotButton("hullSlots", 1);

        Assert.NotNull(hullSlot0);
        Assert.NotNull(hullSlot1);

        SimulatePointerEnter(hullSlot0);
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(_shipStatsText.text.Contains("Health"), "Hull hover did not show Health");

        hullSlot0.onClick.Invoke();
        yield return new WaitForSeconds(0.7f);

        string afterHull0 = _shipStatsText.text;

        hullSlot1.onClick.Invoke();
        yield return new WaitForSeconds(0.7f);

        Assert.AreNotEqual(afterHull0, _shipStatsText.text, "Stats did not update after changing hull");

        Debug.Log("[System Test 05] Hull hover + selection - PASSED");

        var weaponSlot0 = FindSlotButton("weaponSlots", 0);
        var weaponSlot1 = FindSlotButton("weaponSlots", 1);

        Assert.NotNull(weaponSlot0);
        Assert.NotNull(weaponSlot1);

        SimulatePointerEnter(weaponSlot0);
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(_shipStatsText.text.Contains("Damage"), "Weapon hover did not show Damage");

        weaponSlot0.onClick.Invoke();
        yield return new WaitForSeconds(0.7f);

        string afterWeapon0 = _shipStatsText.text;

        weaponSlot1.onClick.Invoke();
        yield return new WaitForSeconds(0.7f);

        Assert.AreNotEqual(afterWeapon0, _shipStatsText.text, "Stats did not update after changing weapon");

        Debug.Log("[System Test 05] Weapon hover + selection - PASSED");

        var engineSlot0 = FindSlotButton("engineSlots", 0);
        var engineSlot1 = FindSlotButton("engineSlots", 1);

        Assert.NotNull(engineSlot0);
        Assert.NotNull(engineSlot1);

        SimulatePointerEnter(engineSlot0);
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(_shipStatsText.text.Contains("Power") || _shipStatsText.text.Contains("Mass"),
            "Engine hover did not show Power/Mass");

        engineSlot0.onClick.Invoke();
        yield return new WaitForSeconds(0.7f);

        string afterEngine0 = _shipStatsText.text;

        engineSlot1.onClick.Invoke();
        yield return new WaitForSeconds(0.7f);

        Assert.AreNotEqual(afterEngine0, _shipStatsText.text, "Stats did not update after changing engine");

        Debug.Log("[System Test 05] Engine hover + selection - PASSED");

        Debug.Log("[System Test 05] === ALL COMPONENTS TEST PASSED ===");
    }

    private TextMeshProUGUI FindStatsText()
    {
        var allTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        return allTexts.FirstOrDefault(t => t.text.Contains("SHIP STATISTICS") ||
                                            t.text.Contains("Health") ||
                                            t.text.Contains("Damage"));
    }

    private Button FindSlotButton(string fieldName, int index)
    {
        var field = typeof(ShipEditorUI).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) return null;

        var slots = field.GetValue(_shipEditorUI) as Button[];
        if (slots == null || index >= slots.Length) return null;

        return slots[index];
    }

    private void SimulatePointerEnter(Button button)
    {
        var context = button.GetComponent<ShowContextPanel>();
        if (context != null)
        {
            var method = typeof(ShowContextPanel).GetMethod("OnPointerEnter", BindingFlags.Public | BindingFlags.Instance);
            method?.Invoke(context, new object[] { new UnityEngine.EventSystems.PointerEventData(null) });
        }
    }

    private void AggressiveCleanup()
    {
        var managers = Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var m in managers)
            if (m != null) Object.DestroyImmediate(m.gameObject);

        ResetSingleton<UIManager>();
        ResetSingleton<GameResources>();
    }

    private void ResetSingleton<T>() where T : MonoBehaviour
    {
        var field = typeof(T).GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }
}