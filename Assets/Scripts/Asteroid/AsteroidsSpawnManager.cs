using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class AsteroidSpawnManager : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject asteroidPrefab;
    [SerializeField] private int maxAsteroids = 50;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private Vector3 spawnArea = new Vector3(50, 50, 50);
    
    [Header("Movement Settings")]
    [SerializeField] [Range(1, 20f)]
    private float _minThrustForce = 1f;
    [SerializeField] [Range(1, 20f)]
    private float _maxThrustForce = 5f;
    [SerializeField] [Range(0, 10f)]
    private float _minRotationForce = 1f;
    [SerializeField] [Range(0, 10f)]
    private float _maxRotationForce = 5f;

    private List<GameObject> _spawnedAsteroids = new List<GameObject>();
    private float _spawnTimer;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _spawnTimer = spawnInterval;
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
        Vector3 spawnPosition = new Vector3(
            Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
            Random.Range(-spawnArea.y / 2, spawnArea.y / 2),
            Random.Range(-spawnArea.z / 2, spawnArea.z / 2)
        );

        GameObject asteroid = Instantiate(asteroidPrefab, spawnPosition, Random.rotation);
        
        // Настройка параметров движения
        SetupAsteroidMovement(asteroid);
        
        NetworkServer.Spawn(asteroid);
        _spawnedAsteroids.Add(asteroid);
    }

    [Server]
    private void SetupAsteroidMovement(GameObject asteroid)
    {
        AsteroidMovement movement = asteroid.GetComponent<AsteroidMovement>();
        if (movement != null)
        {
            float thrustForce = Random.Range(_minThrustForce, _maxThrustForce);
            float rotationForce = Random.Range(_minRotationForce, _maxRotationForce);
            
            // Случайное направление движения (в плоскости XZ)
            Vector3 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 force = new Vector3(randomDirection.x, 0, randomDirection.y) * thrustForce;
            
            // Случайный вращательный момент
            Vector3 torque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ) * rotationForce;
            
            movement.SetMovementParameters(thrustForce, force, torque);
        }
    }

    [Server]
    public void SpawnAsteroidAtPosition(Vector3 position, Quaternion rotation)
    {
        if (_spawnedAsteroids.Count >= maxAsteroids) return;

        GameObject asteroid = Instantiate(asteroidPrefab, position, rotation);
        SetupAsteroidMovement(asteroid);
        NetworkServer.Spawn(asteroid);
        _spawnedAsteroids.Add(asteroid);
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

    // Визуализация области спавна в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, spawnArea);
    }
}