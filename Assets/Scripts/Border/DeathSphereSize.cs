using UnityEngine;

public class DeathSphere : MonoBehaviour
{	
    void Awake()
    {
        float diameter = BorderConfiguration.borderRadius * 2f;
        transform.localScale = new Vector3(diameter, diameter, diameter);
    }
}
