using TMPro;
using UnityEngine;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }

    public TMP_Text username;

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

    public void ShowProfile()
    {
        if (!GameManager.Instance.profilePanel.activeSelf)
        {
            username.text = AuthenticationManager.Instance.GetUsername();

            AudioManager.Instance.PlayClickSound();
            GameManager.Instance.touchScreenText.SetActive(false);
            GameManager.Instance.leaderboardPanel.SetActive(false);
            GameManager.Instance.profilePanel.SetActive(true);
            GameManager.Instance.upgradePanel.SetActive(false);
        }
    }

    public void HideProfile()
    {
        GameManager.Instance.profilePanel.SetActive(false);
        GameManager.Instance.touchScreenText.SetActive(true);
    }
}
