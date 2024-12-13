using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject coinsPanel;
    public GameObject menuPanel;

    public GameObject roadPrefab;
    public GameObject carPrefab;
    public GameObject coinPrefab;
    public GameObject streetLampPrefab;

    private GameObject currentCar;

    private readonly float spawnInterval = 0.1f; 
    private readonly float segmentLength = 20f;

    private readonly int maxSegmentsAhead = 40;

    private readonly Queue<GameObject> roadSegments = new();
    private readonly Queue<GameObject> coins = new();
    private readonly Queue<GameObject> streetLamps = new();
    private Vector3 nextSpawnPosition;
    private Camera mainCamera;

    private int coinCount = 0;
    public bool isLoggedIn = false;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        mainCamera = Camera.main;

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async void AddCoin()
    {
        coinCount++;

        var data = new Dictionary<string, object> { { "coins", coinCount } };
        await CloudSaveService.Instance.Data.Player.SaveAsync(data);

        Debug.Log($"Coins: {coinCount}");
    }

    public int GetCoinCount()
    {
        return coinCount;
    }


    private void OnEnable()
    {
        AuthenticationManager.OnGameShown += StartSpawning;
        AuthenticationManager.OnLoggedIn += OnLoggedIn;
    }

    private void OnDisable()
    {
        AuthenticationManager.OnGameShown -= StartSpawning;
        AuthenticationManager.OnLoggedIn -= OnLoggedIn;
    }

    private async void OnLoggedIn()
    {
        isLoggedIn = true;

        AudioManager.Instance.PlayEngineSound();

        try
        {
            await InitializeCloudSave();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error initializing Unity Services: {e.Message}");
        }
    }

    private async Task InitializeCloudSave()
    {
        try
        {
            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "coins" });

            if (playerData.TryGetValue("coins", out var coinsData))
            {
                coinCount = coinsData.Value.GetAs<int>();
                Debug.Log($"Coins loaded: {coinCount}");
            }
            else
            {
                Debug.Log("The 'coins' data could not be found. Initializing with default value.");

                coinCount = 0;
                var defaultData = new Dictionary<string, object> { { "coins", coinCount } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(defaultData);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading cloud save data: {e.Message}");
        }
    }


    private void StartSpawning()
    {
        ResumeGame();
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

        currentCar = Instantiate(carPrefab, carStartPosition, carRotation);

        if (mainCamera.TryGetComponent<CameraFollow>(out var cameraFollow))
        {
            cameraFollow.target = currentCar.transform;
        }
    }

    private void SpawnRoadSegment()
    {
        RemoveOldObjects();

        if (roadSegments.Count >= maxSegmentsAhead)
        {
            return;
        }

        GameObject segment = Instantiate(roadPrefab, nextSpawnPosition, Quaternion.identity);
        roadSegments.Enqueue(segment);

        if (nextSpawnPosition.z % 3 == 0 && nextSpawnPosition.z > 10)
        {
            SpawnCoins(nextSpawnPosition);
            SpawnStreetLamps(nextSpawnPosition);
        }

        nextSpawnPosition += new Vector3(0, 0, segmentLength);
    }

    private void RemoveOldObjects()
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

        if (coins.Count > 0)
        {
            GameObject oldestCoin = coins.Peek();

            if (oldestCoin == null || oldestCoin.transform.position.z < mainCamera.transform.position.z - 10)
            {
                coins.Dequeue();
                if (oldestCoin != null)
                {
                    Destroy(oldestCoin);
                }
            }
        }

        if (streetLamps.Count > 0)
        {
            GameObject oldestLamp = streetLamps.Peek();

            if (oldestLamp.transform.position.z < mainCamera.transform.position.z - 10)
            {
                streetLamps.Dequeue();
                Destroy(oldestLamp);
            }
        }
    }

    private void SpawnCoins(Vector3 startPosition)
    {
        int numColumns = UnityEngine.Random.Range(0, 3);

        int[] linePositions = { -8, 0, 8 };
        List<int> selectedLines = new();

        while (selectedLines.Count < numColumns)
        {
            int randomLine = linePositions[UnityEngine.Random.Range(0, linePositions.Length)];
            if (!selectedLines.Contains(randomLine))
            {
                selectedLines.Add(randomLine);
            }
        }

        foreach (int line in selectedLines)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector3 coinPosition = startPosition + new Vector3(line, 1.3f, i * ( segmentLength / 4.0f));
                GameObject coin = Instantiate(coinPrefab, coinPosition, Quaternion.Euler(0, 0, 0));

                coins.Enqueue(coin);
                coin.AddComponent<Coin>();
            }
        }
    }

    private void SpawnStreetLamps(Vector3 position)
    {
        Vector3 leftLampPosition = position + new Vector3(-11.95f, 0, 0);
        Vector3 rightLampPosition = position + new Vector3(11.95f, 0, 0);

        GameObject leftLamp = Instantiate(streetLampPrefab, leftLampPosition, Quaternion.Euler(0, 90f, 0));
        GameObject rightLamp = Instantiate(streetLampPrefab, rightLampPosition, Quaternion.Euler(0, -90f, 0));

        streetLamps.Enqueue(leftLamp);
        streetLamps.Enqueue(rightLamp);
    }

    private void ShowMenu()
    {
        menuPanel.SetActive(true);
    }

    private void HideMenu()
    {
        menuPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        HideMenu();
        Time.timeScale = 1;
        AudioManager.Instance.PlayEngineSound();
    }

    public void PauseGame()
    {
        ShowMenu();
        Time.timeScale = 0;
        AudioManager.Instance.StopEngineSound();
    }

    public void LogOut()
    {
        HideMenu();
        coinsPanel.SetActive(false);

        coinCount = 0;

        foreach (var segment in roadSegments)
        {
            Destroy(segment);
        }
        roadSegments.Clear();

        foreach (var coin in coins)
        {
            Destroy(coin);
        }
        coins.Clear();

        foreach (var lamp in streetLamps)
        {
            Destroy(lamp);
        }
        streetLamps.Clear();

        if (currentCar != null)
        {
            Destroy(currentCar);
            currentCar = null;
        }

        nextSpawnPosition = Vector3.zero;
        mainCamera.transform.position = nextSpawnPosition;

        CancelInvoke(nameof(SpawnRoadSegment));
        AudioManager.Instance.StopEngineSound();

        AuthenticationService.Instance.SignOut();

        AuthenticationManager.Instance.ShowLoginPanel();
    }
}
