using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(ShipShooting))]
[RequireComponent(typeof(ShipAssembler))]
public class Player : NetworkBehaviour
{
    
    public static Dictionary<uint, Player> ActivePlayers = new Dictionary<uint, Player>();

    private Health health;
    private PlayerController controller;
    private ShipShooting shooting;
    private ShipAssembler assembler;

    [SyncVar] public string Nickname = "Player";

    [Header("Respawn Settings")]
    [SerializeField] private float invulnerabilityDuration = 3.0f;

    [Header("Death VFX")]
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float debrisLifetime = 10f;

    [Header("Ability Visuals")]
    public GameObject shieldBubblePrefab;
    private GameObject currentShieldInstance;
    private Renderer currentShieldRenderer;

    private void Awake()
    {
        health = GetComponent<Health>();
        controller = GetComponent<PlayerController>();
        shooting = GetComponent<ShipShooting>();
        assembler = GetComponent<ShipAssembler>();

    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!ActivePlayers.ContainsKey(netId))
        {
            ActivePlayers.Add(netId, this);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (ActivePlayers.ContainsKey(netId))
        {
            ActivePlayers.Remove(netId);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        health.OnDeath += ServerHandleDeath;

        if (assembler != null)
        {
            assembler.OnHullEquipped += HandleHullChange;

            if (assembler.CurrentHull != null)
            {
                HandleHullChange(assembler.CurrentHull);
            }
        }
    }
    public override void OnStopServer()
    {
        base.OnStopServer();
        health.OnDeath -= ServerHandleDeath;
    }

    public override void OnStartLocalPlayer()
    {
        string myName = $"Player {netId}";
        CmdSetNickname(myName);
        base.OnStartLocalPlayer();
        health.OnHealthUpdate += HandleHealthUpdate;
        UIManager.Instance.UpdateHealth(100, 100);
    }
    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        if (health != null) health.OnHealthUpdate -= HandleHealthUpdate;
    }

