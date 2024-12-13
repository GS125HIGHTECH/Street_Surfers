using System.Collections;
using System.Collections.Generic;
using Unity.Services.CloudSave;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public Slider fpsLimitSlider;
    public Slider volumeSlider;
    public Button saveButton;
    public Button returnButton;

    public TMP_Text fpsValueText;
    public TMP_Text volumeValueText;

    private const int DefaultFpsLimit = 60;
    private const int DefaultVolume = 100;

    private int fpsLimit = DefaultFpsLimit;
    private int volume = DefaultVolume;

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

        fpsLimitSlider.value = fpsLimit;
        volumeSlider.value = volume;

        LoadSettings();

        Application.targetFrameRate = fpsLimit;
        AudioListener.volume = volume / 100f;

        fpsLimitSlider.onValueChanged.AddListener(OnFpsSliderChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);

        saveButton.onClick.AddListener(SaveSettings);
        returnButton.onClick.AddListener(ReturnToMenu);
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

    private void SaveSettings()
    {
        fpsLimit = Mathf.RoundToInt(fpsLimitSlider.value);
        volume = Mathf.RoundToInt(volumeSlider.value);

        PlayerPrefs.SetInt("FpsLimit", fpsLimit);
        PlayerPrefs.SetInt("Volume", volume);
        PlayerPrefs.Save();

        var settingsData = new Dictionary<string, object>
        {
            { "fpsLimit", fpsLimit },
            { "volume", volume }
        };
        CloudSaveService.Instance.Data.Player.SaveAsync(settingsData);

        Application.targetFrameRate = fpsLimit;
        AudioListener.volume = volume / 100f;

        ReturnToMenu();
    }

    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey("FpsLimit"))
        {
            fpsLimit = PlayerPrefs.GetInt("FpsLimit", DefaultFpsLimit);
            volume = PlayerPrefs.GetInt("Volume", DefaultVolume);
        }

        fpsLimitSlider.value = fpsLimit;
        volumeSlider.value = volume;

        fpsValueText.gameObject.SetActive(false);
        volumeValueText.gameObject.SetActive(false);
    }

    private void ReturnToMenu()
    {
        fpsValueText.gameObject.SetActive(false);
        volumeValueText.gameObject.SetActive(false);
        GameManager.Instance.HideSettings();
    }
}
