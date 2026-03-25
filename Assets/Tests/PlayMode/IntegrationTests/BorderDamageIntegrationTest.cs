using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class BorderDamageIntegrationTests
{
    private Player _player;
    private Health _health;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[Test 13] === SETUP ===");

        // Полная очистка перед запуском этого теста
        AggressiveCleanup();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.6f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm, "NetworkManager not found");

        nm.StartHost();
        yield return new WaitForSeconds(1.5f);

        _player = Object.FindObjectsByType<Player>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.isLocalPlayer);

        Assert.NotNull(_player, "Player not found");

        _health = _player.GetComponent<Health>();
        Assert.NotNull(_health, "Health component not found");

        EquipBasicShip(_player);

        yield return new WaitForSeconds(0.8f);

        Debug.Log("[Test 13] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator BorderZone_Damages_Player_Leads_To_Death_And_Spawns_Debris()
    {
        Debug.Log("[Test 13] === TEST START ===");

        float startHealth = _health.GetHealthPercentage();

        // Выносим игрока далеко за границу
        _player.transform.position = new Vector3(0, 0, BorderConfiguration.borderRadius + 1000f);

        yield return new WaitForSeconds(13f);

        Assert.IsTrue(_health.IsDead, "Player did not die from border damage");

        Debug.Log($"[Test 13] Player died from border damage (health {startHealth * 100:F0}% ? 0%) ?");

        yield return new WaitForSeconds(1.0f);

        bool debrisSpawned = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None)
            .Any(rb => rb != null
                    && rb.gameObject != _player.gameObject
                    && !rb.gameObject.name.Contains("Player")
                    && rb.gameObject.transform.parent == null);

        Assert.IsTrue(debrisSpawned, "Debris (objects with Rigidbody) was not spawned after death");

        Debug.Log("[Test 13] Debris successfully spawned after death ?");
        Debug.Log("[Test 13] === PASSED ===");
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

        // Уничтожаем ВСЕ NetworkManager
        var managers = Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var m in managers)
        {
            if (m != null)
                Object.DestroyImmediate(m.gameObject);
        }

        // Уничтожаем ВСЕ KcpTransport
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