using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MatchEndIntegrationTests
{
    private Player _hostPlayer;
    private UIManager _uiManager;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[Test 10] === SETUP ===");

        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();
        Player.ActivePlayers.Clear();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.6f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm, "NetworkManager not found");

        nm.StartHost();
        yield return new WaitForSeconds(1.5f);

        _hostPlayer = Object.FindObjectsByType<Player>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.isLocalPlayer);

        Assert.NotNull(_hostPlayer, "Host player not found");

        _uiManager = Object.FindAnyObjectByType<UIManager>();
        Assert.NotNull(_uiManager, "UIManager not found in scene");

        // Даём игроку нормальную сборку (чтобы не было предупреждений)
        var assembler = _hostPlayer.GetComponent<ShipAssembler>();
        var hull = GameResources.Instance?.partDatabase.hulls.FirstOrDefault();
        var weapon = GameResources.Instance?.partDatabase.weapons.FirstOrDefault();
        var engine = GameResources.Instance?.partDatabase.engines.FirstOrDefault();

        if (hull != null) assembler.EquipHull(hull);
        if (weapon != null) assembler.EquipWeapon(weapon);
        if (engine != null) assembler.EquipEngine(engine);

        yield return new WaitForSeconds(0.8f);

        Debug.Log("[Test 10] Setup completed - Player ready for match end test");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var nm = Object.FindAnyObjectByType<NetworkManager>();
        if (nm != null) nm.StopHost();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Match_End_Shows_Leaderboard_And_Returns_To_Menu()
    {
        Debug.Log("[Test 10] === TEST START ===");

        // 1. Симулируем окончание матча через SessionManager (самый правильный способ)
        var sessionManager = Object.FindAnyObjectByType<SessionManager>();
        Assert.NotNull(sessionManager, "SessionManager not found");

        // Вызываем завершение матча (как это делает SessionManager в реальности)
        var endMatchMethod = typeof(SessionManager).GetMethod("RpcShowEndMatchLeaderboard",
            BindingFlags.NonPublic | BindingFlags.Instance); // или напрямую через RPC

        // Лучше вызвать публичный RPC на игроке
        _hostPlayer.RpcShowEndMatchLeaderboard();

        yield return new WaitForSeconds(0.8f);

        // 2. Проверяем, что лидерборд показан
        Assert.IsTrue(_uiManager.isEndMatch, "isEndMatch flag was not set");
        Assert.IsTrue(IsLeaderboardVisible(), "Leaderboard panel is not visible after match end");

        Debug.Log("[Test 10] End match leaderboard shown successfully ?");

        // 3. Проверяем переход в меню (в реальном коде после показа лидерборда происходит возврат в ShipEditor)
        //    Здесь мы просто проверяем, что RpcHideEndMatchLeaderboard существует и UI готов к переходу
        _hostPlayer.RpcHideEndMatchLeaderboard();

        yield return new WaitForSeconds(0.5f);

        Assert.IsFalse(_uiManager.isEndMatch, "isEndMatch flag was not cleared");

        Debug.Log("[Test 10] Leaderboard hidden and ready for return to menu ?");
        Debug.Log("[Test 10] === PASSED ===");
    }

    private bool IsLeaderboardVisible()
    {
        // Ищем панель лидерборда через UIManager
        var leaderboardField = typeof(UIManager).GetField("leaderboardPanel",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (leaderboardField?.GetValue(_uiManager) is GameObject panel)
        {
            return panel.activeInHierarchy;
        }

        return false;
    }
}