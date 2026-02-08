using Mirror;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
public class Asteroid : NetworkBehaviour
{
    private Health _health;
    private Rigidbody _rb;
    [SerializeField] private float baseHP = 50f;
    [SerializeField] private float hpPower = 2.0f;
    [SerializeField] private float baseMass = 5f;
    [SerializeField] private float massPower = 3.0f;
    [SerializeField] private float baseScale = 100f;
    [SerializeField] private GameObject HitVFX;

    [SyncVar(hook = nameof(OnSizeChanged))]
    private float _size = 1f;

    public float Size => _size;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _health.OnDeath += OnDie;
        ApplySizeServer(_size * baseScale);
    }
    [Server]
    public void SetSize(float size)
    {
        _size = Mathf.Clamp(size, 0.3f, 5f);
        ApplySizeServer(_size * baseScale);
    }


    [Server]
    private void ApplySizeServer(float size)
    {
        transform.localScale = Vector3.one * size;

        float maxHp = baseHP * Mathf.Pow(size/baseScale, hpPower);
        _health.SetMaxHealth(maxHp);

        if (_rb != null)
            _rb.mass = baseMass * Mathf.Pow(size/baseScale, massPower);
    }

    private void OnSizeChanged(float oldSize, float newSize)
    {
        transform.localScale = Vector3.one * newSize * baseScale;
    }
    private void OnDie(DamageContext ctx)
    {
        GameObject vfx = Instantiate(HitVFX, transform.position, transform.rotation);
        NetworkServer.Spawn(vfx);
    }
}
