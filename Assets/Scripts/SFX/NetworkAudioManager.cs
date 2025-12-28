using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkAudio))]
public class NetworkAudioManager : NetworkBehaviour
{
    public static NetworkAudioManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Server]
    public void PlaySoundOnAllClients(SoundType type, Vector3 position)
    {
        RpcPlaySound(type, position);
    }

    [ClientRpc]
    private void RpcPlaySound(SoundType type, Vector3 position)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayOneShot(type, position);
        }
    }
}