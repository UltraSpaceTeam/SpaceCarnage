using UnityEngine;

[CreateAssetMenu(fileName = "New Engine", menuName = "Ship/Engine")]
public class EngineData : ShipPartData
{
    public float power;
    public AbstractAbility ability;
}