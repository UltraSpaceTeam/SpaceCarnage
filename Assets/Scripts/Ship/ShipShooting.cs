using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(ShipAssembler))]
public class ShipShooting : NetworkBehaviour
{
    private ShipAssembler _assembler;
    private Player _player;
    private float _lastFireTime = 0f;
    private Transform _muzzlePoint;

    private int _currentAmmo;
    private bool _isReloading = false;
    private WeaponData _cachedWeapon;

    [Header("Components")]
    [SerializeField] private LineRenderer laserBeamRenderer;

    private const float DEFAULT_AIM_DISTANCE = 1000f;

    public WeaponData CurrentWeaponData => _assembler.CurrentWeapon;

    public string ShooterName => _player != null ? _player.Nickname : "Unknown";

    void Awake()
    {
        _assembler = GetComponent<ShipAssembler>();
        _player = GetComponent<Player>();
        if (laserBeamRenderer == null)
            laserBeamRenderer = GetComponent<LineRenderer>();

        if (laserBeamRenderer != null)
        {
            laserBeamRenderer.enabled = false;
            laserBeamRenderer.useWorldSpace = true;
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (_assembler.CurrentWeapon == null) return;

        if (_cachedWeapon != CurrentWeaponData) ResetWeaponState();
        if (_isReloading) return;

        if (_currentAmmo <= 0)
        {
            StartCoroutine(ReloadCoroutine());
            return;
        }

        if (Input.GetKeyDown(KeyCode.R) && _currentAmmo < CurrentWeaponData.ammo)
        {
            StartCoroutine(ReloadCoroutine());
            return;
        }

        if (Input.GetKey(KeyCode.Mouse0))
        {
            float rate = CurrentWeaponData.fireRate > 0 ? CurrentWeaponData.fireRate : 1f;

            if (Time.time >= _lastFireTime + (1f / rate))
            {
                if (_muzzlePoint == null) RefreshMuzzlePoint();

                _lastFireTime = Time.time;
                _currentAmmo--;

                Quaternion aimRotation = GetAimRotation();

                CmdFire(_muzzlePoint.position, aimRotation);
            }
        }
    }

    private Quaternion GetAimRotation()
    {
        if (Camera.main == null) return _muzzlePoint.rotation;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = GetTargetPoint(ray);

        Vector3 aimDirection = (targetPoint - _muzzlePoint.position).normalized;
        return Quaternion.LookRotation(aimDirection);
    }

    private Vector3 GetTargetPoint(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, DEFAULT_AIM_DISTANCE);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.transform.root == transform) continue;

            Vector3 dirToHit = hit.point - _muzzlePoint.position;
            if (Vector3.Dot(_muzzlePoint.forward, dirToHit.normalized) > 0)
            {
                return hit.point;
            }
        }
        return ray.GetPoint(DEFAULT_AIM_DISTANCE);
    }

    private void ResetWeaponState()
    {
        _cachedWeapon = CurrentWeaponData;
        _isReloading = false;
        StopAllCoroutines();
        if (CurrentWeaponData != null)
        {
            _currentAmmo = CurrentWeaponData.ammo;
            RefreshMuzzlePoint();
        }
    }

    private IEnumerator ReloadCoroutine()
    {
        _isReloading = true;
        yield return new WaitForSeconds(CurrentWeaponData.reload);
        if (CurrentWeaponData != null) _currentAmmo = CurrentWeaponData.ammo;
        _isReloading = false;
    }

    public void RefreshMuzzlePoint()
    {
        if (_assembler.CurrentWeaponObject != null)
        {
            _muzzlePoint = _assembler.CurrentWeaponObject.transform.Find("Muzzle");

            if (_muzzlePoint == null)
            {
                var allTransforms = _assembler.CurrentWeaponObject.GetComponentsInChildren<Transform>();
                foreach (var t in allTransforms)
                {
                    if (t.name == "Muzzle")
                    {
                        _muzzlePoint = t;
                        break;
                    }
                }
            }
        }

        if (_muzzlePoint != null)
        {
            Debug.Log($"Muzzle found on {_muzzlePoint.parent.name}");
        }
        else
        {
            _muzzlePoint = transform;
        }
    }

    [Command]
    private void CmdFire(Vector3 pos, Quaternion rot)
    {
        if (CurrentWeaponData != null && CurrentWeaponData.strategy != null)
        {
            CurrentWeaponData.strategy.Fire(this, pos, rot);
            RpcMuzzleFlash(pos, rot);
        }

        // —брос инвиза при атаке (если ability есть)
        var assembler = GetComponent<ShipAssembler>();
        var ability = assembler?.CurrentEngine?.ability;
        var invisAbility = ability as InvisAbility;
        if (invisAbility != null && invisAbility.breakOnAttack)
        {
            invisAbility.BreakInvisibility();
        }
    }

    [ClientRpc]
    private void RpcMuzzleFlash(Vector3 pos, Quaternion rot)
    {
        if (CurrentWeaponData.muzzleFlashVFX != null)
            Instantiate(CurrentWeaponData.muzzleFlashVFX, pos, rot);
    }

    [ClientRpc]
    public void RpcSpawnHitEffect(Vector3 pos, Quaternion rot)
    {
        if (CurrentWeaponData.hitVFX != null)
            Instantiate(CurrentWeaponData.hitVFX, pos, rot);
    }

    [ClientRpc]
    public void RpcSpawnBeam(Vector3 start, Vector3 end)
    {
        if (laserBeamRenderer != null)
        {
            StopCoroutine(nameof(ShowBeamCoroutine));
            StartCoroutine(ShowBeamCoroutine(end));
        }
    }

    private IEnumerator ShowBeamCoroutine(Vector3 endPointWorld)
    {
        if (_muzzlePoint == null) RefreshMuzzlePoint();

        laserBeamRenderer.enabled = true;

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {

            if (_muzzlePoint != null)
                laserBeamRenderer.SetPosition(0, _muzzlePoint.position);
            else
                laserBeamRenderer.SetPosition(0, transform.position);

            laserBeamRenderer.SetPosition(1, endPointWorld);

            elapsed += Time.deltaTime;
            yield return null;
        }

        laserBeamRenderer.enabled = false;
    }
}