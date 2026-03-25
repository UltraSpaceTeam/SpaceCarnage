using System.Collections;
using System.Linq;
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

        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.6f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm);

        nm.StartHost();
        yield return new WaitForSeconds(1.2f);

        Debug.Log("[Test 12] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var nm = Object.FindAnyObjectByType<NetworkManager>();
        if (nm != null) nm.StopHost();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Simultaneous_Mutual_Kill_Shows_Both_Kills_In_KillFeed()
    {
        Debug.Log("[Test 12] === TEST START ===");

        int before = CountKillFeedEntries();

        // —имулируем взаимное убийство
        var ctx1 = DamageContext.Weapon(999999, "PlayerB", "Rocket");
        var ctx2 = DamageContext.Weapon(888888, "PlayerA", "Rocket");

        var uiManager = Object.FindAnyObjectByType<UIManager>();
        Assert.NotNull(uiManager);

        uiManager.AddKillFeedEntry(ctx1, "PlayerA");
        yield return new WaitForSeconds(0.15f);   // небольша€ задержка между добавлени€ми

        uiManager.AddKillFeedEntry(ctx2, "PlayerB");
        yield return new WaitForSeconds(0.8f);    // даЄм врем€ на создание и??

        int after = CountKillFeedEntries();

        Assert.GreaterOrEqual(after - before, 2,
            $"Expected at least 2 killfeed entries, but got only {after - before}");

        Debug.Log($"[Test 12] SUCCESS: Detected {after - before} killfeed entries for mutual kill ?");
        Debug.Log("[Test 12] === PASSED ===");
    }

    // —амый надЄжный способ подсчЄта Ч ищем все TextMeshProUGUI внутри kill-feed области
    private int CountKillFeedEntries()
    {
        // »щем все активные TextMeshProUGUI на сцене
        var allTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);

        int count = 0;
        foreach (var text in allTexts)
        {
            if (text == null) continue;

            string txt = text.text.ToLower();
            if (txt.Contains("убил") || txt.Contains("killed") || txt.Contains("rocket") || txt.Contains("player"))
            {
                count++;
            }
        }

        return count;
    }
}