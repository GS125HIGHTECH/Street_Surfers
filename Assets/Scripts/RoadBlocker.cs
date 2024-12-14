using UnityEngine;

public class RoadBlocker : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AudioManager.Instance.StopEngineSound();
            AudioManager.Instance.PlayCarCrashSound();

            Debug.Log("Car crashed!");

            GameManager.Instance.GameOver();
        }
    }
}
