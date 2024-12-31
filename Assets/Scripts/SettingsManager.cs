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
    public TMP_Dropdown languageDropdown;

    public Button saveButton;
    public Button returnButton;

    public TMP_Text fpsValueText;
    public TMP_Text volumeValueText;

    private const int DefaultFpsLimit = 60;
    private const int DefaultVolume = 100;
    private const int DefaultGraphicsQuality = 0;
    private const int DefaultLanguage = 0;

    private int fpsLimit = DefaultFpsLimit;
    private int volume = DefaultVolume;
    private int graphicsQuality = DefaultGraphicsQuality;
    private int language = DefaultLanguage;

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
        SetLanguageDropdownValue();
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
        UpdateValueText(fpsLimitSlider, fpsValueText);
        UpdateValuePosition(fpsLimitSlider, fpsValueRect, -40);
    }

    private void OnVolumeSliderChanged(float value)
    {
        UpdateValueText(volumeSlider, volumeValueText);
        UpdateValuePosition(volumeSlider, volumeValueRect, -12);
    }

    private void UpdateValueText(Slider slider, TMP_Text text)
    {
        text.text = Mathf.RoundToInt(slider.value).ToString();
        text.gameObject.SetActive(true);
    }

    private void UpdateValuePosition(Slider slider, RectTransform rect, float offset)
    {
        float sliderWidth = slider.GetComponent<RectTransform>().rect.width;
        float xPosition = slider.transform.position.x - sliderWidth / 2 + (sliderWidth * slider.value / slider.maxValue) + offset;
        rect.position = new Vector3(xPosition, rect.position.y, rect.position.z);
    }

    private void SetLanguageDropdownValue()
    {
        string systemLanguage = LocalizationSettings.SelectedLocale.Identifier.Code;
        if (systemLanguage == "pl")
        {
            language = 1;
            languageDropdown.value = 1;
        }
        else
        {
            language = 0;
            languageDropdown.value = 0;
        }
    }

    private void SetDropdownOptions()
    {
        graphicsQualityDropdown.options.Clear();
        languageDropdown.options.Clear();

        string localizedHigh = LocalizationSettings.StringDatabase.GetLocalizedString("highQuality");
        string localizedMedium = LocalizationSettings.StringDatabase.GetLocalizedString("mediumQuality");
        string localizedLow = LocalizationSettings.StringDatabase.GetLocalizedString("lowQuality");

        string localizedPolish = LocalizationSettings.StringDatabase.GetLocalizedString("polish");
        string localizedEnglish = LocalizationSettings.StringDatabase.GetLocalizedString("english");

        graphicsQualityDropdown.options.Add(new TMP_Dropdown.OptionData(localizedHigh));
        graphicsQualityDropdown.options.Add(new TMP_Dropdown.OptionData(localizedMedium));
        graphicsQualityDropdown.options.Add(new TMP_Dropdown.OptionData(localizedLow));

        graphicsQualityDropdown.RefreshShownValue();

        languageDropdown.options.Add(new TMP_Dropdown.OptionData(localizedEnglish));
        languageDropdown.options.Add(new TMP_Dropdown.OptionData(localizedPolish));

        languageDropdown.RefreshShownValue();
    }

    private void OnLanguageChanged(Locale newLocale)
    {
        Locale selectedLocale = language == 0 ? LocalizationSettings.AvailableLocales.Locales[0] : LocalizationSettings.AvailableLocales.Locales[1];
        LocalizationSettings.SelectedLocale = selectedLocale;

        SetDropdownOptions();
    }

    private void SaveSettings()
    {
        fpsLimit = Mathf.RoundToInt(fpsLimitSlider.value);
        volume = Mathf.RoundToInt(volumeSlider.value);
        graphicsQuality = graphicsQualityDropdown.value;
        language = languageDropdown.value;

        PlayerPrefs.SetInt("fpsLimit", fpsLimit);
        PlayerPrefs.SetInt("volume", volume);
        PlayerPrefs.SetInt("graphicsQuality", graphicsQuality);
        PlayerPrefs.SetInt("language", language);
        PlayerPrefs.Save();

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            var settingsData = new Dictionary<string, object>
            {
                { "fpsLimit", fpsLimit },
                { "volume", volume },
                { "graphicsQuality", graphicsQuality },
                { "language", language },
            };
            CloudSaveService.Instance.Data.Player.SaveAsync(settingsData);
        }

        Application.targetFrameRate = fpsLimit;
        AudioListener.volume = volume / 100f;
        QualitySettings.SetQualityLevel(graphicsQuality, true);
        Locale selectedLocale = language == 0 ? LocalizationSettings.AvailableLocales.Locales[0] : LocalizationSettings.AvailableLocales.Locales[1];
        LocalizationSettings.SelectedLocale = selectedLocale;

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
            language = settingsData["language"];
        }
        else
        {
            if (PlayerPrefs.HasKey("fpsLimit") && PlayerPrefs.HasKey("volume") && PlayerPrefs.HasKey("graphicsQuality") && PlayerPrefs.HasKey("language"))
            {
                fpsLimit = PlayerPrefs.GetInt("fpsLimit", DefaultFpsLimit);
                volume = PlayerPrefs.GetInt("volume", DefaultVolume);
                graphicsQuality = PlayerPrefs.GetInt("graphicsQuality", DefaultGraphicsQuality);
                language = PlayerPrefs.GetInt("language", DefaultLanguage);
            }
        }

        fpsLimitSlider.value = fpsLimit;
        volumeSlider.value = volume;
        graphicsQualityDropdown.value = graphicsQuality;
        languageDropdown.value = language;

        Application.targetFrameRate = fpsLimit;
        AudioListener.volume = volume / 100f;
        QualitySettings.SetQualityLevel(graphicsQuality, true);
        Locale selectedLocale = language == 0 ? LocalizationSettings.AvailableLocales.Locales[0] : LocalizationSettings.AvailableLocales.Locales[1];
        LocalizationSettings.SelectedLocale = selectedLocale;

        fpsValueText.gameObject.SetActive(false);
        volumeValueText.gameObject.SetActive(false);
    }

    private async Task<Dictionary<string, int>> LoadSettingsFromCloud()
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            try
            {
                var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "fpsLimit", "volume", "graphicsQuality", "language" });
                if (playerData.TryGetValue("fpsLimit", out var fpsValue) && playerData.TryGetValue("volume", out var volumeValue) && playerData.TryGetValue("graphicsQuality", out var graphicsQualityValue) && playerData.TryGetValue("language", out var languageValue))
                {
                    return new Dictionary<string, int>
                {
                    { "fpsLimit", fpsValue.Value.GetAs<int>() },
                    { "volume", volumeValue.Value.GetAs<int>() },
                    { "graphicsQuality", graphicsQualityValue.Value.GetAs<int>() },
                    { "language", languageValue.Value.GetAs<int>() }
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
        else
        {
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
