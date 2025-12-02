using UnityEngine;

[CreateAssetMenu(fileName = "New Engine", menuName = "Ship/Engine")]
public class EngineData : ShipPartData
{
    public float maxSpeed;
    public AbstractAbility ability;
}