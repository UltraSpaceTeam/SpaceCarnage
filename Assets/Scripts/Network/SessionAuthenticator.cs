using Mirror;
using UnityEngine;
using System.Collections;

public class SessionAuthenticator : NetworkAuthenticator
{
    public struct AuthMessage : NetworkMessage
    {
        public string clientKey;
    }

    private void Awake()
    {
        base.OnServerAuthenticated.AddListener(OnAuthSuccess);
    }

    private void OnAuthSuccess(NetworkConnectionToClient conn)
    {
        StartCoroutine(DelayedSpawn(conn));
    }

    private IEnumerator DelayedSpawn(NetworkConnectionToClient conn)
    {
        yield return new WaitForSeconds(1.2f);

        if (conn != null)
        {
            Debug.Log($"[Auth] Spawning player for connection {conn.connectionId}");
            NetworkManager.singleton.OnServerAddPlayer(conn);
        }
    }

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<AuthMessage>(OnAuthRequestMessage, false);
    }

    public override void OnStopServer()
    {
        NetworkServer.UnregisterHandler<AuthMessage>();
    }

    public override void OnServerAuthenticate(NetworkConnectionToClient conn)
    {
        // ∆дем сообщени€...
    }

    public void OnAuthRequestMessage(NetworkConnectionToClient conn, AuthMessage msg)
    {
        string expectedKey = (GameData.Instance != null) ? GameData.Instance.SessionKey : "";

        Debug.Log($"[Auth] Client sent: {msg.clientKey}. Expected: {expectedKey}");

        if (string.IsNullOrEmpty(expectedKey))
        {
            Debug.LogWarning("[Auth] Server has no key (Debug Mode). Allowing connection.");
            ServerAccept(conn);
            return;
        }

        if (msg.clientKey == expectedKey)
        {
            Debug.Log($"[Auth] Connection {conn.connectionId} Authenticated!");
            ServerAccept(conn);
        }
        else
        {
            Debug.LogError($"[Auth] Connection {conn.connectionId} Rejected! Wrong key.");
            ServerReject(conn);
        }
    }

    public override void OnClientAuthenticate()
    {
        string myKey = "";

        if (GameData.Instance != null && !string.IsNullOrEmpty(GameData.Instance.SessionKey))
        {
            myKey = GameData.Instance.SessionKey;
        }
        else
        {
            Debug.LogWarning("[Auth] No SessionKey found in GameData! Sending empty key.");
        }

        AuthMessage msg = new AuthMessage { clientKey = myKey };
        NetworkClient.Send(msg);
    }

    public override void OnStartClient() { }
}