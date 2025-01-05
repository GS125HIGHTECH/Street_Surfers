using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    public TMP_Dropdown missionDropdown;
    public GameObject notificationPanel;
    public TMP_Text notificationText;
    public RectTransform toastNotification;

    private readonly List<Mission> activeMissions = new();
    private readonly Queue<string> notificationQueue = new();
    private bool canCloseDropdown = true;
    private bool isProcessingQueue = false;
    private readonly float additionalBoost = 0.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        notificationPanel.SetActive(false);
        GenerateMissions();
        UpdateMissionDropdown();

        var eventTrigger = missionDropdown.gameObject.AddComponent<EventTrigger>();

        var pointerClickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        pointerClickEntry.callback.AddListener((eventData) => HandleDropdownClick());
        eventTrigger.triggers.Add(pointerClickEntry);
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(Locale newLocale)
    {
        UpdateMissionDisplayNames();
        UpdateMissionDropdown();
    }

    public void HandleDropdownClick()
    {
        if (canCloseDropdown && missionDropdown.IsExpanded)
        {
            canCloseDropdown = false; 
            DOVirtual.DelayedCall(4f, () =>
            {
                missionDropdown.Hide();
                canCloseDropdown = true;
            });
        }
    }

    private void GenerateMissions()
    {
        activeMissions.Clear();

        string localizedCollectCoins = LocalizationSettings.StringDatabase.GetLocalizedString("collectCoins");
        string localizedDriveDistance = LocalizationSettings.StringDatabase.GetLocalizedString("driveDistance");
        string localizedCollectSpeedBoosts = LocalizationSettings.StringDatabase.GetLocalizedString("collectSpeedBoosts");
        string localizedChangeLanes = LocalizationSettings.StringDatabase.GetLocalizedString("changeLanes");

        activeMissions.Add(new Mission("Collect Coins", localizedCollectCoins, new[] { 20, 50, 100, 200, 300, 500, 800, 1000, 1200, 1500 }, MissionType.Coins));
        activeMissions.Add(new Mission("Drive Distance", localizedDriveDistance, new[] { 500, 1000, 2000, 5000, 10000, 20000, 50000, 75000, 100000, 150000 }, MissionType.Distance));
        activeMissions.Add(new Mission("Collect Speed Boosts", localizedCollectSpeedBoosts, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, MissionType.SpeedBoost));
        activeMissions.Add(new Mission("Change Lanes", localizedChangeLanes, new[] { 20, 40, 80, 100, 150, 200, 300, 400, 500, 1000 }, MissionType.LaneChange));
    }

    private void UpdateMissionDisplayNames()
    {
        foreach (var mission in activeMissions)
        {
            switch (mission.Type)
            {
                case MissionType.Coins:
                    mission.DisplayedName = LocalizationSettings.StringDatabase.GetLocalizedString("collectCoins");
                    break;
                case MissionType.Distance:
                    mission.DisplayedName = LocalizationSettings.StringDatabase.GetLocalizedString("driveDistance");
                    break;
                case MissionType.SpeedBoost:
                    mission.DisplayedName = LocalizationSettings.StringDatabase.GetLocalizedString("collectSpeedBoosts");
                    break;
                case MissionType.LaneChange:
                    mission.DisplayedName = LocalizationSettings.StringDatabase.GetLocalizedString("changeLanes");
                    break;
            }
        }
    }

    private void UpdateMissionDropdown()
    {
        missionDropdown.ClearOptions();
        List<string> options = new();

        foreach (var mission in activeMissions)
        {
            options.Add(mission.GetDescription());
        }

        missionDropdown.AddOptions(options);

        missionDropdown.RefreshShownValue();
    }

    private void Update()
    {
        if(GameManager.Instance.IsGamePlayable())
        {
            foreach (var mission in activeMissions)
            {
                mission.UpdateProgress();
                if (mission.IsCompleted() && !mission.HasReceivedNotification)
                {
                    CompleteMission(mission);
                }
                UpdateMissionDropdown();
            }
        }
    }

    public async void CompleteMission(Mission mission)
    {
        if (mission.IsCompleted())
        {
            string localizedMissionCompleted = LocalizationSettings.StringDatabase.GetLocalizedString("missionCompleted");
            mission.HasReceivedNotification = true;

            string message = $"{localizedMissionCompleted} {mission.DisplayedName}";
            ShowNotification(message);

            mission.AdvanceToNextGoal();
            CarController.Instance.IncreaseDistanceBoost(additionalBoost);

            await SaveMissionProgress();
        }
    }

    private void ShowNotification(string message)
    {
        notificationQueue.Enqueue(message);

        if (!isProcessingQueue)
        {
            ProcessNotificationQueue();
        }
    }

    private void ProcessNotificationQueue()
    {
        if (notificationQueue.Count == 0)
        {
            isProcessingQueue = false;
            return;
        }

        isProcessingQueue = true;
        string message = notificationQueue.Dequeue();
        notificationPanel.SetActive(true);

        notificationText.text = message;
        toastNotification.gameObject.SetActive(true);

        float screenHeight = Screen.height;
        toastNotification.DOAnchorPos(new Vector2(0, 450), 0.5f)
            .SetEase(Ease.OutBounce)
            .OnComplete(() =>
            {
                DOVirtual.DelayedCall(3f, () =>
                {
                    toastNotification.DOAnchorPos(new Vector2(0, screenHeight + 50), 0.5f)
                        .SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            toastNotification.gameObject.SetActive(false);
                            notificationPanel.SetActive(false);
                            ProcessNotificationQueue();
                        });
                });
            });
    }


    public async Task LoadMissionProgress()
    {
        int completedMissions = 0;

        foreach (var mission in activeMissions)
        {
            string formattedKey = FormatMissionKey(mission.Name);
            int savedLevel = await GameManager.Instance.LoadData($"mission_{formattedKey}_level", 0, true);
            mission.SetCurrentGoalIndex(savedLevel);
            completedMissions += savedLevel;
        }

        float newBoost = completedMissions * additionalBoost;
        CarController.Instance.SetDistanceBoost(newBoost);
    }

    public async Task SaveMissionProgress()
    {
        foreach (var mission in activeMissions)
        {
            string formattedKey = FormatMissionKey(mission.Name);
            await GameManager.Instance.SaveData($"mission_{formattedKey}_level", mission.GetCurrentGoalIndex());
        }
    }

    private string FormatMissionKey(string missionName)
    {
        return missionName.ToLower().Replace(" ", "_");
    }
}

