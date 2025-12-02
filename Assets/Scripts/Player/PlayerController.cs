using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ShipAssembler))]
public class PlayerController : NetworkBehaviour
{
    private Rigidbody rb;
    private ShipAssembler shipAssembler;

    private float thrustInput;
    private Vector3 rotationInput;

    [Header("Input Settings")]
    [SerializeField] private float mouseSensivity = 2.0f;
    [SerializeField] private bool invertY = false;

    private Vector2 mouseCursorPos = Vector2.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shipAssembler = GetComponent<ShipAssembler>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        var cam = FindAnyObjectByType<PlayerCamera>();
        if (cam != null)
        {
            cam.SetTarget(this.transform);
        }
    }

    void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        thrustInput = Input.GetAxis("Vertical");
        float roll = Input.GetAxis("Horizontal");
        float mouseX = Input.GetAxis("Mouse X") * mouseSensivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensivity * Time.deltaTime * (invertY ? 1 : -1);
        mouseCursorPos = Vector2.ClampMagnitude(mouseCursorPos, 1.0f);

        mouseCursorPos += new Vector2(mouseX, mouseY);
        Vector3 curRotationInput = new Vector3(mouseCursorPos.y, mouseCursorPos.x, -roll);
        CmdUpdateInputs(thrustInput, curRotationInput);
    }

    [Command]
    void CmdUpdateInputs(float thrust, Vector3 rotation)
    {
        thrustInput = thrust;
        rotationInput = rotation;
    }

    private void FixedUpdate()
    {
        if (!isServer) return;
        if (shipAssembler.CurrentHull == null)
        {
            Debug.LogWarning("No hull equipped!");
            return;
        }
        HullData hull = shipAssembler.CurrentHull;
        if (Mathf.Abs(thrustInput) > 0.01f)
        {
            Vector3 force = Vector3.forward * thrustInput * hull.acceleration;
            rb.AddRelativeForce(force, ForceMode.Acceleration);
        }


        if (rb.linearVelocity.magnitude > hull.maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * hull.maxSpeed;
        }
        if (rotationInput.sqrMagnitude > 0.01f)
        {
            float pitch = rotationInput.x * hull.rotationXYSpeed;
            float yaw = rotationInput.y * hull.rotationXYSpeed;
            float roll = rotationInput.z * hull.rotationZSpeed;

            Vector3 torque = new Vector3(pitch, yaw, roll);
            rb.AddRelativeTorque(torque, ForceMode.Force);
        }

        if (shipAssembler.CurrentEngine == null)
        {
            Debug.LogWarning("No engine equipped!");
            return;
        }
        EngineData engine = shipAssembler.CurrentEngine;
        if (Input.GetMouseButton(0))
        {
            engine.ability.RunAbility();
        }
    }

    void OnGUI()
    {
        if (!isLocalPlayer) return;

        float x = (Screen.width / 2) + (mouseCursorPos.x * Screen.height / 2);
        float y = (Screen.height / 2) + (mouseCursorPos.y * Screen.height / 2);
        GUI.Box(new Rect(x - 5, y - 5, 10, 10), "");
    }
}
