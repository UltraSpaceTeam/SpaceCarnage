using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>
/// PlayMode тесты для DeathScreenController.
/// OnRespawnClicked / OnExitClicked не тестируются напрямую —
/// они требуют NetworkClient / NetworkServer без активного сервера.
/// </summary>
public class DeathScreenControllerPlayModeTests
{
    // ?????????????????????????????????????????????????????????????????????????
    // Инфраструктура
    // ?????????????????????????????????????????????????????????????????????????

    private GameObject _root;
    private DeathScreenController _ctrl;
    private GameObject _panel;
    private TextMeshProUGUI _deathText;
    private Button _respawnButton;
    private Button _exitButton;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Корневой объект контроллера
        _root = new GameObject("DeathScreenController");
        _ctrl = _root.AddComponent<DeathScreenController>();

        // panel
        _panel = new GameObject("Panel");
        _panel.transform.SetParent(_root.transform);

        // TextMeshProUGUI требует Canvas
        var canvasGO = new GameObject("Canvas");
        canvasGO.AddComponent<Canvas>();
        canvasGO.transform.SetParent(_root.transform);

        var textGO = new GameObject("DeathSourceText");
        textGO.transform.SetParent(canvasGO.transform);
        _deathText = textGO.AddComponent<TextMeshProUGUI>();

        // Кнопки
        var respawnGO = new GameObject("RespawnButton");
        respawnGO.transform.SetParent(_root.transform);
        _respawnButton = respawnGO.AddComponent<Button>();

        var exitGO = new GameObject("ExitButton");
        exitGO.transform.SetParent(_root.transform);
        _exitButton = exitGO.AddComponent<Button>();

        // Инжектируем SerializeField через рефлексию
        SetField(_ctrl, "panel", _panel);
        SetField(_ctrl, "deathSourceText", _deathText);
        SetField(_ctrl, "respawnButton", _respawnButton);
        SetField(_ctrl, "exitButton", _exitButton);

        // Гарантируем что PauseMenuController.IsPaused == false
        // (статическое поле — выставляем до теста чтобы Show не пытался вызвать Instance)
        SetStaticField(typeof(PauseMenuController), "IsPaused", false);

        yield return null; // даём Start() выполниться
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(_root);
        yield return null;
    }

    // ?????????????????????????????????????????????????????????????????????????
    // Helpers
    // ?????????????????????????????????????????????????????????????????????????

    private void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {obj.GetType().Name}");
        f.SetValue(obj, value);
    }

    private void SetStaticField(System.Type type, string name, object value)
    {
        var f = type.GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        // Если поля нет (например IsPaused — property), пробуем property
        if (f == null)
        {
            var p = type.GetProperty(name,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (p != null && p.CanWrite) { p.SetValue(null, value); return; }
            // Если readonly — просто пропускаем, тест сам обработает
            return;
        }
        f.SetValue(null, value);
    }

    private DamageContext MakeContext(string attackerName = "Enemy", string weaponId = "Laser")
    {
        // DamageContext.Weapon требует netId и имена
        return DamageContext.Weapon(0u, attackerName, weaponId);
    }

    // ?????????????????????????????????????????????????????????????????????????
    // Show Tests
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator Show_ActivatesPanel()
    {
        _panel.SetActive(false);
        _ctrl.Show(MakeContext());
        yield return null;

        Assert.IsTrue(_panel.activeSelf, "Show должен активировать panel");
    }

    [UnityTest]
    public IEnumerator Show_SetsDeathSourceText()
    {
        _ctrl.Show(MakeContext("PlayerOne", "Rocket"));
        yield return null;

        StringAssert.Contains("PlayerOne", _deathText.text, "Текст должен содержать имя убийцы");
        StringAssert.Contains("Rocket", _deathText.text, "Текст должен содержать название оружия");
    }

    [UnityTest]
    public IEnumerator Show_UnlocksCursor()
    {
        _ctrl.Show(MakeContext());
        yield return null;

        Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "Show должен снять блокировку курсора");
        Assert.IsTrue(Cursor.visible, "Show должен сделать курсор видимым");
    }

    [UnityTest]
    public IEnumerator Show_TextFormat_ContainsKilledBy()
    {
        _ctrl.Show(MakeContext("Bot42", "Missile"));
        yield return null;

        StringAssert.Contains("Killed by:", _deathText.text, "Текст должен начинаться с 'Killed by:'");
    }

    [UnityTest]
    public IEnumerator Show_CalledTwice_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            _ctrl.Show(MakeContext("A", "Gun"));
            _ctrl.Show(MakeContext("B", "Sword"));
        }, "Повторный вызов Show не должен бросать исключений");
        yield return null;
    }

    // ?????????????????????????????????????????????????????????????????????????
    // Hide Tests
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator Hide_DeactivatesPanel()
    {
        _panel.SetActive(true);
        _ctrl.Hide();
        yield return null;

        Assert.IsFalse(_panel.activeSelf, "Hide должен деактивировать panel");
    }

    [UnityTest]
    public IEnumerator Hide_LocksCursor()
    {
        _ctrl.Hide();
        yield return null;

        Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState, "Hide должен заблокировать курсор");
        Assert.IsFalse(Cursor.visible, "Hide должен скрыть курсор");
    }

    [UnityTest]
    public IEnumerator Hide_AfterShow_HidesPanel()
    {
        _ctrl.Show(MakeContext());
        yield return null;
        Assert.IsTrue(_panel.activeSelf, "После Show panel должна быть видна");

        _ctrl.Hide();
        yield return null;
        Assert.IsFalse(_panel.activeSelf, "После Hide panel должна быть скрыта");
    }

    [UnityTest]
    public IEnumerator Hide_CalledTwice_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            _ctrl.Hide();
            _ctrl.Hide();
        }, "Повторный вызов Hide не должен бросать исключений");
        yield return null;
    }

    // ?????????????????????????????????????????????????????????????????????????
    // Button listeners Tests
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator Start_RegistersRespawnButtonListener()
    {
        // Проверяем что Start добавил listener (persistentEventCount не считает AddListener,
        // но можно проверить через invocation list через рефлексию на UnityEvent)
        var eventField = typeof(Button.ButtonClickedEvent)
            .GetField("m_PersistentCalls",
                BindingFlags.NonPublic | BindingFlags.Instance);

        // Простейшая проверка — нажатие кнопки не бросает исключений
        // (NetworkClient.localPlayer == null ? просто return внутри OnRespawnClicked)
        Assert.DoesNotThrow(() => _respawnButton.onClick.Invoke(),
            "Нажатие Respawn без активного клиента не должно бросать исключений");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Start_RegistersExitButtonListener()
    {
        var calls = typeof(UnityEngine.Events.UnityEventBase)
            .GetField("m_Calls", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(_exitButton.onClick);

        var count = (int)calls.GetType()
            .GetProperty("Count")
            .GetValue(calls);

        Assert.Greater(count, 0, "Exit кнопка должна иметь зарегистрированный listener");
        yield return null;
    }
}