using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AsteroidSpawnManager : NetworkBehaviour
{
    [Header("Warmup Spawn")]
    [SerializeField] private bool warmupFillToMaxOnStart = true;
    [SerializeField, Min(1)] private int warmupPerFrame = 10;
    [SerializeField] private float warmupFrameDelay = 0f;

    [Header("Spawn Settings")]
    [SerializeField] private GameObject asteroidPrefab;
    [SerializeField] private int maxAsteroids = 100;
    [SerializeField] private float spawnInterval = 2f;
    
    [Header("Movement Settings")]
    [SerializeField] [Range(1, 20f)]
    private float _minThrustForce = 1f;
    [SerializeField] [Range(1, 20f)]
    private float _maxThrustForce = 5f;
    [SerializeField] [Range(0, 10f)]
    private float _minRotationForce = 1f;
    [SerializeField] [Range(0, 10f)]
    private float _maxRotationForce = 5f;

    [Header("Asteroid Size")]
    [SerializeField] private float minSize = 0.6f;
    [SerializeField] private float maxSize = 2.5f;

    [Header("Center Bias")]
    [SerializeField, Range(0f, 1f)] private float centerBias = 0.65f;
    [SerializeField] private float centerBiasMinRadius = 0f;
    [SerializeField] private float centerBiasMaxRadiusPadding = 5f;

    private List<GameObject> _spawnedAsteroids = new List<GameObject>();
    private float _spawnTimer;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _spawnTimer = spawnInterval;
        if (warmupFillToMaxOnStart)
            StartCoroutine(WarmupFillCoroutine());
    }

    [Server]
    private void Update()
    {
        
    }

	[Server]
	private void FixedUpdate()
	{
		if (!isServer) return;

        _spawnTimer -= Time.deltaTime;
        
        if (_spawnTimer <= 0 && _spawnedAsteroids.Count < maxAsteroids)
        {
            SpawnAsteroid();
            _spawnTimer = spawnInterval;
        }
        
        // Очистка уничтоженных астероидов из списка
        _spawnedAsteroids.RemoveAll(asteroid => asteroid == null);
	}

    [Server]
    private void SpawnAsteroid()
    {
        // Случайная позиция в области спавна
        Vector3 spawnPosition = Random.insideUnitSphere * (BorderConfiguration.borderRadius - 5f);

        GameObject asteroid = Instantiate(asteroidPrefab, spawnPosition, Random.rotation);

        float size = Random.Range(minSize, maxSize);
        var asteroidComp = asteroid.GetComponent<Asteroid>();
        if (asteroidComp != null)
        {
            asteroidComp.SetSize(size);
        }


        // Настройка параметров движения
        SetupAsteroidMovement(asteroid, size);
        
        NetworkServer.Spawn(asteroid);
        _spawnedAsteroids.Add(asteroid);
    }

    [Server]
    private void SetupAsteroidMovement(GameObject asteroid, float size)
    {
        AsteroidMovement movement = asteroid.GetComponent<AsteroidMovement>();
        if (movement == null) return;


        float thrustForce = Random.Range(_minThrustForce, _maxThrustForce);
        float rotationForce = Random.Range(_minRotationForce, _maxRotationForce);

        Vector3 pos = asteroid.transform.position;
        Vector3 toCenter = (-pos);
        if (toCenter.sqrMagnitude < 0.0001f) toCenter = Random.onUnitSphere;
        toCenter.Normalize();

        // Случайное направление движения (в плоскости XZ)
        Vector3 randomDirection = Random.onUnitSphere;
        float maxR = BorderConfiguration.borderRadius - centerBiasMaxRadiusPadding;
        float t = Mathf.Clamp01(pos.magnitude / Mathf.Max(0.0001f, maxR));
        float bias = Mathf.Lerp(centerBias * 0.25f, centerBias, t);
        Vector3 dir = Vector3.Slerp(randomDirection, toCenter, bias).normalized;

        Vector3 force = dir * thrustForce;
            
        // Случайный вращательный момент
        Vector3 torque = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ) * rotationForce;
            
        movement.SetMovementParameters(thrustForce, force, torque);
    }

    [Server]
    public void ClearAllAsteroids()
    {
        foreach (var asteroid in _spawnedAsteroids)
        {
            if (asteroid != null)
                NetworkServer.Destroy(asteroid);
        }
        _spawnedAsteroids.Clear();
    }

    [Server]
    private IEnumerator WarmupFillCoroutine()
    {
        _spawnedAsteroids.RemoveAll(a => a == null);

        while (_spawnedAsteroids.Count < maxAsteroids)
        {
            int left = maxAsteroids - _spawnedAsteroids.Count;
            int batch = Mathf.Min(warmupPerFrame, left);

            for (int i = 0; i < batch; i++)
                SpawnAsteroid();

            if (warmupFrameDelay > 0f)
                yield return new WaitForSeconds(warmupFrameDelay);
            else
                yield return null;
        }

        _spawnTimer = spawnInterval;
    }

    // Визуализация области спавна в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, BorderConfiguration.borderRadius);
    }
}