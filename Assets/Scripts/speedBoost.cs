using System.Collections;
using UnityEngine;

public class SpeedBoost : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(TriggerSpeedBoost());

            AudioManager.Instance.PlaySpeedBoostSound();
        }
    }

    private IEnumerator TriggerSpeedBoost()
    {
        yield return StartCoroutine(CarController.Instance.ApplySpeedBoost());

        Destroy(gameObject);
    }
}
