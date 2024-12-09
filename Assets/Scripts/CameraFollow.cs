using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new(0, 4, -10);
    public float lookAheadDistance = 5f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 newPosition = target.position + offset;
        transform.position = newPosition;

        Vector3 lookAtPosition = new (target.position.x, target.position.y + 2, target.position.z + lookAheadDistance);

        transform.LookAt(lookAtPosition);
    }
}
