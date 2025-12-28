using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(ShipAssembler))]
[RequireComponent(typeof(AudioSource))]
public class ShipEngineFX : MonoBehaviour
{
    private class ParticleSnapshot
    {
        public ParticleSystem ps;
        public float maxRate;
        public float maxLifetime;
    }

    private PlayerController controller;
    private ShipAssembler assembler;
    private AudioSource audioSource;
    private Rigidbody rb;

    private List<ParticleSnapshot> currentParticles = new List<ParticleSnapshot>();

    [Header("Scaling Settings")]
    [Range(0f, 1f)]
    [Tooltip("Процент от базового значения на холостом ходу (0.2 = 20%)")]
    [SerializeField] private float idleRatio = 0.2f;

    [SerializeField] private float responseSpeed = 5f;

    [Header("Sound Settings")]
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.5f;
    [SerializeField] private float minVolume = 0.2f;
    [SerializeField] private float maxVolume = 1.0f;

    private float smoothThrust;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        assembler = GetComponent<ShipAssembler>();
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (assembler != null) assembler.OnEngineEquipped += HandleNewEngine;
    }

    void OnDisable()
    {
        if (assembler != null) assembler.OnEngineEquipped -= HandleNewEngine;
    }

    private void HandleNewEngine(GameObject engineObject)
    {
        currentParticles.Clear();

        var systems = engineObject.GetComponentsInChildren<ParticleSystem>();

        foreach (var ps in systems)
        {
            ParticleSnapshot snapshot = new ParticleSnapshot();
            snapshot.ps = ps;

            snapshot.maxRate = ps.emission.rateOverTime.constantMax;
            snapshot.maxLifetime = ps.main.startLifetime.constantMax;

            currentParticles.Add(snapshot);
        }
    }

    void Start()
    {
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 10f;
        audioSource.maxDistance = 80f;

        if (!audioSource.isPlaying) audioSource.Play();

        if (currentParticles.Count == 0 && assembler.CurrentEngineObject != null)
        {
            HandleNewEngine(assembler.CurrentEngineObject);
        }
    }

    void Update()
    {
        float targetThrust = Mathf.Abs(controller.CurrentThrustOutput);
        smoothThrust = Mathf.Lerp(smoothThrust, targetThrust, Time.deltaTime * responseSpeed);

        if (audioSource != null)
        {
            audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, smoothThrust);
            audioSource.volume = Mathf.Lerp(minVolume, maxVolume, smoothThrust);
        }

        float speedFactor = rb != null ? (rb.linearVelocity.magnitude / 30f) : 0f;
        float combinedFactor = Mathf.Clamp01(smoothThrust + speedFactor);

        foreach (var p in currentParticles)
        {
            if (p.ps == null) continue;

            var emission = p.ps.emission;
            var main = p.ps.main;

            float currentRate = Mathf.Lerp(p.maxRate * idleRatio, p.maxRate, smoothThrust);
            emission.rateOverTime = currentRate;

            float currentLife = Mathf.Lerp(p.maxLifetime * idleRatio, p.maxLifetime, combinedFactor);
            main.startLifetime = currentLife;
        }
    }
}