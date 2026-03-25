using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkTestUtils
{
    public Player HostPlayer { get; private set; }      // Хост (Client A)
    public Player RemoteClientPlayer { get; private set; } // Второй клиент (Client B)

    private NetworkManager _nm;

    public IEnumerator StartHostWithOneClient()
    {
        Debug.Log("[NetworkTestUtils] Starting Host + 1 Client...");

        _nm = Object.FindAnyObjectByType<NetworkManager>();
        if (_nm == null)
        {
            Debug.LogError("NetworkManager not found in scene!");
            yield break;
        }

        // Запускаем хост
        _nm.StartHost();
        yield return new WaitForSeconds(0.8f);

        // Подключаем один клиент
        _nm.StartClient();
        yield return new WaitForSeconds(1.8f);

        // Ждём появления двух игроков
        yield return WaitForTwoPlayers(4.0f);

        Debug.Log($"[NetworkTestUtils] Found ? Host: {HostPlayer?.netId} | RemoteClient: {RemoteClientPlayer?.netId}");
    }

    private IEnumerator WaitForTwoPlayers(float timeout)
    {
        float time = 0f;
        while (time < timeout)
        {
            var players = Object.FindObjectsByType<Player>(FindObjectsSortMode.None);

            HostPlayer = players.FirstOrDefault(p => p.isServer);
            RemoteClientPlayer = players.FirstOrDefault(p => p.isLocalPlayer && !p.isServer);

            if (HostPlayer != null && RemoteClientPlayer != null)
                yield break;

            time += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning("[NetworkTestUtils] Failed to find 2 players in time!");
    }

    public IEnumerator EquipEngineWithShield(Player player)
    {
        if (player == null) yield break;

        var assembler = player.GetComponent<ShipAssembler>();
        if (assembler == null) yield break;

        var shieldEngine = GameResources.Instance?.partDatabase.engines
            .FirstOrDefault(e => e.ability is ShieldAbility);

        if (shieldEngine != null)
        {
            assembler.EquipEngine(shieldEngine);
            Debug.Log($"[Test] Equipped shield engine on player {player.netId}");
        }

        yield return new WaitForSeconds(0.4f);
    }

    public void Cleanup()
    {
        if (_nm != null)
            _nm.StopHost();
    }
}