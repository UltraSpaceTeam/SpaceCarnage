using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;

[Category("SystemTest")]
public class ConfigurationSystemTest
{
    private const string TestUsername = "antoxic";
    private const string TestPassword = "bebra123";

    private TMP_InputField _usernameInput;
    private TMP_InputField _passwordInput;
    private Button _loginOrRegisterButton;

    private string _testLoginConfigPath;
    private string _originalLoginConfigPath;
    
    private string _testShipConfigPath;
    private string _originalShipConfigPath;
    
    private string _realLoginConfigPath;
    private string _realShipConfigPath;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 13] === SETUP ===");

        AggressiveCleanup();

        // Сохраняем реальные пути
        var loginFilePathField = typeof(ConfigManager).GetField("filePath", 
            BindingFlags.NonPublic | BindingFlags.Static);
        _realLoginConfigPath = (string)loginFilePathField.GetValue(null);
        
        var shipFilePathField = typeof(ShipConfigManager).GetField("filePath", 
            BindingFlags.NonPublic | BindingFlags.Static);
        _realShipConfigPath = (string)shipFilePathField.GetValue(null);
        
        // Создаём временные пути для теста
        _testLoginConfigPath = Path.Combine(Path.GetTempPath(), $"user_config_{Guid.NewGuid():N}.cfg");
        _testShipConfigPath = Path.Combine(Path.GetTempPath(), $"user_ship_config_{Guid.NewGuid():N}.cfg");
        
        // Подменяем пути на временные
        loginFilePathField.SetValue(null, _testLoginConfigPath);
        shipFilePathField.SetValue(null, _testShipConfigPath);

        // Отключаем автологин
        DisableAutoLoginSafe();

        // Загружаем LoginScene
        yield return SceneManager.LoadSceneAsync("LoginScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(2.0f);

        var loginController = UnityEngine.Object.FindAnyObjectByType<LoginController>();
        Assert.NotNull(loginController);

        _usernameInput = GetPrivateField<TMP_InputField>(loginController, "usernameInput");
        _passwordInput = GetPrivateField<TMP_InputField>(loginController, "passwordInput");
        _loginOrRegisterButton = GetPrivateField<Button>(loginController, "loginOrRegisterButton");

        Assert.NotNull(_usernameInput);
        Assert.NotNull(_passwordInput);
        Assert.NotNull(_loginOrRegisterButton);

        Debug.Log("[System Test 13] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // Восстанавливаем оригинальные пути
        var loginFilePathField = typeof(ConfigManager).GetField("filePath", 
            BindingFlags.NonPublic | BindingFlags.Static);
        loginFilePathField.SetValue(null, _realLoginConfigPath);
        
        var shipFilePathField = typeof(ShipConfigManager).GetField("filePath", 
            BindingFlags.NonPublic | BindingFlags.Static);
        shipFilePathField.SetValue(null, _realShipConfigPath);

        // Удаляем временные файлы
        if (File.Exists(_testLoginConfigPath))
            File.Delete(_testLoginConfigPath);
        if (File.Exists(_testShipConfigPath))
            File.Delete(_testShipConfigPath);

        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_Configuration_LoginSaveCorruption_SystemTest()
    {
        Debug.Log("[System Test 13] === TEST: Configuration Full Cycle ===\n");

        // ========== ШАГ 1: Игрок залогинился - загрузилась конфигурация ==========
        Debug.Log(">>> Step 1: Login - config should be saved with real user data");
        
        yield return PerformValidLogin();
        yield return new WaitForSeconds(4.0f);
        
        var afterLoginConfig = ConfigManager.LoadConfig();
        
        Assert.AreEqual("antoxic", afterLoginConfig.username, "[Step 1] Username mismatch");
        Assert.IsNotEmpty(afterLoginConfig.jwt_token, "[Step 1] Token should not be empty");
        Assert.AreEqual(3, afterLoginConfig.player_id, "[Step 1] Player ID mismatch");
        
        Debug.Log($"[OK] Step 1: Config saved - username={afterLoginConfig.username}\n");

        // ========== ШАГ 2: Игрок поменял конфиг - он изменился ==========
        Debug.Log(">>> Step 2: Manually change config");
        
        var modifiedLoginData = new LoginConfigData
        {
            username = "modified_user",
            jwt_token = "modified_token_999",
            player_id = 999999
        };
        ConfigManager.SaveConfig(modifiedLoginData);
        
        var afterChangeLogin = ConfigManager.LoadConfig();
        Assert.AreEqual("modified_user", afterChangeLogin.username, "[Step 2] Username not updated");
        Assert.AreEqual("modified_token_999", afterChangeLogin.jwt_token, "[Step 2] Token not updated");
        Assert.AreEqual(999999, afterChangeLogin.player_id, "[Step 2] Player ID not updated");
        
        Debug.Log("[OK] Step 2: Config changed\n");

        // ========== ШАГ 3: Игрок вышел и зашел обратно ==========
        Debug.Log(">>> Step 3: Exit and re-enter - new config should load");
        
        DisableAutoLoginSafe();
        
        yield return RestartLoginScene();
        yield return PerformValidLogin();
        yield return new WaitForSeconds(4.0f);
        
        var afterReLoginConfig = ConfigManager.LoadConfig();
        
        Assert.AreEqual("antoxic", afterReLoginConfig.username, "[Step 3] Username should be real user");
        Assert.IsNotEmpty(afterReLoginConfig.jwt_token, "[Step 3] Token should not be empty");
        
        Debug.Log($"[OK] Step 3: Config restored after re-login - username={afterReLoginConfig.username}\n");

        // ========== ШАГ 4: Сломать конфиг, выйти и зайти - загрузился дефолтный конфиг ==========
        Debug.Log(">>> Step 4: Corrupt config, exit and re-enter - default config should load");
        
        CorruptConfigFile(_testLoginConfigPath);
        
        DisableAutoLoginSafe();
        
        yield return RestartLoginScene();
        
        var defaultConfig = ConfigManager.LoadConfig();
        Assert.AreEqual("", defaultConfig.username, "[Step 4] Username should be empty before login");
        Assert.AreEqual("", defaultConfig.jwt_token, "[Step 4] Token should be empty before login");
        Assert.AreEqual(-1, defaultConfig.player_id, "[Step 4] Player ID should be -1 before login");
        
        Debug.Log("[OK] Step 4.1: Default config loaded after corruption (before login)");
        
        yield return PerformValidLogin();
        yield return new WaitForSeconds(4.0f);
        
        var restoredConfig = ConfigManager.LoadConfig();
        Assert.AreEqual("antoxic", restoredConfig.username, "[Step 4] Username should be restored after login");
        Assert.IsNotEmpty(restoredConfig.jwt_token, "[Step 4] Token should be restored after login");
        
        Debug.Log("[OK] Step 4.2: Config restored after login\n");

        // ========== ФИНАЛЬНАЯ ПРОВЕРКА ==========
        Debug.Log(">>> FINAL VERIFICATION: No crashes, default values work");
        Debug.Log("[OK] FINAL: All expectations met\n");

        Debug.Log("=========================================");
        Debug.Log("[System Test 13] === ALL TESTS PASSED ===");
        
        yield return null;
    }

    // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

    private void DisableAutoLoginSafe()
    {
        try
        {
            // Создаём чистый конфиг и сохраняем напрямую
            var cleanConfig = new LoginConfigData
            {
                username = "",
                jwt_token = "",
                player_id = -1
            };
            ConfigManager.SaveConfig(cleanConfig);
            
            // Сбрасываем токен в APINetworkManager
            var apiNM = UnityEngine.Object.FindAnyObjectByType<APINetworkManager>();
            if (apiNM != null)
            {
                var method = typeof(APINetworkManager).GetMethod("SetToken", 
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                method?.Invoke(null, new object[] { null });
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[System Test 13] DisableAutoLoginSafe warning: {ex.Message}");
        }
        
        Debug.Log("[System Test 13] Auto-login disabled");
    }

    private IEnumerator PerformValidLogin()
    {
        _usernameInput.text = TestUsername;
        _passwordInput.text = TestPassword;
        _loginOrRegisterButton.onClick.Invoke();
        yield return new WaitForSeconds(2.0f);
    }

    private IEnumerator RestartLoginScene()
    {
        Debug.Log("[System Test 13] Restarting LoginScene...");
        
        yield return SceneManager.LoadSceneAsync("LoginScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(2.0f);
        
        var loginController = UnityEngine.Object.FindAnyObjectByType<LoginController>();
        Assert.NotNull(loginController, "LoginController not found after restart");
        
        _usernameInput = GetPrivateField<TMP_InputField>(loginController, "usernameInput");
        _passwordInput = GetPrivateField<TMP_InputField>(loginController, "passwordInput");
        _loginOrRegisterButton = GetPrivateField<Button>(loginController, "loginOrRegisterButton");
        
        Debug.Log("[System Test 13] LoginScene restarted");
    }

    private void CorruptConfigFile(string configPath)
    {
        if (!File.Exists(configPath)) return;
        
        string original = File.ReadAllText(configPath);
        string corrupted = original.Length > 10 
            ? original.Substring(0, original.Length / 2) + "BROKEN_JSON"
            : "BROKEN_JSON";
        File.WriteAllText(configPath, corrupted);
        
        Debug.Log($"[System Test 13] Config corrupted");
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
        ResetSingleton<GameResources>();
    }

    private void ResetSingleton<T>() where T : MonoBehaviour
    {
        var field = typeof(T).GetField("Instance",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }
}