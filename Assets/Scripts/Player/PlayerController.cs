using UnityEngine;
using Mirror;
using System;
using Unity.VisualScripting.Antlr3.Runtime;

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

    [SyncVar] private double abilityReadyTime;
    [SyncVar] public float AbilityStatusValue;

    [Header("Input Settings")]
    [SerializeField] private bool invertY = false;
    [SerializeField] private float reverseModifier = 0.2f;


    [Header("Aim Settings")]
    [SerializeField] private float deadzoneRadius = 0.15f;

    [Header("PID Controller (Physics)")]
    [SerializeField] private float pFactor = 5.0f;
    [SerializeField] private float dFactor = 1.0f;


    private Health health; // temp
    private AbstractAbility currentAbility;


    [Header("Physics Settings")]
    [SerializeField] private float overSpeedDragFactor = 1f;

    [Header("Networking Input")]
    [SerializeField] private float sendRateHz = 20f;
    [SerializeField] private float inputEpsilon = 0.001f;

    [Header("Mouse Aim")]
    [SerializeField] private float aimSensitivity = 2.0f;
    [SerializeField] private float aimMax = 1.0f;

    [Header("Recenter")]
    [SerializeField] private KeyCode recenterKey = KeyCode.Mouse1;
    [SerializeField] private bool recenterHold = false;
    [SerializeField] private float recenterSpeed = 12f;
    [SerializeField] private bool snapRecenter = false;

    private Vector2 _aim;

    private float _nextSendTime;
    private float _lastSentThrust;
    private float _lastSentRoll;
    private Vector2 _lastSentAim;

    private bool _abilityQueued;
    private bool _recenterQueued;


    public float AbilityCooldownRemaining
    {
        get
        {
            double remaining = abilityReadyTime - CurrentTime;
            return (float)Math.Max(0, remaining);
        }
    }
    public bool AbilityOnCooldown => AbilityCooldownRemaining > 0f;

    private double CurrentTime => (NetworkClient.active || NetworkServer.active) ? NetworkTime.time : Time.timeAsDouble;

    private bool isPaused => PauseMenuController.Instance != null && PauseMenuController.IsPaused;

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

        if (isPaused)
        {
            thrustInput = 0f;
            rollInput = 0f;
            aimTargetInput = Vector2.zero;
            activateAbility = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("[PlayerController] [INFO] ESC pressed detected!");
            PauseMenuController.Instance.TogglePauseMenu();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Delete)) // temp
        {
            CmdSelfDestruct();
        }

        float rawThrust = Input.GetAxis("Vertical");
        float rawRoll = Input.GetAxis("Horizontal");
        float mouseX = Input.GetAxis("Mouse X") * aimSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * aimSensitivity * (invertY ? -1 : 1);

        Vector2 delta = new Vector2(mouseX, mouseY);
        if (delta.sqrMagnitude > 0.000001f)
        {
            _aim += delta * Time.deltaTime;
            _aim.x = Mathf.Clamp(_aim.x, -aimMax, aimMax);
            _aim.y = Mathf.Clamp(_aim.y, -aimMax, aimMax);
        }

        bool abilityPressed = Input.GetKeyDown(KeyCode.Space);
        if (abilityPressed) _abilityQueued = true;

        bool recenterPressed = Input.GetKeyDown(recenterKey);
        if (recenterPressed) _recenterQueued = true;

        bool recenterHeld = Input.GetKey(recenterKey);

        bool doRecenter = recenterHold ? recenterHeld : recenterPressed;
        if (doRecenter)
        {
            if (snapRecenter)
            {
                _aim = Vector2.zero;
            }
            else
            {
                _aim = Vector2.MoveTowards(_aim, Vector2.zero, recenterSpeed * Time.deltaTime);
            }
        }

        Vector2 aimTarget = _aim;
        if (aimTarget.magnitude < deadzoneRadius) aimTarget = Vector2.zero;

        if (rawThrust < 0) rawThrust *= reverseModifier;


        bool timeOk = Time.time >= _nextSendTime;

        bool inputsChanged =
            Mathf.Abs(rawThrust - _lastSentThrust) > inputEpsilon ||
            Mathf.Abs(rawRoll - _lastSentRoll) > inputEpsilon ||
            (aimTarget - _lastSentAim).sqrMagnitude > (inputEpsilon * inputEpsilon);

        bool shouldSend = (timeOk && inputsChanged) || _abilityQueued || _recenterQueued;

        if (shouldSend)
        {
            if (timeOk) _nextSendTime = Time.time + 1f / sendRateHz;

            _lastSentThrust = rawThrust;
            _lastSentRoll = rawRoll;
            _lastSentAim = aimTarget;
            bool sendAbility = _abilityQueued;

            _abilityQueued = false;
            _recenterQueued = false;

            CmdUpdateInputs(rawThrust, rawRoll, aimTarget, sendAbility);
        }
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
            Debug.LogWarning("[PlayerController] [WARN] No hull equipped!");
            return;
        }
        HullData hull = shipAssembler.CurrentHull;

        if (shipAssembler.CurrentEngine == null)
        {
            Debug.LogWarning("[PlayerController] [WARN] No engine equipped!");
            return;
        }
        EngineData engine = shipAssembler.CurrentEngine;

        if (shipAssembler.CurrentWeapon == null)
        {
            Debug.LogWarning("[PlayerController] [WARN] No weapon equipped!");
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

            if (currentAbility != null)
            {
                AbilityStatusValue = currentAbility.GetVisualStatus();
            }
            else
            {
                AbilityStatusValue = 0f;
            }
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

        if (activateAbility)
        {
            if (CurrentTime >= abilityReadyTime && currentAbility != null)
            {
                currentAbility.RunAbility(rb);
                abilityReadyTime = CurrentTime + currentAbility.cooldown;
            }
        }
        activateAbility = false;
    }

    void OnGUI()
    {
        if (!isLocalPlayer) return;

        if (isPaused) return;

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        float scale = 200f;
        float x = cx + (_aim.x * scale);
        float y = cy - (_aim.y * scale);

        GUI.color = Color.red;
        GUI.Box(new Rect(x - 10, y - 10, 20, 20), "+");

        GUI.color = new Color(.2f, .2f, 1, 0.4f);
        GUI.Box(new Rect(cx - 2, cy - 2, 4, 4), ".");
    }
}
