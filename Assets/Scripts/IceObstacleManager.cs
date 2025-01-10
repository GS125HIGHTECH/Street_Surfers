using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IceObstacleManager : MonoBehaviour
{
    public static IceObstacleManager Instance { get; private set; }

    public Image iceObstacleImage;

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

    private void Start()
    {
        iceObstacleImage.enabled = false;
    }

    public void StartIceObstacleEffect(float duration)
    {
        StartCoroutine(IceObstacleEffectCoroutine(duration));
    }

    private IEnumerator IceObstacleEffectCoroutine(float duration)
    {
        iceObstacleImage.enabled = true;
        iceObstacleImage.fillAmount = 1f;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress = elapsedTime / duration;
            iceObstacleImage.fillAmount = 1f - progress;

            yield return null;
        }

        iceObstacleImage.enabled = false;
    }
}
