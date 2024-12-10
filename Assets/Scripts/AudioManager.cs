using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioClip coinCollectSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip tiresSound;
    [SerializeField] private AudioClip engineSound;

    [SerializeField] private AudioSource clickAudioSource;
    [SerializeField] private AudioSource tiresAudioSource;
    [SerializeField] private AudioSource coinAudioSource;
    [SerializeField] private AudioSource engineAudioSource;

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
        if (coinCollectSound != null && coinAudioSource != null && Time.time >= lastPlayTime + minPlayInterval)
        {
            coinAudioSource.PlayOneShot(coinCollectSound);
            lastPlayTime = Time.time;
        }
    }

    public void PlayClickSound()
    {
        if (clickSound != null && clickAudioSource != null)
        {
            clickAudioSource.PlayOneShot(clickSound);
        }
    }

    public void PlayTiresSound()
    {
        if (tiresSound != null && tiresAudioSource != null)
        {
            tiresAudioSource.clip = tiresSound;
            tiresAudioSource.volume = 1.0f;
            tiresAudioSource.Play();     

            StartCoroutine(FadeOutSound(tiresAudioSource, 1.0f));
        }
    }

    private IEnumerator FadeOutSound(AudioSource source, float fadeDuration)
    {
        float startVolume = source.volume;

        while (source.volume > 0)
        {
            source.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        source.Stop();
        source.volume = startVolume;
    }

    public void PlayEngineSound()
    {
        if (engineSound != null && engineAudioSource != null)
        {
            engineAudioSource.clip = engineSound;
            engineAudioSource.loop = true;
            engineAudioSource.Play();    
        }
    }

    public void StopEngineSound()
    {
        if (engineAudioSource != null && engineAudioSource.clip == engineSound)
        {
            engineAudioSource.loop = false; 
            engineAudioSource.Stop();  
        }
    }
}
