using Mirror;
using UnityEngine;

public class Health : NetworkBehaviour, IDieable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SyncVar(hook = nameof(OnHealthChanged))] 
    private float currentHealth;
    
    [SyncVar]
    private bool isDead = false;
    
    // Реализация свойства из интерфейса
    public bool IsDead => isDead;
    
    // Событие для UI (опционально)
    public event System.Action<float, float> OnHealthUpdate;
    
    private void Start()
    {
        if (isServer)
        {
            currentHealth = maxHealth;
        }
    }
    
    [Server]
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }
    
    [Server]
    public void Die()
    {
        if (isDead) return;
        
        isDead = true;
        RpcDie();
        
    }
    
    [ClientRpc]
    private void RpcDie()
    {

    }
    
    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        // Обновление UI здоровья
        OnHealthUpdate?.Invoke(newHealth, maxHealth);
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
}
