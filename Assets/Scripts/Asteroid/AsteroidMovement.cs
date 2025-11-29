using Mirror;
using UnityEngine;

public class AsteroidMovement : NetworkBehaviour
{
	[SerializeField]
	private Rigidbody _rigidBody;
	
    [SyncVar] private float _thrustForce;
    [SyncVar] private Vector3 _initialForce;
    [SyncVar] private Vector3 _initialTorque;
	
	public override void OnStartServer()
	{
		base.OnStartServer();
		_rigidBody = GetComponent<Rigidbody>();
				
		//_rigidBody.AddForce(_initialForce, ForceMode.Impulse);
		_rigidBody.AddTorque(_initialTorque, ForceMode.Impulse);
	}	
	
	// Метод для установки параметров движения при спавне
    [Server]
    public void SetMovementParameters(float thrustForce, Vector3 force, Vector3 torque)
    {
        _thrustForce = thrustForce;
        _initialForce = force;
        _initialTorque = torque;
    }
}
