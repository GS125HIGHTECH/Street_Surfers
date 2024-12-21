using System.Collections;
using UnityEngine;

public class SpeedBoost : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject);
            AudioManager.Instance.PlaySpeedBoostSound();

            CarController.Instance.StartSpeedBoost();
        }
    }
}
