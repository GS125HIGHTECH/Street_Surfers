using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TMP_Text coinCountText;

    private void Update()
    {
        coinCountText.text = GameManager.Instance.GetCoinCount().ToString();
    }
}
