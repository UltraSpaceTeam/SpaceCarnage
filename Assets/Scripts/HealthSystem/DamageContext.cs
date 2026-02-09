public enum DamageType
{
    Generic,
    Weapon,
    Collision, 
    Suicide,
    Runaway
}

[System.Serializable]
public struct DamageContext
{
    public uint AttackerId;
    public string AttackerName;
    public string WeaponID;
    public DamageType Type;

    public DamageContext(uint id, string attacker, string weapon, DamageType type)
    {
        AttackerId = id;
        AttackerName = attacker;
        WeaponID = weapon;
        Type = type;
    }

    public static DamageContext Suicide(string name) => new DamageContext(0, name, "Suicide", DamageType.Suicide);
    public static DamageContext Environment(string objectName) => new DamageContext(0, "Environment", objectName, DamageType.Collision);
    public static DamageContext Weapon(uint id, string attacker, string weaponId) => new DamageContext(id, attacker, weaponId, DamageType.Weapon);
    public static DamageContext Collision(uint id, string attacker, string weaponId) => new DamageContext(id, attacker, weaponId, DamageType.Collision);
    public static DamageContext Runaway() => new DamageContext(0, "Space radiation", "Zone", DamageType.Runaway);

    public override readonly string ToString() => AttackerName + " " + WeaponID + " " + Type.ToString();
}