using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 5, -10);

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 newPosition = target.position + offset;
        transform.position = newPosition;

        transform.LookAt(target);
    }
}
