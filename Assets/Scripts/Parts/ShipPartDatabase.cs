using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "ShipPartDatabase", menuName = "Ship/Database")]
public class ShipPartDatabase : ScriptableObject
{
    public List<HullData> hulls;
    public List<WeaponData> weapons;
    public List<EngineData> engines;

    public ShipPartData GetPartByID(string id)
    {
        var allParts = new List<ShipPartData>();
        allParts.AddRange(hulls);
        allParts.AddRange(weapons);
        allParts.AddRange(engines);

        return allParts.FirstOrDefault(p => p.id == id);
    }
}