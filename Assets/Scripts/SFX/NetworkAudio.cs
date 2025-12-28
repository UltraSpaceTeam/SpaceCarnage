using Mirror;
using UnityEngine;

public class NetworkAudio : NetworkBehaviour
{
    public void PlaySound(SoundType type, Vector3 position)
    {
        AudioManager.Instance.PlayOneShot(type, position);

        if (NetworkClient.active)
        {
            CmdPlaySound(type, position);
        }
    }

    [Command]
    private void CmdPlaySound(SoundType type, Vector3 position)
    {
        RpcPlaySound(type, position);
    }

    [ClientRpc(includeOwner = false)]
    private void RpcPlaySound(SoundType type, Vector3 position)
    {
        AudioManager.Instance.PlayOneShot(type, position);
    }
}