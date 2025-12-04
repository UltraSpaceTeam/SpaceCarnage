using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Ship/Weapon")]
public class WeaponData : ShipPartData
{
    [Header("Behavior")]
    public WeaponStrategy strategy;
    [Space]
    [Header("Stats")]
    public float damage;
    public float fireRate;
    public float range;
    public int ammo;
    public float reload;
    [Tooltip("Only for prefab shooters")]
    public float projectileSpeed;
    [Space]
    [Header("Visuals")]
    public GameObject projectilePrefab;
    public GameObject hitVFX;
    public GameObject muzzleFlashVFX;
}