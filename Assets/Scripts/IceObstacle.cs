using UnityEngine;

public class IceObstacle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject);

            CarController.Instance.SetSliding();
        }
    }
}
