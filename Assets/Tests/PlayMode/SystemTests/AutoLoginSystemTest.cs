using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Mirror;

[Category("SystemTest")]
public class AutoLoginSystemTest
{
    private const string TestUsername = "antoxic";
    private const string TestPassword = "bebra123";

    private TMP_InputField _usernameInput;
    private TMP_InputField _passwordInput;
    private Button _loginOrRegisterButton;
    private TextMeshProUGUI _feedbackText;

    private string _configPath;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 14] === SETUP ===");

        AggressiveCleanup();

        _configPath = Path.Combine(Application.persistentDataPath, "user_config.cfg");

        RepairConfigFile();      // Чиним конфиг
        DisableAutoLogin();      // Отключаем автологин

        yield return SceneManager.LoadSceneAsync("LoginScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(3.0f);

        var loginController = UnityEngine.Object.FindAnyObjectByType<LoginController>();
        Assert.NotNull(loginController);

        _usernameInput = GetPrivateField<TMP_InputField>(loginController, "usernameInput");
        _passwordInput = GetPrivateField<TMP_InputField>(loginController, "passwordInput");
        _loginOrRegisterButton = GetPrivateField<Button>(loginController, "loginOrRegisterButton");
        _feedbackText = GetPrivateField<TextMeshProUGUI>(loginController, "feedbackText");

        Assert.NotNull(_usernameInput);
        Assert.NotNull(_passwordInput);
        Assert.NotNull(_loginOrRegisterButton);
        Assert.NotNull(_feedbackText);

        Debug.Log("[System Test 14] Setup completed. Auto-login disabled.");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        RepairConfigFile();      // Чиним после теста
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_FullAutoLoginSystem_Workflow()
    {
        Debug.Log("[System Test 14] === TEST START: Полная работа системы автологина ===");

        // Шаг 1: Создаём валидный токен
        yield return PerformValidLogin();
        yield return new WaitForSeconds(3.0f);

        Assert.AreEqual("ShipEditor", SceneManager.GetActiveScene().name,
            "Автологин не сработал при наличии валидного токена");

        Debug.Log("[System Test 14] Шаг 1: Автологин с валидным токеном — PASSED");

        // Шаг 2: Повреждаем конфиг
        Debug.Log("[System Test 14] === Шаг 2: Повреждаем конфигурационный файл ===");

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Failed to load config", System.Text.RegularExpressions.RegexOptions.IgnoreCase));

        CorruptConfigFile();

        yield return RestartLoginScene();

        Assert.AreEqual("LoginScene", SceneManager.GetActiveScene().name,
            "Автологин сработал после повреждения конфига");

        Debug.Log("[System Test 14] Шаг 2: Повреждённый конфиг — автологин отключён корректно");

        // Шаг 3-4: Ручной логин
        yield return PerformValidLogin();
        yield return new WaitForSeconds(3.5f);

        Assert.AreEqual("ShipEditor", SceneManager.GetActiveScene().name);

        var config = ConfigManager.LoadConfig();
        Assert.IsFalse(string.IsNullOrEmpty(config.jwt_token), "Новый токен не сохранился");

        Debug.Log("[System Test 14] Шаг 3-4: Ручной логин + сохранение токена — PASSED");

        Debug.Log("[System Test 14] === TEST PASSED: Система автологина работает полностью корректно ===");
    }

    // ====================== Вспомогательные методы ======================

    private void RepairConfigFile()
    {
        try
        {
            var cleanConfig = new LoginConfigData
            {
                jwt_token = "",
                username = "",
                player_id = -1
            };
            ConfigManager.SaveConfig(cleanConfig);
            Debug.Log("[System Test 14] Config repaired to clean state");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[System Test 14] Repair warning: {ex.Message}");
        }
    }

    private void DisableAutoLogin()
    {
        var config = ConfigManager.LoadConfig();
        config.jwt_token = "";
        config.username = "";
        config.player_id = -1;
        ConfigManager.SaveConfig(config);

        APINetworkManager.SetToken(null);
    }

    private void CorruptConfigFile()
    {
        if (!File.Exists(_configPath)) return;

        string original = File.ReadAllText(_configPath);
        string corrupted = original.Substring(0, Mathf.Max(10, original.Length / 2)) + "BROKEN_JSON";
        File.WriteAllText(_configPath, corrupted);

        Debug.Log("[System Test 14] Config file intentionally corrupted");
    }

    private IEnumerator PerformValidLogin()
    {
        _usernameInput.text = TestUsername;
        _passwordInput.text = TestPassword;
        _loginOrRegisterButton.onClick.Invoke();
        RestartLoginScene();

        yield return new WaitForSeconds(5.0f);
    }

    private IEnumerator RestartLoginScene()
    {
        yield return SceneManager.LoadSceneAsync("LoginScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(3.0f);

        var loginController = UnityEngine.Object.FindAnyObjectByType<LoginController>();
        if (loginController != null)
        {
            _usernameInput = GetPrivateField<TMP_InputField>(loginController, "usernameInput");
            _passwordInput = GetPrivateField<TMP_InputField>(loginController, "passwordInput");
            _loginOrRegisterButton = GetPrivateField<Button>(loginController, "loginOrRegisterButton");
        }
    }

    private T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.NotNull(field, $"Field '{fieldName}' not found");
        return (T)field.GetValue(obj);
    }

    private void AggressiveCleanup()
    {
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        var managers = UnityEngine.Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var m in managers)
            if (m != null) UnityEngine.Object.DestroyImmediate(m.gameObject);

        ResetSingleton<LoginController>();
        ResetSingleton<UIManager>();
        ResetSingleton<SessionManager>();
    }

    private void ResetSingleton<T>() where T : MonoBehaviour
    {
        var field = typeof(T).GetField("Instance",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }
}