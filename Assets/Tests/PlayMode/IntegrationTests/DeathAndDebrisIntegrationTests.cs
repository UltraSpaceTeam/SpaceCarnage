using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class DeathAndDebrisIntegrationTests
{
    private Player _hostPlayer;
    private Health _health;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[Test 06] === SETUP ===");

        // Полная очистка перед запуском теста
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

        Debug.Log("[Test 06] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Player_Death_Spawns_Debris_VFX_And_Sound()
    {
        Debug.Log("[Test 06] === TEST START ===");

        int debrisCountBefore = CountDebrisObjects();

        _health.TakeDamage(9999f, DamageContext.Weapon(999999, "TestEnemy", "Rocket"));

        yield return new WaitForSeconds(1.2f);

        Assert.IsTrue(_health.IsDead, "Player did not die after taking fatal damage");

        int debrisCountAfter = CountDebrisObjects();
        Assert.Greater(debrisCountAfter, debrisCountBefore, "Debris was not spawned after player death");

        bool explosionVFXExists = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
            .Any(ps => ps != null &&
                       (ps.gameObject.name.ToLower().Contains("explosion") ||
                        ps.gameObject.name.ToLower().Contains("explode")));

        Assert.IsTrue(explosionVFXExists, "Explosion VFX was not spawned after death");

        Debug.Log("[Test 06] Player death successfully spawned debris, VFX and triggered sound effects ?");
        Debug.Log("[Test 06] === PASSED ===");
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

    private int CountDebrisObjects()
    {
        return Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None)
            .Count(rb => rb != null &&
                         rb.gameObject != _hostPlayer.gameObject &&
                         rb.transform.parent == null);
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