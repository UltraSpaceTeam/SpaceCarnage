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

    private bool activateAbility;
    private float abilityCooldownTimer = 0f;

    [Header("Input Settings")]
    [SerializeField] private float mouseSensivity = 2.0f;
    [SerializeField] private bool invertY = false;

    private Vector2 mouseCursorPos = Vector2.zero;

    [Header("Physics Settings")]
    [SerializeField] private float overSpeedDragFactor = 1f;

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

        bool abilityPressed = Input.GetKeyDown(KeyCode.Space);
        CmdUpdateInputs(thrustInput, curRotationInput, abilityPressed);
    }

    [Command]
    void CmdUpdateInputs(float thrust, Vector3 rotation, bool abilityPressed)
    {
        thrustInput = thrust;
        rotationInput = rotation;
        activateAbility |= abilityPressed;
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

        if (shipAssembler.CurrentEngine == null)
        {
            Debug.LogWarning("No engine equipped!");
            return;
        }
        EngineData engine = shipAssembler.CurrentEngine;

        if (shipAssembler.CurrentWeapon == null)
        {
            Debug.LogWarning("No weapon equipped!");
            return;
        }
        WeaponData weapon = shipAssembler.CurrentWeapon;

        float sumMass = weapon.mass + engine.mass + hull.mass;
        rb.mass = sumMass;
        rb.linearDamping = hull.linearDamping;
        rb.angularDamping = hull.rotationDamping;

        if (Mathf.Abs(thrustInput) > 0.01f)
        {
            Vector3 force = Vector3.forward * thrustInput * engine.power;
            rb.AddRelativeForce(force, ForceMode.Acceleration);
        }

        if (rotationInput.sqrMagnitude > 0.001f)
        {
            float pitch = rotationInput.x * hull.rotationXYSpeed;
            float yaw = rotationInput.y * hull.rotationXYSpeed;
            float roll = rotationInput.z * hull.rotationZSpeed;

            Vector3 torque = new Vector3(pitch, yaw, roll);
            rb.AddRelativeTorque(torque, ForceMode.Force);
        }

        abilityCooldownTimer -= Time.fixedDeltaTime;
        if (activateAbility)
        {
            if (abilityCooldownTimer <= 0)
            {
                engine.ability.RunAbility(rb);
                abilityCooldownTimer = engine.ability.cooldown;
            }
        }
        activateAbility = false;

        Debug.Log("Speed at end of FixedUpdate: " + rb.linearVelocity.magnitude);
    }

    void OnGUI()
    {
        if (!isLocalPlayer) return;

        float x = (Screen.width / 2) + (mouseCursorPos.x * Screen.height / 2);
        float y = (Screen.height / 2) + (mouseCursorPos.y * Screen.height / 2);
        GUI.Box(new Rect(x - 5, y - 5, 10, 10), "");
    }
}
