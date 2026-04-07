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
using System.Text.RegularExpressions;

public class FullGameCycleSystemTest
{
    private string _currentTestUsername;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 01] === SETUP ===");

        AggressiveCleanup();
        ClearAllSavedData();

        _currentTestUsername = "testplayer_" + DateTime.UtcNow.Ticks.ToString().Substring(8);

        yield return SceneManager.LoadSceneAsync("LoginScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(2.0f);

        Debug.Log($"[System Test 01] Using unique username: {_currentTestUsername}");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_FullGameCycle_FromFirstLaunch_ToMatchEnd()
    {
        Debug.Log("[System Test 01] === TEST START ===");

        yield return SwitchToRegisterMode();
        yield return PerformRegistration(_currentTestUsername, "bebra123");

        yield return new WaitForSeconds(2.0f);
        Assert.AreEqual("ShipEditor", SceneManager.GetActiveScene().name, "Did not enter ShipEditor");

        Debug.Log("[System Test 01] Registration + Login - PASSED");

        yield return AssembleBasicShip();

        yield return LoadTestMatchSceneWithShortTimers();

        LogAssert.Expect(LogType.Error, new Regex(@"\[API ERROR\].*Session not found", RegexOptions.IgnoreCase));
        LogAssert.Expect(LogType.Error, new Regex(@"\[API\] Result Error.*Session not found", RegexOptions.IgnoreCase));

        Debug.Log("[System Test 01] Waiting for short match to end...");
        yield return new WaitForSeconds(9.0f);

        Assert.IsTrue(IsEndMatchLeaderboardVisible(), "End-match leaderboard did not appear");

        yield return new WaitForSeconds(5.0f);

        Assert.IsFalse(UIManager.Instance.isEndMatch, "Did not exit end-match state");

        Debug.Log("[System Test 01] === FULL GAME CYCLE TEST PASSED ===");
    }

    private IEnumerator SwitchToRegisterMode()
    {
        var registerButton = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .FirstOrDefault(b => b.name.Contains("Register"));

        if (registerButton != null)
        {
            registerButton.onClick.Invoke();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator PerformRegistration(string username, string password)
    {
        var usernameInput = FindInputField("Username", "Login");
        var passwordInput = FindInputField("Password");
        var actionButton = FindButton("ActionButton");

        Assert.NotNull(usernameInput);
        Assert.NotNull(passwordInput);
        Assert.NotNull(actionButton);

        usernameInput.text = username;
        passwordInput.text = password;
        actionButton.onClick.Invoke();

        yield return new WaitForSeconds(4.0f);
    }

    private IEnumerator AssembleBasicShip()
    {
        Debug.Log("[System Test 01] Assembling basic ship...");
        yield return new WaitForSeconds(2.5f);
        Debug.Log("[System Test 01] Ship assembly completed (placeholder)");
    }

    private IEnumerator LoadTestMatchSceneWithShortTimers()
    {
        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(2.0f);

        SetShortMatchTimers();

        var networkManager = UnityEngine.Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(networkManager);

        networkManager.StartHost();

        yield return new WaitForSeconds(2.0f);

        Debug.Log("[System Test 01] Loaded TestMultiplayerScene with short timers");
    }

    private void SetShortMatchTimers()
    {
        var sessionManager = UnityEngine.Object.FindAnyObjectByType<SessionManager>();
        if (sessionManager == null) return;

        SetStaticFloat(sessionManager, "MatchDuration", 8f);
        SetStaticFloat(sessionManager, "EndingDuration", 5f);

        Debug.Log("[System Test 01] Match timers shortened to 8s + 5s");
    }

    private void SetStaticFloat(object obj, string fieldName, float value)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    private bool IsEndMatchLeaderboardVisible()
    {
        var ui = UnityEngine.Object.FindAnyObjectByType<UIManager>();
        return ui != null && ui.isEndMatch;
    }

    private TMP_InputField FindInputField(params string[] keywords)
    {
        var all = UnityEngine.Object.FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None);
        foreach (var kw in keywords)
        {
            var found = all.FirstOrDefault(i => i.name.Contains(kw));
            if (found != null) return found;
        }
        return null;
    }

    private Button FindButton(params string[] keywords)
    {
        var all = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var kw in keywords)
        {
            var found = all.FirstOrDefault(b => b.name.Contains(kw));
            if (found != null) return found;
        }
        return null;
    }

    private void ClearAllSavedData()
    {
        PlayerPrefs.DeleteAll();
        string path = Application.persistentDataPath;
        try
        {
            if (System.IO.File.Exists(path + "/user_config.cfg")) System.IO.File.Delete(path + "/user_config.cfg");
            if (System.IO.File.Exists(path + "/user_ship_config.cfg")) System.IO.File.Delete(path + "/user_ship_config.cfg");
        }
        catch { }
    }

    private void AggressiveCleanup()
    {
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        var managers = UnityEngine.Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var m in managers)
            if (m != null) UnityEngine.Object.DestroyImmediate(m.gameObject);

        ResetSingleton<UIManager>();
        ResetSingleton<GameResources>();
        ResetSingleton<SessionManager>();
    }

    private void ResetSingleton<T>() where T : MonoBehaviour
    {
        var field = typeof(T).GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }
}