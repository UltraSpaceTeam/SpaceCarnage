using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using System.Collections.Generic;

namespace ShipEditorTests
{
    [TestFixture]
    public class ShipEditorInitializationTests
    {
        private ShipEditorUI editor;
        private GameObject editorGO;
        
        [SetUp]
        public void SetUp()
        {
            editorGO = new GameObject("ShipEditor");
            editor = editorGO.AddComponent<ShipEditorUI>();
            
            CreateUIElements();
            
            CreateTestComponents();
        }

        private void CreateUIElements()
        {
            var battleButtonGO = new GameObject("BattleButton");
            var battleButton = battleButtonGO.AddComponent<Button>();
            SetPrivateField(editor, "battleButton", battleButton);
            
            var statsTextGO = new GameObject("StatsText");
            var statsText = statsTextGO.AddComponent<TextMeshProUGUI>();
            SetPrivateField(editor, "shipStatsText", statsText);
            
            var settingsMenuGO = new GameObject("SettingsMenuButton");
            var settingsMenuButton = settingsMenuGO.AddComponent<Button>();
            SetPrivateField(editor, "settingsMenuButton", settingsMenuButton);
            
            var dropdownGO = new GameObject("DropdownPanel");
            SetPrivateField(editor, "dropdownPanel", dropdownGO);
            
            var leaderboardGO = new GameObject("LeaderboardButton");
            var leaderboardButton = leaderboardGO.AddComponent<Button>();
            SetPrivateField(editor, "leaderboardButton", leaderboardButton);
            
            var settingsGO = new GameObject("SettingsButton");
            var settingsButton = settingsGO.AddComponent<Button>();
            SetPrivateField(editor, "settingsButton", settingsButton);
            
            var logoutGO = new GameObject("LogoutButton");
            var logoutButton = logoutGO.AddComponent<Button>();
            SetPrivateField(editor, "logoutButton", logoutButton);
            
            var settingsWindowGO = new GameObject("SettingsWindow");
            SetPrivateField(editor, "settingsWindow", settingsWindowGO);
            
            var applyGO = new GameObject("ApplyButton");
            var applyButton = applyGO.AddComponent<Button>();
            SetPrivateField(editor, "applySettingsButton", applyButton);
            
            var closeGO = new GameObject("CloseButton");
            var closeButton = closeGO.AddComponent<Button>();
            SetPrivateField(editor, "closeSettingsButton", closeButton);
            
            var hullSlots = new Button[4];
            var weaponSlots = new Button[4];
            var engineSlots = new Button[4];
            
            for (int i = 0; i < 4; i++)
            {
                hullSlots[i] = CreateSlotWithImage($"HullSlot{i}");
                weaponSlots[i] = CreateSlotWithImage($"WeaponSlot{i}");
                engineSlots[i] = CreateSlotWithImage($"EngineSlot{i}");
            }
            
            SetPrivateField(editor, "hullSlots", hullSlots);
            SetPrivateField(editor, "weaponSlots", weaponSlots);
            SetPrivateField(editor, "engineSlots", engineSlots);
            
            editorGO.AddComponent<ShipAssembler>();
        }

        private Button CreateSlotWithImage(string name)
        {
            var go = new GameObject(name);
            var button = go.AddComponent<Button>();
            go.AddComponent<Image>();
            
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(go.transform);
            iconGO.AddComponent<Image>();
            
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(go.transform);
            nameGO.AddComponent<TextMeshProUGUI>();
            
            return button;
        }

        private void CreateTestComponents()
        {
            var hullData = ScriptableObject.CreateInstance<HullData>();
            var weaponData = ScriptableObject.CreateInstance<WeaponData>();
            var engineData = ScriptableObject.CreateInstance<EngineData>();

            SetPrivateField(editor, "hullComponents", new ShipComponent[]
            {
                new ShipComponent { componentId = 1, componentName = "Hull 1", componentType = ShipComponentType.Hull, componentData = hullData, isDefault = true }
            });

            SetPrivateField(editor, "weaponComponents", new ShipComponent[]
            {
                new ShipComponent { componentId = 2, componentName = "Weapon 1", componentType = ShipComponentType.Weapon, componentData = weaponData, isDefault = true }
            });

            SetPrivateField(editor, "engineComponents", new ShipComponent[]
            {
                new ShipComponent { componentId = 3, componentName = "Engine 1", componentType = ShipComponentType.Engine, componentData = engineData, isDefault = true }
            });
        }

        [Test]
        public void SetupUIEvents_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => CallPrivateMethod("SetupUIEvents"));
        }

        [Test]
        public void PopulateSlots_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => CallPrivateMethod("PopulateSlots"));
        }

        private object CallPrivateMethod(string methodName, params object[] parameters)
        {
            MethodInfo method = typeof(ShipEditorUI).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            
            if (method == null)
                return null;
            
            return method.Invoke(editor, parameters);
        }

        private T GetPrivateField<T>(string fieldName)
        {
            FieldInfo field = typeof(ShipEditorUI).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            return (T)field?.GetValue(editor);
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
            Object.DestroyImmediate(editorGO);
        }
    }
}