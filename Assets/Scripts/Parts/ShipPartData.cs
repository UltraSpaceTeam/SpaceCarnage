using UnityEngine;

public enum PartType
{
    Hull,
    Weapon,
    Engine
}

public abstract class ShipPartData : ScriptableObject
{
    [Header("General Info")]
    public string id;
    public string partName;
    public PartType partType;
    public GameObject prefab;

    [Header("Stats")]
    public float weight;
}





