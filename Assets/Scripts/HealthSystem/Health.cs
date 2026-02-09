using System.Collections;
using Mirror;
using UnityEngine;

public class Health : NetworkBehaviour, IDieable
{
    [Header("Health info(readonly)")]
    [SyncVar(hook = nameof(OnMaxHealthChanged))]
    [SerializeField] private float maxHealth = 100f;
    [SyncVar(hook = nameof(OnHealthChanged))] 
    private float currentHealth;
    [Header("Audio Settings")]
    [SerializeField] private SoundType deathSound = SoundType.None;

    [SyncVar]
    private bool isDead = false;
    [SyncVar]
    private bool isInvincible = false;

    // Реализация свойства из интерфейса
    public bool IsDead => isDead;
    
    public event System.Action<DamageContext> OnDeath;
    // Событие для UI (опционально)
    public event System.Action<float, float> OnHealthUpdate;

    [Server]
    public void SetMaxHealth(float value)
    {
        maxHealth = value;
        currentHealth = value;
    }

    private void Start()
    {
        if (isServer)
        {
            currentHealth = maxHealth;
        }
    }

    [Server]
    public void TakeDamage(float damage, DamageContext source)
    {
        if (isDead || isInvincible) return;

        var assembler = GetComponent<ShipAssembler>();
        var ability = assembler?.CurrentEngine?.ability;

        // If ability is shield — absorb
        if (TryGetComponent<PlayerController>(out var selfController))
        {
            damage = selfController.ServerAbsorbDamage(damage);
        }

        if (damage <= 0.01f) return;
        selfController?.ServerNotifyDamaged();


        if (source.Type == DamageType.Weapon && source.AttackerId != 0)
        {
            if (Player.ActivePlayers.TryGetValue(source.AttackerId, out Player attackerPlayer))
            {
                source = DamageContext.Weapon(source.AttackerId, attackerPlayer.Nickname, source.WeaponID);
                var attackerAssembler = attackerPlayer.GetComponent<ShipAssembler>();
                var attackerAbility = attackerAssembler?.CurrentEngine?.ability;
                var attackerInvis = attackerAbility as InvisAbility;

                if (attackerPlayer.TryGetComponent<PlayerController>(out var attackerController))
                {
                    attackerController.ServerNotifyAttacked();
                    Debug.Log($"{attackerPlayer.Nickname} broke invisibility by attacking {gameObject.name}");
                }
            }
        }

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} Took {damage} damage, current health: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die(source);
        }
    }

    [Server]
    public void Die(DamageContext source)
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

        if (NetworkAudioManager.Instance != null)
        {
            NetworkAudioManager.Instance.PlaySoundOnAllClients(deathSound, transform.position);
        }
        else
        {
            Debug.LogWarning("NetworkAudioManager instance is null, cannot play death sound.");
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
        //мб удалить
        // foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
        // foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = true;
    }


    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        // Обновление UI здоровья
        OnHealthUpdate?.Invoke(newHealth, maxHealth);
    }
    private void OnMaxHealthChanged(float oldMax, float newMax)
    {
        OnHealthUpdate?.Invoke(currentHealth, newMax);
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
}
