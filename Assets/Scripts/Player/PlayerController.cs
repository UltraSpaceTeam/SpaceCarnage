using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(ShipAssembler))]
[RequireComponent(typeof(Health))] // temp
public class PlayerController : NetworkBehaviour
{
    private Rigidbody rb;
    private ShipAssembler shipAssembler;

    [SyncVar] public float CurrentThrustOutput;

    private float thrustInput;
    private float rollInput;
    private Vector2 aimTargetInput;

    private bool activateAbility;

    [SyncVar] private float abilityCooldownTimer = 0f;

    [Header("Input Settings")]
    [SerializeField] private bool invertY = false;
    [SerializeField] private float centeringSpeed = 0.5f;
    [SerializeField] private float centeringDelay = 0.5f;
    [SerializeField] private float centeringSmoothTime = 0.2f;
    [SerializeField] private float sensitivityCurve = 2.0f;
    [SerializeField] private float reverseModifier = 0.2f;


    [Header("Aim Settings")]
    [SerializeField] private float deadzoneRadius = 0.15f;
    [SerializeField] private float mouseSensitivity = 1.0f;
    [SerializeField] private float cursorInputSmoothTime = 0.05f;

    [Header("PID Controller (Physics)")]
    [SerializeField] private float pFactor = 5.0f;
    [SerializeField] private float dFactor = 1.0f;

    private Vector2 _clientCursorPos = new Vector2(0.5f, 0.5f);

    private float _inputIdleTime = 0f;
    private Vector2 _renderVelocity;
    private Vector2 _targetRawPos = new Vector2(0.5f, 0.5f);


    private Health health; // temp
    private AbstractAbility currentAbility;


    [Header("Physics Settings")]
    [SerializeField] private float overSpeedDragFactor = 1f;

    public float AbilityCooldownRemaining => abilityCooldownTimer;
    public bool AbilityOnCooldown => abilityCooldownTimer > 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shipAssembler = GetComponent<ShipAssembler>();

        health = GetComponent<Health>(); // temp
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
        if (health != null && health.IsDead) return;

        if (Input.GetKeyDown(KeyCode.Delete)) // temp
        {
            CmdSelfDestruct();
        }

        float rawThrust = Input.GetAxis("Vertical");
        float rawRoll = Input.GetAxis("Horizontal");
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime * (invertY ? -1 : 1);

        if (Mathf.Abs(mouseX) > 0.0001f || Mathf.Abs(mouseY) > 0.0001f)
        {
            _inputIdleTime = 0f;
            _targetRawPos.x += mouseX;
            _targetRawPos.y += mouseY;
        }
        else
        {
            _inputIdleTime += Time.deltaTime;
            if (_inputIdleTime > centeringDelay)
            {
                _targetRawPos = Vector2.MoveTowards(_targetRawPos, new Vector2(0.5f, 0.5f), centeringSpeed * Time.deltaTime);
            }
        }

        _targetRawPos.x = Mathf.Clamp01(_targetRawPos.x);
        _targetRawPos.y = Mathf.Clamp01(_targetRawPos.y);

        _clientCursorPos = Vector2.SmoothDamp(_clientCursorPos, _targetRawPos, ref _renderVelocity, cursorInputSmoothTime);

        Vector2 rawInput = (_clientCursorPos - new Vector2(0.5f, 0.5f)) * 2f;

        float curvedX = Mathf.Sign(rawInput.x) * Mathf.Pow(Mathf.Abs(rawInput.x), sensitivityCurve);
        float curvedY = Mathf.Sign(rawInput.y) * Mathf.Pow(Mathf.Abs(rawInput.y), sensitivityCurve);

        Vector2 targetViewport = new Vector2(curvedX, curvedY);
        if (rawThrust < 0)
        {
            rawThrust *= reverseModifier;
        }

        bool abilityPressed = Input.GetKeyDown(KeyCode.Space);
        CmdUpdateInputs(rawThrust, rawRoll, targetViewport, abilityPressed);

        if (UIManager.Instance != null && !UIManager.Instance.isEndMatch)
        {
            bool tabHeld = Input.GetKey(KeyCode.Tab);
            UIManager.Instance.SetLeaderboardVisible(tabHeld);
        }
    }

    [Command]
    void CmdUpdateInputs(float thrust, float roll, Vector2 rotation, bool abilityPressed)
    {
        thrustInput = thrust;
        CurrentThrustOutput = thrust;
        rollInput = roll;
        aimTargetInput = rotation;
        activateAbility |= abilityPressed;
    }

    [Command]
    void CmdSelfDestruct() // temp
    {
        if (health != null && !health.IsDead)
        {
            health.TakeDamage(9999, DamageContext.Suicide(name));
        }
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

        if (engine != null)
        {
            if (engine.ability != currentAbility)
            {
                currentAbility?.OnUnequipped();
                currentAbility = engine.ability;
                currentAbility?.OnEquipped();
            }

            currentAbility?.ServerUpdate(rb);
        }

        float speedMult = currentAbility?.GetSpeedMultiplier() ?? 1f;

        float sumMass = weapon.mass + engine.mass + hull.mass;
        rb.mass = sumMass;
        rb.linearDamping = hull.linearDamping;
        rb.angularDamping = hull.rotationDamping;

        if (Mathf.Abs(thrustInput) > 0.01f)
        {
            Vector3 force = Vector3.forward * thrustInput * engine.power * speedMult;
            rb.AddRelativeForce(force, ForceMode.Acceleration);
        }

        float targetYaw = aimTargetInput.x;
        float targetPitch = aimTargetInput.y;

        if (Mathf.Abs(targetYaw) < deadzoneRadius) targetYaw = 0;
        if (Mathf.Abs(targetPitch) < deadzoneRadius) targetPitch = 0;

        Vector3 desiredAngularVel = new Vector3(-targetPitch * hull.rotationXYSpeed,
        targetYaw * hull.rotationXYSpeed,
        -rollInput * hull.rotationZSpeed);

        Vector3 currentAngularVel = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 error = desiredAngularVel - currentAngularVel;
        Vector3 torque = (error * pFactor) - (currentAngularVel * dFactor);

        rb.AddRelativeTorque(torque, ForceMode.Acceleration);
        abilityCooldownTimer -= Time.fixedDeltaTime;

        if (activateAbility)
        {
            if (abilityCooldownTimer <= 0 && currentAbility != null)
            {
                currentAbility.RunAbility(rb);
                abilityCooldownTimer = currentAbility.cooldown;
            }
        }

        activateAbility = false;
    }

    void OnGUI()
    {
        if (!isLocalPlayer) return;

        float x = _clientCursorPos.x * Screen.width;
        float y = (1f - _clientCursorPos.y) * Screen.height;

        GUI.color = Color.red;
        GUI.Box(new Rect(x - 10, y - 10, 20, 20), "+");
        float cx = Screen.width / 2;
        float cy = Screen.height / 2;
        GUI.color = new Color(.2f, .2f, 1, 0.4f);
        GUI.Box(new Rect(cx - 2, cy - 2, 4, 4), ".");
    }
}
