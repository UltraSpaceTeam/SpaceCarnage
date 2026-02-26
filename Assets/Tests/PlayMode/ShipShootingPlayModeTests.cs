using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mirror;
using Object = UnityEngine.Object;

public class ShipShootingPlayModeTests
{
    private GameObject _go;
    private ShipShooting _shooting;
    private ShipAssembler _assembler;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        _go = new GameObject("Ship");
        _go.SetActive(false);

        _go.AddComponent<NetworkIdentity>();
        _assembler = _go.AddComponent<ShipAssembler>();
        _go.AddComponent<Player>();

        _shooting = _go.AddComponent<ShipShooting>();

        SetField(_shooting, "_assembler", _assembler);

        _go.SetActive(true);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.DestroyImmediate(_go);
        yield return null;
    }

    private void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {obj.GetType().Name}");
        f.SetValue(obj, value);
    }

    private T GetField<T>(object obj, string name)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {obj.GetType().Name}");
        return (T)f.GetValue(obj);
    }

    private void InvokeMethod(object obj, string name, params object[] args)
    {
        var m = obj.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{name}' not found on {obj.GetType().Name}");
        m.Invoke(obj, args);
    }

    private T InvokeMethod<T>(object obj, string name, params object[] args)
    {
        var m = obj.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{name}' not found on {obj.GetType().Name}");
        return (T)m.Invoke(obj, args);
    }

    private WeaponData MakeWeaponData(int ammo = 10, float fireRate = 5f, float reload = 1f)
    {
        var data = ScriptableObject.CreateInstance<WeaponData>();
        SetField(data, "ammo", ammo);
        SetField(data, "fireRate", fireRate);
        SetField(data, "reload", reload);
        return data;
    }

    private void SetCurrentWeapon(WeaponData data)
    {
        var prop = typeof(ShipAssembler).GetProperty("CurrentWeapon",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(prop, "Property 'CurrentWeapon' not found on ShipAssembler");
        prop.SetValue(_assembler, data);
    }

    private void SetCurrentWeaponObject(GameObject go)
    {
        var prop = typeof(ShipAssembler).GetProperty("CurrentWeaponObject",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(prop, "Property 'CurrentWeaponObject' not found on ShipAssembler");
        prop.SetValue(_assembler, go);
    }

    [UnityTest]
    public IEnumerator ShooterName_WithoutPlayer_ReturnsUnknown()
    {
        SetField(_shooting, "_player", null);
        yield return null;

        Assert.AreEqual("Unknown", _shooting.ShooterName,
            "ShooterName без Player должен возвращать 'Unknown'");
    }

    [UnityTest]
    public IEnumerator CurrentAmmo_ReflectsInternalField()
    {
        SetField(_shooting, "_currentAmmo", 7);
        yield return null;

        Assert.AreEqual(7, _shooting.CurrentAmmo, "CurrentAmmo должен отражать _currentAmmo");
    }

    [UnityTest]
    public IEnumerator IsReloading_ReflectsInternalField()
    {
        SetField(_shooting, "_isReloading", true);
        yield return null;

        Assert.IsTrue(_shooting.IsReloading, "IsReloading должен отражать _isReloading");
    }

    [UnityTest]
    public IEnumerator IsReloading_InitiallyFalse()
    {
        yield return null;
        Assert.IsFalse(_shooting.IsReloading, "IsReloading должен быть false при старте");
    }

    [UnityTest]
    public IEnumerator CurrentAmmo_InitiallyZero()
    {
        yield return null;
        Assert.AreEqual(0, _shooting.CurrentAmmo, "CurrentAmmo должен быть 0 до ResetWeaponState");
    }

    [UnityTest]
    public IEnumerator ResetWeaponState_SetsAmmoFromWeaponData()
    {
        var data = MakeWeaponData(ammo: 15);
        SetCurrentWeapon(data);

        InvokeMethod(_shooting, "ResetWeaponState");
        yield return null;

        Assert.AreEqual(15, _shooting.CurrentAmmo, "ResetWeaponState должен установить ammo из WeaponData");

        Object.DestroyImmediate(data);
    }

    [UnityTest]
    public IEnumerator ResetWeaponState_SetsIsReloadingFalse()
    {
        var data = MakeWeaponData();
        SetCurrentWeapon(data);
        SetField(_shooting, "_isReloading", true);

        InvokeMethod(_shooting, "ResetWeaponState");
        yield return null;

        Assert.IsFalse(_shooting.IsReloading, "ResetWeaponState должен сбросить _isReloading в false");

        Object.DestroyImmediate(data);
    }

    [UnityTest]
    public IEnumerator ResetWeaponState_CachesCachedWeapon()
    {
        var data = MakeWeaponData();
        SetCurrentWeapon(data);

        InvokeMethod(_shooting, "ResetWeaponState");
        yield return null;

        var cached = GetField<WeaponData>(_shooting, "_cachedWeapon");
        Assert.AreSame(data, cached, "_cachedWeapon должен совпадать с CurrentWeaponData после ResetWeaponState");

        Object.DestroyImmediate(data);
    }

    [UnityTest]
    public IEnumerator ResetWeaponState_WithNullWeapon_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => InvokeMethod(_shooting, "ResetWeaponState"),
            "ResetWeaponState с null WeaponData не должен бросать исключений");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReloadCoroutine_SetsIsReloadingTrue_DuringReload()
    {
        var data = MakeWeaponData(ammo: 5, reload: 0.1f);
        SetCurrentWeapon(data);
        SetField(_shooting, "_currentAmmo", 5);

        _shooting.StartCoroutine(InvokeCoroutine("ReloadCoroutine"));
        yield return null;

        Assert.IsTrue(_shooting.IsReloading, "¬о врем€ перезар€дки IsReloading должен быть true");

        Object.DestroyImmediate(data);
    }

    [UnityTest]
    public IEnumerator ReloadCoroutine_RestoresAmmo_AfterDelay()
    {
        var data = MakeWeaponData(ammo: 8, reload: 0.05f);
        SetCurrentWeapon(data);
        SetField(_shooting, "_currentAmmo", 0);

        _shooting.StartCoroutine(InvokeCoroutine("ReloadCoroutine"));

        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual(8, _shooting.CurrentAmmo, "ѕосле перезар€дки ammo должен восстановитьс€");
        Assert.IsFalse(_shooting.IsReloading, "ѕосле перезар€дки IsReloading должен быть false");

        Object.DestroyImmediate(data);
    }

    private IEnumerator InvokeCoroutine(string name)
    {
        var m = _shooting.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Coroutine '{name}' not found");
        return (IEnumerator)m.Invoke(_shooting, null);
    }

    [UnityTest]
    public IEnumerator RefreshMuzzlePoint_WithNoWeaponObject_FallsBackToTransform()
    {
        SetCurrentWeaponObject(null);

        InvokeMethod(_shooting, "RefreshMuzzlePoint");
        yield return null;

        var muzzle = GetField<Transform>(_shooting, "_muzzlePoint");
        Assert.AreSame(_shooting.transform, muzzle,
            "Ѕез WeaponObject _muzzlePoint должен быть transform самого корабл€");
    }

    [UnityTest]
    public IEnumerator RefreshMuzzlePoint_FindsMuzzleChild()
    {
        var weaponGO = new GameObject("Weapon");
        var muzzleGO = new GameObject("Muzzle");
        muzzleGO.transform.SetParent(weaponGO.transform);

        SetCurrentWeaponObject(weaponGO);

        InvokeMethod(_shooting, "RefreshMuzzlePoint");
        yield return null;

        var muzzle = GetField<Transform>(_shooting, "_muzzlePoint");
        Assert.AreSame(muzzleGO.transform, muzzle,
            "RefreshMuzzlePoint должен найти дочерний объект 'Muzzle'");

        Object.DestroyImmediate(weaponGO);
    }

    [UnityTest]
    public IEnumerator RefreshMuzzlePoint_NoMuzzleChild_FallsBackToTransform()
    {
        var weaponGO = new GameObject("WeaponNoMuzzle");
        SetCurrentWeaponObject(weaponGO);

        InvokeMethod(_shooting, "RefreshMuzzlePoint");
        yield return null;

        var muzzle = GetField<Transform>(_shooting, "_muzzlePoint");
        Assert.AreSame(_shooting.transform, muzzle,
            "Ѕез дочернего 'Muzzle' должен использоватьс€ transform корабл€");

        Object.DestroyImmediate(weaponGO);
    }

    [UnityTest]
    public IEnumerator GetTargetPoint_NoHits_ReturnsRayEndPoint()
    {
        var muzzleGO = new GameObject("Muzzle");
        muzzleGO.transform.SetParent(_go.transform);
        muzzleGO.transform.localPosition = Vector3.zero;
        SetField(_shooting, "_muzzlePoint", muzzleGO.transform);

        var ray = new Ray(Vector3.zero, Vector3.forward);
        Vector3 result = InvokeMethod<Vector3>(_shooting, "GetTargetPoint", ray);
        yield return null;

        Vector3 expected = ray.GetPoint(1000f);
        Assert.AreEqual(expected, result,
            "Ѕез попаданий GetTargetPoint должен вернуть конец луча на DEFAULT_AIM_DISTANCE");

        Object.DestroyImmediate(muzzleGO);
    }

    [UnityTest]
    public IEnumerator Awake_LaserBeamRenderer_DisabledByDefault()
    {
        var lr = _go.GetComponent<LineRenderer>();
        if (lr != null)
        {
            Assert.IsFalse(lr.enabled, "laserBeamRenderer должен быть отключЄн при старте");
            Assert.IsTrue(lr.useWorldSpace, "laserBeamRenderer должен использовать world space");
        }
        else
        {
            Assert.Pass("LineRenderer не добавлен Ч тест не применим");
        }
        yield return null;
    }
}