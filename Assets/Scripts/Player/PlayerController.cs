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
    [SerializeField] private float cursorReturnStrength = 0.5f;
    [SerializeField] private float cursorSmoothTime = 0.1f;

    private Vector2 mouseCursorPos = Vector2.zero;

    private Vector2 _targetCursorPos = Vector2.zero;

    private Vector2 _cursorVelocity;

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

        _targetCursorPos += new Vector2(mouseX, mouseY);

        if (cursorReturnStrength > 0)
        {
            Vector2 springReturn = Vector2.Lerp(_targetCursorPos, Vector2.zero, cursorReturnStrength * Time.deltaTime);

            float minReturnSpeed = 0.3f;
            _targetCursorPos = Vector2.MoveTowards(springReturn, Vector2.zero, minReturnSpeed * Time.deltaTime);
        }

        mouseCursorPos = Vector2.SmoothDamp(mouseCursorPos, _targetCursorPos, ref _cursorVelocity, cursorSmoothTime);
        mouseCursorPos = Vector2.ClampMagnitude(mouseCursorPos, 1.0f);

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
        if (rotationInput.sqrMagnitude > 0.001f)
        {
            float pitch = rotationInput.x * hull.rotationXYSpeed;
            float yaw = rotationInput.y * hull.rotationXYSpeed;
            float roll = rotationInput.z * hull.rotationZSpeed;

            Vector3 torque = new Vector3(pitch, yaw, roll);
            rb.AddRelativeTorque(torque, ForceMode.Force);
        }
    }

    void OnGUI()
    {
        if (!isLocalPlayer) return;

        float x = (Screen.width / 2) + (mouseCursorPos.x * Screen.height / 2);
        float y = (Screen.height / 2) + (mouseCursorPos.y * Screen.height / 2);
        GUI.color = Color.black;
        GUI.Box(new Rect(x - 6, y - 6, 12, 12), "");
        GUI.color = Color.white;
        GUI.Box(new Rect(x - 4, y - 4, 8, 8), "");
    }
}
