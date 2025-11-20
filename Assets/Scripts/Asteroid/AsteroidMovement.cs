using Mirror;
using UnityEngine;

public class AsteroidMovement : NetworkBehaviour
{
	[SerializeField] [Range(1, 20f)]
	float _thrustForce = 1f,
		  _pitchForce,
		  _rollForce,
		  _yawForce;
	
	Rigidbody _rigidBody;
	float _thrustAmout, _pitchAmount, _rollAmount, _yawAmount = 0;
	
	public override void OnStartServer()
	{
		base.OnStartServer();
		_rigidBody = GetComponent<Rigidbody>();
		
		Vector3 flatten = new Vector3(1, 1, 0);
        Vector3 randomDirection = Random.insideUnitCircle.normalized;
		Vector3 force =  new Vector3(randomDirection.x, 0, randomDirection.y) * _thrustForce;
		Vector3 torque = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ) * _thrustForce;
		
		_rigidBody.AddForce(force, ForceMode.Impulse);
		_rigidBody.AddTorque(torque, ForceMode.Impulse);
	}
}