    private void HandleHealthUpdate(float current, float max)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(current, max);
        }
    }

    [Server]
    private void HandleHullChange(HullData newHull)
    {
        if (newHull != null)
        {
            health.SetMaxHealth(newHull.maxHealth);
        }
    }

    [Server]
    private void ServerHandleDeath(DamageContext source)
    {
        Debug.Log("Player " + gameObject.name + " died due to " + source);
        RpcSpawnDebris();

        SetPlayerState(false);

        TargetShowDeathScreen(connectionToClient, source);

        RpcAddKillFeed(source, Nickname);
    }

    [ClientRpc]
    private void RpcAddKillFeed(DamageContext ctx, string victimName)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.AddKillFeedEntry(ctx, victimName);
        }
    }

    [ClientRpc]
    private void RpcSpawnDebris()
    {
        List<GameObject> partsToExplode = new List<GameObject>();

        if (assembler.CurrentHullObject != null) partsToExplode.Add(assembler.CurrentHullObject);
        if (assembler.CurrentWeaponObject != null) partsToExplode.Add(assembler.CurrentWeaponObject);

        if (partsToExplode.Count == 0)
        {
            foreach (Transform t in transform) partsToExplode.Add(t.gameObject);
        }

        foreach (var originalPart in partsToExplode)
        {
            if (originalPart == null) continue;

            GameObject debrisPart = Instantiate(originalPart, originalPart.transform.position, originalPart.transform.rotation);

            SetupDebrisRecursive(debrisPart.transform);

            Destroy(debrisPart, debrisLifetime);
        }
    }

    private void SetupDebrisRecursive(Transform parent)
    {
        var allRenderers = parent.GetComponentsInChildren<Renderer>(true);

        foreach (var r in allRenderers)
        {
            if (r is LineRenderer || r is TrailRenderer || r is ParticleSystemRenderer)
            {
                Destroy(r.gameObject);
                continue;
            }

            GameObject go = r.gameObject;

            r.enabled = true;
            r.material.color = Color.gray;

            foreach (var col in go.GetComponents<Collider>()) Destroy(col);
            foreach (var comp in go.GetComponents<MonoBehaviour>()) Destroy(comp);

            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();

            rb.useGravity = false;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;

            Vector3 randomDir = Random.insideUnitSphere;
            rb.AddForce(randomDir * explosionForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * explosionForce, ForceMode.Impulse);

            if (go.transform != parent)
            {
                go.transform.SetParent(null);

                Destroy(go, debrisLifetime);
            }
        }
    }

    [TargetRpc]
    private void TargetShowDeathScreen(NetworkConnection target, DamageContext source)
    {
        Debug.Log("YOU DIED! Source: " + source);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDeathScreen(source);
        }
        else
        {
            Debug.LogWarning("UIManager instance is null!");
        }
    }


    [Command]
    public void CmdRequestRespawn()
    {
        if (!health.IsDead) return;

        StartCoroutine(RespawnRoutine());
    }

    [Command]
    private void CmdSetNickname(string newName)
    {
        Nickname = newName;
    }

    [Server]
    private IEnumerator RespawnRoutine()
    {
        Transform spawnPoint = GetSafeSpawnPoint();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        yield return new WaitForSeconds(0.1f); 

        if (rb != null) rb.isKinematic = false;

        health.Ressurect();

        SetPlayerState(true);

        health.SetInvincibility(true);
        TargetRespawnFeedback(connectionToClient);

        yield return new WaitForSeconds(invulnerabilityDuration);

        health.SetInvincibility(false);
    }

    [Server]
    private void SetPlayerState(bool isActive)
    {
        controller.enabled = isActive;
        shooting.enabled = isActive;

        var colliders = GetComponentsInChildren<Collider>();
        var renderers = GetComponentsInChildren<Renderer>();

        foreach (var c in colliders) c.enabled = isActive;
        foreach (var r in renderers)
        {
            if(r is LineRenderer || r is TrailRenderer || r is ParticleSystemRenderer)
            {
                continue;
            }
            r.enabled = isActive;

        }

        RpcSetState(isActive);
    }

    [ClientRpc]
    private void RpcSetState(bool isActive)
    {
        Debug.Log("Setting player state to " + isActive);
        controller.enabled = isActive;
        shooting.enabled = isActive;

        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = isActive;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if(r is LineRenderer||  r is TrailRenderer || r is ParticleSystemRenderer)
            {
                continue;
            }
            r.enabled = isActive;
        }
    }

    [TargetRpc]
    private void TargetRespawnFeedback(NetworkConnection target)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideDeathScreen();
        }
    }

    [Server]
    private Transform GetSafeSpawnPoint()
    {
        var startPositions = NetworkManager.startPositions;
        foreach (var pos in startPositions)
        {
            if (Physics.OverlapSphere(pos.position, 10f).Length == 0)
            {
                return pos;
            }
        }
        if (startPositions.Count > 0)
            return startPositions[Random.Range(0, startPositions.Count)];

        return NetworkManager.singleton.GetStartPosition();
    }

    [ClientRpc]
    public void RpcSetShipVisible(bool visible)
    {
        var invisManager = GetComponent<InvisManager>();
        if (invisManager != null)
        {
            invisManager.SetVisible(visible);  // Это вызовет SyncVar и применит на всех клиентах
        }
    }

    [ClientRpc]
    public void RpcShowShield(bool show, float healthRatio)
    {
        if (shieldBubblePrefab == null)
        {
            Debug.LogWarning("Shield prefab not assigned in Player!");
            return;
        }

        if (show)
        {
            // Создаём, если ещё нет
            if (currentShieldInstance == null)
            {
                currentShieldInstance = Instantiate(shieldBubblePrefab, transform);
                currentShieldInstance.transform.localPosition = Vector3.zero;
                currentShieldInstance.transform.localRotation = Quaternion.identity;
                currentShieldRenderer = currentShieldInstance.GetComponentInChildren<Renderer>();
            }

            currentShieldInstance.SetActive(true);

            // Прозрачность по здоровью щита
            if (currentShieldRenderer != null)
            {
                Color col = currentShieldRenderer.material.color;
                col.a = Mathf.Lerp(0.1f, 0.4f, healthRatio);  // от слабой до полной видимости
                currentShieldRenderer.material.color = col;
            }
        }
        else
        {
            if (currentShieldInstance != null)
            {
                currentShieldInstance.SetActive(false);
            }
        }
    }
}

