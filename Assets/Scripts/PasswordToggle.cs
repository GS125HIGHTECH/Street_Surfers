using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PasswordToggle : MonoBehaviour
{
    public TMP_InputField passwordInput;
    public Button toggleButton;
    public Animator passwordAnimator;

    void Start()
    {
        toggleButton.onClick.AddListener(TogglePasswordVisibility);
    }

    void TogglePasswordVisibility()
    {
        bool isPasswordVisible = passwordInput.contentType != TMP_InputField.ContentType.Password;

        isPasswordVisible = !isPasswordVisible;

        passwordAnimator.SetBool("isPasswordVisible", isPasswordVisible);

        if (isPasswordVisible)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Standard;
        }
        else
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
        }

        passwordInput.ForceLabelUpdate();
    }

    public void ResetPasswordVisibility()
    {
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        passwordAnimator.SetBool("isPasswordVisible", false);

        passwordInput.ForceLabelUpdate();
    }
}
