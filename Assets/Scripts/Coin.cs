using UnityEngine;

public class Coin : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject);

            AudioManager.Instance.PlayCoinCollectSound();

            GameManager.Instance.AddCoin();
        }
    }
}
