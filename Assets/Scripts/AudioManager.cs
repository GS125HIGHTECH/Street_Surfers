using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioClip coinCollectSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip tiresSound;
    [SerializeField] private AudioClip engineSound;
    [SerializeField] private AudioClip carCrashSound;
    [SerializeField] private AudioClip speedBoostSound;

    [SerializeField] private AudioSource clickAudioSource;
    [SerializeField] private AudioSource tiresAudioSource;
    [SerializeField] private AudioSource coinAudioSource;
    [SerializeField] private AudioSource engineAudioSource;
    [SerializeField] private AudioSource crashAudioSource;
    [SerializeField] private AudioSource speedBoostAudioSource;

    private float lastPlayTime;
    private const float minPlayInterval = 0.1f;
    private bool isPaused = false;

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

    public void PlayCarCrashSound()
    {
        if (carCrashSound != null && crashAudioSource != null)
        {
            crashAudioSource.PlayOneShot(carCrashSound);
        }
    }

    public void PlaySpeedBoostSound()
    {
        if (speedBoostSound != null && speedBoostAudioSource != null)
        {
            speedBoostAudioSource.clip = speedBoostSound;
            speedBoostAudioSource.volume = 1.0f;
            speedBoostAudioSource.Play();

            StartCoroutine(FadeOutSound(speedBoostAudioSource, 2.5f));
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

    public void StopSpeedBoostSound()
    {
        if (speedBoostAudioSource.isPlaying)
        {
            speedBoostAudioSource.Stop();
        }
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

    public void PauseAllSounds()
    {
        isPaused = true;
        PauseAudioSource(tiresAudioSource);
        PauseAudioSource(coinAudioSource);
        PauseAudioSource(speedBoostAudioSource);
    }

    public void ResumeAllSounds()
    {
        isPaused = false;
        ResumeAudioSource(tiresAudioSource);
        ResumeAudioSource(coinAudioSource);
        ResumeAudioSource(speedBoostAudioSource);
    }

    private void PauseAudioSource(AudioSource source)
    {
        if (source.isPlaying)
        {
            source.Pause();
        }
    }

    private void ResumeAudioSource(AudioSource source)
    {
        if (source.clip != null && isPaused)
        {
            source.UnPause();
        }
    }
}
