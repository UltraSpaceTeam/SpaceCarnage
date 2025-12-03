using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(ShipAssembler))]
public class ShipShooting : NetworkBehaviour
{
    private ShipAssembler _assembler;
    private float _lastFireTime = 0f;
    private Transform _muzzlePoint;

    private int _currentAmmo;
    private bool _isReloading = false;
    private WeaponData _cachedWeapon;

    [Header("Components")]
    [SerializeField] private LineRenderer laserBeamRenderer;

    public WeaponData CurrentWeaponData => _assembler.CurrentWeapon;

    void Awake()
    {
        _assembler = GetComponent<ShipAssembler>();

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

                CmdFire(_muzzlePoint.position, _muzzlePoint.rotation);
            }
        }
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
            _muzzlePoint = _assembler.CurrentWeaponObject.transform.Find("Muzzle");
        if (_muzzlePoint == null) _muzzlePoint = transform;
    }

    [Command]
    private void CmdFire(Vector3 pos, Quaternion rot)
    {
        if (CurrentWeaponData != null && CurrentWeaponData.strategy != null)
        {
            CurrentWeaponData.strategy.Fire(this, pos, rot);
            RpcMuzzleFlash(pos, rot);
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