using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BoostManager : MonoBehaviour
{
    public static BoostManager Instance { get; private set; }

    public Image boostImage;

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
        boostImage.enabled = false;
    }

    public void StartBoostEffect(float duration)
    {
        StartCoroutine(BoostEffectCoroutine(duration));
    }

    private IEnumerator BoostEffectCoroutine(float duration)
    {
        boostImage.enabled = true;
        boostImage.fillAmount = 1f;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress = elapsedTime / duration;
            boostImage.fillAmount = 1f - progress;

            yield return null;
        }

        boostImage.enabled = false;
    }
}
