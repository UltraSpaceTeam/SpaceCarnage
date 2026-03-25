using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using System.Collections.Generic;

namespace ShipEditorTests
{
    [TestFixture]
    public class ShipEditorLoadSaveTests
    {
        private ShipEditorUI editor;
        private GameObject editorGO;
        private Dictionary<ShipComponentType, ShipComponent> selectedComponents;
        
        [SetUp]
        public void SetUp()
        {
            editorGO = new GameObject("ShipEditor");
            editor = editorGO.AddComponent<ShipEditorUI>();
            
            var statsTextGO = new GameObject("StatsText");
            var statsText = statsTextGO.AddComponent<TextMeshProUGUI>();
            SetPrivateField(editor, "shipStatsText", statsText);
            
            selectedComponents = new Dictionary<ShipComponentType, ShipComponent>();
            SetPrivateField(editor, "selectedComponents", selectedComponents);
            
            CreateSlots();
            
            CreateTestComponents();
        }

        private void CreateSlots()
        {
            var hullSlots = new Button[2];
            var weaponSlots = new Button[2];
            var engineSlots = new Button[2];
            
            for (int i = 0; i < 2; i++)
            {
                hullSlots[i] = CreateSlotWithImage($"HullSlot{i}");
                weaponSlots[i] = CreateSlotWithImage($"WeaponSlot{i}");
                engineSlots[i] = CreateSlotWithImage($"EngineSlot{i}");
            }
            
            SetPrivateField(editor, "hullSlots", hullSlots);
            SetPrivateField(editor, "weaponSlots", weaponSlots);
            SetPrivateField(editor, "engineSlots", engineSlots);
        }

        private Button CreateSlotWithImage(string name)
        {
            var go = new GameObject(name);
            var button = go.AddComponent<Button>();
            go.AddComponent<Image>();
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
        public void SaveConfiguration_WithAllComponents_ShouldNotThrow()
        {
            selectedComponents[ShipComponentType.Hull] = GetPrivateField<ShipComponent[]>("hullComponents")[0];
            selectedComponents[ShipComponentType.Weapon] = GetPrivateField<ShipComponent[]>("weaponComponents")[0];
            selectedComponents[ShipComponentType.Engine] = GetPrivateField<ShipComponent[]>("engineComponents")[0];
            
            Assert.DoesNotThrow(() => CallPrivateMethod("SaveConfiguration"));
        }

        [Test]
        public void SaveConfiguration_WithMissingComponents_ShouldNotThrow()
        {
            selectedComponents[ShipComponentType.Hull] = GetPrivateField<ShipComponent[]>("hullComponents")[0];
            
            Assert.DoesNotThrow(() => CallPrivateMethod("SaveConfiguration"));
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