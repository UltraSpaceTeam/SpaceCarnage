using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MutualKillIntegrationTests
{
    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[Test 12] === SETUP ===");

        // Полная агрессивная очистка перед тестом
        AggressiveCleanup();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.6f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm, "NetworkManager not found");

        nm.StartHost();
        yield return new WaitForSeconds(1.2f);

        Debug.Log("[Test 12] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Simultaneous_Mutual_Kill_Shows_Both_Kills_In_KillFeed()
    {
        Debug.Log("[Test 12] === TEST START ===");

        int before = CountKillFeedEntries();

        var ctx1 = DamageContext.Weapon(999999, "PlayerB", "Rocket");
        var ctx2 = DamageContext.Weapon(888888, "PlayerA", "Rocket");

        var uiManager = Object.FindAnyObjectByType<UIManager>();
        Assert.NotNull(uiManager, "UIManager not found");

        uiManager.AddKillFeedEntry(ctx1, "PlayerA");
        yield return new WaitForSeconds(0.15f);

        uiManager.AddKillFeedEntry(ctx2, "PlayerB");
        yield return new WaitForSeconds(0.8f);

        int after = CountKillFeedEntries();

        Assert.GreaterOrEqual(after - before, 2,
            $"Expected at least 2 killfeed entries, but got only {after - before}");

        Debug.Log($"[Test 12] SUCCESS: Detected {after - before} killfeed entries for mutual kill ?");
        Debug.Log("[Test 12] === PASSED ===");
    }

    private int CountKillFeedEntries()
    {
        var allTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);

        int count = 0;
        foreach (var text in allTexts)
        {
            if (text == null) continue;

            string txt = text.text.ToLower();
            if (txt.Contains("killed") || txt.Contains("убил") || txt.Contains("rocket") || txt.Contains("player"))
            {
                count++;
            }
        }

        return count;
    }

    // ====================== АГРЕССИВНАЯ ОЧИСТКА ======================
    private void AggressiveCleanup()
    {
        // Полностью выключаем сеть
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        // Уничтожаем все NetworkManager
        var managers = Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var m in managers)
        {
            if (m != null)
                Object.DestroyImmediate(m.gameObject);
        }

        // Уничтожаем все KcpTransport
        var transports = Object.FindObjectsByType<kcp2k.KcpTransport>(FindObjectsSortMode.None);
        foreach (var t in transports)
        {
            if (t != null)
                Object.DestroyImmediate(t.gameObject);
        }

        // Сбрасываем важные Singletons
        ResetSingleton<UIManager>();
        ResetSingleton<GameResources>();
        ResetSingleton<SessionManager>();
        ResetSingleton<AudioManager>();

        // Очищаем статические данные
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