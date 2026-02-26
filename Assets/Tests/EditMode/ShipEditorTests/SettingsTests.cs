using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

namespace ShipEditorTests
{
    [TestFixture]
    public class ShipEditorSettingsTests
    {
        private ShipEditorUI editor;
        private GameObject editorGO;
        private GameObject settingsWindow;
        private GameObject dropdownPanel;
        
        [SetUp]
        public void SetUp()
        {
            editorGO = new GameObject("ShipEditor");
            editor = editorGO.AddComponent<ShipEditorUI>();
            
            settingsWindow = new GameObject("SettingsWindow");
            SetPrivateField(editor, "settingsWindow", settingsWindow);
            
            dropdownPanel = new GameObject("DropdownPanel");
            SetPrivateField(editor, "dropdownPanel", dropdownPanel);
            
            var graphicsGO = new GameObject("GraphicsDropdown");
            var graphicsDropdown = graphicsGO.AddComponent<TMP_Dropdown>();
            SetPrivateField(editor, "graphicsDropdown", graphicsDropdown);
            
            var musicGO = new GameObject("MusicSlider");
            var musicSlider = musicGO.AddComponent<Slider>();
            SetPrivateField(editor, "musicSlider", musicSlider);
            
            var sfxGO = new GameObject("SFXSlider");
            var sfxSlider = sfxGO.AddComponent<Slider>();
            SetPrivateField(editor, "sfxSlider", sfxSlider);
            
            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void OpenSettingsWindow_ShouldHideDropdownAndShowSettings()
        {
            dropdownPanel.SetActive(true);
            settingsWindow.SetActive(false);
            
            CallPrivateMethod("OpenSettingsWindow");
            
            Assert.IsFalse(dropdownPanel.activeSelf);
            Assert.IsTrue(settingsWindow.activeSelf);
        }

        [Test]
        public void CloseSettingsWindow_ShouldHideSettings()
        {
            settingsWindow.SetActive(true);
            
            CallPrivateMethod("CloseSettingsWindow");
            
            Assert.IsFalse(settingsWindow.activeSelf);
        }

        [Test]
        public void ApplyGraphicsQuality_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => CallPrivateMethod("ApplyGraphicsQuality", 2));
        }

        [Test]
        public void Logout_ShouldNotThrow()
        {
            Assert.DoesNotThrow(() => CallPrivateMethod("Logout"));
        }

        [Test]
        public void VolumeMapping_AtZero_ReturnsMinus200()
        {
            var result = (float)CallPrivateMethod("VolumeMapping", 0f);
            Assert.AreEqual(-200f, result);
        }

        [Test]
        public void VolumeMapping_AtOne_ReturnsZero()
        {
            var result = (float)CallPrivateMethod("VolumeMapping", 1f);
            Assert.AreEqual(0f, result, 0.1f);
        }

        [Test]
        public void VolumeMapping_AtMidValue_ReturnsCorrectDecibel()
        {
            var result = (float)CallPrivateMethod("VolumeMapping", 0.1f);
            Assert.AreEqual(-20f, result, 0.1f);
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
            PlayerPrefs.DeleteAll();
        }
    }
}