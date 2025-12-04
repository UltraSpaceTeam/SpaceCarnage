using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(NetworkIdentity))]
public class NetworkEffectDestroyer : NetworkBehaviour
{
    public override void OnStartServer()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        var main = ps.main;

        float maxLifetime = main.startLifetime.constantMax;
        float duration = main.duration;

        float totalTime = duration + maxLifetime + 0.2f;

        StartCoroutine(ServerDestroy(totalTime));
    }

    [Server]
    private IEnumerator ServerDestroy(float time)
    {
        yield return new WaitForSeconds(time);

        NetworkServer.Destroy(gameObject);
    }
}