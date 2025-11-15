using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    private Rigidbody rb;
    private Vector3 movement;
    private Vector3 torque;
    [SerializeField] private Vector3 Sensitivity;
    [SerializeField] private Vector3 Power;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (isClient)
        {
            rb.isKinematic = false;
        }
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;
    }

    void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        movement = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Jump"), Input.GetAxis("Vertical"));
        movement = Vector3.ClampMagnitude(movement, 1);
        movement.Scale(Power);

        torque = new Vector3(Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), Input.GetAxis("Roll"));
        torque = Vector3.ClampMagnitude(torque, 1);
        torque.Scale(Sensitivity);

        CmdUpdateInputs(movement, torque);
    }

    [Command]
    void CmdUpdateInputs(Vector3 movement, Vector3 torque)
    {
        rb.AddRelativeForce(movement);

        rb.AddRelativeTorque(torque);
    }
}
