using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject roadPrefab;
    public GameObject carPrefab;

    public float spawnInterval = 5f; 
    public float segmentLength = 2f; 

    private Vector3 nextSpawnPosition;
    private bool canSpawn = false;

    private void OnEnable()
    {
        AuthenticationManager.OnGameShown += StartSpawning;
    }

    private void OnDisable()
    {
        AuthenticationManager.OnGameShown -= StartSpawning;
    }

    private void StartSpawning()
    {
        SpawnCar();

        canSpawn = true;
        InvokeRepeating(nameof(SpawnRoadSegment), 0f, spawnInterval);
    }

    private void SpawnCar()
    {
        Vector3 carStartPosition = new(0, 0, 0);
        Quaternion carRotation = Quaternion.Euler(0, -90, 0);

        GameObject car = Instantiate(carPrefab, carStartPosition, carRotation);

        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.target = car.transform;
        }
    }

    private void SpawnRoadSegment()
    {
        if (!canSpawn) return;

        Instantiate(roadPrefab, nextSpawnPosition, Quaternion.identity);
        nextSpawnPosition += new Vector3(0, 0, segmentLength);
    }
}
