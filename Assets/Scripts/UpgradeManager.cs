using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

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

    public void ShowUpgrade()
    {
        if (!GameManager.Instance.upgradePanel.activeSelf)
        {
            AudioManager.Instance.PlayClickSound();
            GameManager.Instance.touchScreenText.SetActive(false);
            GameManager.Instance.leaderboardPanel.SetActive(false);
            GameManager.Instance.profilePanel.SetActive(false);
            GameManager.Instance.upgradePanel.SetActive(true);
        }
    }

    public void HideUpgrade()
    {
        GameManager.Instance.upgradePanel.SetActive(false);
        GameManager.Instance.touchScreenText.SetActive(true);
    }
}
