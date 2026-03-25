using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Mirror;

public class SuccessfulLoginIntegrationTest
{
    private const string LoginScene = "LoginScene";
    private const string NextScene = "ShipEditor";
    private const string TestUser = "antoxic";
    private const string TestPass = "bebra123";

    private string _configPath;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[Test 01] === SETUP ===");

        AggressiveCleanup();

        _configPath = Path.Combine(Application.persistentDataPath, "user_config.cfg");
        if (File.Exists(_configPath))
            File.Delete(_configPath);

        yield return SceneManager.LoadSceneAsync(LoginScene, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();

        if (File.Exists(_configPath))
            File.Delete(_configPath);

        yield return null;
    }

    [UnityTest]
    public IEnumerator Successful_Login_Saves_Token_And_AutoLogins_On_Next_Launch()
    {
        Debug.Log("[Test 01] === TEST START ===");

        // ?? Шаг 1-2: Убеждаемся что конфиг пуст, вводим креды и логинимся ????

        var configBefore = ConfigManager.LoadConfig();
        Assert.IsTrue(string.IsNullOrEmpty(configBefore?.jwt_token),
            "Precondition failed: token should be empty before login");

        var usernameField = GameObject.Find("Login")?.GetComponent<TMP_InputField>();
        var passwordField = GameObject.Find("Password")?.GetComponent<TMP_InputField>();

        Assert.IsNotNull(usernameField, "Login InputField not found in scene");
        Assert.IsNotNull(passwordField, "Password InputField not found in scene");

        usernameField.text = TestUser;
        passwordField.text = TestPass;

        var controller = Object.FindFirstObjectByType<LoginController>();
        Assert.IsNotNull(controller, "LoginController not found in scene");

        var loginTask = controller.LoginOrRegisterAsync();

        float elapsed = 0f;
        yield return new WaitUntil(() =>
        {
            elapsed += Time.unscaledDeltaTime;
            return loginTask.IsCompleted || elapsed > 15f;
        });

        Assert.IsTrue(loginTask.IsCompleted, "Login task did not complete in time");
        Assert.IsNull(loginTask.Exception, $"Login task threw exception: {loginTask.Exception}");

        // ?? Шаг 3: Проверяем что токен и player_id сохранены ?????????????????

        var configAfterLogin = ConfigManager.LoadConfig();

        Assert.IsFalse(string.IsNullOrEmpty(configAfterLogin?.jwt_token),
            "Token should be saved after successful login");
        Assert.AreNotEqual(-1, configAfterLogin.player_id,
            "player_id should be saved after successful login");
        Assert.AreEqual(TestUser, configAfterLogin.username,
            "username should be saved after successful login");

        Debug.Log($"[Test 01] Login OK: token={configAfterLogin.jwt_token}, id={configAfterLogin.player_id}");

        // ?? Шаги 4-5: Перезапускаем сцену — имитируем повторный запуск ???????
        // CheckAutoLoginAsync сработает автоматически в Start()

        yield return SceneManager.LoadSceneAsync(LoginScene, LoadSceneMode.Single);

        elapsed = 0f;
        yield return new WaitUntil(() =>
        {
            elapsed += Time.unscaledDeltaTime;
            return SceneManager.GetActiveScene().name == NextScene || elapsed > 15f;
        });

        // ?? Ожидаемый результат: автологин прошёл, перешли на NextScene ???????

        string currentScene = SceneManager.GetActiveScene().name;
        Assert.AreEqual(NextScene, currentScene,
            "Should auto-login and navigate to ShipEditor without manual credentials");

        Debug.Log("[Test 01] === PASSED: Auto-login navigated to " + NextScene + " ===");
    }

    // ??????????????????????????????????????????????????????????????????????????

    private void AggressiveCleanup()
    {
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        foreach (var nm in Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None))
            if (nm != null) Object.DestroyImmediate(nm.gameObject);

        foreach (var t in Object.FindObjectsByType<kcp2k.KcpTransport>(FindObjectsSortMode.None))
            if (t != null) Object.DestroyImmediate(t.gameObject);

        ResetSingleton<UIManager>();
        ResetSingleton<GameResources>();
        ResetSingleton<SessionManager>();
        ResetSingleton<AudioManager>();
        ResetSingleton<APINetworkManager>();

        Player.ActivePlayers.Clear();
        NetworkManager.startPositions.Clear();
    }

    private void ResetSingleton<T>() where T : MonoBehaviour
    {
        var field = typeof(T).GetField("Instance",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }
}