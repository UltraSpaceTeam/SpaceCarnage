using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MatchCompletionSystemTest
{
    private float _originalMatchDuration;
    private float _originalEndingDuration;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 02] === SETUP ===");

        AggressiveFullCleanup();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(1.5f);

        SaveOriginalDurations();
        SetShortDurations(8f, 5f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm);

        nm.StartHost();
        yield return new WaitForSeconds(2.5f);

        Debug.Log("[System Test 02] New clean session started");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        RestoreOriginalDurations();
        AggressiveFullCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_FullMatch_FromStart_ToLeaderboard_And_ReturnToEditor()
    {
        Debug.Log("[System Test 02] === TEST START ===");

        LogAssert.Expect(LogType.Log, new Regex(@"\[API RAW RESPONSE\].*Session not found", RegexOptions.IgnoreCase));
        LogAssert.Expect(LogType.Error, new Regex(@"\[API ERROR\].*Session not found", RegexOptions.IgnoreCase));
        LogAssert.Expect(LogType.Error, new Regex(@"\[API\] Result Error.*Session not found", RegexOptions.IgnoreCase));

        yield return new WaitForSeconds(2.0f);
        Assert.IsTrue(IsMatchPlaying(), "Match did not start");

        yield return new WaitForSeconds(8.0f);

        Assert.IsTrue(IsEndMatchLeaderboardVisible(), "End-match leaderboard did not appear");

        yield return new WaitForSeconds(5.0f);

        Assert.IsFalse(UIManager.Instance.isEndMatch, "Did not exit end-match state");

        Debug.Log("[System Test 02] === TEST PASSED ===");
    }

    private void SaveOriginalDurations()
    {
        _originalMatchDuration = GetStaticFloat("MatchDuration");
        _originalEndingDuration = GetStaticFloat("EndingDuration");
    }

    private void SetShortDurations(float match, float ending)
    {
        SetStaticFloat("MatchDuration", match);
        SetStaticFloat("EndingDuration", ending);
    }

    private void RestoreOriginalDurations()
    {
        SetStaticFloat("MatchDuration", _originalMatchDuration);
        SetStaticFloat("EndingDuration", _originalEndingDuration);
    }

    private float GetStaticFloat(string fieldName)
    {
        var field = typeof(SessionManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        return field != null ? (float)field.GetValue(null) : 600f;
    }

    private void SetStaticFloat(string fieldName, float value)
    {
        var field = typeof(SessionManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, value);
    }

    private bool IsMatchPlaying()
    {
        var session = Object.FindAnyObjectByType<SessionManager>();
        if (session == null) return false;

        var stateField = typeof(SessionManager).GetField("currentState", BindingFlags.NonPublic | BindingFlags.Instance);
        var state = stateField?.GetValue(session)?.ToString();
        return state != null && state.Contains("Playing");
    }

    private bool IsEndMatchLeaderboardVisible()
    {
        return UIManager.Instance != null && UIManager.Instance.isEndMatch;
    }

    private void AggressiveFullCleanup()
    {
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        var session = Object.FindAnyObjectByType<SessionManager>();
        if (session != null)
        {
            session.StopAllCoroutines();
            Object.DestroyImmediate(session.gameObject);
        }

        var managers = Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var m in managers)
            if (m != null) Object.DestroyImmediate(m.gameObject);

        var transports = Object.FindObjectsByType<kcp2k.KcpTransport>(FindObjectsSortMode.None);
        foreach (var t in transports)
            if (t != null) Object.DestroyImmediate(t.gameObject);

        ResetSingleton<UIManager>();
        ResetSingleton<SessionManager>();
        ResetSingleton<GameResources>();

        Player.ActivePlayers.Clear();
        NetworkManager.startPositions.Clear();
    }

    private void ResetSingleton<T>() where T : MonoBehaviour
    {
        var field = typeof(T).GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }
}