[Serializable]
public class Mission
{
    public string Name;
    public string DisplayedName;
    public int[] Goals;
    private int currentGoalIndex;
    public MissionType Type;
    private long progress;
    private long previousProgress;

    public bool HasReceivedNotification { get; set; }

    public Mission(string name, string displayedName, int[] goals, MissionType type)
    {
        Name = name;
        DisplayedName = displayedName;
        Goals = goals;
        Type = type;
        currentGoalIndex = 0;
        progress = 0;
        previousProgress = 0;
        HasReceivedNotification = false;
    }

    public void UpdateProgress()
    {
        switch (Type)
        {
            case MissionType.Coins:
                progress = (long)GameManager.Instance.GetCurrentCoinCount() - previousProgress;
                break;
            case MissionType.Distance:
                progress = (long)CarController.Instance.GetCurrentDistance() - previousProgress;
                break;
            case MissionType.SpeedBoost:
                progress = GameManager.Instance.GetCurrentSpeedBoostCount() - previousProgress;
                break;
            case MissionType.LaneChange:
                progress = CarController.Instance.GetCurrentLaneChangeCount() - previousProgress;
                break;
        }
    }

    public int GetCurrentGoalIndex()
    {
        return currentGoalIndex;
    }

    public void SetCurrentGoalIndex(int index)
    {
        if (index >= 0 && index < Goals.Length)
        {
            progress = 0;
            previousProgress = 0;
            currentGoalIndex = index;
            HasReceivedNotification = false;
        }
    }

    public void AdvanceToNextGoal()
    {
        if (currentGoalIndex < Goals.Length - 1)
        {
            currentGoalIndex++;
            previousProgress = progress;
            HasReceivedNotification = false;
        }
        else
        {
            Debug.Log($"Mission fully completed: {Name}");
        }
    }

    public string GetDescription()
    {
        return $"{DisplayedName}\n{progress}/{Goals[currentGoalIndex]}";
    }

    public bool IsCompleted()
    {
        return progress >= Goals[currentGoalIndex];
    }
}

public enum MissionType
{
    Coins,
    Distance,
    SpeedBoost,
    LaneChange
}
