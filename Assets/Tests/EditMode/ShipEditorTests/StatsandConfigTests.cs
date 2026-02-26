using NUnit.Framework;
using UnityEngine;
using TMPro;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ShipEditorTests
{
    [TestFixture]
    public class StatsAndConfigTests
    {
        private ShipEditorUI editor;
        private GameObject editorGO;
        private TextMeshProUGUI statsText;
        private Dictionary<ShipComponentType, ShipComponent> selectedComponents;
        
        [SetUp]
        public void SetUp()
        {
            editorGO = new GameObject("ShipEditor");
            editor = editorGO.AddComponent<ShipEditorUI>();
            
            var textGO = new GameObject("StatsText");
            statsText = textGO.AddComponent<TextMeshProUGUI>();
            SetPrivateField(editor, "shipStatsText", statsText);
            
            selectedComponents = new Dictionary<ShipComponentType, ShipComponent>();
            SetPrivateField(editor, "selectedComponents", selectedComponents);
            
            CreateTestComponents();
            
            statsText.text = "";
        }

        private void CreateTestComponents()
        {
            var hullData1 = ScriptableObject.CreateInstance<HullData>();
            hullData1.maxHealth = 100;
            hullData1.mass = 50;
            
            var hullData2 = ScriptableObject.CreateInstance<HullData>();
            hullData2.maxHealth = 200;
            hullData2.mass = 100;
            
            var weaponData1 = ScriptableObject.CreateInstance<WeaponData>();
            weaponData1.damage = 25;
            weaponData1.mass = 10;
            
            var weaponData2 = ScriptableObject.CreateInstance<WeaponData>();
            weaponData2.damage = 50;
            weaponData2.mass = 20;
            
            var engineData1 = ScriptableObject.CreateInstance<EngineData>();
            engineData1.power = 75;
            engineData1.mass = 15;
            
            var engineData2 = ScriptableObject.CreateInstance<EngineData>();
            engineData2.power = 150;
            engineData2.mass = 30;

            SetPrivateField(editor, "hullComponents", new ShipComponent[]
            {
                new ShipComponent { componentId = 1, componentName = "Light Hull", componentType = ShipComponentType.Hull, componentData = hullData1 },
                new ShipComponent { componentId = 2, componentName = "Heavy Hull", componentType = ShipComponentType.Hull, componentData = hullData2 }
            });

            SetPrivateField(editor, "weaponComponents", new ShipComponent[]
            {
                new ShipComponent { componentId = 3, componentName = "Light Weapon", componentType = ShipComponentType.Weapon, componentData = weaponData1 },
                new ShipComponent { componentId = 4, componentName = "Heavy Weapon", componentType = ShipComponentType.Weapon, componentData = weaponData2 }
            });

            SetPrivateField(editor, "engineComponents", new ShipComponent[]
            {
                new ShipComponent { componentId = 5, componentName = "Light Engine", componentType = ShipComponentType.Engine, componentData = engineData1 },
                new ShipComponent { componentId = 6, componentName = "Heavy Engine", componentType = ShipComponentType.Engine, componentData = engineData2 }
            });
        }

        private string StripHTML(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            return Regex.Replace(input, "<.*?>", string.Empty);
        }

        [Test]
        public void UpdateStats_WithCompleteConfig_ShouldShowCorrectStats()
        {
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            var weaponComponents = GetPrivateField<ShipComponent[]>("weaponComponents");
            var engineComponents = GetPrivateField<ShipComponent[]>("engineComponents");
            
            selectedComponents[ShipComponentType.Hull] = hullComponents[0];
            selectedComponents[ShipComponentType.Weapon] = weaponComponents[0];
            selectedComponents[ShipComponentType.Engine] = engineComponents[0];
            
            CallPrivateMethod("UpdateStats");
            
            string result = statsText.text;
            string cleanResult = StripHTML(result);
            
            Debug.Log($"Original: {result}");
            Debug.Log($"Cleaned: {cleanResult}");
            
            Assert.IsFalse(string.IsNullOrEmpty(result), "Текст статистики не должен быть пустым");
            
            Assert.IsTrue(cleanResult.Contains("SHIP STATISTICS"), "Должен быть заголовок");
            Assert.IsTrue(cleanResult.Contains("Light Hull"), "Должно быть название корпуса");
            Assert.IsTrue(cleanResult.Contains("Light Weapon"), "Должно быть название оружия");
            Assert.IsTrue(cleanResult.Contains("Light Engine"), "Должно быть название двигателя");
            Assert.IsTrue(cleanResult.Contains("Damage: 25"), "Должен быть урон 25");
            Assert.IsTrue(cleanResult.Contains("Health: 100"), "Должно быть здоровье 100");
            Assert.IsTrue(cleanResult.Contains("Mass: 75"), "Должна быть масса 75");
            Assert.IsTrue(cleanResult.Contains("Power: 75"), "Должна быть мощность 75");
        }

        [Test]
        public void UpdateStats_WithDifferentConfig_ShouldShowDifferentStats()
        {
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            var weaponComponents = GetPrivateField<ShipComponent[]>("weaponComponents");
            var engineComponents = GetPrivateField<ShipComponent[]>("engineComponents");
            
            selectedComponents[ShipComponentType.Hull] = hullComponents[0];
            selectedComponents[ShipComponentType.Weapon] = weaponComponents[0];
            selectedComponents[ShipComponentType.Engine] = engineComponents[0];
            
            CallPrivateMethod("UpdateStats");
            string stats1 = StripHTML(statsText.text);
            
            selectedComponents[ShipComponentType.Hull] = hullComponents[1];
            selectedComponents[ShipComponentType.Weapon] = weaponComponents[1];
            selectedComponents[ShipComponentType.Engine] = engineComponents[1];
            
            CallPrivateMethod("UpdateStats");
            string stats2 = StripHTML(statsText.text);
            
            Assert.AreNotEqual(stats1, stats2, "Статистика для разных конфигураций должна различаться");
            
            Assert.IsTrue(stats1.Contains("Health: 100"), "Легкая конфигурация: здоровье 100");
            Assert.IsTrue(stats2.Contains("Health: 200"), "Тяжелая конфигурация: здоровье 200");
            
            Assert.IsTrue(stats1.Contains("Damage: 25"), "Легкая конфигурация: урон 25");
            Assert.IsTrue(stats2.Contains("Damage: 50"), "Тяжелая конфигурация: урон 50");
            
            Assert.IsTrue(stats1.Contains("Mass: 75"), "Легкая конфигурация: масса 75");
            Assert.IsTrue(stats2.Contains("Mass: 150"), "Тяжелая конфигурация: масса 150");
            
            Assert.IsTrue(stats1.Contains("Power: 75"), "Легкая конфигурация: мощность 75");
            Assert.IsTrue(stats2.Contains("Power: 150"), "Тяжелая конфигурация: мощность 150");
        }

        [Test]
        public void UpdateStats_WithMissingComponents_ShouldNotUpdate()
        {
            string initialText = "Initial Text";
            statsText.text = initialText;
            
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            selectedComponents[ShipComponentType.Hull] = hullComponents[0];
            
            CallPrivateMethod("UpdateStats");
            
            Assert.AreEqual(initialText, statsText.text, "Текст не должен измениться при неполной сборке");
        }

        [Test]
        public void CalculateTotalMass_ShouldSumAllComponentMasses()
        {
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            var weaponComponents = GetPrivateField<ShipComponent[]>("weaponComponents");
            var engineComponents = GetPrivateField<ShipComponent[]>("engineComponents");
            
            selectedComponents[ShipComponentType.Hull] = hullComponents[0];
            selectedComponents[ShipComponentType.Weapon] = weaponComponents[0];
            selectedComponents[ShipComponentType.Engine] = engineComponents[0];
            
            var hullData = (HullData)hullComponents[0].componentData;
            var weaponData = (WeaponData)weaponComponents[0].componentData;
            var engineData = (EngineData)engineComponents[0].componentData;
            
            float expectedMass = hullData.mass + weaponData.mass + engineData.mass;
            
            CallPrivateMethod("UpdateStats");
            
            string result = statsText.text;
            string cleanResult = StripHTML(result);
            
            Debug.Log($"Original: {result}");
            Debug.Log($"Cleaned: {cleanResult}");
            
            Assert.IsFalse(string.IsNullOrEmpty(result), "Текст статистики не должен быть пустым");
            Assert.IsTrue(cleanResult.Contains($"Mass: {expectedMass}"), 
                $"Текст должен содержать 'Mass: {expectedMass}'. Очищенный текст: {cleanResult}");
        }

        [Test]
        public void UpdateStats_ShouldCalculateCorrectValues()
        {
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            var weaponComponents = GetPrivateField<ShipComponent[]>("weaponComponents");
            var engineComponents = GetPrivateField<ShipComponent[]>("engineComponents");
            
            var hullData = (HullData)hullComponents[0].componentData;
            var weaponData = (WeaponData)weaponComponents[0].componentData;
            var engineData = (EngineData)engineComponents[0].componentData;
            
            selectedComponents[ShipComponentType.Hull] = hullComponents[0];
            selectedComponents[ShipComponentType.Weapon] = weaponComponents[0];
            selectedComponents[ShipComponentType.Engine] = engineComponents[0];
            
            CallPrivateMethod("UpdateStats");
            
            string result = statsText.text;
            string cleanResult = StripHTML(result);
            
            Debug.Log($"Original: {result}");
            Debug.Log($"Cleaned: {cleanResult}");
            
            Assert.IsTrue(cleanResult.Contains($"Damage: {weaponData.damage}"), 
                $"Должен быть урон {weaponData.damage}");
            Assert.IsTrue(cleanResult.Contains($"Health: {hullData.maxHealth}"), 
                $"Должно быть здоровье {hullData.maxHealth}");
            Assert.IsTrue(cleanResult.Contains($"Power: {engineData.power}"), 
                $"Должна быть мощность {engineData.power}");
        }

        private object CallPrivateMethod(string methodName, params object[] parameters)
        {
            MethodInfo method = typeof(ShipEditorUI).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                Debug.LogError($"Method {methodName} not found");
                return null;
            }
            return method.Invoke(editor, parameters);
        }

        private T GetPrivateField<T>(string fieldName)
        {
            FieldInfo field = typeof(ShipEditorUI).GetField(fieldName, 
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                Debug.LogError($"Field {fieldName} not found");
                return default(T);
            }
            return (T)field.GetValue(editor);
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, 
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                Debug.LogError($"Field {fieldName} not found in {obj.GetType().Name}");
                return;
            }
            field.SetValue(obj, value);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(editorGO);
        }
    }
}