using System;
using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private Health health;


    private void Awake()
    {
        health = GetComponent<Health>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        health.OnDeath += HandleDeath;
    }
    public override void OnStopServer()
    {
        base.OnStopServer();
        health.OnDeath -= HandleDeath;
    }

    private void HandleDeath(string source)
    {
        Debug.Log("Player " + gameObject.name + " died due to " + source);
    }
    
}

