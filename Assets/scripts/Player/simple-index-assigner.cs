using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using System.Linq;

public class SimpleIndexAssigner : MonoBehaviour
{
    public simplePlayer player;
    private List<int> activeControllerIndices = new List<int>();
    private List<SteamVR_TrackedObject> trackedObjects = new List<SteamVR_TrackedObject>();
    private bool conRAssigned = false;
    private float searchInterval = 0.05f;

    void Start()
    {
        trackedObjects.AddRange(GetComponentsInChildren<SteamVR_TrackedObject>());

        if (trackedObjects.Count == 0)
        {
            Debug.LogError("No SteamVR_TrackedObjects found in children.");
            return;
        }

        StartCoroutine(AssignActiveIndices());
    }

    private IEnumerator AssignActiveIndices()
    {
        while (!conRAssigned)
        {
            yield return StartCoroutine(SearchForControllers());
            yield return StartCoroutine(PickIndices());

            if (conRAssigned && player.righthand != null)
            {
                player.righthand.GetComponent<Renderer>().material.color = Color.red;
                Debug.Log("Right controller (conR) has been assigned.");
            }
            else
            {
                Debug.Log("Continuing search for right controller (conR)...");
            }

            yield return new WaitForSeconds(searchInterval);
        }

        Debug.Log($"Index assignment complete. Active indices found: {activeControllerIndices.Count}");
    }

    private IEnumerator SearchForControllers()
    {
        for (int i = 1; i <= 16; i++)
        {
            foreach (var trackedObject in trackedObjects)
            {
                trackedObject.SetDeviceIndex(i);
                yield return null;

                Vector3 startPosition = trackedObject.transform.position;
                Quaternion startRotation = trackedObject.transform.rotation;

                yield return new WaitForSeconds(searchInterval);

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
    }

    private IEnumerator PickIndices()
    {
        if (activeControllerIndices.Count == 0) yield break;

        var indexPositions = new Dictionary<int, Vector3>();

        foreach (var index in activeControllerIndices)
        {
            SteamVR_TrackedObject tempTrackedObject = trackedObjects.FirstOrDefault();
            if (tempTrackedObject != null)
            {
                tempTrackedObject.SetDeviceIndex(index);
                yield return null;
                Vector3 currentPosition = tempTrackedObject.transform.position;
                indexPositions[index] = currentPosition;
            }
        }

        Vector3 hmdForward = player.hmd.transform.forward;
        Vector3 hmdRight = player.hmd.transform.right;
        Vector3 hmdPosition = player.hmd.transform.position;

        var sortedIndices = indexPositions.OrderByDescending(kvp => 
        {
            Vector3 controllerToHmd = kvp.Value - hmdPosition;
            float rightness = Vector3.Dot(controllerToHmd, hmdRight);
            float forwardness = Vector3.Dot(controllerToHmd, hmdForward);
            return rightness + forwardness * 0.5f;
        }).Select(kvp => kvp.Key).ToList();

        if (sortedIndices.Count > 0 && !conRAssigned)
        {
            AssignToController(player.conR, sortedIndices[0]);
            conRAssigned = true;
        }

        if (sortedIndices.Count > 1 && conRAssigned)
        {
            AssignToController(player.conL, sortedIndices[1]);
        }
    }

    private void AssignToController(GameObject controller, int index)
    {
        SteamVR_TrackedObject trackedObject = controller.GetComponent<SteamVR_TrackedObject>();
        if (trackedObject != null)
        {
            trackedObject.SetDeviceIndex(index);
            Debug.Log($"Assigned index {index} to {controller.name}");
            if (controller == player.conR && player.righthand != null)
            {
                player.righthand.GetComponent<Renderer>().material.color = Color.red;
            }
        }
    }
}