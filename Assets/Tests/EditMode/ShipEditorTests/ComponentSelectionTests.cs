using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace ShipEditorTests
{
    [TestFixture]
    public class ComponentSelectionTests
    {
        private ShipEditorUI editor;
        private GameObject editorGO;
        private Dictionary<ShipComponentType, ShipComponent> selectedComponents;
        
        [SetUp]
        public void SetUp()
        {
            editorGO = new GameObject("ShipEditor");
            editor = editorGO.AddComponent<ShipEditorUI>();
            
            selectedComponents = new Dictionary<ShipComponentType, ShipComponent>();
            SetPrivateField(editor, "selectedComponents", selectedComponents);
            
            var hullSlots = CreateSlots(2);
            var weaponSlots = CreateSlots(2);
            var engineSlots = CreateSlots(2);
            
            SetPrivateField(editor, "hullSlots", hullSlots);
            SetPrivateField(editor, "weaponSlots", weaponSlots);
            SetPrivateField(editor, "engineSlots", engineSlots);
            
            CreateTestComponents();
            
            var shipAssembler = editorGO.AddComponent<ShipAssembler>();
            SetPrivateField(editor, "shipAssembler", shipAssembler);
            
            var statsTextGO = new GameObject("StatsText");
            var statsText = statsTextGO.AddComponent<TextMeshProUGUI>();
            SetPrivateField(editor, "shipStatsText", statsText);
        }

        private Button[] CreateSlots(int count)
        {
            var slots = new Button[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Slot_{i}");
                var button = go.AddComponent<Button>();
                go.AddComponent<Image>();
                
                var slotData = go.AddComponent<SlotData>();
                slots[i] = button;
            }
            return slots;
        }

        private void CreateTestComponents()
        {
            var hullData = ScriptableObject.CreateInstance<HullData>();
            var weaponData = ScriptableObject.CreateInstance<WeaponData>();
            var engineData = ScriptableObject.CreateInstance<EngineData>();

            var hullComponents = new ShipComponent[]
            {
                new ShipComponent { componentId = 1, componentName = "Hull 1", componentType = ShipComponentType.Hull, componentData = hullData, isDefault = true },
                new ShipComponent { componentId = 2, componentName = "Hull 2", componentType = ShipComponentType.Hull, componentData = hullData }
            };

            var weaponComponents = new ShipComponent[]
            {
                new ShipComponent { componentId = 3, componentName = "Weapon 1", componentType = ShipComponentType.Weapon, componentData = weaponData, isDefault = true },
                new ShipComponent { componentId = 4, componentName = "Weapon 2", componentType = ShipComponentType.Weapon, componentData = weaponData }
            };

            var engineComponents = new ShipComponent[]
            {
                new ShipComponent { componentId = 5, componentName = "Engine 1", componentType = ShipComponentType.Engine, componentData = engineData, isDefault = true },
                new ShipComponent { componentId = 6, componentName = "Engine 2", componentType = ShipComponentType.Engine, componentData = engineData }
            };

            SetPrivateField(editor, "hullComponents", hullComponents);
            SetPrivateField(editor, "weaponComponents", weaponComponents);
            SetPrivateField(editor, "engineComponents", engineComponents);

            var hullSlots = GetPrivateField<Button[]>("hullSlots");
            for (int i = 0; i < hullSlots.Length && i < hullComponents.Length; i++)
            {
                var slotData = hullSlots[i].GetComponent<SlotData>();
                if (slotData != null)
                    slotData.component = hullComponents[i];
            }

            var weaponSlots = GetPrivateField<Button[]>("weaponSlots");
            for (int i = 0; i < weaponSlots.Length && i < weaponComponents.Length; i++)
            {
                var slotData = weaponSlots[i].GetComponent<SlotData>();
                if (slotData != null)
                    slotData.component = weaponComponents[i];
            }

            var engineSlots = GetPrivateField<Button[]>("engineSlots");
            for (int i = 0; i < engineSlots.Length && i < engineComponents.Length; i++)
            {
                var slotData = engineSlots[i].GetComponent<SlotData>();
                if (slotData != null)
                    slotData.component = engineComponents[i];
            }
        }

        private void InitializeAllComponents()
        {
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            var weaponComponents = GetPrivateField<ShipComponent[]>("weaponComponents");
            var engineComponents = GetPrivateField<ShipComponent[]>("engineComponents");
            
            Debug.Log("Initializing all components directly in dictionary...");
            
            selectedComponents[ShipComponentType.Hull] = hullComponents[0];
            selectedComponents[ShipComponentType.Weapon] = weaponComponents[0];
            selectedComponents[ShipComponentType.Engine] = engineComponents[0];
            
            Debug.Log($"After init: Count={selectedComponents.Count}");
            Debug.Log($"Keys: {string.Join(", ", selectedComponents.Keys)}");
        }

        private void SelectAllComponents()
        {
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            var weaponComponents = GetPrivateField<ShipComponent[]>("weaponComponents");
            var engineComponents = GetPrivateField<ShipComponent[]>("engineComponents");
            
            Debug.Log("Selecting all components via SelectComponent...");
            
            CallPrivateMethod("SelectComponent", hullComponents[0], ShipComponentType.Hull);
            CallPrivateMethod("SelectComponent", weaponComponents[0], ShipComponentType.Weapon);
            CallPrivateMethod("SelectComponent", engineComponents[0], ShipComponentType.Engine);
            
            Debug.Log($"After select all: Count={selectedComponents.Count}");
            Debug.Log($"Keys: {string.Join(", ", selectedComponents.Keys)}");
        }

        [Test]
        public void SelectComponent_ShouldAddToDictionary()
        {
            selectedComponents.Clear();
            
            Debug.Log("Test: SelectComponent_ShouldAddToDictionary");
            
            SelectAllComponents();
            
            Assert.AreEqual(3, selectedComponents.Count, "Должно быть 3 элемента в словаре");
            Assert.IsTrue(selectedComponents.ContainsKey(ShipComponentType.Hull), "Ключ Hull должен существовать");
            Assert.IsTrue(selectedComponents.ContainsKey(ShipComponentType.Weapon), "Ключ Weapon должен существовать");
            Assert.IsTrue(selectedComponents.ContainsKey(ShipComponentType.Engine), "Ключ Engine должен существовать");
        }

        [Test]
        public void SelectComponent_ForDifferentTypes_ShouldAddAll()
        {
            selectedComponents.Clear();
            
            Debug.Log("Test: SelectComponent_ForDifferentTypes_ShouldAddAll");
            
            SelectAllComponents();
            
            Assert.AreEqual(3, selectedComponents.Count, "Должно быть 3 элемента в словаре");
            Assert.IsTrue(selectedComponents.ContainsKey(ShipComponentType.Hull), "Ключ Hull должен существовать");
            Assert.IsTrue(selectedComponents.ContainsKey(ShipComponentType.Weapon), "Ключ Weapon должен существовать");
            Assert.IsTrue(selectedComponents.ContainsKey(ShipComponentType.Engine), "Ключ Engine должен существовать");
        }

        [Test]
        public void SelectComponent_ForSameType_ShouldReplacePrevious()
        {
            selectedComponents.Clear();
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            
            Debug.Log("Test: SelectComponent_ForSameType_ShouldReplacePrevious");
            
            SelectAllComponents();
            
            var originalHull = selectedComponents[ShipComponentType.Hull];
            var originalWeapon = selectedComponents[ShipComponentType.Weapon];
            var originalEngine = selectedComponents[ShipComponentType.Engine];
            
            Debug.Log("Replacing Hull with Hull 2");
            CallPrivateMethod("SelectComponent", hullComponents[1], ShipComponentType.Hull);
            
            Assert.AreEqual(3, selectedComponents.Count, "Должно быть 3 элемента в словаре");
            Assert.AreEqual(hullComponents[1], selectedComponents[ShipComponentType.Hull], 
                "Корпус должен замениться на второй");
            Assert.AreNotEqual(originalHull, selectedComponents[ShipComponentType.Hull], 
                "Корпус должен измениться");
            Assert.AreEqual(originalWeapon, selectedComponents[ShipComponentType.Weapon], 
                "Оружие не должно измениться");
            Assert.AreEqual(originalEngine, selectedComponents[ShipComponentType.Engine], 
                "Двигатель не должно измениться");
        }

        [Test]
        public void OnHullSlotClicked_ShouldSelectCorrectComponent()
        {
            selectedComponents.Clear();
            
            Debug.Log("Test: OnHullSlotClicked_ShouldSelectCorrectComponent");
            
            InitializeAllComponents();
            
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            var weaponComponents = GetPrivateField<ShipComponent[]>("weaponComponents");
            var engineComponents = GetPrivateField<ShipComponent[]>("engineComponents");
            
            var originalHull = selectedComponents[ShipComponentType.Hull];
            
            CallPrivateMethod("OnHullSlotClicked", 1);
            
            Assert.AreEqual(3, selectedComponents.Count, "Должно быть 3 элемента в словаре");
            Assert.AreEqual(hullComponents[1], selectedComponents[ShipComponentType.Hull], 
                "Должен быть выбран второй корпус");
            Assert.AreNotEqual(originalHull, selectedComponents[ShipComponentType.Hull], 
                "Корпус должен измениться");
            Assert.AreEqual(weaponComponents[0], selectedComponents[ShipComponentType.Weapon], 
                "Оружие не должно измениться");
            Assert.AreEqual(engineComponents[0], selectedComponents[ShipComponentType.Engine], 
                "Двигатель не должен измениться");
        }

        [Test]
        public void OnWeaponSlotClicked_ShouldSelectCorrectComponent()
        {
            selectedComponents.Clear();
            
            Debug.Log("Test: OnWeaponSlotClicked_ShouldSelectCorrectComponent");
            
            InitializeAllComponents();
            
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            var weaponComponents = GetPrivateField<ShipComponent[]>("weaponComponents");
            var engineComponents = GetPrivateField<ShipComponent[]>("engineComponents");
            
            var originalWeapon = selectedComponents[ShipComponentType.Weapon];
            
            CallPrivateMethod("OnWeaponSlotClicked", 1);
            
            Assert.AreEqual(3, selectedComponents.Count, "Должно быть 3 элемента в словаре");
            Assert.AreEqual(weaponComponents[1], selectedComponents[ShipComponentType.Weapon], 
                "Должно быть выбрано второе оружие");
            Assert.AreNotEqual(originalWeapon, selectedComponents[ShipComponentType.Weapon], 
                "Оружие должно измениться");
            Assert.AreEqual(hullComponents[0], selectedComponents[ShipComponentType.Hull], 
                "Корпус не должен измениться");
            Assert.AreEqual(engineComponents[0], selectedComponents[ShipComponentType.Engine], 
                "Двигатель не должен измениться");
        }

        [Test]
        public void OnEngineSlotClicked_ShouldSelectCorrectComponent()
        {
            selectedComponents.Clear();
            
            Debug.Log("Test: OnEngineSlotClicked_ShouldSelectCorrectComponent");
            
            InitializeAllComponents();
            
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            var weaponComponents = GetPrivateField<ShipComponent[]>("weaponComponents");
            var engineComponents = GetPrivateField<ShipComponent[]>("engineComponents");
            
            var originalEngine = selectedComponents[ShipComponentType.Engine];
            
            CallPrivateMethod("OnEngineSlotClicked", 1);
            
            Assert.AreEqual(3, selectedComponents.Count, "Должно быть 3 элемента в словаре");
            Assert.AreEqual(engineComponents[1], selectedComponents[ShipComponentType.Engine], 
                "Должен быть выбран второй двигатель");
            Assert.AreNotEqual(originalEngine, selectedComponents[ShipComponentType.Engine], 
                "Двигатель должен измениться");
            Assert.AreEqual(hullComponents[0], selectedComponents[ShipComponentType.Hull], 
                "Корпус не должен измениться");
            Assert.AreEqual(weaponComponents[0], selectedComponents[ShipComponentType.Weapon], 
                "Оружие не должно измениться");
        }

        [Test]
        public void SelectDefaultComponents_ShouldSelectDefaultOnes()
        {
            selectedComponents.Clear();
            
            Debug.Log("Test: SelectDefaultComponents");
            
            CallPrivateMethod("SelectDefaultComponents");
            
            Assert.AreEqual(3, selectedComponents.Count, "Должно быть 3 элемента в словаре");
            Assert.IsTrue(selectedComponents.ContainsKey(ShipComponentType.Hull), "Ключ Hull должен существовать");
            Assert.IsTrue(selectedComponents.ContainsKey(ShipComponentType.Weapon), "Ключ Weapon должен существовать");
            Assert.IsTrue(selectedComponents.ContainsKey(ShipComponentType.Engine), "Ключ Engine должен существовать");
            
            Assert.IsTrue(selectedComponents[ShipComponentType.Hull].isDefault, "Корпус должен быть default");
            Assert.IsTrue(selectedComponents[ShipComponentType.Weapon].isDefault, "Оружие должно быть default");
            Assert.IsTrue(selectedComponents[ShipComponentType.Engine].isDefault, "Двигатель должен быть default");
        }

        [Test]
        public void SelectDefaultForType_WhenNoDefault_ShouldSelectFirst()
        {
            selectedComponents.Clear();
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            
            foreach (var component in hullComponents)
            {
                component.isDefault = false;
            }
            
            CallPrivateMethod("SelectDefaultForType", ShipComponentType.Hull, hullComponents);
            
            Assert.IsTrue(selectedComponents.ContainsKey(ShipComponentType.Hull), "Ключ Hull должен существовать");
            Assert.AreEqual(hullComponents[0], selectedComponents[ShipComponentType.Hull], 
                "Должен быть выбран первый элемент");
        }

        [Test]
        public void FindSlotIndexByComponent_ShouldReturnCorrectIndex()
        {
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            var component = hullComponents[1];
            
            int index = (int)CallPrivateMethod("FindSlotIndexByComponent", ShipComponentType.Hull, component);
            
            Assert.AreEqual(1, index, "Индекс должен быть 1");
        }

        [Test]
        public void HighlightSelectedSlot_ShouldChangeColors()
        {
            var hullSlots = GetPrivateField<Button[]>("hullSlots");
            var normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            var selectedColor = new Color(0.1f, 0.7f, 0.2f, 1f);
            
            SetPrivateField(editor, "normalComponentColor", normalColor);
            SetPrivateField(editor, "selectedComponentColor", selectedColor);
            
            foreach (var slot in hullSlots)
            {
                var image = slot.GetComponent<Image>();
                if (image != null)
                    image.color = normalColor;
            }
            
            CallPrivateMethod("HighlightSelectedSlot", hullSlots, 0);
            
            Assert.AreEqual(selectedColor, hullSlots[0].GetComponent<Image>().color, 
                "Первый слот должен быть выделен");
            Assert.AreEqual(normalColor, hullSlots[1].GetComponent<Image>().color, 
                "Второй слот должен быть обычным");
        }

        [Test]
        public void GetComponentsByCategory_ShouldReturnCorrectArray()
        {
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            var weaponComponents = GetPrivateField<ShipComponent[]>("weaponComponents");
            var engineComponents = GetPrivateField<ShipComponent[]>("engineComponents");
            
            var resultHulls = (ShipComponent[])CallPrivateMethod("GetComponentsByCategory", ShipComponentType.Hull);
            var resultWeapons = (ShipComponent[])CallPrivateMethod("GetComponentsByCategory", ShipComponentType.Weapon);
            var resultEngines = (ShipComponent[])CallPrivateMethod("GetComponentsByCategory", ShipComponentType.Engine);
            
            Assert.AreEqual(hullComponents, resultHulls, "Должен вернуть массив корпусов");
            Assert.AreEqual(weaponComponents, resultWeapons, "Должен вернуть массив оружия");
            Assert.AreEqual(engineComponents, resultEngines, "Должен вернуть массив двигателей");
        }

        [Test]
        public void FindComponentById_WithExistingId_ShouldReturnComponent()
        {
            var hullComponents = GetPrivateField<ShipComponent[]>("hullComponents");
            
            var result = (ShipComponent)CallPrivateMethod("FindComponentById", ShipComponentType.Hull, 2);
            
            Assert.IsNotNull(result, "Компонент должен быть найден");
            Assert.AreEqual(2, result.componentId, "Должен быть компонент с ID 2");
        }

        [Test]
        public void FindComponentById_WithNonExistentId_ShouldReturnNull()
        {
            var result = (ShipComponent)CallPrivateMethod("FindComponentById", ShipComponentType.Hull, 999);
            
            Assert.IsNull(result, "Должен вернуть null для несуществующего ID");
        }

        private object CallPrivateMethod(string methodName, params object[] parameters)
        {
            MethodInfo method = typeof(ShipEditorUI).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                Debug.LogError($"Method {methodName} not found in ShipEditorUI");
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
                Debug.LogError($"Field {fieldName} not found in ShipEditorUI");
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