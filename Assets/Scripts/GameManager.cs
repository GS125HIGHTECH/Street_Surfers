using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject coinsPanel;
    public GameObject menuPanel;
    public GameObject settingsPanel;
    public GameObject gameOverPanel;

    public GameObject roadPrefab;
    public GameObject carPrefab;
    public GameObject coinPrefab;
    public GameObject streetLampPrefab;
    public GameObject roadBlockerPrefab;

    private GameObject currentCar;

    private CanvasGroup gameOverCanvasGroup;

    private readonly float spawnInterval = 0.1f; 
    private readonly float segmentLength = 20f;
    private readonly float fadeDuration = 1.5f;

    private readonly int maxSegmentsAhead = 40;

    private readonly Queue<GameObject> roadSegments = new();
    private readonly Queue<GameObject> coins = new();
    private readonly Queue<GameObject> streetLamps = new();
    private readonly Queue<GameObject> roadBlockers = new();
    private Vector3 nextSpawnPosition;
    private Camera mainCamera;

    private long coinCount = 0;
    private double bestScore = 0;
    public bool isLoggedIn = false;

    private void Awake()
    {
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

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (gameOverCanvasGroup == null)
            {
                gameOverCanvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
            }
            gameOverCanvasGroup.alpha = 0f;
            gameOverPanel.SetActive(false);
        }
    }

    public async void AddCoin()
    {
        coinCount++;

        await SaveData("coins", coinCount);
    }

    public long GetCoinCount()
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

        SettingsManager.Instance.LoadSettings();

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
        coinCount = await LoadData("coins", coinCount);
        bestScore = await LoadData("bestScore", bestScore);
    }

    private void StartSpawning()
    {
        SpawnCar();
        ResumeGame();

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

        if (currentCar.TryGetComponent<CarController>(out var carController))
        {
            CarController.Instance = carController;
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

        if (roadBlockers.Count > 0)
        {
            GameObject oldestRoadBlocker = roadBlockers.Peek();

            if (oldestRoadBlocker.transform.position.z < mainCamera.transform.position.z - 10)
            {
                roadBlockers.Dequeue();
                Destroy(oldestRoadBlocker);
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

        bool[] isCoinInLine = new bool[linePositions.Length];

        foreach (int line in selectedLines)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector3 coinPosition = startPosition + new Vector3(line, 1.3f, i * ( segmentLength / 4.0f));
                GameObject coin = Instantiate(coinPrefab, coinPosition, Quaternion.Euler(0, 0, 0));

                coins.Enqueue(coin);
                coin.AddComponent<Coin>();
            }

            int lineIndex = Array.IndexOf(linePositions, line);
            if (lineIndex != -1)
            {
                isCoinInLine[lineIndex] = true;
            }
        }

        if (UnityEngine.Random.value < 0.6f)
        {
            SpawnRoadBlockers(startPosition, linePositions, isCoinInLine);
        }
    }

    private void SpawnRoadBlockers(Vector3 startPosition, int[] linePositions, bool[] isCoinInLine)
    {
        List<int> availableLines = new();

        for (int i = 0; i < linePositions.Length; i++)
        {
            if (!isCoinInLine[i])
            {
                availableLines.Add(i);
            }
        }

        for (int i = 0; i < availableLines.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableLines.Count);
            (availableLines[i], availableLines[randomIndex]) = (availableLines[randomIndex], availableLines[i]);
        }

        int blockersToSpawn = Mathf.Min(2, availableLines.Count);
        for (int i = 0; i < blockersToSpawn; i++)
        {
            int lineIndex = availableLines[i];
            Vector3 blockerPosition = startPosition + new Vector3(linePositions[lineIndex], 0, 0);
            GameObject roadBlocker = Instantiate(roadBlockerPrefab, blockerPosition, Quaternion.Euler(-90, 0, 0));

            roadBlockers.Enqueue(roadBlocker);
            roadBlocker.AddComponent<RoadBlocker>();
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

    public void ShowSettings()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void HideSettings()
    {
        menuPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    private void ShowMenu()
    {
        menuPanel.SetActive(true);
        coinsPanel.SetActive(false);
    }

    private void HideMenu()
    {
        menuPanel.SetActive(false);
        coinsPanel.SetActive(true);
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

    public void RestartGameOver()
    {
        RestartGame();
        StartSpawning();
    }

    private void RestartGame()
    {
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

        foreach (var roadBlocker in roadBlockers)
        {
            Destroy(roadBlocker);
        }
        roadBlockers.Clear();

        if (currentCar != null)
        {
            Destroy(currentCar);
            currentCar = null;
        }

        nextSpawnPosition = Vector3.zero;
        mainCamera.transform.position = nextSpawnPosition;

        if (CarController.Instance != null)
        {
            Destroy(CarController.Instance.gameObject);
            CarController.Instance = null;
        }

        CancelInvoke(nameof(SpawnRoadSegment));

        gameOverPanel.SetActive(false);
    }

    public void LogOut()
    {
        HideMenu();
        coinsPanel.SetActive(false);

        coinCount = 0;
        bestScore = 0;

        Application.targetFrameRate = 60;

        RestartGame();

        AudioManager.Instance.StopEngineSound();

        AuthenticationService.Instance.SignOut();

        AuthenticationManager.Instance.ShowLoginPanel();
    }

    public async void GameOver()
    {
        Time.timeScale = 0;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            StartCoroutine(FadeIn(gameOverCanvasGroup));
        }

        await SaveBestScoreAsync(CarController.Instance.GetTotalDistance());

        coinsPanel.SetActive(false);
    }

    private IEnumerator FadeIn(CanvasGroup canvasGroup)
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private async Task SaveBestScoreAsync(double currentDistance)
    {
        double bestScoreData = await LoadData("bestScore", bestScore);

        if (currentDistance > bestScoreData)
        {
            double roundedDistance = Math.Floor(currentDistance * 100) / 100;

            await SaveData("bestScore", roundedDistance);
            LeaderboardManager.Instance.AddScoreToLeaderboard(roundedDistance);

            bestScore = roundedDistance;
        }
    }

    private async Task SaveData<T>(string key, T value)
    {
        try
        {
            var data = new Dictionary<string, object> { { key, value } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving data for '{key}': {e.Message}");
        }
    }

    private async Task<T> LoadData<T>(string key, T defaultValue)
    {
        try
        {
            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });

            if (playerData.TryGetValue(key, out var dataValue))
            {
                return dataValue.Value.GetAs<T>();
            }
            else
            {
                Debug.Log($"The '{key}' data could not be found. Initializing with default value.");

                var defaultData = new Dictionary<string, object> { { key, defaultValue } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(defaultData);
                return defaultValue;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading cloud save data for key '{key}': {e.Message}");
            return defaultValue;
        }
    }
}
