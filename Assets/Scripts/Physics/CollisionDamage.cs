using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CollisionDamage : NetworkBehaviour
{
    [Header("Enable/Disable")]
    [SerializeField] private bool damageSelf = true;
    [SerializeField] private bool damageOther = true;

    [Header("Collision Filtering")]
    [SerializeField] private LayerMask damageLayers = ~0;
    [SerializeField] private bool requireOtherRigidbody = true;
    [SerializeField] private bool ignoreIfNoHealthOnOther = true;

    [Header("Damage Model")]
    [SerializeField] private float energyToDamage = 0.02f;
    [SerializeField] private float minRelativeSpeed = 2.0f;
    [SerializeField] private float maxRelativeSpeed = 25.0f;

    [SerializeField] private float massProtection = 0.02f;

    [Header("Spam Protection")]
    [SerializeField] private float perTargetCooldown = 0.15f;

    [Header("Impulse Scaling")]
    [SerializeField] private bool useImpulseScale = true;
    [SerializeField] private float impulseForFullDamage = 50f;

    [Header("Context Labels")]
    [SerializeField] private string collisionWeaponId = "Collision";
    [SerializeField] private string environmentName = "Environment";
    [SerializeField] private string asteroidWeaponId = "Asteroid";

    private Rigidbody _rb;
    private readonly System.Collections.Generic.Dictionary<uint, double> _lastHitTimeByNetId = new();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        ApplyCollisionDamage(collision);
    }

    [Server]
    private void ApplyCollisionDamage(Collision collision)
    {
        int otherLayer = collision.gameObject.layer;
        if (((1 << otherLayer) & damageLayers.value) == 0) return;

        Rigidbody otherRb = collision.rigidbody;
        if (requireOtherRigidbody && otherRb == null) return;

        Health otherHealth = collision.gameObject.GetComponentInParent<Health>();
        if (ignoreIfNoHealthOnOther && otherHealth == null) return;

        Health selfHealth = GetComponent<Health>();

        uint otherNetId = 0;
        var otherIdentity = collision.gameObject.GetComponentInParent<NetworkIdentity>();
        if (otherIdentity != null) otherNetId = otherIdentity.netId;

        double now = NetworkTime.time;
        if (otherNetId != 0)
        {
            if (_lastHitTimeByNetId.TryGetValue(otherNetId, out double last) && (now - last) < perTargetCooldown)
                return;
            _lastHitTimeByNetId[otherNetId] = now;
        }

        float vRel = collision.relativeVelocity.magnitude;
        if (vRel < minRelativeSpeed) return;

        float vClamped = Mathf.Min(vRel, maxRelativeSpeed);

        float m1 = Mathf.Max(0.01f, _rb.mass);
        float m2 = otherRb != null ? Mathf.Max(0.01f, otherRb.mass) : m1;

        float reducedMass = (m1 * m2) / (m1 + m2);
        float energy = 0.5f * reducedMass * vClamped * vClamped;
        float rawDamage = energy * energyToDamage;

        float selfTakenMult = 1f / (1f + massProtection * m1);
        float otherTakenMult = 1f / (1f + massProtection * m2);

        float selfDamage = rawDamage * selfTakenMult;
        float otherDamage = rawDamage * otherTakenMult;

        if (useImpulseScale)
        {
            float impulse = collision.impulse.magnitude;
            float k = Mathf.Clamp01(impulse / Mathf.Max(0.0001f, impulseForFullDamage));
            selfDamage *= k;
            otherDamage *= k;
        }

        DamageContext selfCtx = BuildContextForVictim(victim: gameObject, other: collision.gameObject);
        DamageContext otherCtx = BuildContextForVictim(victim: collision.gameObject, other: gameObject);

        if (damageOther && otherHealth != null && otherDamage > 0.01f)
            otherHealth.TakeDamage(otherDamage, otherCtx);

        if (damageSelf && selfHealth != null && selfDamage > 0.01f)
            selfHealth.TakeDamage(selfDamage, selfCtx);
    }

    [Server]
    private DamageContext BuildContextForVictim(GameObject victim, GameObject other)
    {
        if (TryGetPlayerInfo(other, out uint attackerId, out string attackerNick))
        {
            return DamageContext.Collision(attackerId, attackerNick, collisionWeaponId);
        }

        if (other.GetComponentInParent<Asteroid>() != null)
        {
            return DamageContext.Collision(0, environmentName, asteroidWeaponId);
        }

        return DamageContext.Collision(0, environmentName, collisionWeaponId);
    }

    [Server]
    private bool TryGetPlayerInfo(GameObject go, out uint attackerNetId, out string nickname)
    {
        attackerNetId = 0;
        nickname = "Player";

        var p = go.GetComponentInParent<Player>();
        if (p == null) return false;

        attackerNetId = p.netId;
        nickname = string.IsNullOrEmpty(p.Nickname) ? "Player" : p.Nickname;
        return true;
    }
}
