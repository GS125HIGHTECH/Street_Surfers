using UnityEngine;

public class Coin : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject);

            AudioManager.Instance.PlayCoinCollectSound();

            Debug.Log("Coin collected!");

            GameManager.Instance.AddCoin();
        }
    }
}
