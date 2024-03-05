using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class ControllerIndexAssignment : MonoBehaviour
{
    private person PersonScript;
    private List<int> activeControllerIndices = new List<int>();

    void Start()
    {
        PersonScript = GetComponent<person>();
        if (PersonScript == null)
        {
            Debug.LogError("Person script not found on the GameObject.");
            return;
        }

        StartCoroutine(AssignActiveIndices());
    }

    private IEnumerator AssignActiveIndices()
    {
        bool isCalibrated = false;

        while (!isCalibrated)
        {
            for (int i = 1; i <= 16; i++)
            {
                SteamVR_TrackedObject trackedObject = PersonScript.conR.GetComponent<SteamVR_TrackedObject>();
                trackedObject.SetDeviceIndex(i);

                yield return null; // Wait for the next frame so the position and rotation can update.

                Vector3 startPosition = trackedObject.transform.position;
                Quaternion startRotation = trackedObject.transform.rotation;

                yield return new WaitForSeconds(0.1f); // Wait a bit for any potential movement.

                if (startPosition != trackedObject.transform.position || startRotation != trackedObject.transform.rotation)
                {
                    Debug.Log($"Index {i} is active.");
                    activeControllerIndices.Add(i);
                }

                // Check if the system has been calibrated
                isCalibrated = PersonScript.player1.currentUpdate.Method.Name == "PostCalibrationUpdate";
                if (isCalibrated)
                {
                    break; // Exit the loop if calibrated
                }
            }

            if (!isCalibrated)
            {
                yield return new WaitForSeconds(1); // Wait before re-checking to reduce load
            }
        }

        Debug.Log($"Calibration complete. Active indices found: {activeControllerIndices.Count}");
        // Here, you can assign the detected active indices to the appropriate controllers.
    }
}
