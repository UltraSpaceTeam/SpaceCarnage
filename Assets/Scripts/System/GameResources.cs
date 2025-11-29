using UnityEngine;

public class GameResources : MonoBehaviour
{
    public static GameResources Instance;
    public ShipPartDatabase partDatabase;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(gameObject);
    }
}