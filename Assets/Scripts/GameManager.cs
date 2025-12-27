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
    public GameObject controlsPanel;

    public GameObject roadPrefab;
    public GameObject carPrefab;
    public GameObject coinPrefab;
    public GameObject streetLampPrefab;
    public GameObject roadBlockerPrefab;
    public GameObject mandatoryCarriagewayPrefab;
    public GameObject speedBoostPrefab;
    public GameObject grassPrefab;
    public GameObject simplePolyBillboardPrefab;
    public GameObject treePrefab;
    public GameObject rockPrefab;
    public GameObject iceObstaclePrefab;
    public GameObject[] buildingPrefabs;

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
    private readonly Queue<GameObject> grassSegments = new();
    private readonly Queue<GameObject> simplePolyBillboards = new();
    private readonly Queue<GameObject> rocks = new();
    private readonly Queue<GameObject> trees = new();
    private readonly Queue<GameObject> iceObstacles = new();
    private readonly Queue<GameObject> buildings = new();
    private Vector3 nextSpawnPosition;
    private Camera mainCamera;
    private long coinCount = 0;
    private long coinsEarned = 0;
    private long coinsSpent = 0;
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
    }

    public async void AddCoin()
    {
        coinsEarned++;
        currentCoinCount++;

        coinCount = coinsEarned - coinsSpent;

        await SaveData("coinsEarned", coinsEarned);
    }

    public void AddSpeedBoost() => currentSpeedBoostCount++;

    public async Task<long> GetCoinCountAsync()
    {
        coinsEarned = await LoadData("coinsEarned", 0L, true);
        coinsSpent = await LoadData("coinsSpent", 0L, true);
        coinCount = coinsEarned - coinsSpent;

        return coinCount;
    }

    public long GetCoinCount() => coinCount;

    public long GetCoinSpent() => coinsSpent;

    public float GetCurrentCoinCount() => currentCoinCount;

    public int GetCurrentSpeedBoostCount() => currentSpeedBoostCount;

    public bool IsGamePlayable() => isGamePlayable;

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
        await InitializeCloudSave();
    }

    private void Update()
    {
        RemoveOldObjects();
    }

    private async Task InitializeCloudSave()
    {
        currentCoinCount = 0;
        currentSpeedBoostCount = 0;
        lastBoostSpawnDistance = 0;

        bestScore = await LoadData("bestScore", 0.0, true);

        double roundedDistance = Math.Floor(bestScore * 100) / 100;
        LeaderboardManager.Instance.AddScoreToLeaderboard(roundedDistance);

        await GetCoinCountAsync();
        await UpgradeManager.Instance.LoadUpgradeData();
        await MissionManager.Instance.LoadMissionProgress();
    }

    private void StartSpawning()
    {
        Time.timeScale = 1;
        menuPanel.SetActive(false);
        startPanel.SetActive(true);
        touchScreenText.SetActive(true);
        coinsPanel.SetActive(false);

        if(!isGamePlayable)
        {
            SpawnCar();
            CarController.Instance.PauseController();
        }
        else if (isGamePlayable)
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

        GameObject grassSegment = Instantiate(grassPrefab, nextSpawnPosition + new Vector3(82, -0.01f, 0), Quaternion.identity);
        grassSegments.Enqueue(grassSegment);

        GameObject grassSegment2 = Instantiate(grassPrefab, nextSpawnPosition + new Vector3(-82, -0.01f, 0), Quaternion.identity);
        grassSegments.Enqueue(grassSegment2);

        if (nextSpawnPosition.z % 3 == 0 && nextSpawnPosition.z > 10 && nextSpawnPosition.z > mainCamera.transform.position.z)
        {
            SpawnCoins(nextSpawnPosition);
            SpawnStreetLamps(nextSpawnPosition);
            SpawnNature(nextSpawnPosition);
            SpawnBuildings(nextSpawnPosition);
        }

        if (nextSpawnPosition.z % 17 == 0 && nextSpawnPosition.z > 10 && nextSpawnPosition.z > mainCamera.transform.position.z)
        {
            SpawnSimplePolyBillboards(nextSpawnPosition);
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

        if (grassSegments.Count > 0)
        {
            GameObject oldestSegment = grassSegments.Peek();

            if (oldestSegment.transform.position.z < mainCamera.transform.position.z - 10)
            {
                grassSegments.Dequeue();
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

        if (simplePolyBillboards.Count > 0)
        {
            GameObject oldestSimplePolyBillboard = simplePolyBillboards.Peek();

            if (oldestSimplePolyBillboard.transform.position.z < mainCamera.transform.position.z - 10)
            {
                simplePolyBillboards.Dequeue();
                Destroy(oldestSimplePolyBillboard);
            }
        }

        if (trees.Count > 0)
        {
            GameObject oldestTree = trees.Peek();

            if (oldestTree.transform.position.z < mainCamera.transform.position.z - 10)
            {
                trees.Dequeue();
                Destroy(oldestTree);
            }
        }

        if (rocks.Count > 0)
        {
            GameObject oldestRock = rocks.Peek();

            if (oldestRock.transform.position.z < mainCamera.transform.position.z - 10)
            {
                rocks.Dequeue();
                Destroy(oldestRock);
            }
        }

        if (iceObstacles.Count > 0)
        {
            GameObject oldestIceObstacle = iceObstacles.Peek();

            if (oldestIceObstacle == null || oldestIceObstacle.transform.position.z < mainCamera.transform.position.z - 10)
            {
                iceObstacles.Dequeue();
                if (oldestIceObstacle != null)
                {
                    Destroy(oldestIceObstacle);
                }
            }
        }

        if (buildings.Count > 0)
        {
            GameObject oldestBuilding = buildings.Peek();

            if (oldestBuilding.transform.position.z < mainCamera.transform.position.z - 10)
            {
                buildings.Dequeue();
                Destroy(oldestBuilding);
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

        double currentDistance = CarController.Instance.GetBaseDistance();
        double distanceSinceLastBoost = currentDistance - lastBoostSpawnDistance;

        int remainingSpaces = availableLines.Count - blockersToSpawn;

        if (remainingSpaces == 1 && distanceSinceLastBoost >= MinDistanceForBoost)
        {
            Vector3 speedBoostPosition = startPosition + new Vector3(linePositions[availableLines[availableLines.Count - 1]], 1.8f, 0);
            GameObject speedBoost = Instantiate(speedBoostPrefab, speedBoostPosition, Quaternion.identity);
            speedBoosts.Enqueue(speedBoost);

            lastBoostSpawnDistance = currentDistance;
        }
        else if (remainingSpaces == 1)
        {
            Vector3 iceObstaclePosition = startPosition + new Vector3(linePositions[availableLines[availableLines.Count - 1]], 0.01f, 0);
            GameObject iceObstacle = Instantiate(iceObstaclePrefab, iceObstaclePosition, Quaternion.identity);
            iceObstacles.Enqueue(iceObstacle);
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

    private void SpawnSimplePolyBillboards(Vector3 position)
    {
        Vector3 billboardPosition = position + new Vector3(16f, 0, 0);

        GameObject billboard = Instantiate(simplePolyBillboardPrefab, billboardPosition, Quaternion.Euler(0, 180f, 0));

        simplePolyBillboards.Enqueue(billboard);
    }

    private void SpawnNature(Vector3 position)
    {
        Vector3 treePosition = position + new Vector3(18f, 0, 4f + UnityEngine.Random.Range(-2.0f, 2.0f));
        Vector3 leftTreePosition = position + new Vector3(-18f, 0, 4f + UnityEngine.Random.Range(-2.0f, 2.0f));
        Vector3 rockPosition = position + new Vector3(20f, 0, UnityEngine.Random.Range(-2.0f, 2.0f));
        Vector3 leftRockPosition = position + new Vector3(-20f, 0, UnityEngine.Random.Range(-2.0f, 2.0f));
        Vector3 rockBehindPosition = position + new Vector3(20f, 0, 8f + UnityEngine.Random.Range(-2.0f, 2.0f));
        Vector3 leftRockBehindPosition = position + new Vector3(-20f, 0, 8f + UnityEngine.Random.Range(-2.0f, 2.0f));

        GameObject tree = Instantiate(treePrefab, treePosition, Quaternion.identity);
        GameObject leftTree = Instantiate(treePrefab, leftTreePosition, Quaternion.identity);
        GameObject rock = Instantiate(rockPrefab, rockPosition, Quaternion.identity);
        GameObject leftRock = Instantiate(rockPrefab, leftRockPosition, Quaternion.identity);
        GameObject rockBehind = Instantiate(rockPrefab, rockBehindPosition, Quaternion.identity);
        GameObject leftRockBehind = Instantiate(rockPrefab, leftRockBehindPosition, Quaternion.identity);

        trees.Enqueue(tree);
        trees.Enqueue(leftTree);
        rocks.Enqueue(rock);
        rocks.Enqueue(rockBehind);
        rocks.Enqueue(leftRock);
        rocks.Enqueue(leftRockBehind);
    }

    private void SpawnBuildings(Vector3 position)
    {
        Vector3 building1Position = position + new Vector3(31f, 0, -25 + UnityEngine.Random.Range(-3.0f, 3.0f));
        int randomIndex1 = UnityEngine.Random.Range(0, buildingPrefabs.Length);
        GameObject selectedPrefab1 = buildingPrefabs[randomIndex1];

        Vector3 building2Position = position + new Vector3(-31f, 0, -25 + UnityEngine.Random.Range(-3.0f, 3.0f));
        int randomIndex2 = UnityEngine.Random.Range(0, buildingPrefabs.Length);
        GameObject selectedPrefab2 = buildingPrefabs[randomIndex2];

        GameObject building1 = Instantiate(selectedPrefab1, building1Position, Quaternion.Euler(0, 180.0f, 0));
        GameObject building2 = Instantiate(selectedPrefab2, building2Position, Quaternion.Euler(0, 180.0f, 0));

        buildings.Enqueue(building1);
        buildings.Enqueue(building2);
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

    public void ShowControls()
    {
        menuPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void HideControls()
    {
        menuPanel.SetActive(true);
        controlsPanel.SetActive(false);
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

    public void ExitGame()
    {
#if UNITY_STANDALONE_WIN || UNITY_ANDROID
        Application.Quit();
        System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
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
        StartCoroutine(StartGameWithDelay());
    }

    private IEnumerator StartGameWithDelay()
    {
        yield return new WaitForSeconds(0.5f);

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

    public async void RestartGameOver()
    {
        RestartGame();
        StartSpawning();
        await InitializeCloudSave();
    }

    private void RestartGame()
    {
        foreach (var segment in roadSegments)
        {
            Destroy(segment);
        }
        roadSegments.Clear();

        foreach (var segment in grassSegments)
        {
            Destroy(segment);
        }
        grassSegments.Clear();

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

        foreach (var billboard in simplePolyBillboards)
        {
            Destroy(billboard);
        }
        simplePolyBillboards.Clear();

        foreach (var tree in trees)
        {
            Destroy(tree);
        }
        trees.Clear();

        foreach (var rock in rocks)
        {
            Destroy(rock);
        }
        rocks.Clear();

        foreach (var iceObstacle in iceObstacles)
        {
            Destroy(iceObstacle);
        }
        iceObstacles.Clear();

        foreach (var building in buildings)
        {
            Destroy(building);
        }
        buildings.Clear();

        nextSpawnPosition = new Vector3(0, 0, -10); ;
        mainCamera.transform.position = nextSpawnPosition;
        isGamePlayable = false;

        CarController.Instance.ResetCurrentDistance();
        CarController.Instance.ResetCurrentLaneChangeCount();
        CarController.Instance.ResetIsSliding();

        if (CarController.Instance != null)
        {
            Destroy(CarController.Instance.gameObject);
            CarController.Instance = null;
        }

        CancelInvoke(nameof(SpawnRoadSegment));

        gameOverPanel.SetActive(false);
        startPanel.SetActive(false);
        profilePanel.SetActive(false);

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
        AudioManager.Instance.PauseAllSounds();
        Time.timeScale = 0;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            StartCoroutine(FadeIn(gameOverCanvasGroup));
        }

        await SaveBestScoreAsync(CarController.Instance.GetCurrentDistance());

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
        double bestScoreData = await LoadData("bestScore", 0.0);
        double roundedDistance = Math.Floor(currentDistance * 100) / 100;

        if (roundedDistance > bestScoreData)
        {
            await SaveData("bestScore", roundedDistance);
        }

        if(Application.internetReachability != NetworkReachability.NotReachable)
        {
            LeaderboardManager.Instance.AddScoreToLeaderboard(roundedDistance);
        }
    }

    public async Task SaveData<T>(string key, T value)
    {
        try
        {
            string username = AuthenticationManager.Instance.GetUsername();
            SaveToPlayerPrefs(key + username, value);
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                var data = new Dictionary<string, object> { { key, value } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving data for '{key}': {e.Message}");
        }
    }

    private void SaveToPlayerPrefs<T>(string key, T value)
    {
        PlayerPrefs.SetString(key, value.ToString());
        PlayerPrefs.Save();
    }

    public async Task<T> LoadData<T>(string key, T defaultValue, bool overwrite = false)
    {
        string username = AuthenticationManager.Instance.GetUsername();
        string prefsKey = key + username;

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });

            if (playerData.TryGetValue(key, out var dataValue))
            {
                var cloudValue = dataValue.Value.GetAs<T>();

                if (overwrite && PlayerPrefs.HasKey(prefsKey))
                {
                    var prefsValue = LoadFromPlayerPrefs<T>(prefsKey);
                    if (Comparer<T>.Default.Compare(prefsValue, cloudValue) > 0)
                    {
                        await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { key, prefsValue } });
                        return prefsValue;
                    }
                    else if(Comparer<T>.Default.Compare(prefsValue, cloudValue) < 0)
                    {
                        PlayerPrefs.SetString(prefsKey, cloudValue.ToString());
                        PlayerPrefs.Save();
                        return cloudValue;
                    }
                }
                else
                {
                    PlayerPrefs.SetString(prefsKey, cloudValue.ToString());
                    PlayerPrefs.Save();
                }

                return cloudValue;
            }
            else if (PlayerPrefs.HasKey(prefsKey))
            {
                var prefsValue = LoadFromPlayerPrefs<T>(prefsKey);
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { key, prefsValue } });
                return prefsValue;
            }
            else
            {
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { key, defaultValue } });

                PlayerPrefs.SetString(prefsKey, defaultValue.ToString());
                PlayerPrefs.Save();
               
                return defaultValue;
            }
        }
        else
        {
            if (PlayerPrefs.HasKey(prefsKey))
            {
                return LoadFromPlayerPrefs<T>(prefsKey);
            }
            else
            {
                PlayerPrefs.SetString(prefsKey, defaultValue.ToString());
                PlayerPrefs.Save();
                return defaultValue;
            }
        }
    }

    private T LoadFromPlayerPrefs<T>(string key)
    {
        var jsonValue = PlayerPrefs.GetString(key);

        if (string.IsNullOrEmpty(jsonValue))
        {
            return default;
        }

        try
        {
            return (T)Convert.ChangeType(jsonValue, typeof(T));
        }
        catch (InvalidCastException)
        {
            Debug.LogError($"Cannot convert {jsonValue} to type {typeof(T)}");
            return default;
        }
    }
}
