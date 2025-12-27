using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class AuthenticationManager : MonoBehaviour
{
    public static AuthenticationManager Instance;

    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("Login Fields")]
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;
    public Button loginButton;
    public PasswordToggle loginPasswordToggle;

    [Header("Register Fields")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField repeatPasswordInput;
    public Button registerButton;
    public PasswordToggle registerPasswordToggle;
    public PasswordToggle repeatPasswordToggle;

    [Header("Switch Texts")]
    public TextMeshProUGUI switchToRegisterText;
    public TextMeshProUGUI switchToLoginText;

    [Header("Error Messages")]
    public TextMeshProUGUI loginErrorText;
    public TextMeshProUGUI registerErrorText;

    [Header("Checkbox")]
    public Toggle saveLoginToggle;
    public Toggle saveRegisterToggle;

    public static event Action OnGameShown;
    public static event Action OnLoggedIn;

    private readonly Dictionary<Outline, Coroutine> activeOutlineCoroutines = new();
    private readonly Dictionary<TextMeshProUGUI, Coroutine> activeTextCoroutines = new();


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

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        ShowLoginPanel();

        loginButton.onClick.AddListener(HandleLogin);
        registerButton.onClick.AddListener(HandleRegister);

        switchToRegisterText.GetComponent<Button>().onClick.AddListener(ShowRegisterPanel);
        switchToLoginText.GetComponent<Button>().onClick.AddListener(ShowLoginPanel);

        GameManager.Instance.coinsPanel.SetActive(false);
        GameManager.Instance.menuPanel.SetActive(false);
        GameManager.Instance.settingsPanel.SetActive(false);
        GameManager.Instance.gameOverPanel.SetActive(false);
        GameManager.Instance.startPanel.SetActive(false);
        GameManager.Instance.creditsPanel.SetActive(false);
        GameManager.Instance.leaderboardPanel.SetActive(false);
        GameManager.Instance.profilePanel.SetActive(false);
        GameManager.Instance.upgradePanel.SetActive(false);
        GameManager.Instance.controlsPanel.SetActive(false);

        LoadSavedLoginData();
    }

    public void ShowLoginPanel()
    {
        loginButton.enabled = true;
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);

        Time.timeScale = 1;

        loginPasswordToggle.ResetPasswordVisibility();
    }

    private void ShowRegisterPanel()
    {
        registerButton.enabled = true;
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);

        registerPasswordToggle.ResetPasswordVisibility();
        repeatPasswordToggle.ResetPasswordVisibility();
    }

    private void ShowGame()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);

        loginPasswordToggle.ResetPasswordVisibility();
        registerPasswordToggle.ResetPasswordVisibility();
        repeatPasswordToggle.ResetPasswordVisibility();

        OnGameShown?.Invoke();
        OnLoggedIn?.Invoke();
    }

    private void HighlightInputField(TMP_InputField inputField, Color color)
    {
        if (inputField.TryGetComponent<Outline>(out var outline))
        {
            outline.effectColor = color;
            outline.enabled = true;

            if (activeOutlineCoroutines.ContainsKey(outline))
            {
                StopCoroutine(activeOutlineCoroutines[outline]);
                activeOutlineCoroutines.Remove(outline);
            }

            Coroutine newCoroutine = StartCoroutine(RemoveHighlightCoroutine(outline, 5f));
            activeOutlineCoroutines[outline] = newCoroutine;
        }
    }

    private IEnumerator RemoveHighlightCoroutine(Outline outline, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (outline != null)
        {
            outline.enabled = false;
        }

        if (activeOutlineCoroutines.ContainsKey(outline))
        {
            activeOutlineCoroutines.Remove(outline);
        }
    }

    private void ResetInputFieldHighlight(TMP_InputField inputField)
    {
        if (inputField.TryGetComponent<Outline>(out var outline))
        {
            outline.effectColor = Color.clear;
            outline.enabled = false;
        }
    }

    private IEnumerator ClearErrorMessageAfterDelay(TextMeshProUGUI errorText, float delay)
    {
        if (activeTextCoroutines.ContainsKey(errorText))
        {
            StopCoroutine(activeTextCoroutines[errorText]);
            activeTextCoroutines.Remove(errorText);
        }

        yield return new WaitForSeconds(delay);
        errorText.text = "";

        if (activeTextCoroutines.ContainsKey(errorText))
        {
            activeTextCoroutines.Remove(errorText);
        }
    }

    private async void HandleLogin()
    {
        loginButton.enabled = false;
        string username = loginUsernameInput.text;
        string password = loginPasswordInput.text;

        ResetInputFieldHighlight(loginUsernameInput);
        ResetInputFieldHighlight(loginPasswordInput);
        loginErrorText.text = "";

        if (string.IsNullOrEmpty(username))
        {
            HighlightInputField(loginUsernameInput, Color.red);
            loginErrorText.text = LocalizationSettings.StringDatabase.GetLocalizedString("usernameNotEmpty");
            StartCoroutine(ClearErrorMessageAfterDelay(loginErrorText, 5f));
            loginButton.enabled = true;
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            HighlightInputField(loginPasswordInput, Color.red);
            loginErrorText.text = LocalizationSettings.StringDatabase.GetLocalizedString("passwordNotEmpty");
            StartCoroutine(ClearErrorMessageAfterDelay(loginErrorText, 5f));
            loginButton.enabled = true;
            return;
        }

        if (!ValidatePassword(password))
        {
            HighlightInputField(loginPasswordInput, Color.red);
            loginErrorText.text = LocalizationSettings.StringDatabase.GetLocalizedString("passwordInvalid");
            StartCoroutine(ClearErrorMessageAfterDelay(loginErrorText, 5f));
            loginButton.enabled = true;
            return;
        }

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

            if (saveLoginToggle.isOn)
            {
                SaveLoginData(username, password);
            }

            SaveUsername(username);
            ShowGame();
        }
        catch (Exception e)
        {
            HandleException(e, loginErrorText);
        }
        finally
        {
            loginButton.enabled = true;
        }
    }

    private async void HandleRegister()
    {
        registerButton.enabled = false;
        string username = registerUsernameInput.text;
        string password = registerPasswordInput.text;
        string repeatPassword = repeatPasswordInput.text;

        ResetInputFieldHighlight(registerUsernameInput);
        ResetInputFieldHighlight(registerPasswordInput);
        ResetInputFieldHighlight(repeatPasswordInput);
        registerErrorText.text = "";

        if (string.IsNullOrEmpty(username))
        {
            HighlightInputField(registerUsernameInput, Color.red);
            registerErrorText.text = LocalizationSettings.StringDatabase.GetLocalizedString("usernameNotEmpty");
            StartCoroutine(ClearErrorMessageAfterDelay(registerErrorText, 5f));
            registerButton.enabled = true;
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            HighlightInputField(registerPasswordInput, Color.red);
            registerErrorText.text = LocalizationSettings.StringDatabase.GetLocalizedString("passwordNotEmpty");
            StartCoroutine(ClearErrorMessageAfterDelay(registerErrorText, 5f));
            registerButton.enabled = true;
            return;
        }

        if (string.IsNullOrEmpty(repeatPassword))
        {
            HighlightInputField(repeatPasswordInput, Color.red);
            registerErrorText.text = LocalizationSettings.StringDatabase.GetLocalizedString("repeatPasswordNotEmpty");
            StartCoroutine(ClearErrorMessageAfterDelay(registerErrorText, 5f));
            registerButton.enabled = true;
            return;
        }

        if (password != repeatPassword)
        {
            HighlightInputField(registerPasswordInput, Color.red);
            HighlightInputField(repeatPasswordInput, Color.red);
            registerErrorText.text = LocalizationSettings.StringDatabase.GetLocalizedString("passwordsNotMatch"); 
            StartCoroutine(ClearErrorMessageAfterDelay(registerErrorText, 5f));
            registerButton.enabled = true;
            return;
        }

        if (!ValidatePassword(password))
        {
            HighlightInputField(registerPasswordInput, Color.red);
            HighlightInputField(repeatPasswordInput, Color.red);
            registerErrorText.text = LocalizationSettings.StringDatabase.GetLocalizedString("passwordInvalid");
            StartCoroutine(ClearErrorMessageAfterDelay(registerErrorText, 5f));
            registerButton.enabled = true;
            return;
        }

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);

            var data = new Dictionary<string, object> { { "username", username } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);

            if (saveRegisterToggle.isOn)
            {
                SaveLoginData(username, password);
            }

            SaveUsername(username);
            ShowGame();
        }
        catch (Exception e)
        {
            HandleException(e, registerErrorText);
        }
        finally
        {
            registerButton.enabled = true;
        }
    }

    private void SaveLoginData(string username, string password)
    {
        PlayerPrefs.SetString("savedUsername", username);
        PlayerPrefs.SetString("savedPassword", password);
        PlayerPrefs.Save();
    }

    private void SaveUsername(string username)
    {
        PlayerPrefs.SetString("username", username);
    }

    private void LoadSavedLoginData()
    {
        if (PlayerPrefs.HasKey("savedUsername") && PlayerPrefs.HasKey("savedPassword"))
        {
            string savedUsername = PlayerPrefs.GetString("savedUsername");
            string savedPassword = PlayerPrefs.GetString("savedPassword");

            loginUsernameInput.text = savedUsername;
            loginPasswordInput.text = savedPassword;
        }
    }

    public string GetUsername()
    {
        if (PlayerPrefs.HasKey("username"))
        {
            return PlayerPrefs.GetString("username");
        }
        else
        {
            return "Guest";
        }
    }

    private void HandleException(Exception e, TextMeshProUGUI errorText)
    {
        if (e is AuthenticationException authException)
        {
            if (authException.Message.Contains("username already exists"))
            {
                errorText.text = LocalizationSettings.StringDatabase.GetLocalizedString("usernameExists");
            }
            else
            {
                errorText.text = $"Authentication failed: {authException.Message}";
            }
        }
        else if (e is RequestFailedException requestFailedException)
        {
            if (requestFailedException.Message.Contains("Invalid username or password"))
            {
                errorText.text = LocalizationSettings.StringDatabase.GetLocalizedString("invalidUsernameOrPassword");
            }
            else if (requestFailedException.Message.Contains("Cannot resolve destination host"))
            {
                errorText.text = LocalizationSettings.StringDatabase.GetLocalizedString("networkErrorException");
            }
            else
            {
                errorText.text = $"Request failed: {requestFailedException.Message}";
            }
        }
        else    
        {
            errorText.text = $"Unexpected error: {e.Message}";
        }

        StartCoroutine(ClearErrorMessageAfterDelay(errorText, 5f));
    }

    private bool ValidatePassword(string password)
    {
        if (password.Length < 8 || password.Length > 30) return false;

        if (!password.Any(char.IsUpper)) return false;

        if (!password.Any(char.IsLower)) return false;

        if (!password.Any(char.IsDigit)) return false;

        if (!password.Any(ch => !char.IsLetterOrDigit(ch))) return false;

        return true;
    }
}
