using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject roadPrefab;
    public GameObject carPrefab;

    public float spawnInterval = 10f; 
    public float segmentLength = 2f;

    public int maxSegmentsAhead = 100;
    public float maxVisibleDistance = 200f;

    private readonly Queue<GameObject> roadSegments = new();
    private Vector3 nextSpawnPosition;
    private Camera mainCamera;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        mainCamera = Camera.main;
    }

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

        nextSpawnPosition = new Vector3(0, 0, -10);

        for (int i = 0; i < maxSegmentsAhead; i++)
        {
            SpawnRoadSegment();
        }

        InvokeRepeating(nameof(SpawnRoadSegment), 0f, spawnInterval);
    }

    private void SpawnCar()
    {
        Vector3 carStartPosition = new(0, 0, 0);
        Quaternion carRotation = Quaternion.Euler(0, -90, 0);

        GameObject car = Instantiate(carPrefab, carStartPosition, carRotation);

        if (mainCamera.TryGetComponent<CameraFollow>(out var cameraFollow))
        {
            cameraFollow.target = car.transform;
        }
    }

    private void SpawnRoadSegment()
    {
        RemoveOldSegments();

        if (roadSegments.Count >= maxSegmentsAhead || (nextSpawnPosition.z - mainCamera.transform.position.z) > maxVisibleDistance)
        {
            return;
        }

        GameObject segment = Instantiate(roadPrefab, nextSpawnPosition, Quaternion.identity);
        roadSegments.Enqueue(segment);

        nextSpawnPosition += new Vector3(0, 0, segmentLength);
    }

    private void RemoveOldSegments()
    {
         if (roadSegments.Count > 0)
        {
            GameObject oldestSegment = roadSegments.Peek();

            if (oldestSegment.transform.position.z < mainCamera.transform.position.z - 10)
            {
                roadSegments.Dequeue();
                Destroy(oldestSegment);
            }
        }
    }
}
