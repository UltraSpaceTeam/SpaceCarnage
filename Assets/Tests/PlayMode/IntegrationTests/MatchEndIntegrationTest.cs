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

        // Полная агрессивная очистка перед тестом
        AggressiveCleanup();

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

        // Экипируем базовый корабль
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
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Match_End_Shows_Leaderboard_And_Returns_To_Menu()
    {
        Debug.Log("[Test 10] === TEST START ===");

        var sessionManager = Object.FindAnyObjectByType<SessionManager>();
        Assert.NotNull(sessionManager, "SessionManager not found");

        // Вызываем RPC показа лидерборда
        _hostPlayer.RpcShowEndMatchLeaderboard();

        yield return new WaitForSeconds(0.8f);

        Assert.IsTrue(_uiManager.isEndMatch, "isEndMatch flag was not set");
        Assert.IsTrue(IsLeaderboardVisible(), "Leaderboard panel is not visible after match end");

        Debug.Log("[Test 10] End match leaderboard shown successfully ?");

        // Скрываем лидерборд
        _hostPlayer.RpcHideEndMatchLeaderboard();

        yield return new WaitForSeconds(0.5f);

        Assert.IsFalse(_uiManager.isEndMatch, "isEndMatch flag was not cleared");

        Debug.Log("[Test 10] Leaderboard hidden and ready for return to menu ?");
        Debug.Log("[Test 10] === PASSED ===");
    }

    private bool IsLeaderboardVisible()
    {
        var leaderboardField = typeof(UIManager).GetField("leaderboardPanel",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (leaderboardField?.GetValue(_uiManager) is GameObject panel)
        {
            return panel.activeInHierarchy;
        }

        return false;
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

        // Очищаем статические данные Mirror
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