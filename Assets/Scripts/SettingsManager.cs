using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.CloudSave;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public Slider fpsLimitSlider;
    public Slider volumeSlider;
    public TMP_Dropdown graphicsQualityDropdown;

    public Button saveButton;
    public Button returnButton;

    public TMP_Text fpsValueText;
    public TMP_Text volumeValueText;

    private const int DefaultFpsLimit = 60;
    private const int DefaultVolume = 100;
    private const int DefaultGraphicsQuality = 0;

    private int fpsLimit = DefaultFpsLimit;
    private int volume = DefaultVolume;
    private int graphicsQuality = DefaultGraphicsQuality;

    private RectTransform fpsValueRect;
    private RectTransform volumeValueRect;

    private void Awake()
    {
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
        fpsValueText.gameObject.SetActive(false);
        volumeValueText.gameObject.SetActive(false);

        fpsValueRect = fpsValueText.GetComponent<RectTransform>();
        volumeValueRect = volumeValueText.GetComponent<RectTransform>();

        fpsLimitSlider.onValueChanged.AddListener(OnFpsSliderChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);

        saveButton.onClick.AddListener(SaveSettings);
        returnButton.onClick.AddListener(ReturnToMenu);

        SetDropdownOptions();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private void OnFpsSliderChanged(float value)
    {
        UpdateFpsValueText();
        UpdateFpsValuePosition();
    }

    private void OnVolumeSliderChanged(float value)
    {
        UpdateVolumeValueText();
        UpdateVolumeValuePosition();
    }

    private void UpdateFpsValueText()
    {
        fpsValueText.text = Mathf.RoundToInt(fpsLimitSlider.value).ToString();
        fpsValueText.gameObject.SetActive(true);
    }

    private void UpdateVolumeValueText()
    {
        volumeValueText.text = Mathf.RoundToInt(volumeSlider.value).ToString();
        volumeValueText.gameObject.SetActive(true);
    }

    private void UpdateFpsValuePosition()
    {
        float sliderWidth = fpsLimitSlider.GetComponent<RectTransform>().rect.width;
        float xPosition = fpsLimitSlider.transform.position.x - sliderWidth / 2 + (sliderWidth * fpsLimitSlider.value / fpsLimitSlider.maxValue) - 40;
        fpsValueRect.position = new Vector3(xPosition, fpsValueRect.position.y, fpsValueRect.position.z);
    }

    private void UpdateVolumeValuePosition()
    {
        float sliderWidth = volumeSlider.GetComponent<RectTransform>().rect.width;
        float xPosition = volumeSlider.transform.position.x - sliderWidth / 2 + (sliderWidth * volumeSlider.value / volumeSlider.maxValue) - 12;
        volumeValueRect.position = new Vector3(xPosition, volumeValueRect.position.y, volumeValueRect.position.z);
    }

    private void SetDropdownOptions()
    {
        graphicsQualityDropdown.options.Clear(); 

        string localizedHigh = LocalizationSettings.StringDatabase.GetLocalizedString("highQuality");
        string localizedMedium = LocalizationSettings.StringDatabase.GetLocalizedString("mediumQuality");
        string localizedLow = LocalizationSettings.StringDatabase.GetLocalizedString("lowQuality");

        graphicsQualityDropdown.options.Add(new TMP_Dropdown.OptionData(localizedHigh));
        graphicsQualityDropdown.options.Add(new TMP_Dropdown.OptionData(localizedMedium));
        graphicsQualityDropdown.options.Add(new TMP_Dropdown.OptionData(localizedLow));

        graphicsQualityDropdown.RefreshShownValue(); 
    }

    private void OnLanguageChanged(Locale newLocale)
    {
        SetDropdownOptions();
    }

    private void SaveSettings()
    {
        fpsLimit = Mathf.RoundToInt(fpsLimitSlider.value);
        volume = Mathf.RoundToInt(volumeSlider.value);
        graphicsQuality = graphicsQualityDropdown.value;

        PlayerPrefs.SetInt("FpsLimit", fpsLimit);
        PlayerPrefs.SetInt("Volume", volume);
        PlayerPrefs.SetInt("GraphicsQuality", graphicsQuality);
        PlayerPrefs.Save();

        var settingsData = new Dictionary<string, object>
        {
            { "fpsLimit", fpsLimit },
            { "volume", volume },
            { "graphicsQuality", graphicsQuality }
        };
        CloudSaveService.Instance.Data.Player.SaveAsync(settingsData);

        Application.targetFrameRate = fpsLimit;
        AudioListener.volume = volume / 100f;
        QualitySettings.SetQualityLevel(graphicsQuality, true);

        ReturnToMenu();
    }

    public async void LoadSettings()
    {
        var settingsData = await LoadSettingsFromCloud();

        if (settingsData != null)
        {
            fpsLimit = settingsData["fpsLimit"];
            volume = settingsData["volume"];
            graphicsQuality = settingsData["graphicsQuality"];
        }
        else
        {
            if (PlayerPrefs.HasKey("FpsLimit") && PlayerPrefs.HasKey("Volume") && PlayerPrefs.HasKey("GraphicsQuality"))
            {
                fpsLimit = PlayerPrefs.GetInt("FpsLimit", DefaultFpsLimit);
                volume = PlayerPrefs.GetInt("Volume", DefaultVolume);
                graphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", DefaultGraphicsQuality);
            }
        }

        fpsLimitSlider.value = fpsLimit;
        volumeSlider.value = volume;
        graphicsQualityDropdown.value = graphicsQuality;

        Application.targetFrameRate = fpsLimit;
        AudioListener.volume = volume / 100f;
        QualitySettings.SetQualityLevel(graphicsQuality, true);

        fpsValueText.gameObject.SetActive(false);
        volumeValueText.gameObject.SetActive(false);
    }

    private async Task<Dictionary<string, int>> LoadSettingsFromCloud()
    {
        try
        {
            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "fpsLimit", "volume", "graphicsQuality" });
            if (playerData.TryGetValue("fpsLimit", out var fpsValue) && playerData.TryGetValue("volume", out var volumeValue) && playerData.TryGetValue("graphicsQuality", out var graphicsQualityValue))
            {
                return new Dictionary<string, int>
                {
                    { "fpsLimit", fpsValue.Value.GetAs<int>() },
                    { "volume", volumeValue.Value.GetAs<int>() },
                    { "graphicsQuality", graphicsQualityValue.Value.GetAs<int>() }
                };
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading cloud settings: {e.Message}");
            return null;
        }
    }

    private void ReturnToMenu()
    {
        fpsValueText.gameObject.SetActive(false);
        volumeValueText.gameObject.SetActive(false);
        GameManager.Instance.HideSettings();
    }
}
