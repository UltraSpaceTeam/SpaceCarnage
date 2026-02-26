using NUnit.Framework;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Reflection;

namespace ShipEditorTests
{
    [TestFixture]
    public class ShowContextPanelTests
    {
        private ShowContextPanel contextPanel;
        private GameObject panelGO;
        private GameObject contextMenuGO;
        private TextMeshProUGUI statsText;
        
        [SetUp]
        public void SetUp()
        {
            panelGO = new GameObject("ContextPanel");
            contextPanel = panelGO.AddComponent<ShowContextPanel>();
            
            contextMenuGO = new GameObject("ContextMenu");
            SetPrivateField(contextPanel, "contextMenuPanel", contextMenuGO);
            
            var textGO = new GameObject("StatsText");
            statsText = textGO.AddComponent<TextMeshProUGUI>();
            SetPrivateField(contextPanel, "shipStatsText", statsText);
        }

        [Test]
        public void OnPointerEnter_WithHullData_ShouldShowHealthAndMass()
        {
            var hullData = ScriptableObject.CreateInstance<HullData>();
            hullData.maxHealth = 150;
            hullData.mass = 75.5f;
            SetPrivateField(contextPanel, "component", hullData);
            
            var eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            var eventData = new PointerEventData(eventSystem);
            
            contextPanel.OnPointerEnter(eventData);
            
            Assert.IsTrue(contextMenuGO.activeSelf);
            StringAssert.Contains("Health: 150", statsText.text);
            StringAssert.Contains("Mass: 75.5", statsText.text);
            
            Object.DestroyImmediate(eventSystem.gameObject);
        }

        [Test]
        public void OnPointerEnter_WithWeaponData_ShouldShowWeaponStats()
        {
            var weaponData = ScriptableObject.CreateInstance<WeaponData>();
            weaponData.damage = 45;
            weaponData.fireRate = 2.5f;
            weaponData.range = 100f;
            weaponData.mass = 22.5f;
            SetPrivateField(contextPanel, "component", weaponData);
            
            var eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            var eventData = new PointerEventData(eventSystem);
            
            contextPanel.OnPointerEnter(eventData);
            
            StringAssert.Contains("Damage: 45", statsText.text);
            StringAssert.Contains("Rate of fire: 2.5", statsText.text);
            StringAssert.Contains("Range: 100", statsText.text);
            StringAssert.Contains("Mass: 22.5", statsText.text);
            
            Object.DestroyImmediate(eventSystem.gameObject);
        }

        [Test]
        public void OnPointerEnter_WithEngineData_ShouldShowEngineStats()
        {
            var engineData = ScriptableObject.CreateInstance<EngineData>();
            engineData.power = 120;
            engineData.mass = 35;
            SetPrivateField(contextPanel, "component", engineData);
            
            var eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            var eventData = new PointerEventData(eventSystem);
            
            contextPanel.OnPointerEnter(eventData);
            
            StringAssert.Contains("Power: 120", statsText.text);
            StringAssert.Contains("Mass: 35", statsText.text);
            
            Object.DestroyImmediate(eventSystem.gameObject);
        }

        [Test]
        public void OnPointerExit_ShouldHidePanel()
        {
            contextMenuGO.SetActive(true);
            var eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            var eventData = new PointerEventData(eventSystem);
            
            contextPanel.OnPointerExit(eventData);
            
            Assert.IsFalse(contextMenuGO.activeSelf);
            
            Object.DestroyImmediate(eventSystem.gameObject);
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, 
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            field?.SetValue(obj, value);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(panelGO);
            Object.DestroyImmediate(contextMenuGO);
            Object.DestroyImmediate(statsText.gameObject);
        }
    }
}