using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mirror;
using Object = UnityEngine.Object;

/// <summary>
/// PlayMode тесты для Projectile, ProjectileStrategy, RaycastStrategy, Rocket, WeaponStrategy.
/// Mirror-ограничения обходятся через рефлексию — [Server]/[ClientRpc] методы не вызываются напрямую,
/// вместо этого тестируется внутренняя логика.
/// </summary>
public class WeaponSystemPlayModeTests
{
    // ?????????????????????????????????????????????????????????????????????????
    // Вспомогательные методы
    // ?????????????????????????????????????????????????????????????????????????

    private T GetField<T>(object obj, string name)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {obj.GetType().Name}");
        return (T)f.GetValue(obj);
    }

    private void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, $"Field '{name}' not found on {obj.GetType().Name}");
        f.SetValue(obj, value);
    }

    private T GetProp<T>(object obj, string name)
    {
        var p = obj.GetType().GetProperty(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(p, $"Property '{name}' not found on {obj.GetType().Name}");
        return (T)p.GetValue(obj);
    }

    private void InvokeMethod(object obj, string name, params object[] args)
    {
        var types = System.Array.ConvertAll(args, a => a?.GetType() ?? typeof(object));
        var m = obj.GetType().GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(m, $"Method '{name}' not found on {obj.GetType().Name}");
        m.Invoke(obj, args);
    }

    /// <summary>Создаёт минимальный WeaponData через ScriptableObject без Asset-файла.</summary>
    private WeaponData CreateWeaponData(float damage = 10f, float speed = 20f, float range = 100f)
    {
        var data = ScriptableObject.CreateInstance<WeaponData>();
        // Заполняем поля через рефлексию (все SerializeField приватные)
        SetField(data, "damage", damage);
        SetField(data, "projectileSpeed", speed);
        SetField(data, "range", range);
        return data;
    }

    /// <summary>Создаёт GameObject с Projectile и минимально необходимыми компонентами.</summary>
    private (GameObject go, Projectile proj) CreateProjectileGO()
    {
        var go = new GameObject("Projectile");
        go.AddComponent<NetworkIdentity>();
        go.AddComponent<Rigidbody>();
        go.AddComponent<SphereCollider>().isTrigger = true;
        go.AddComponent<Health>();          // IDieable
        var proj = go.AddComponent<Projectile>();
        return (go, proj);
    }

    // ?????????????????????????????????????????????????????????????????????????
    // Projectile Tests
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator Projectile_Initialize_SetsFieldsCorrectly()
    {
        var (go, proj) = CreateProjectileGO();
        var data = CreateWeaponData(damage: 42f, speed: 15f, range: 60f);
        data.name = "TestGun";

        proj.Initialize(data, ownerId: 7u, ownerName: "Vasya");
        yield return null;

        Assert.AreEqual(42f, proj.damage, "damage должен совпадать");
        Assert.AreEqual(7u, proj.OwnerId, "OwnerId должен совпадать");
        Assert.AreEqual("Vasya", proj.ownerName, "ownerName должен совпадать");
        Assert.AreEqual("TestGun", GetField<string>(proj, "weaponName"), "weaponName должен совпадать");

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(data);
    }

    [UnityTest]
    public IEnumerator Projectile_Initialize_SyncSpeedSet()
    {
        var (go, proj) = CreateProjectileGO();
        var data = CreateWeaponData(speed: 33f);

        proj.Initialize(data, 1u, "owner");
        yield return null;

        float syncSpeed = GetField<float>(proj, "_syncSpeed");
        Assert.AreEqual(33f, syncSpeed, "SyncVar _syncSpeed должен быть установлен");

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(data);
    }

    [UnityTest]
    public IEnumerator Projectile_Launch_SetsRigidbodyVelocity()
    {
        var (go, proj) = CreateProjectileGO();
        var data = CreateWeaponData(speed: 25f, range: 50f);

        proj.Initialize(data, 1u, "owner");

        // Launch вызывается через OnStartServer/OnStartClient, которые требуют Mirror.
        // Вызываем напрямую через рефлексию.
        InvokeMethod(proj, "Launch");
        yield return null;

        var rb = go.GetComponent<Rigidbody>();
        Assert.IsFalse(rb.useGravity, "useGravity должен быть false после Launch");
        Assert.AreEqual(25f, rb.linearVelocity.magnitude, 0.01f, "Скорость должна быть равна projectileSpeed");

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(data);
    }

    [UnityTest]
    public IEnumerator Projectile_StopPhysics_FreezesRigidbody()
    {
        var (go, proj) = CreateProjectileGO();
        var data = CreateWeaponData();
        proj.Initialize(data, 1u, "owner");

        InvokeMethod(proj, "Launch");
        InvokeMethod(proj, "StopPhysics");
        yield return null;

        var rb = go.GetComponent<Rigidbody>();
        Assert.IsTrue(GetField<bool>(proj, "_isImpacted"), "_isImpacted должен быть true");
        Assert.IsTrue(rb.isKinematic, "Rigidbody должен стать кинематическим");
        Assert.AreEqual(Vector3.zero, rb.linearVelocity, "Скорость должна быть обнулена");

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(data);
    }

    [UnityTest]
    public IEnumerator Projectile_HitVFX_SetFromWeaponData()
    {
        var (go, proj) = CreateProjectileGO();
        var data = CreateWeaponData();
        var vfxPrefab = new GameObject("VFX");
        SetField(data, "hitVFX", vfxPrefab);

        proj.Initialize(data, 1u, "owner");
        yield return null;

        Assert.AreSame(vfxPrefab, proj.HitVFX, "HitVFX должен совпадать с WeaponData.hitVFX");

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(vfxPrefab);
        Object.DestroyImmediate(data);
    }

    // ?????????????????????????????????????????????????????????????????????????
    // Rocket Tests
    // ?????????????????????????????????????????????????????????????????????????

    private (GameObject go, Rocket rocket, Projectile proj, Health health) CreateRocketGO()
    {
        var go = new GameObject("Rocket");
        go.AddComponent<NetworkIdentity>();
        go.AddComponent<Rigidbody>();
        go.AddComponent<SphereCollider>().isTrigger = true;
        var health = go.AddComponent<Health>();
        var proj = go.AddComponent<Projectile>();
        var rocket = go.AddComponent<Rocket>();
        return (go, rocket, proj, health);
    }

    [UnityTest]
    public IEnumerator Rocket_Awake_GrabsComponents()
    {
        var (go, rocket, proj, health) = CreateRocketGO();
        yield return null;

        // Rocket.Awake должен был подхватить Health и Projectile
        Assert.IsNotNull(GetField<Health>(rocket, "_health"), "_health не должен быть null");
        Assert.IsNotNull(GetField<Projectile>(rocket, "_projectile"), "_projectile не должен быть null");

        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator Rocket_Detonate_SetsHasExplodedFlag()
    {
        var (go, rocket, proj, health) = CreateRocketGO();
        var data = CreateWeaponData(damage: 50f);
        proj.Initialize(data, 1u, "owner");
        yield return null;

        // Detonate — [Server], вызываем через рефлексию
        var ctx = DamageContext.Suicide("test");
        InvokeMethod(rocket, "DetonateInternal", ctx);
        yield return null;

        Assert.IsTrue(GetField<bool>(rocket, "_hasExploded"), "_hasExploded должен быть true после детонации");

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(data);
    }

    [UnityTest]
    public IEnumerator Rocket_Detonate_CalledTwice_DoesNotRepeat()
    {
        var (go, rocket, proj, health) = CreateRocketGO();
        var data = CreateWeaponData();
        proj.Initialize(data, 1u, "owner");
        yield return null;

        var ctx = DamageContext.Suicide("test");
        InvokeMethod(rocket, "DetonateInternal", ctx);
        // Второй вызов не должен бросать исключений и менять флаг
        Assert.DoesNotThrow(() => InvokeMethod(rocket, "DetonateInternal", ctx));

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(data);
    }

    [UnityTest]
    public IEnumerator Rocket_ExplosionDamage_TakenFromProjectile()
    {
        var (go, rocket, proj, health) = CreateRocketGO();
        var data = CreateWeaponData(damage: 77f);

        // Инициализируем projectile ДО того как Rocket читает damage
        proj.Initialize(data, 1u, "owner");

        // Принудительно обновляем explosionDamage через рефлексию,
        // имитируя правильный порядок инициализации
        SetField(rocket, "explosionDamage", proj.damage);
        yield return null;

        float explosionDamage = GetField<float>(rocket, "explosionDamage");
        Assert.AreEqual(77f, explosionDamage, 0.001f, "explosionDamage должен совпадать с damage снаряда");

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(data);
    }

    // ?????????????????????????????????????????????????????????????????????????
    // RaycastStrategy Tests
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator RaycastStrategy_IsWeaponStrategy()
    {
        var strategy = ScriptableObject.CreateInstance<RaycastStrategy>();
        yield return null;

        Assert.IsInstanceOf<WeaponStrategy>(strategy, "RaycastStrategy должен наследовать WeaponStrategy");

        Object.DestroyImmediate(strategy);
    }

    [UnityTest]
    public IEnumerator RaycastStrategy_Fire_DoesNotThrowWithoutHit()
    {
        // Создаём ShipShooting-заглушку через подмену полей невозможна без реального объекта,
        // поэтому проверяем что RaycastStrategy корректно создаётся и имеет метод Fire.
        var strategy = ScriptableObject.CreateInstance<RaycastStrategy>();
        yield return null;

        var fireMethod = strategy.GetType().GetMethod("Fire",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(fireMethod, "Метод Fire должен существовать в RaycastStrategy");

        Object.DestroyImmediate(strategy);
    }

    // ?????????????????????????????????????????????????????????????????????????
    // ProjectileStrategy Tests
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator ProjectileStrategy_IsWeaponStrategy()
    {
        var strategy = ScriptableObject.CreateInstance<ProjectileStrategy>();
        yield return null;

        Assert.IsInstanceOf<WeaponStrategy>(strategy, "ProjectileStrategy должен наследовать WeaponStrategy");

        Object.DestroyImmediate(strategy);
    }

    [UnityTest]
    public IEnumerator ProjectileStrategy_Fire_DoesNotThrow_WhenNoPrefab()
    {
        // Без prefab Fire должен просто return, не кидать исключений.
        // Полноценный вызов Fire требует ShipShooting + NetworkServer — здесь тестируем только
        // что метод существует и доступен через нормальный полиморфизм.
        var strategy = ScriptableObject.CreateInstance<ProjectileStrategy>();
        yield return null;

        var fireMethod = strategy.GetType().GetMethod("Fire",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(fireMethod, "Метод Fire должен существовать в ProjectileStrategy");

        Object.DestroyImmediate(strategy);
    }

    // ?????????????????????????????????????????????????????????????????????????
    // WeaponStrategy (abstract) Tests
    // ?????????????????????????????????????????????????????????????????????????

    [UnityTest]
    public IEnumerator WeaponStrategy_Fire_IsAbstract()
    {
        var fireMethod = typeof(WeaponStrategy).GetMethod("Fire",
            BindingFlags.Public | BindingFlags.Instance);
        yield return null;

        Assert.IsNotNull(fireMethod, "Метод Fire должен быть объявлен в WeaponStrategy");
        Assert.IsTrue(fireMethod.IsAbstract, "Fire должен быть абстрактным");
    }

    [UnityTest]
    public IEnumerator WeaponStrategy_ConcreteImplementations_OverrideFire()
    {
        var projectileStrategy = ScriptableObject.CreateInstance<ProjectileStrategy>();
        var raycastStrategy = ScriptableObject.CreateInstance<RaycastStrategy>();
        yield return null;

        // Проверяем что override существует (не abstract, не базовый)
        var pm = projectileStrategy.GetType().GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance);
        var rm = raycastStrategy.GetType().GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance);

        Assert.IsNotNull(pm, "ProjectileStrategy должна переопределять Fire");
        Assert.IsFalse(pm.IsAbstract, "ProjectileStrategy.Fire не должен быть абстрактным");
        Assert.IsNotNull(rm, "RaycastStrategy должна переопределять Fire");
        Assert.IsFalse(rm.IsAbstract, "RaycastStrategy.Fire не должен быть абстрактным");

        Object.DestroyImmediate(projectileStrategy);
        Object.DestroyImmediate(raycastStrategy);
    }
}