using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace ShipEditorTests
{
    [TestFixture]
    public class ShipComponentTypeTests
    {
        [Test]
        public void ShipComponentType_ShouldHaveThreeValues()
        {
            var values = System.Enum.GetValues(typeof(ShipComponentType));
            
            Assert.AreEqual(3, values.Length);
            Assert.Contains(ShipComponentType.Hull, values);
            Assert.Contains(ShipComponentType.Weapon, values);
            Assert.Contains(ShipComponentType.Engine, values);
        }
    }

    [TestFixture]
    public class ShipComponentTests
    {
        [Test]
        public void ShipComponent_DefaultValues_ShouldBeSetCorrectly()
        {
            var component = new ShipComponent();
            
            Assert.AreEqual(0, component.componentId);
            Assert.IsNull(component.componentName);
            Assert.IsNull(component.modelPrefab);
            Assert.IsNull(component.componentIcon);
            Assert.AreEqual(0, component.damage);
            Assert.AreEqual(0, component.health);
            Assert.AreEqual(0, component.speed);
            Assert.IsNull(component.description);
            Assert.IsFalse(component.isDefault);
            Assert.IsNull(component.componentData);
        }

        [Test]
        public void ShipComponent_CanSetAndGetProperties()
        {
            var component = new ShipComponent();
            var testData = ScriptableObject.CreateInstance<HullData>();
            
            component.componentId = 42;
            component.componentName = "Test Component";
            component.componentType = ShipComponentType.Weapon;
            component.isDefault = true;
            component.componentData = testData;
            
            Assert.AreEqual(42, component.componentId);
            Assert.AreEqual("Test Component", component.componentName);
            Assert.AreEqual(ShipComponentType.Weapon, component.componentType);
            Assert.IsTrue(component.isDefault);
            Assert.AreEqual(testData, component.componentData);
        }
    }

    [TestFixture]
    public class JoinGameResponseTests
    {
        [Test]
        public void JoinGameResponse_ShouldStoreConnectionData()
        {
            var response = new JoinGameResponse
            {
                ip = "192.168.1.100",
                port = 7777,
                key = "secret-key-123"
            };
            
            Assert.AreEqual("192.168.1.100", response.ip);
            Assert.AreEqual(7777, response.port);
            Assert.AreEqual("secret-key-123", response.key);
        }

        [Test]
        public void JoinGameResponse_DefaultConstructor_ShouldSetNulls()
        {
            var response = new JoinGameResponse();
            
            Assert.IsNull(response.ip);
            Assert.AreEqual(0, response.port);
            Assert.IsNull(response.key);
        }
    }

    [TestFixture]
    public class SlotDataTests
    {
        [Test]
        public void SlotData_ShouldStoreComponent()
        {
            var slotGO = new GameObject();
            var slotData = slotGO.AddComponent<SlotData>();
            var component = new ShipComponent();
            
            slotData.component = component;
            
            Assert.AreEqual(component, slotData.component);
            
            Object.DestroyImmediate(slotGO);
        }
    }
}