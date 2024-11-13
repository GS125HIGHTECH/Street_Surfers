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
    public GameObject nicknamePanel;

    [Header("Login Fields")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public Button loginButton;

    [Header("Register Fields")]
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField repeatPasswordInput;
    public Button registerButton;

    [Header("Switch Texts")]
    public TextMeshProUGUI switchToRegisterText;
    public TextMeshProUGUI switchToLoginText;

    [Header("Nickname Field")]
    public TMP_InputField nicknameInput;
    public Button submitNicknameButton;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        ShowRegisterPanel();

        loginButton.onClick.AddListener(HandleLogin);
        registerButton.onClick.AddListener(HandleRegister);
        submitNicknameButton.onClick.AddListener(SubmitNickname);

        switchToRegisterText.GetComponent<Button>().onClick.AddListener(ShowRegisterPanel);
        switchToLoginText.GetComponent<Button>().onClick.AddListener(ShowLoginPanel);
    }

    private void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        nicknamePanel.SetActive(false);
    }

    private void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        nicknamePanel.SetActive(false);
    }

    private void ShowNicknamePanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        nicknamePanel.SetActive(true);
    }

    private void ShowGame()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        nicknamePanel.SetActive(false);
    }

    private async void HandleLogin()
    {
        string email = loginEmailInput.text;
        string password = loginPasswordInput.text;

        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(email, password);
            Debug.Log("Login successful!");

            bool nicknameExists = await CheckIfNicknameExists();

            if (nicknameExists)
            {
                ShowGame();
            }
            else
            {
                ShowNicknamePanel();
            }
        }
        catch (AuthenticationException e)
        {
            Debug.LogError($"Login failed: {e.Message}");
        }
    }

    private async Task<bool> CheckIfNicknameExists()
    {
        try
        {
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "nickname" });

            return result.ContainsKey("nickname") && !string.IsNullOrEmpty(result["nickname"].Value.GetAsString());
        }
        catch (CloudSaveException e)
        {
            Debug.LogError($"Error checking nickname: {e.Message}");
            return false;
        }
    }

    private async void HandleRegister()
    {
        string email = registerEmailInput.text;
        string password = registerPasswordInput.text;
        string repeatPassword = repeatPasswordInput.text;

        if (password != repeatPassword)
        {
            Debug.Log("Passwords do not match.");
            return;
        }

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(email, password);
            Debug.Log("Registration successful!");

            ShowNicknamePanel();
        }
        catch (AuthenticationException e)
        {
            Debug.LogError($"Registration failed: {e.Message}");
        }
    }

    private async void SubmitNickname()
    {
        string nickname = nicknameInput.text;

        if (string.IsNullOrEmpty(nickname))
        {
            Debug.Log("Please enter a valid nickname.");
            return;
        }

        try
        {
            var data = new Dictionary<string, object> { { "nickname", nickname } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("Nickname saved to Cloud Save!");

            ShowGame();

        }
        catch (CloudSaveException e)
        {
            Debug.LogError($"Failed to save nickname: {e.Message}");
        }
    }
}
