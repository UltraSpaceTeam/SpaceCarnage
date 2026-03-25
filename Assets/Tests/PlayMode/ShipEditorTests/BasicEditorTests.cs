using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

namespace ShipEditorTests
{
    [TestFixture]
    public class ShipEditorBasicTests
    {
        private ShipEditorUI editor;
        private GameObject editorGO;
        
        [SetUp]
        public void SetUp()
        {
            editorGO = new GameObject("ShipEditor");
            editor = editorGO.AddComponent<ShipEditorUI>();
            
            var battleButtonGO = new GameObject("BattleButton");
            var battleButton = battleButtonGO.AddComponent<Button>();
            SetPrivateField(editor, "battleButton", battleButton);
            
            var statsTextGO = new GameObject("StatsText");
            var statsText = statsTextGO.AddComponent<TextMeshProUGUI>();
            SetPrivateField(editor, "shipStatsText", statsText);
            
            SetPrivateField(editor, "hullComponents", new ShipComponent[2]);
            SetPrivateField(editor, "weaponComponents", new ShipComponent[2]);
            SetPrivateField(editor, "engineComponents", new ShipComponent[2]);
        }

        [Test]
        public void GetComponentsByCategory_WithHullType_ShouldReturnHullArray()
        {
            var hulls = new ShipComponent[] { new ShipComponent(), new ShipComponent() };
            SetPrivateField(editor, "hullComponents", hulls);
            
            var result = CallPrivateMethod("GetComponentsByCategory", ShipComponentType.Hull) as ShipComponent[];
            
            Assert.AreEqual(hulls, result);
        }

        [Test]
        public void GetComponentsByCategory_WithWeaponType_ShouldReturnWeaponArray()
        {
            var weapons = new ShipComponent[] { new ShipComponent(), new ShipComponent() };
            SetPrivateField(editor, "weaponComponents", weapons);
            
            var result = CallPrivateMethod("GetComponentsByCategory", ShipComponentType.Weapon) as ShipComponent[];
            
            Assert.AreEqual(weapons, result);
        }

        [Test]
        public void GetComponentsByCategory_WithEngineType_ShouldReturnEngineArray()
        {
            var engines = new ShipComponent[] { new ShipComponent(), new ShipComponent() };
            SetPrivateField(editor, "engineComponents", engines);
            
            var result = CallPrivateMethod("GetComponentsByCategory", ShipComponentType.Engine) as ShipComponent[];
            
            Assert.AreEqual(engines, result);
        }

        [Test]
        public void FindComponentById_WithExistingId_ShouldReturnComponent()
        {
            var targetComponent = new ShipComponent { componentId = 999 };
            SetPrivateField(editor, "hullComponents", new ShipComponent[] 
            { 
                new ShipComponent { componentId = 1 },
                targetComponent,
                new ShipComponent { componentId = 2 }
            });
            
            var result = CallPrivateMethod("FindComponentById", ShipComponentType.Hull, 999) as ShipComponent;
            
            Assert.AreEqual(targetComponent, result);
        }

        [Test]
        public void VolumeMapping_AtZero_ShouldReturnVeryLowValue()
        {
            var result = (float)CallPrivateMethod("VolumeMapping", 0f);
            
            Assert.AreEqual(-200f, result, 0.1f);
        }

        [Test]
        public void VolumeMapping_AtMax_ShouldReturnZero()
        {
            var result = (float)CallPrivateMethod("VolumeMapping", 1f);
            
            Assert.AreEqual(0f, result, 0.1f);
        }

        private object CallPrivateMethod(string methodName, params object[] parameters)
        {
            MethodInfo method = typeof(ShipEditorUI).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            return method?.Invoke(editor, parameters);
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