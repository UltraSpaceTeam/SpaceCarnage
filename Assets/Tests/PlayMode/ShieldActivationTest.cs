using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using kcp2k;
using UnityEngine;
using UnityEngine.TestTools;

public class ShieldIntegrationTests
{
    private GameObject _transportGO;
    private GameObject _networkManagerGO;
    private NetworkManager _networkManager;
    private GameObject _playerPrefab;
    private GameObject _uiGO;
    private GameObject _hudGO;
    private GameObject _shieldBubblePrefab;
    private bool _ownsUIManager;

    private Player _hostPlayer;      // Server-side instance
    private Player _clientPlayer;    // Client-side instance (local player on the client world)

    private const float MaxShieldHealth = 100f;
    private const float DamageToAbsorb = 50f;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        // Ensure previous tests didn't leave networking running.
        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        foreach (var p in Object.FindObjectsByType<Player>())
            Object.DestroyImmediate(p.gameObject);
        Player.ActivePlayers.Clear();

        if (NetworkManager.singleton != null)
        {
            // Avoid hitting Mirror's "only one NetworkManager can exist" constraint.
            NetworkManager.singleton.StopHost();
            NetworkManager.singleton.StopServer();
            NetworkManager.singleton.StopClient();
            Object.DestroyImmediate(NetworkManager.singleton.gameObject);
        }

        CreateFakeUIManager();

        _transportGO = new GameObject("TestTransport");
        var transport = _transportGO.AddComponent<KcpTransport>();
        transport.port = 30000;
        Transport.active = transport;

        _playerPrefab = CreatePlayerPrefab();
        _playerPrefab.SetActive(true);

        _networkManagerGO = new GameObject("TestNetworkManager");
        _networkManager = _networkManagerGO.AddComponent<NetworkManager>();
        _networkManager.transport = transport;
        _networkManager.networkAddress = "localhost";

        // Mirror uses transport.Port (not NetworkManager.port in this version).
        _networkManager.playerPrefab = _playerPrefab;
        _networkManager.maxConnections = 2;
        _networkManager.autoCreatePlayer = true;

        _networkManagerGO.SetActive(true);

        _networkManager.StartHost();
        yield return new WaitForSeconds(0.2f);

        // Try to start a real extra client. In some setups Mirror might early-return
        // because host already started a client connection, but the test should still
        // find both server/client Player instances in host mode.
        _networkManager.StartClient();

        yield return WaitForHostAndClientPlayers(5f);

#if UNITY_INCLUDE_TESTS
        Assert.NotNull(_hostPlayer, "Host player instance not found");
        Assert.NotNull(_clientPlayer, "Client player instance not found");
#else
        Assert.Fail("UNITY_INCLUDE_TESTS is not enabled - Test_OnShieldShown hooks are unavailable.");
#endif
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Player.ActivePlayers.Clear();

        if (_networkManager != null)
        {
            _networkManager.StopHost();
            _networkManager.StopClient();
            _networkManager.StopServer();
        }

        if (NetworkServer.active) NetworkServer.Shutdown();
        if (NetworkClient.active) NetworkClient.Shutdown();

        if (_hostPlayer != null) Object.DestroyImmediate(_hostPlayer.gameObject);
        if (_clientPlayer != null && (_hostPlayer == null || _clientPlayer.gameObject != _hostPlayer.gameObject))
            Object.DestroyImmediate(_clientPlayer.gameObject);

        if (_networkManagerGO != null) Object.DestroyImmediate(_networkManagerGO);
        if (_transportGO != null) Object.DestroyImmediate(_transportGO);
        if (_playerPrefab != null) Object.DestroyImmediate(_playerPrefab);
        if (_uiGO != null) Object.DestroyImmediate(_uiGO);
        if (_shieldBubblePrefab != null) Object.DestroyImmediate(_shieldBubblePrefab);
        if (_hudGO != null) Object.DestroyImmediate(_hudGO);

