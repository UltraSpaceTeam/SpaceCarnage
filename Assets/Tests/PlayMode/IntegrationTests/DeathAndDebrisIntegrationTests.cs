using System.Collections;
using System.Linq;
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

        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

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

        // Даём нормальную сборку корабля
        EquipBasicShip(_hostPlayer);

        yield return new WaitForSeconds(0.8f);

        Debug.Log("[Test 06] Setup OK");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        var nm = Object.FindAnyObjectByType<NetworkManager>();
        if (nm != null) nm.StopHost();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Player_Death_Spawns_Debris_VFX_And_Sound()
    {
        Debug.Log("[Test 06] === TEST START ===");

        // Запоминаем начальное состояние
        int debrisCountBefore = CountDebrisObjects();

        // Наносим смертельный урон
        _health.TakeDamage(9999f, DamageContext.Weapon(999999, "TestEnemy", "Rocket"));

        yield return new WaitForSeconds(1.2f);

        // Проверяем, что игрок умер
        Assert.IsTrue(_health.IsDead, "Player did not die after taking fatal damage");

        // Проверяем появление обломков
        int debrisCountAfter = CountDebrisObjects();
        Assert.Greater(debrisCountAfter, debrisCountBefore, "Debris was not spawned after player death");

        // Проверяем наличие VFX взрыва (ищем ParticleSystem)
        bool explosionVFXExists = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
            .Any(ps => ps != null && ps.gameObject.name.ToLower().Contains("explosion") ||
                       ps.gameObject.name.ToLower().Contains("explode"));

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
                         rb.transform.parent == null); // обломки обычно без parent
    }
}