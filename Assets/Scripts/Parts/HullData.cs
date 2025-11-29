using UnityEngine;

[CreateAssetMenu(fileName = "New Hull", menuName = "Ship/Hull")]
public class HullData : ShipPartData
{
    public float maxHealth;
    public float baseDefense;
    public float acceleration;
    public float rotationSpeed;

}