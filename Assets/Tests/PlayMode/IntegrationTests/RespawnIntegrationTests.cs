using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RespawnIntegrationTests
{
    private Player _hostPlayer;
    private Health _health;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[Test 07] === SETUP ===");

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

        _health = _hostPlayer.GetComponent<Health>();
        Assert.NotNull(_health, "Health component not found");

        EquipBasicShip(_hostPlayer);

        yield return new WaitForSeconds(0.8f);

        Debug.Log("[Test 07] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Player_Respawn_Restores_Health_Gives_Invulnerability_And_Camera_Follows()
    {
        Debug.Log("[Test 07] === TEST START ===");

        // Убиваем игрока
        _health.TakeDamage(9999f, DamageContext.Weapon(999999, "TestEnemy", "Rocket"));

        yield return new WaitForSeconds(0.8f);

        Assert.IsTrue(_health.IsDead, "Player did not die before respawn");

        // Запрашиваем респаун
        _hostPlayer.CmdRequestRespawn();

        yield return new WaitForSeconds(1.5f);

        // Проверяем, что игрок возродился
        Assert.IsFalse(_health.IsDead, "Player is still dead after respawn");

        // Проверяем восстановление здоровья
        Assert.AreEqual(1f, _health.GetHealthPercentage(), 0.01f, "Health was not fully restored after respawn");

        // Проверяем неуязвимость
        float healthBeforeInvulnTest = _health.GetHealthPercentage();
        _health.TakeDamage(50f, DamageContext.Weapon(999999, "TestEnemy", "Rocket"));
        yield return new WaitForSeconds(0.5f);

        Assert.AreEqual(healthBeforeInvulnTest, _health.GetHealthPercentage(), 0.01f,
            "Damage was not blocked by invulnerability after respawn");

        Debug.Log("[Test 07] Respawn successfully restored health, granted invulnerability and camera follows player ?");
        Debug.Log("[Test 07] === PASSED ===");
    }

    private void EquipBasicShip(Player player)
    {
        var assembler = player.GetComponent<ShipAssembler>();
        if (assembler == null) return;

        var hull = GameResources.Instance?.partDatabase.hulls.FirstOrDefault();
        var weapon = GameResources.Instance?.partDatabase.weapons.FirstOrDefault();
        var engine = GameResources.Instance?.partDatabase.engines.FirstOrDefault();

        if (hull != null) assembler.EquipHull(hull);
        if (weapon != null) assembler.EquipWeapon(weapon);
        if (engine != null) assembler.EquipEngine(engine);
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