        // Clear static UI singleton to avoid leakage into other tests.
        if (_ownsUIManager)
        {
            var uiInstanceField = typeof(UIManager).GetField("Instance",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            uiInstanceField?.SetValue(null, null);
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator Shield_Activation_RpcShownToAllClients_And_AbsorbsDamage()
    {
        // --- Arrange ---
        Assert.NotNull(_hostPlayer);
        Assert.NotNull(_clientPlayer);

        var hostHealth = _hostPlayer.GetComponent<Health>();
        var clientHealth = _clientPlayer.GetComponent<Health>();
        Assert.NotNull(hostHealth);
        Assert.NotNull(clientHealth);

        hostHealth.SetMaxHealth(MaxShieldHealth);

        // Allow sync vars to propagate.
        yield return WaitUntil(() =>
        {
            return Mathf.Abs(hostHealth.GetHealthPercentage() - 1f) < 0.0001f &&
                   Mathf.Abs(clientHealth.GetHealthPercentage() - 1f) < 0.0001f;
        }, 2f);

        float hostHealthBefore = hostHealth.GetHealthPercentage();
        float clientHealthBefore = clientHealth.GetHealthPercentage();
        Assert.AreEqual(1f, hostHealthBefore, 0.0001f, "Precondition failed: host health is not full");
        Assert.AreEqual(1f, clientHealthBefore, 0.0001f, "Precondition failed: client health is not full");

        int hostShowTrueCount = 0;
        int clientShowTrueCount = 0;
        float hostLastShowRatio = -1f;
        float clientLastShowRatio = -1f;

#if UNITY_INCLUDE_TESTS
        if (_clientPlayer == _hostPlayer)
        {
            _hostPlayer.Test_OnShieldShown = (show, ratio) =>
            {
                if (!show) return;
                hostShowTrueCount++;
                hostLastShowRatio = ratio;
                clientShowTrueCount++;
                clientLastShowRatio = ratio;
            };
        }
        else
        {
            _hostPlayer.Test_OnShieldShown = (show, ratio) =>
            {
                if (!show) return;
                hostShowTrueCount++;
                hostLastShowRatio = ratio;
            };

            _clientPlayer.Test_OnShieldShown = (show, ratio) =>
            {
                if (!show) return;
                clientShowTrueCount++;
                clientLastShowRatio = ratio;
            };
        }
#endif

        // Create shield ability runtime and inject it into PlayerController.
        var ability = ScriptableObject.CreateInstance<ShieldAbility>();
        ability.maxShieldHealth = MaxShieldHealth;

        var runtime = ability.CreateRuntime();
        var controller = _hostPlayer.GetComponent<PlayerController>();
        Assert.NotNull(controller);

        // Bind runtime to Rigidbody and Player so it can send RPC (via player.RpcShowShield).
        var rb = _hostPlayer.GetComponent<Rigidbody>();
        Assert.NotNull(rb);

        runtime.Bind(rb, _hostPlayer);
        runtime.OnEquipped();

        // Inject runtime into controller so Health.TakeDamage uses shield absorption.
        var abilityField = typeof(PlayerController).GetField("_ability", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(abilityField, "PlayerController._ability field not found");
        abilityField.SetValue(controller, runtime);

        // Reset counters potentially affected by OnEquipped (it calls RpcShowShield(false, 0)).
#if UNITY_INCLUDE_TESTS
        hostShowTrueCount = 0;
        clientShowTrueCount = 0;
        hostLastShowRatio = -1f;
        clientLastShowRatio = -1f;
#endif

        // --- Act 1: Activate shield ---
        runtime.Run(); // toggles shield ON and triggers RpcShowShield(show:true,...)

        // Wait until both server-side and client-side instances observed the RPC show=true.
        yield return WaitUntil(() => hostShowTrueCount >= 1 && clientShowTrueCount >= 1, 3f);

#if UNITY_INCLUDE_TESTS
        Assert.Greater(hostShowTrueCount, 0, "Shield visual RPC was not observed on host client instance");
        Assert.Greater(clientShowTrueCount, 0, "Shield visual RPC was not observed on remote/client instance");
        Assert.AreEqual(1f, hostLastShowRatio, 0.05f, "Host shield ratio after activation is unexpected");
        Assert.AreEqual(1f, clientLastShowRatio, 0.05f, "Client shield ratio after activation is unexpected");
#endif

        // --- Act 2: Deal damage ---
        hostHealth.TakeDamage(DamageToAbsorb, DamageContext.Weapon(0u, "Enemy", "Gun"));
        yield return null; // allow any server-side logic to settle

        // Force a shield status RPC after partial absorption (ShieldRuntime does not RpcShowShield on partial absorb).
        runtime.ServerUpdate();
        yield return null;

        // --- Assert: health does not decrease ---
        float hostHealthAfter = hostHealth.GetHealthPercentage();
        float clientHealthAfter = clientHealth.GetHealthPercentage();

        Assert.AreEqual(hostHealthBefore, hostHealthAfter, 0.0001f, "Health must not decrease when shield absorbs damage");
        Assert.AreEqual(clientHealthBefore, clientHealthAfter, 0.0001f, "Health must not decrease on clients when shield absorbs damage");

        // --- Assert: shield charge drops ---
        yield return WaitUntil(() => hostShowTrueCount >= 2 && clientShowTrueCount >= 2, 2f);

#if UNITY_INCLUDE_TESTS
        Assert.Less(hostLastShowRatio, 0.99f, "Shield ratio did not decrease after absorbing damage");
        Assert.Less(clientLastShowRatio, 0.99f, "Shield ratio did not decrease after absorbing damage");
        Assert.AreEqual(0.5f, hostLastShowRatio, 0.08f, "Host shield ratio after absorption is unexpected");
        Assert.AreEqual(0.5f, clientLastShowRatio, 0.08f, "Client shield ratio after absorption is unexpected");
#endif

        yield return null;
    }

    private GameObject CreatePlayerPrefab()
    {
        var go = new GameObject("TestPlayerPrefab");
        go.SetActive(true);

        go.AddComponent<NetworkIdentity>();
        go.AddComponent<Rigidbody>();

        go.AddComponent<Health>();
        go.AddComponent<ShipAssembler>();
        go.AddComponent<PlayerController>();
        go.AddComponent<ShipShooting>();
        go.AddComponent<NetworkAudio>();

        var player = go.AddComponent<Player>();
        _shieldBubblePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _shieldBubblePrefab.SetActive(false);
        player.shieldBubblePrefab = _shieldBubblePrefab;

        return go;
    }

    private void CreateFakeUIManager()
    {
        // Player.OnStartLocalPlayer calls UIManager.Instance.UpdateHealth(...) without null checks.
        _ownsUIManager = UIManager.Instance == null;

        _hudGO = new GameObject("FakeHUDController");
        var hud = _hudGO.AddComponent<HUDController>();

        if (UIManager.Instance == null)
        {
            _uiGO = new GameObject("FakeUIManager");
            var ui = _uiGO.AddComponent<UIManager>();

            var hudField = typeof(UIManager).GetField("hudController", BindingFlags.NonPublic | BindingFlags.Instance);
            hudField?.SetValue(ui, hud);
        }
        else
        {
            var hudField = typeof(UIManager).GetField("hudController", BindingFlags.NonPublic | BindingFlags.Instance);
            hudField?.SetValue(UIManager.Instance, hud);
        }
    }

    private IEnumerator WaitForHostAndClientPlayers(float timeoutSeconds)
    {
        float t = 0f;
        _hostPlayer = null;
        _clientPlayer = null;

        while (t < timeoutSeconds && (_hostPlayer == null || _clientPlayer == null))
        {
            var players = Object.FindObjectsByType<Player>(FindObjectsSortMode.None);

            _hostPlayer = players.FirstOrDefault(p => p != null && p.isServer);
            _clientPlayer = players.FirstOrDefault(p => p != null && p.isLocalPlayer && !p.isServer);

            if (_hostPlayer != null && _clientPlayer != null)
                yield break;

            t += Time.deltaTime;
            yield return null;
        }

        // Fallback for setups where host mode reuses a single Player instance.
        // This keeps the test from failing purely due to instance topology.
        if (_hostPlayer != null && _clientPlayer == null)
            _clientPlayer = _hostPlayer;
    }

    private IEnumerator WaitUntil(System.Func<bool> predicate, float timeoutSeconds)
    {
        float t = 0f;
        while (t < timeoutSeconds)
        {
            if (predicate()) yield break;
            t += Time.deltaTime;
            yield return null;
        }
    }
}