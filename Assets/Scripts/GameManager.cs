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
    public GameObject startPanel;
    public GameObject creditsPanel;
    public GameObject leaderboardPanel;
    public GameObject touchScreenText;
    public GameObject profilePanel;
    public GameObject upgradePanel;

    public GameObject roadPrefab;
    public GameObject carPrefab;
    public GameObject coinPrefab;
    public GameObject streetLampPrefab;
    public GameObject roadBlockerPrefab;
    public GameObject mandatoryCarriagewayPrefab;
    public GameObject speedBoostPrefab;

    private GameObject currentCar;

    private CanvasGroup gameOverCanvasGroup;

    private readonly float spawnInterval = 0.2f; 
    private readonly float segmentLength = 20f;
    private readonly float fadeDuration = 1.5f;
    private const float MinDistanceForBoost = 300f;
    private double lastBoostSpawnDistance = 0;
    private readonly int maxSegmentsAhead = 35;

    private readonly Queue<GameObject> roadSegments = new();
    private readonly Queue<GameObject> coins = new();
    private readonly Queue<GameObject> streetLamps = new();
    private readonly Queue<GameObject> roadBlockers = new();
    private readonly Queue<GameObject> mandatoryCarriageways = new();
    private readonly Queue<GameObject> speedBoosts = new();
    private Vector3 nextSpawnPosition;
    private Camera mainCamera;
    private long coinCount = 0;
    private long currentCoinCount = 0;
    private int currentSpeedBoostCount = 0;
    private double bestScore = 0;
    public bool isLoggedIn = false;
    private bool isGamePlayable = false;

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

        nextSpawnPosition = new Vector3(0, 0, -10);
        currentCoinCount = 0;
        currentSpeedBoostCount = 0;
    }

    public async void AddCoin()
    {
        coinCount++;
        currentCoinCount++;

        await SaveData("coins", coinCount);
    }

    public void AddSpeedBoost()
    {
        currentSpeedBoostCount++;
    }

    public async Task<long> GetCoinCountAsync()
    {
        coinCount = await LoadData("coins", coinCount);
        return coinCount;
    }

    public long GetCoinCount()
    {
        return coinCount;
    }

    public float GetCurrentCoinCount()
    {
        return currentCoinCount;
    }

    public int GetCurrentSpeedBoostCount()
    {
        return currentSpeedBoostCount;
    }

    public bool IsGamePlayable()
    {
        return isGamePlayable;
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

    private void Update()
    {
        RemoveOldObjects();
    }

    private async Task InitializeCloudSave()
    {
        coinCount = await LoadData("coins", coinCount);
        bestScore = await LoadData("bestScore", bestScore);
    }

    private async void StartSpawning()
    {
        Time.timeScale = 1;
        menuPanel.SetActive(false);
        startPanel.SetActive(true);
        coinsPanel.SetActive(false);
        if(!isGamePlayable)
        {
            SpawnCar();
        }
        await UpgradeManager.Instance.LoadUpgradeData();
        CarController.Instance.PauseController();

        if (isGamePlayable)
        {
            currentCar.SetActive(isGamePlayable);
            startPanel.SetActive(false);
            coinsPanel.SetActive(true);
            AudioManager.Instance.PlayEngineSound();
            AttachCameraToCar();
            CarController.Instance.ResumeController();
            ResumeGame();

            for (int i = 0; i < maxSegmentsAhead; i++)
            {
                SpawnRoadSegment();
            }

            await MissionManager.Instance.LoadMissionProgress();

            InvokeRepeating(nameof(SpawnRoadSegment), 0.5f, spawnInterval);
        }
    }

    private void SpawnCar()
    {
        Vector3 carStartPosition = new(0, 0, 0);
        Quaternion carRotation = Quaternion.Euler(0, -90, 0);

        currentCar = Instantiate(carPrefab, carStartPosition, carRotation);

        if (currentCar.TryGetComponent<CarController>(out var carController))
        {
            CarController.Instance = carController;
        }

        currentCar.SetActive(isGamePlayable);
    }

    private void AttachCameraToCar()
    {
        if (mainCamera.TryGetComponent<CameraFollow>(out var cameraFollow))
        {
            cameraFollow.target = currentCar.transform;
        }
    }

    private void SpawnRoadSegment()
    {
        if (roadSegments.Count >= maxSegmentsAhead)
        {
            return;
        }

        GameObject segment = Instantiate(roadPrefab, nextSpawnPosition, Quaternion.identity);
        roadSegments.Enqueue(segment);

        if (nextSpawnPosition.z % 3 == 0 && nextSpawnPosition.z > 10 && nextSpawnPosition.z > mainCamera.transform.position.z)
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

        if (mandatoryCarriageways.Count > 0)
        {
            GameObject oldestmandatoryDirectionArrow45Down = mandatoryCarriageways.Peek();

            if (oldestmandatoryDirectionArrow45Down.transform.position.z < mainCamera.transform.position.z - 10)
            {
                mandatoryCarriageways.Dequeue();
                Destroy(oldestmandatoryDirectionArrow45Down);
            }
        }

        if (speedBoosts.Count > 0)
        {
            GameObject oldestSpeedBoost = speedBoosts.Peek();

            if (oldestSpeedBoost == null || oldestSpeedBoost.transform.position.z < mainCamera.transform.position.z - 10)
            {
                speedBoosts.Dequeue();

                if (oldestSpeedBoost != null)
                {
                    Destroy(oldestSpeedBoost);
                }
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
        List<int> spawnedBlockerIndices = new();

        for (int i = 0; i < blockersToSpawn; i++)
        {
            int lineIndex = availableLines[i];
            spawnedBlockerIndices.Add(lineIndex);

            Vector3 blockerPosition = startPosition + new Vector3(linePositions[lineIndex], 0, 0);
            GameObject roadBlocker = Instantiate(roadBlockerPrefab, blockerPosition, Quaternion.Euler(-90, 0, 0));

            roadBlockers.Enqueue(roadBlocker);
            roadBlocker.AddComponent<RoadBlocker>();
        }

        double totalDistance = CarController.Instance.GetTotalDistance();
        double distanceSinceLastBoost = totalDistance - lastBoostSpawnDistance;

        int remainingSpaces = availableLines.Count - blockersToSpawn;

        if (remainingSpaces == 1 && distanceSinceLastBoost >= MinDistanceForBoost)
        {
            Vector3 speedBoostPosition = startPosition + new Vector3(linePositions[availableLines[availableLines.Count - 1]], 1.8f, 0);
            GameObject speedBoost = Instantiate(speedBoostPrefab, speedBoostPosition, Quaternion.identity);
            speedBoosts.Enqueue(speedBoost);

            lastBoostSpawnDistance = totalDistance;
        }

        if (IsSpawnedInRightmostLines(spawnedBlockerIndices, linePositions.Length))
        {
            SpawnMandatoryArrow(startPosition, linePositions[linePositions.Length - 2], true);
        }
        else if (IsSpawnedInLeftmostLines(spawnedBlockerIndices))
        {
            SpawnMandatoryArrow(startPosition, linePositions[1], false);
        }
    }

    private bool IsSpawnedInRightmostLines(List<int> indices, int lineCount)
    {
        return indices.Contains(lineCount - 1) && indices.Contains(lineCount - 2);
    }

    private bool IsSpawnedInLeftmostLines(List<int> indices)
    {
        return indices.Contains(1) && indices.Contains(0);
    }

    private void SpawnMandatoryArrow(Vector3 startPosition, float lineOffset, bool isRight)
    {
        Vector3 arrowPosition = startPosition + new Vector3(lineOffset, 0, 2);
        GameObject mandatoryCarriageway = Instantiate(mandatoryCarriagewayPrefab, arrowPosition, Quaternion.Euler(0, 90, 0));

        Transform roadSignLeft = mandatoryCarriageway.transform.Find("road sign_left");
        if (roadSignLeft != null)
        {
            roadSignLeft.localRotation = Quaternion.Euler(isRight ? 0 : -90, 0, 0);
        }

        mandatoryCarriageways.Enqueue(mandatoryCarriageway);
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

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus && isGamePlayable && !settingsPanel.activeSelf && !creditsPanel.activeSelf)
        {
            PauseGame();
        }
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

    public void ShowCredits()
    {
        menuPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void HideCredits()
    {
        menuPanel.SetActive(true);
        creditsPanel.SetActive(false);
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
        AudioManager.Instance.ResumeAllSounds();
        Time.timeScale = 1;
        AudioManager.Instance.PlayEngineSound();
    }

    public void StartGame()
    {
        isGamePlayable = true;
        StartSpawning();
    }

    public void PauseGame()
    {
        if(!CarController.Instance.isBoostActive && !CarController.Instance.isChangingLane)
        {
            AudioManager.Instance.PlayClickSound();
            ShowMenu();
            AudioManager.Instance.PauseAllSounds();
            Time.timeScale = 0;
            AudioManager.Instance.StopEngineSound();
        }
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

        foreach (var mandatoryCarriageway in mandatoryCarriageways)
        {
            Destroy(mandatoryCarriageway);
        }
        mandatoryCarriageways.Clear();

        foreach (var speedBoost in speedBoosts)
        {
            Destroy(speedBoost);
        }
        speedBoosts.Clear();

        if (currentCar != null)
        {
            Destroy(currentCar);
            currentCar = null;
        }

        nextSpawnPosition = new Vector3(0, 0, -10); ;
        mainCamera.transform.position = nextSpawnPosition;
        isGamePlayable = false;

        CarController.Instance.ResetCurrentDistance();
        CarController.Instance.ResetCurrentLaneChangeCount();

        if (CarController.Instance != null)
        {
            Destroy(CarController.Instance.gameObject);
            CarController.Instance = null;
        }

        CancelInvoke(nameof(SpawnRoadSegment));

        gameOverPanel.SetActive(false);

        currentCoinCount = 0;
        currentSpeedBoostCount = 0;
    }

    public void LogOut()
    {
        HideMenu();
        coinsPanel.SetActive(false);

        coinCount = 0;
        bestScore = 0;

        Application.targetFrameRate = 60;

        RestartGame();

        AuthenticationService.Instance.SignOut();

        AuthenticationManager.Instance.ShowLoginPanel();
    }

    public async void GameOver()
    {
        Time.timeScale = 0;
        AudioManager.Instance.StopSpeedBoostSound();

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

    public async Task SaveData<T>(string key, T value)
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

    public async Task<T> LoadData<T>(string key, T defaultValue)
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
