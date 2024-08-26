using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using System.Linq;


public class ControllerIndexAssignment : MonoBehaviour
{
    private person PersonScript;
    private List<int> activeControllerIndices = new List<int>();
    private List<SteamVR_TrackedObject> trackedObjects = new List<SteamVR_TrackedObject>();
    public GameObject conR;
    public GameObject conL;
    void Start()
    {
        PersonScript = GetComponent<person>();
        trackedObjects.AddRange(GetComponents<SteamVR_TrackedObject>());

        if (PersonScript == null || trackedObjects.Count == 0)
        {
            Debug.LogError("Required components not found on the GameObject.");
            return;
        }

        StartCoroutine(AssignActiveIndices());
    }

    private IEnumerator AssignActiveIndices()
    {
        bool isCalibrated = false;

        // Hide Hip body parts before calibration
        PersonScript.HideHipBodyParts();

        while (!isCalibrated)
        {
            isCalibrated = PersonScript.player1.currentUpdate.Method.Name == "PostCalibrationUpdate";

            activeControllerIndices.Clear();
            for (int i = 1; i <= 16 && !isCalibrated; i++)
            {
                foreach (var trackedObject in trackedObjects)
                {
                    trackedObject.SetDeviceIndex(i);

                    yield return null;

                    Vector3 startPosition = trackedObject.transform.position;
                    Quaternion startRotation = trackedObject.transform.rotation;

                    yield return new WaitForSeconds(0.1f);

                    if (startPosition != trackedObject.transform.position || startRotation != trackedObject.transform.rotation)
                    {
                        if (!activeControllerIndices.Contains(i))
                        {
                            Debug.Log($"Index {i} is active.");
                            activeControllerIndices.Add(i);
                        }
                    }
                }
            }

            // Update torso visibility based on the number of active controllers
            PersonScript.player1.UpdateTorsoVisibility(activeControllerIndices.Count);
            PersonScript.mirroredPlayer.mirroredPlayer.UpdateTorsoVisibility(activeControllerIndices.Count);

            StartCoroutine(PickIndices());
   
            yield return new WaitForSeconds(1);

            isCalibrated = PersonScript.player1.currentUpdate.Method.Name == "PostCalibrationUpdate";
        }

        Debug.Log($"Calibration complete. Active indices found: {activeControllerIndices.Count}");

        // After calibration, remove Hip body parts if only 2 controllers are active
        if (activeControllerIndices.Count == 2)
        {
            Debug.Log("Only 2 controllers detected. Removing Hip-related body parts.");
            PersonScript.RemoveHipBodyParts();
        }
        else
        {
            // If more than 2 controllers, show Hip body parts
            PersonScript.ShowHipBodyParts();
        }

        // Final update of torso visibility after calibration
        PersonScript.player1.UpdateTorsoVisibility(activeControllerIndices.Count);
        PersonScript.mirroredPlayer.mirroredPlayer.UpdateTorsoVisibility(activeControllerIndices.Count);
    }   
    private IEnumerator PickIndices()
{
    var indexPositions = new Dictionary<int, Vector3>();

    // Ensure trackedObjects are up-to-date.
    trackedObjects.Clear();
    trackedObjects.AddRange(GetComponentsInChildren<SteamVR_TrackedObject>());

    foreach (var index in activeControllerIndices)
    {
        SteamVR_TrackedObject tempTrackedObject = trackedObjects.FirstOrDefault();
        if(tempTrackedObject != null)
        {
            tempTrackedObject.SetDeviceIndex(index);
            
            // Wait a frame to ensure the position updates with the new index.
            yield return null;

            // Now capture the position.
            Vector3 currentPosition = tempTrackedObject.transform.position;

            // Store the index and position for sorting.
            indexPositions[index] = currentPosition;
        }
    }

    // New code starts here
    Vector3 hmdForward = PersonScript.hmd.transform.forward;
    Vector3 hmdRight = PersonScript.hmd.transform.right;
    Vector3 hmdPosition = PersonScript.hmd.transform.position;

    var sortedIndices = indexPositions.OrderByDescending(kvp => 
    {
        Vector3 controllerToHmd = kvp.Value - hmdPosition;
        float rightness = Vector3.Dot(controllerToHmd, hmdRight);
        float forwardness = Vector3.Dot(controllerToHmd, hmdForward);
        return rightness + forwardness * 0.5f;  // Prioritize rightness over forwardness
    }).Select(kvp => kvp.Key).ToList();
    // New code ends here

    // Assign sorted indices to controllers. This part must also comply with coroutine logic.
    if (sortedIndices.Count > 0) AssignToController(conR, sortedIndices[0]);
    if (sortedIndices.Count > 1) AssignToController(conL, sortedIndices[1]);
    if (sortedIndices.Count > 2) AssignToController(PersonScript.kneeConR, sortedIndices[2]);
    if (sortedIndices.Count > 3) AssignToController(PersonScript.kneeConL, sortedIndices[3]);
}

private void AssignToController(GameObject controller, int index)
{
    SteamVR_TrackedObject trackedObject = controller.GetComponent<SteamVR_TrackedObject>();
    if (trackedObject != null)
    {
        trackedObject.SetDeviceIndex(index);
        Debug.Log($"Assigned index {index} to {controller.name}");
    }
}
}