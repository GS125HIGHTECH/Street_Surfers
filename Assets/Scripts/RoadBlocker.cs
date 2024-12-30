using UnityEngine;

public class RoadBlocker : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AudioManager.Instance.StopEngineSound();
            AudioManager.Instance.PlayCarCrashSound();

            GameManager.Instance.GameOver();
        }
    }
}
