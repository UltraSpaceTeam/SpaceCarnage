using UnityEngine;

public abstract class WeaponStrategy : ScriptableObject
{
    public abstract void Fire(ShipShooting shooter, Vector3 muzzlePos, Quaternion muzzleRot);
}
