using UnityEngine;
using Mirror;

[CreateAssetMenu(menuName = "Ship/Shooting/Projectile Strategy")]
public class ProjectileStrategy : WeaponStrategy
{
    public override void Fire(ShipShooting shooter, Vector3 pos, Quaternion rot)
    {
        WeaponData stats = shooter.CurrentWeaponData;
        if (stats.projectilePrefab == null) return;

        GameObject bullet = Instantiate(stats.projectilePrefab, pos, rot);

        Projectile projScript = bullet.GetComponent<Projectile>();
        if (projScript != null)
        {
            float lifetime = stats.range / stats.projectileSpeed;
            projScript.Initialize(stats, shooter.netId, shooter.ShooterName);
        }

        NetworkServer.Spawn(bullet);
    }
}