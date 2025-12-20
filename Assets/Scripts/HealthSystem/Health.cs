using System.Collections;
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
    [SyncVar]
    private bool isInvincible = false;

    // Реализация свойства из интерфейса
    public bool IsDead => isDead;
    
    public event System.Action<string> OnDeath;
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
    public void TakeDamage(float damage, string source="unknown")
    {
        if (isDead || isInvincible) return;
        
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} Took {damage} damage, current health: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die(source);
        }
    }
    
    [Server]
    public void Die(string source = "unknown")
    {
        if (isDead) return;
        Debug.Log(gameObject.name + " died due to " + source);
        isDead = true;

        if (OnDeath == null)
        {
        }
        else
        {
            OnDeath.Invoke(source);
        }
        
        if (TryGetComponent<Player>(out Player playerScript))
        {
            return;
        }

        NetworkServer.Destroy(gameObject);

    }

    [Server]
    public void Ressurect()
    {
        isDead = false;
        currentHealth = maxHealth;
        RpcResurrect();
    }
    [Server]
    public void SetInvincibility(bool value)
    {
        isInvincible = value;
    }

    [ClientRpc]
    private void RpcResurrect()
    {
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
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
