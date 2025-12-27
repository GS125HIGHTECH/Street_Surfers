using UnityEngine;
using extOSC;

public class MuseGameController : MonoBehaviour
{
    public OSCReceiver Receiver;

    [Header("Current Muse Data")]
    public int HeadMovementCommand = 0;
    public int JawClenchState = 0;
    void Start()
    {
        if (!Receiver)
        {
            Debug.LogError("OSCReceiver component is missing! Assign it in the Inspector.");
            return;
        }

        Receiver.Bind("/game/head_movement", OnReceiveHeadMovement);

        Receiver.Bind("/game/jaw_clench", OnReceiveJawClench);
    }

    private void OnReceiveHeadMovement(OSCMessage message)
    {
        if (message.ToInt(out int value))
        {
            HeadMovementCommand = value;

            if (CarController.Instance != null)
            {
                CarController.Instance.HandleExternalLaneChange(value);
            }

            if (HeadMovementCommand == -1)
            {
                Debug.Log("Muse: Skręt w LEWO");
            }
            else if (HeadMovementCommand == 1)
            {
                Debug.Log("Muse: Skręt w PRAWO");
            }
        }
    }

    private void OnReceiveJawClench(OSCMessage message)
    {
        if (message.ToInt(out int value))
        {
            JawClenchState = value;

            if (JawClenchState == 1)
            {
                Debug.Log("💥 Jaw Clench Detected!");

                if (MissionManager.Instance != null)
                {
                    MissionManager.Instance.TriggerDropdownBCI();
                }

                // CarController.Instance.StartSpeedBoost();
            }
        }
    }
}
