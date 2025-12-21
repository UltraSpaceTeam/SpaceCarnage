using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AsteroidCollisionDamage : NetworkBehaviour
{
    [SerializeField] private int baseDamage = 20;
	[SerializeField] private float damageMultiplier = 5.0f;
	[SerializeField] private string playerTag = "Player";
	[SerializeField] private float minSpeedForDamage = 2.0f;
	[SerializeField] private float maxSpeedForFullDamage = 15.0f;
	
    
    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
		if (collision.gameObject.CompareTag(playerTag))
        {
			if (collision.gameObject.TryGetComponent<Rigidbody>(out var rb)) {
				if (collision.gameObject.TryGetComponent<Health>(out var health))
				{
					float speedFactor = CalculateSpeedFactor(rb.linearVelocity.magnitude);
					int damage = CalculateFinalDamage(speedFactor);
					
					health.TakeDamage(damage, DamageContext.Environment("Asteroid"));
				}
			}
		}
    }
	
    private float CalculateSpeedFactor(float currentSpeed)
    {
        return Mathf.Clamp01((currentSpeed - minSpeedForDamage) / 
                            (maxSpeedForFullDamage - minSpeedForDamage));
    }
    
    private int CalculateFinalDamage(float speedFactor)
    {
        float scaledDamage = baseDamage * (1f + speedFactor * damageMultiplier);
        return Mathf.RoundToInt(scaledDamage);
    }
	
	
}