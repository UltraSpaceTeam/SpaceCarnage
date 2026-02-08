using Mirror;
using UnityEngine;

public class MatchState : NetworkBehaviour
{
    public static MatchState Instance;

    [SyncVar] public int State;
    [SyncVar] public double MatchStartTime;
    [SyncVar] public double EndingStartTime;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
}
