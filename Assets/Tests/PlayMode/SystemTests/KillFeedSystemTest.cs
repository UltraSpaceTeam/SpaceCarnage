using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

[Category("SystemTest")]
public class KillFeedSystemTest
{
	
	private Player _hostPlayer;
    private PlayerController _controller;
    private ShipAssembler _assembler;
    private Rigidbody _rb;
    private Health _health;

    private GameObject _killfeedGO;
    private HUDController _hud;
    private Button _respawnButton;



    [UnityTest]    
    public IEnumerator Test_KillFeed_WorksCorrectlyWithEveryGun() {
        yield return new WaitForSeconds(1.0f);

        Debug.Log("OK");

        yield return KillPlayerWithAndCheckAfter(DamageContext.Suicide("Test"), "SUICIDE"); 
        yield return KillPlayerWithAndCheckAfter(DamageContext.Environment("Test"), "CRASH"); 
        yield return KillPlayerWithAndCheckAfter(DamageContext.Weapon(1, "Test", "Rocket"), "Rocket"); 
        yield return KillPlayerWithAndCheckAfter(DamageContext.Collision(1, "Test", "Test Collision"), "RAM"); 
        yield return KillPlayerWithAndCheckAfter(DamageContext.Runaway(), "died"); 

        yield return null;
    }

    IEnumerator KillPlayerWithAndCheckAfter(DamageContext weapon, string toCheck) {
        _health.TakeDamage(9999f, weapon);
        yield return new WaitForSeconds(1.0f);

        var text = GetLastKillFeedText(_killfeedGO.transform);
        StringAssert.Contains(toCheck, text);
        StringAssert.Contains(_hostPlayer.Nickname, text);

        Debug.Log("[System Test 11] Killed with " + weapon.WeaponID + " by " + weapon.AttackerName);
        _respawnButton.onClick.Invoke();
        yield return new WaitForSeconds(5.0f);
    }

    [UnitySetUp]
    public IEnumerator Setup()
    {
        Debug.Log("[System Test 11] === SETUP ===");

        AggressiveCleanup();

        yield return SceneManager.LoadSceneAsync("TestMultiplayerScene", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.8f);

        var nm = Object.FindAnyObjectByType<NetworkManager>();
        Assert.NotNull(nm);

        nm.StartHost();
        yield return new WaitForSeconds(1.8f);

        _hostPlayer = Object.FindObjectsByType<Player>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.isLocalPlayer);

        Assert.NotNull(_hostPlayer);

        _controller = _hostPlayer.GetComponent<PlayerController>();
        _assembler = _hostPlayer.GetComponent<ShipAssembler>();
        _rb = _hostPlayer.GetComponent<Rigidbody>();
        _health = _hostPlayer.GetComponent<Health>();

        _respawnButton = FindRespawnButton();
        Assert.NotNull(_respawnButton, "Respawn button not found in DeathScreen");

        _killfeedGO = GameObject.Find("Canvas/HUD/Killfeed");
        Assert.NotNull(_killfeedGO, "Killfeed not found");

        _hud = Object.FindAnyObjectByType<HUDController>();
        Assert.NotNull(_hud, "HUDContoller not found");

        Assert.NotNull(_controller);
        Assert.NotNull(_assembler);
        Assert.NotNull(_rb);

        Debug.Log("[System Test 11] Setup OK");
    }
	
	[UnityTearDown]
    public IEnumerator TearDown()
    {
        AggressiveCleanup();
        yield return null;
    }

    private void AggressiveCleanup()
    {
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        var managers = Object.FindObjectsByType<NetworkManager>(FindObjectsSortMode.None);
        foreach (var m in managers) if (m != null) Object.DestroyImmediate(m.gameObject);

        var transports = Object.FindObjectsByType<kcp2k.KcpTransport>(FindObjectsSortMode.None);
        foreach (var t in transports) if (t != null) Object.DestroyImmediate(t.gameObject);

        ResetSingleton<UIManager>();
        ResetSingleton<GameResources>();
        ResetSingleton<SessionManager>();
        ResetSingleton<AudioManager>();

        Player.ActivePlayers.Clear();
        NetworkManager.startPositions.Clear();
    }

	
	private void ResetSingleton<T>() where T : MonoBehaviour
    {
        var field = typeof(T).GetField("Instance",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(null, null);
    }

    private string GetLastKillFeedText(Transform container)
    {
        var lastChild = container.GetChild(container.childCount - 1);
        var tmp = lastChild.GetComponentInChildren<TextMeshProUGUI>();
        return tmp != null ? tmp.text : null;
    }

    private Button FindRespawnButton()
    {
        var deathScreenPanel = GameObject.Find("DeathScreenPanel");
        if (deathScreenPanel == null) return null;

        var respawnGo = deathScreenPanel.transform.Find("DeathScreen/Respawn");
        if (respawnGo == null) return null;

        return respawnGo.GetComponent<Button>();
    }

}
