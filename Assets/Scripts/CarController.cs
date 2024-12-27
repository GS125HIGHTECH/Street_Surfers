using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    public static CarController Instance;

    public Transform wheelFL;
    public Transform wheelFR;
    public Transform wheelRL;
    public Transform wheelRR;

    [SerializeField]
    private float currentSpeed = 2.5f;
    private double totalDistance = 0;
    [SerializeField]
    private float currentDistance = 0;

    private readonly float wheelRotationBaseSpeed = 90f;

    [SerializeField]
    private float boostDuration = 2f;
    [SerializeField]
    private float laneChangeDuration = 1f;

    private readonly float tiltAngle = 10f;

    private readonly float[] lanes = { -8f, 0f, 8f };
    private int currentLaneIndex = 1;
    private bool isMobile = false;
    private bool isChangingLane = false;
    private bool isGamePlayable = false;
    private Coroutine speedCoroutine;
    [SerializeField]
    private bool isBoostActive = false;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        isMobile = Application.isMobilePlatform;

        currentDistance = 0;
    }

    private void Update()
    {
        if (!isGamePlayable)
            return;

        float distancePerFrame = currentSpeed * Time.deltaTime;

        totalDistance += distancePerFrame;
        currentDistance += distancePerFrame;

        transform.Translate(distancePerFrame * Vector3.right);

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
        float rotationSpeed = wheelRotationBaseSpeed * (currentSpeed / 3.5f);
        float rotationAngle = rotationSpeed * Time.deltaTime;

        if (wheelFL) wheelFL.Rotate(Vector3.right, rotationAngle);
        if (wheelFR) wheelFR.Rotate(Vector3.right, rotationAngle);
        if (wheelRL) wheelRL.Rotate(Vector3.right, rotationAngle);
        if (wheelRR) wheelRR.Rotate(Vector3.right, rotationAngle);
    }

    private void HandleMobileSwipe()
    {
        float swipeThreshold = 50f;

        if (!GameManager.Instance.menuPanel.activeSelf && !GameManager.Instance.settingsPanel.activeSelf)
        {
            if (Touchscreen.current.primaryTouch.press.isPressed)
            {
                Vector2 swipeDelta = Touchscreen.current.primaryTouch.delta.ReadValue();

                if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y) && Mathf.Abs(swipeDelta.x) > swipeThreshold)
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
    }

    private void HandleKeyboardInput()
    {
        if (!GameManager.Instance.menuPanel.activeSelf && !GameManager.Instance.settingsPanel.activeSelf)
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
        AudioManager.Instance.PlayTiresSound();

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
        while (isGamePlayable)
        {
            yield return new WaitForSeconds(1f);
            currentSpeed = Mathf.Clamp(currentSpeed + 0.2f, 0f, 90f);
        }
    }

    public void UpdateHandling(float newHandling)
    {
        laneChangeDuration = Mathf.Clamp(newHandling, 0.1f, 1f);
    }

    public void UpdateBoostDuration(float newDuration)
    {
        boostDuration = newDuration;
    }

    public void StartSpeedBoost()
    {
        if (isBoostActive) return;

        currentSpeed += 8f;
        isBoostActive = true;

        BoostManager.Instance.StartBoostEffect(boostDuration);

        Invoke(nameof(EndSpeedBoost), boostDuration);
    }

    private void EndSpeedBoost()
    {
        currentSpeed -= 8f;
        isBoostActive = false;
    }

    public double GetTotalDistance()
    {
        return totalDistance;
    }

    public float GetCurrentDistance()
    {
        return currentDistance;
    }

    public void ResetCurrentDistance()
    {
        currentDistance = 0f;
    }

    public void ResumeController()
    {
        isGamePlayable = true;
        speedCoroutine = StartCoroutine(IncreaseSpeedOverTime());
    }

    public void PauseController()
    {
        isGamePlayable = false;
        if (speedCoroutine != null)
        {
            StopCoroutine(speedCoroutine);
            speedCoroutine = null;
        }
    }
}
