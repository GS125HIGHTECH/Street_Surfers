using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Upgrade UI Elements")]
    public GameObject handlingUpgradeEntry;
    public GameObject boostUpgradeEntry;

    public Button handlingUpgradeEntryButton;
    public Button boostUpgradeEntryButton;

    public TMP_Text coinCountText;

    private int handlingLevel = 0;
    private int boostDurationLevel = 0;

    private long currentCoinCount = 0;

    private const int maxLevel = 6;
    private readonly int[] handlingUpgradeCosts = { 500, 2000, 5000, 10000, 20000, 50000 };
    private readonly int[] boostUpgradeCosts = { 500, 2000, 5000, 10000, 20000, 50000 };

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
        UpdateUpgradeUI();
    }

    public async void UpgradeHandling()
    {
        if (handlingLevel < maxLevel)
        {
            long cost = handlingUpgradeCosts[handlingLevel];
            currentCoinCount = GameManager.Instance.GetCoinCount();

            if (currentCoinCount >= cost)
            {
                currentCoinCount -= cost;
                handlingLevel++;
                CarController.Instance.UpdateHandling(1f - (handlingLevel * 0.1f));
                await SaveUpgradeData();
                UpdateUpgradeUI();
            }
        }
    }

    public async void UpgradeBoostDuration()
    {
        if (boostDurationLevel < maxLevel)
        {
            long cost = boostUpgradeCosts[boostDurationLevel];
            currentCoinCount = GameManager.Instance.GetCoinCount();

            if (currentCoinCount >= cost)
            {
                currentCoinCount -= cost;
                boostDurationLevel++;
                CarController.Instance.UpdateBoostDuration(2f + (boostDurationLevel * 2f));
                await SaveUpgradeData();
                UpdateUpgradeUI();
            }
        }
    }

    private async void UpdateUpgradeUI()
    {
        string localizedHandling = LocalizationSettings.StringDatabase.GetLocalizedString("handlingUpgrade");
        string localizedBoostDuration = LocalizationSettings.StringDatabase.GetLocalizedString("boostDurationUpgrade");

        currentCoinCount = await GameManager.Instance.GetCoinCountAsync();

        coinCountText.text = UIManager.Instance.FormatNumber(currentCoinCount);

        UpdateEntryUI(
            handlingUpgradeEntry,
            localizedHandling,
            handlingLevel < maxLevel ? handlingUpgradeCosts[handlingLevel] : 0,
            handlingLevel < maxLevel
        );

        UpdateEntryUI(
            boostUpgradeEntry,
            localizedBoostDuration,
            boostDurationLevel < maxLevel ? boostUpgradeCosts[boostDurationLevel] : 0,
            boostDurationLevel < maxLevel
        );
    }

    private void UpdateEntryUI(GameObject entry, string upgradeName, int cost, bool isUpgradable)
    {
        TextMeshProUGUI nameText = entry.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI costText = entry.transform.Find("CostText").GetComponent<TextMeshProUGUI>();
        Button upgradeButton = entry.transform.Find("UpgradeButton").GetComponent<Button>();

        if (isUpgradable)
        {
            string localizedCost = LocalizationSettings.StringDatabase.GetLocalizedString("cost");
            nameText.text = upgradeName;
            costText.text = $"{localizedCost} {cost}";

            upgradeButton.interactable = currentCoinCount >= cost;
        }
        else
        {
            string localizedMaxLevel = LocalizationSettings.StringDatabase.GetLocalizedString("maxLevel");

            nameText.text = $"{upgradeName} - {localizedMaxLevel}";
            costText.text = string.Empty;
            upgradeButton.interactable = false;
        }
    }

    public async void ShowUpgrade()
    {
        if (!GameManager.Instance.upgradePanel.activeSelf)
        {
            boostUpgradeEntryButton.interactable = false;
            handlingUpgradeEntryButton.interactable = false;
     
            AudioManager.Instance.PlayClickSound();
            GameManager.Instance.touchScreenText.SetActive(false);
            GameManager.Instance.leaderboardPanel.SetActive(false);
            GameManager.Instance.profilePanel.SetActive(false);
            GameManager.Instance.upgradePanel.SetActive(true);

            await LoadUpgradeData();
        }
    }

    public void HideUpgrade()
    {
        GameManager.Instance.upgradePanel.SetActive(false);
        GameManager.Instance.touchScreenText.SetActive(true);
    }

    public async Task LoadUpgradeData()
    {
        currentCoinCount = await GameManager.Instance.LoadData<long>("coins", 0);

        handlingLevel = await GameManager.Instance.LoadData<int>("handlingLevel", 0);
        boostDurationLevel = await GameManager.Instance.LoadData<int>("boostDurationLevel", 0);

        CarController.Instance.UpdateHandling(1f - (handlingLevel * 0.1f));
        CarController.Instance.UpdateBoostDuration(2f + (boostDurationLevel * 2f));

        UpdateUpgradeUI();
    }

    private async Task SaveUpgradeData()
    {
        await GameManager.Instance.SaveData("coins", currentCoinCount);
        await GameManager.Instance.SaveData("handlingLevel", handlingLevel);
        await GameManager.Instance.SaveData("boostDurationLevel", boostDurationLevel);
    }
}
