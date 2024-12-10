using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioClip coinCollectSound;
    [SerializeField] private AudioClip clickSound;

    [SerializeField] private AudioSource audioSource;

    private float lastPlayTime;
    private const float minPlayInterval = 0.1f;

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

    public void PlayCoinCollectSound()
    {
        if (coinCollectSound != null && audioSource != null && Time.time >= lastPlayTime + minPlayInterval)
        {
            audioSource.PlayOneShot(coinCollectSound);
            lastPlayTime = Time.time;
        }
    }

    public void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}
