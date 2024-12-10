using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    public Transform wheelFL;
    public Transform wheelFR;
    public Transform wheelRL;
    public Transform wheelRR;

    [SerializeField]
    private float currentSpeed = 2.5f;

    private readonly float wheelRotationSpeed = 90f;
    private readonly float laneChangeDuration = 1f;
    private readonly float tiltAngle = 10f;

    private readonly float[] lanes = { -7f, 0f, 7f };
    private int currentLaneIndex = 1;
    private bool isMobile = false;
    private bool isChangingLane = false;

    private void Start()
    {
        isMobile = Application.isMobilePlatform;
        StartCoroutine(IncreaseSpeedOverTime());
    }

    private void Update()
    {
        transform.Translate(currentSpeed * Time.deltaTime * Vector3.right);

        RotateWheels();

        if (isMobile)
        {
            HandleMobileSwipe();
        }
        else
        {
            HandleKeyboardInput();
        }
    }

    private void RotateWheels()
    {
        float rotationAngle = wheelRotationSpeed * Time.deltaTime;
        if (wheelFL) wheelFL.Rotate(Vector3.right, rotationAngle);
        if (wheelFR) wheelFR.Rotate(Vector3.right, rotationAngle);
        if (wheelRL) wheelRL.Rotate(Vector3.right, rotationAngle);
        if (wheelRR) wheelRR.Rotate(Vector3.right, rotationAngle);
    }

    private void HandleMobileSwipe()
    {
        if (Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 swipeDelta = Touchscreen.current.primaryTouch.delta.ReadValue();

            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                if (swipeDelta.x > 0)
                {
                    ChangeLane(1);
                }
                else
                {
                    ChangeLane(-1);
                }
            }
        }
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
        {
            ChangeLane(1);
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
        {
            ChangeLane(-1);
        }
    }

    private void ChangeLane(int direction)
    {
        int newLaneIndex = currentLaneIndex + direction;

        if (newLaneIndex >= 0 && newLaneIndex < lanes.Length && !isChangingLane)
        {
            currentLaneIndex = newLaneIndex;
            StartCoroutine(SmoothChangeLane(lanes[newLaneIndex]));
        }
    }

    private IEnumerator SmoothChangeLane(float targetX)
    {
        isChangingLane = true;

        Vector3 startPosition = transform.position;
        float initialZ = startPosition.z;
        Vector3 targetPosition = new (targetX, startPosition.y, startPosition.z);

        float elapsedTime = 0f;

        while (elapsedTime < laneChangeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / laneChangeDuration;

            float dynamicZ = initialZ + currentSpeed * elapsedTime;

            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.position = new (transform.position.x, transform.position.y, dynamicZ);

            float tilt = Mathf.Sin(t * Mathf.PI) * tiltAngle * Mathf.Sign(targetX - startPosition.x);
            transform.rotation = Quaternion.Euler(0, -90f + tilt, tilt / 10f);

            float halfDuration = laneChangeDuration / 2f;
            float wheelTurnY;
            if (elapsedTime <= halfDuration)
            {
                wheelTurnY = Mathf.Lerp(
                    -270f,
                    -270f + Mathf.Sign(targetX - startPosition.x) * 15f,
                    t * 2
                );
            }
            else
            {
                float returnT = (elapsedTime - halfDuration) / halfDuration;
                wheelTurnY = Mathf.Lerp(
                    -270f + Mathf.Sign(targetX - startPosition.x) * 15f,
                    -270f,
                    returnT
                );
            }

            if (wheelFL) wheelFL.localRotation = Quaternion.Euler(wheelFL.localRotation.eulerAngles.x, wheelTurnY, 0);
            if (wheelFR) wheelFR.localRotation = Quaternion.Euler(wheelFR.localRotation.eulerAngles.x, wheelTurnY, 0);

            yield return null;
        }

        transform.SetPositionAndRotation(new (targetPosition.x, transform.position.y, transform.position.z), Quaternion.Euler(0, -90f, 0));
        if (wheelFL) wheelFL.localRotation = Quaternion.Euler(wheelFL.localRotation.eulerAngles.x, -270f, 0);
        if (wheelFR) wheelFR.localRotation = Quaternion.Euler(wheelFR.localRotation.eulerAngles.x, -270f, 0);

        isChangingLane = false;
    }

    private IEnumerator IncreaseSpeedOverTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            currentSpeed += 0.2f;
        }
    }
}
