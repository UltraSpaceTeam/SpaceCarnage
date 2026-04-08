using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Reflection;

[Category("SystemTest")]
public class StatsUpdateSystemTest
{
    private TMP_InputField _usernameInput;
    private TMP_InputField _passwordInput;
    private Button _loginOrRegisterButton;
    private TextMeshProUGUI _feedbackText;

    private const string TestUsername = "antoxic";
    private const string TestPassword = "bebra123";

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 03] === SETUP ===");

        AggressiveCleanup();
        DisableAutoLogin();

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

        Debug.Log($"[System Test 03] Using existing user: {TestUsername}");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SetDefaultMatchTimers();
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_StatisticsSavingAfterMatch()
    {
        Debug.Log("[System Test 03] === TEST START ===");

        _usernameInput.text = "antoxic";
        _passwordInput.text = "bebra123";
        _loginOrRegisterButton.onClick.Invoke();
        yield return new WaitForSeconds(3.5f);

        Assert.AreEqual("ShipEditor", SceneManager.GetActiveScene().name,
            "Не удалось войти в ShipEditor после логина");

        Debug.Log("[System Test 03] Login - PASSED");

        yield return AssembleBasicShip();

        yield return OpenGlobalLeaderboard();
        int initialKills = GetKillsFromLeaderboardUI();
        int initialDeaths = GetDeathsFromLeaderboardUI();
        CloseLeaderboardIfOpen();

        Debug.Log($"[System Test 03] Начальная статистика ? Kills: {initialKills}, Deaths: {initialDeaths}");

        yield return LoadTestMatchSceneWithShortTimers();

        Debug.Log("[System Test 03] Матч запущен. Ожидаем завершения...");

        Debug.Log("[System Test 03] Принудительно убиваем игрока для изменения статистики...");
        yield return KillLocalPlayer();

        yield return new WaitForSeconds(9.0f);

        Assert.IsTrue(IsEndMatchLeaderboardVisible(), "Таблица лидеров в конце матча не появилась");

        Debug.Log("[System Test 03] Лидерборд показан. Ждём перезапуска сессии...");

        yield return new WaitForSeconds(6.0f);

        yield return ReturnToShipEditorManually();

        Assert.AreEqual("ShipEditor", SceneManager.GetActiveScene().name,
            "Не удалось вернуться в ShipEditor после матча");

        yield return new WaitForSeconds(3.5f);
        yield return OpenGlobalLeaderboard();

        int finalKills = GetKillsFromLeaderboardUI();
        int finalDeaths = GetDeathsFromLeaderboardUI();

        Debug.Log($"[System Test 03] Финальная статистика из лидерборда ? Kills: {finalKills}, Deaths: {finalDeaths}");

        Assert.IsTrue(finalDeaths > initialDeaths,
            $"Статистика не обновилась в глобальном лидерборде!\n" +
            $"Deaths: {initialDeaths} ? {finalDeaths}");

        CloseLeaderboardIfOpen();

        Debug.Log("[System Test 03] === TEST PASSED ===");
    }

    private IEnumerator OpenGlobalLeaderboard()
    {
        var shipEditorUI = UnityEngine.Object.FindAnyObjectByType<ShipEditorUI>();
        if (shipEditorUI == null)
        {
            Debug.LogWarning("[Test] ShipEditorUI не найден");
            yield break;
        }

        var buttonField = typeof(ShipEditorUI).GetField("leaderboardButton",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (buttonField != null)
        {
            var leaderboardButton = buttonField.GetValue(shipEditorUI) as Button;
            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.Invoke();
                Debug.Log("[Test] Кнопка глобального лидерборда нажата");
                yield return new WaitForSeconds(2.5f);
                yield break;
            }
        }

        Debug.LogWarning("[Test] leaderboardButton не найден через Reflection");
    }

    private void CloseLeaderboardIfOpen()
    {
        var leaderboardUI = UnityEngine.Object.FindAnyObjectByType<GlobalLeaderboardUI>();
        if (leaderboardUI != null)
        {
            var panelField = leaderboardUI.GetType().GetField("leaderboardPanel",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (panelField != null)
            {
                var panel = panelField.GetValue(leaderboardUI) as GameObject;
                if (panel != null) panel.SetActive(false);
            }
        }
    }

    private int GetKillsFromLeaderboardUI()
    {
        return GetStatFromLeaderboardUI(2);
    }

    private int GetDeathsFromLeaderboardUI()
    {
        return GetStatFromLeaderboardUI(3);
    }

    private int GetStatFromLeaderboardUI(int columnIndex)
    {
        var allTexts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);

        foreach (var text in allTexts)
        {
            if (text.text.Contains(TestUsername))
            {
                var row = text.transform.parent;
                var textsInRow = row.GetComponentsInChildren<TextMeshProUGUI>();

                if (textsInRow.Length > columnIndex)
                {
                    if (int.TryParse(textsInRow[columnIndex].text, out int value))
                        return value;
                }
            }
        }
        return 0;
    }

    private IEnumerator KillLocalPlayer()
    {
        var localPlayerObj = Player.ActivePlayers.Values.FirstOrDefault(p => p.isLocalPlayer);
        if (localPlayerObj == null)
        {
            Debug.LogWarning("[System Test 03] Local player not found!");
            yield break;
        }

        var health = localPlayerObj.GetComponent<Health>();
        if (health == null)
        {
            Debug.LogWarning("[System Test 03] Health component not found on local player!");
            yield break;
        }

        health.TakeDamage(9999f, DamageContext.Suicide("Test Self-Destruct"));

        Debug.Log("[System Test 03] Нанесён смертельный урон (9999) через Health.TakeDamage");
        yield return null;
    }

    private IEnumerator AssembleBasicShip()
    {
        Debug.Log("[System Test 03] Сборка базового корабля...");
        yield return new WaitForSeconds(2.5f);
    }

    private IEnumerator LoadTestMatchSceneWithShortTimers()
    {
        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(2.5f);

        SetShortMatchTimers();

        var nm = UnityEngine.Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm, "NetworkManager не найден");

        nm.StartHost();
        yield return new WaitForSeconds(2.0f);
    }

    private void SetShortMatchTimers()
    {
        var sm = UnityEngine.Object.FindAnyObjectByType<SessionManager>();
        if (sm == null) return;

        SetStaticFloat(sm, "MatchDuration", 8f);
        SetStaticFloat(sm, "EndingDuration", 5f);
    }

    private void SetDefaultMatchTimers()
    {
        var sm = UnityEngine.Object.FindAnyObjectByType<SessionManager>();
        if (sm == null) return;

        SetStaticFloat(sm, "MatchDuration", 600f);
        SetStaticFloat(sm, "EndingDuration", 30f);
    }

    private void SetStaticFloat(object obj, string fieldName, float value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);

        field?.SetValue(obj, value);
    }

    private IEnumerator ReturnToShipEditorManually()
    {
        if (SceneManager.GetActiveScene().name == "ShipEditor")
            yield break;

        Debug.Log("[System Test 03] Ручной переход в ShipEditor...");
        yield return SceneManager.LoadSceneAsync("ShipEditor", LoadSceneMode.Single);
        yield return new WaitForSeconds(3.0f);
    }

    private bool IsEndMatchLeaderboardVisible()
    {
        var ui = UnityEngine.Object.FindAnyObjectByType<UIManager>();
        return ui != null && ui.isEndMatch;
    }

    private void DisableAutoLogin()
    {
        var config = ConfigManager.LoadConfig();
        if (config != null)
        {
            config.jwt_token = "";
            config.username = "";
            config.player_id = -1;
            ConfigManager.SaveConfig(config);
        }
        APINetworkManager.SetToken(null);
        Debug.Log("[System Test 09] Auto-login disabled");
    }

    private T GetPrivateField<T>(object obj, string fieldName) where T : class
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(obj) as T;
    }

    private void AggressiveCleanup()
    {
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        var managers = UnityEngine.Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var m in managers)
            if (m != null) UnityEngine.Object.DestroyImmediate(m.gameObject);

        ResetSingleton<UIManager>();
        ResetSingleton<SessionManager>();
        ResetSingleton<GameResources>();

        Player.ActivePlayers.Clear();
        NetworkManager.startPositions.Clear();
    }

    private void ResetSingleton<T>() where T : MonoBehaviour
    {
        var field = typeof(T).GetField("Instance",
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }
}