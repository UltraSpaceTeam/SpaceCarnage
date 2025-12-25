using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class VFXAutoDestroy : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(CheckIfAlive());
    }

    private IEnumerator CheckIfAlive()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();

        yield return null;

        while (ps != null && ps.IsAlive(true))
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}