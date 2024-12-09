using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;

public class AuthenticationManager : MonoBehaviour
{
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

    public static event Action OnGameShown;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        ShowRegisterPanel();

        loginButton.onClick.AddListener(HandleLogin);
        registerButton.onClick.AddListener(HandleRegister);

        switchToRegisterText.GetComponent<Button>().onClick.AddListener(ShowRegisterPanel);
        switchToLoginText.GetComponent<Button>().onClick.AddListener(ShowLoginPanel);
    }

    private void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);

        loginPasswordToggle.ResetPasswordVisibility();
    }

    private void ShowRegisterPanel()
    {
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
    }

    private void HighlightInputField(TMP_InputField inputField, Color color)
    {
        if (inputField.TryGetComponent<Outline>(out var outline))
        {
            outline.effectColor = color;
            outline.enabled = true;

            StartCoroutine(RemoveHighlightCoroutine(outline, 5f));
        }
    }

    private IEnumerator RemoveHighlightCoroutine(Outline outline, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (outline != null)
        {
            outline.enabled = false;
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

    private async void HandleLogin()
    {
        string username = loginUsernameInput.text;
        string password = loginPasswordInput.text;

        ResetInputFieldHighlight(loginUsernameInput);
        ResetInputFieldHighlight(loginPasswordInput);
        loginErrorText.text = "";

        if (string.IsNullOrEmpty(username))
        {
            HighlightInputField(loginUsernameInput, Color.red);
            loginErrorText.text = "Username cannot be empty.";
            StartCoroutine(ClearErrorMessageAfterDelay(loginErrorText, 5f));
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            HighlightInputField(loginPasswordInput, Color.red);
            loginErrorText.text = "Password cannot be empty.";
            StartCoroutine(ClearErrorMessageAfterDelay(loginErrorText, 5f));
            return;
        }

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            Debug.Log("Login successful!");

            ShowGame();
        }
        catch (Exception e)
        {
            HandleException(e, loginErrorText);
        }
    }

    private async void HandleRegister()
    {
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
            registerErrorText.text = "Username cannot be empty.";
            StartCoroutine(ClearErrorMessageAfterDelay(registerErrorText, 5f));
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            HighlightInputField(registerPasswordInput, Color.red);
            registerErrorText.text = "Password cannot be empty.";
            StartCoroutine(ClearErrorMessageAfterDelay(registerErrorText, 5f));
            return;
        }

        if (string.IsNullOrEmpty(repeatPassword))
        {
            HighlightInputField(repeatPasswordInput, Color.red);
            registerErrorText.text = "Repeat password cannot be empty.";
            StartCoroutine(ClearErrorMessageAfterDelay(registerErrorText, 5f));
            return;
        }

        if (password != repeatPassword)
        {
            HighlightInputField(registerPasswordInput, Color.red);
            HighlightInputField(repeatPasswordInput, Color.red);
            registerErrorText.text = "Passwords do not match.";
            StartCoroutine(ClearErrorMessageAfterDelay(registerErrorText, 5f));
            return;
        }

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            Debug.Log("Registration successful!");

            var data = new Dictionary<string, object> { { "username", username } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("Username saved to Cloud Save!");

            ShowGame();
        }
        catch (Exception e)
        {
            HandleException(e, registerErrorText);
        }
    }

    private IEnumerator ClearErrorMessageAfterDelay(TextMeshProUGUI errorText, float delay)
    {
        yield return new WaitForSeconds(delay);
        errorText.text = "";
    }

    private void HandleException(Exception e, TextMeshProUGUI errorText)
    {
        if (e is AuthenticationException authException)
        {
            errorText.text = $"Authentication failed: {authException.Message}";
        }
        else if (e is RequestFailedException requestFailedException)
        {
            errorText.text = $"Request failed: {requestFailedException.Message}";
        }
        else
        {
            errorText.text = $"Unexpected error: {e.Message}";
        }
    }
}
