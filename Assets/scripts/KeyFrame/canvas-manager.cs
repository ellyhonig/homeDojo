using UnityEngine;
using System.Collections.Generic;
using System;


public class CanvasManager : MonoBehaviour
{
    [SerializeField] private GameObject canvasPlane;
    [SerializeField] private SimpleRecorder recorder;
    [SerializeField] private float proximityThreshold = 0.1f;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.white;

    [Header("Visualization")]
    [SerializeField] private float sphereRadius = 0.01f;
    [SerializeField] private Color sphereColor = Color.red;
    [SerializeField, Range(0, 31)] private int visualizationLayer = 0;
    [SerializeField] private int initialPoolSize = 2000;

    private Queue<GameObject> spherePool = new Queue<GameObject>();
    public List<GameObject> activeSpheres = new List<GameObject>();
    private Renderer canvasRenderer;
    private Plane canvasPlaneGeometry;
    private bool isInitialized = false;
    private bool isRecording = false;
    private bool isHandConstrained = false;
    private int hmdSide = 1; // 1 for positive side, -1 for negative side
    private GameObject visualizationParent;
    private GameObject canvasParent;
    public float pointDistance = 0.1f;   
    public float distanceFromHMD = 0.5f;
    public Vector3 offsetFromHMD = new Vector3(0f, -0.2f, 0f);
    public float additionalRotationAngle = 116; // Adjust this value to rotate more or less

    public event Action OnCanvasRepositioned;

     private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (canvasPlane == null || recorder == null || recorder.playerToRecord == null)
        {
            Debug.LogError("CanvasManager: Missing required references!");
            return;
        }

        canvasRenderer = canvasPlane.GetComponent<Renderer>();
        canvasPlaneGeometry = new Plane(canvasPlane.transform.up, canvasPlane.transform.position);
        
        if (recorder != null)
        {
            recorder.OnRecordingStarted += StartProcessing;
            recorder.OnRecordingStopped += StopProcessing;
            recorder.OnRecordingStopped += currentRecordProcessor; // Subscribe to the event
            recorder.OnRecordingLoaded += CreateVisualizationForAllPoints;
        }
        else
        {
            Debug.LogError("SimpleRecorder reference is missing in CanvasManager.");
        }

        CreateVisualizationParent();
        InitializeSpherePool();
        canvasParent = new GameObject("CanvasParent");
        isInitialized = true;
    }
     private void currentRecordProcessor()
    {
        if (recorder == null || recorder.currentRecord == null)
        {
            Debug.LogError("Recorder or currentRecord is null");
            return;
        }

        List<GameObject> spheresToKeep = new List<GameObject>();
        List<GameObject> spheresToRemove = new List<GameObject>();

        // Always keep the first sphere
        if (activeSpheres.Count > 0)
        {
            spheresToKeep.Add(activeSpheres[0]);
        }

        // Filter spheres based on the minimum distance
        for (int i = 1; i < activeSpheres.Count; i++)
        {
            Vector3 lastKeptPosition = spheresToKeep[spheresToKeep.Count - 1].transform.position;
            Vector3 currentPosition = activeSpheres[i].transform.position;

            if (Vector3.Distance(lastKeptPosition, currentPosition) >= pointDistance)
            {
                spheresToKeep.Add(activeSpheres[i]);
            }
            else
            {
                spheresToRemove.Add(activeSpheres[i]);
            }
        }

        // Remove spheres that are too close
        foreach (GameObject sphere in spheresToRemove)
        {
            activeSpheres.Remove(sphere);
            Destroy(sphere);
        }

        // Clear the existing frames in currentRecord
        recorder.currentRecord.frames.Clear();

        // Add new frames based on the positions of kept spheres
        foreach (GameObject sphere in spheresToKeep)
        {
            SimpleFrame newFrame = new SimpleFrame(sphere.transform.position);
            recorder.currentRecord.frames.Add(newFrame);
        }

        Debug.Log($"CurrentRecord processed. New frame count: {recorder.currentRecord.frames.Count}");
        Debug.Log($"Kept spheres: {spheresToKeep.Count}, Removed spheres: {spheresToRemove.Count}");
    }

    private void CreateVisualizationParent()
    {
        visualizationParent = canvasPlane;
        //visualizationParent.transform.SetParent(transform.parent); // Set parent to the same parent as CanvasManager
        //visualizationParent.transform.localPosition = Vector3.zero;
        //visualizationParent.transform.localRotation = Quaternion.identity;
    }

    private void InitializeSpherePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject sphere = CreateSphere(i);
            sphere.SetActive(false);
            spherePool.Enqueue(sphere);
        }
    }
    
    private GameObject CreateSphere(int index)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = $"keypoint_{index}";
        sphere.transform.localScale = Vector3.one * (sphereRadius * 5);
        sphere.GetComponent<Renderer>().material.color = sphereColor;
        sphere.layer = visualizationLayer;
        Destroy(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(visualizationParent.transform);
        return sphere;
    }
    private float updateRate = 0.01f; // Run 100x per second
    private float nextUpdateTime = 0f;

    private void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            
            if (!isInitialized || !isRecording) return;

            UpdateHmdSide();
            CheckHandProximity();
            UpdateCanvasColor();
            nextUpdateTime = Time.time + updateRate;
           
        }
         Debug.Log("kiii");
    }

    private void UpdateHmdSide()
    {
        hmdSide = canvasPlaneGeometry.GetSide(recorder.playerToRecord.hmd.transform.position) ? 1 : -1;
    }

    private void CheckHandProximity()
    {
        Vector3 handPosition = recorder.playerToRecord.conR.transform.position;
        float distanceToPlane = canvasPlaneGeometry.GetDistanceToPoint(handPosition);

        if (Mathf.Abs(distanceToPlane) <= proximityThreshold || 
            canvasPlaneGeometry.GetSide(handPosition) != (hmdSide == 1))
        {
            if (!isHandConstrained)
            {
                isHandConstrained = true;
                recorder.playerToRecord.currentUpdate = ConstrainedHandUpdate;
            }
        }
        else if (isHandConstrained)
        {
            isHandConstrained = false;
            recorder.playerToRecord.currentUpdate = recorder.playerToRecord.regularUpdate;
        }
    }

    private void ConstrainedHandUpdate()
    {
        Vector3 controllerPosition = recorder.playerToRecord.conR.transform.position;
        Vector3 projectedPosition = canvasPlaneGeometry.ClosestPointOnPlane(controllerPosition);
        recorder.playerToRecord.righthand.transform.position = projectedPosition;
        recorder.playerToRecord.lefthand.transform.position = recorder.playerToRecord.conL.transform.position;

        if (canvasPlaneGeometry.GetSide(controllerPosition) == (hmdSide == 1))
        {
            isHandConstrained = false;
            recorder.playerToRecord.currentUpdate = recorder.playerToRecord.regularUpdate;
        }

        if (isRecording)
        {
            CreateVisualizationSphere(projectedPosition);
        }
    }

    private void UpdateCanvasColor()
    {
        if (canvasRenderer != null)
        {
            canvasRenderer.material.color = isHandConstrained ? activeColor : inactiveColor;
        }
    }

    private void StartProcessing()
    {
        isRecording = true;
        ClearVisualization();
    }

    private void StopProcessing()
    {
        isRecording = false;
        isHandConstrained = false;
        recorder.playerToRecord.currentUpdate = recorder.playerToRecord.regularUpdate;
        UpdateCanvasColor();
    }

      public void CreateVisualizationForAllPoints()
    {
        ClearVisualization();

        if (recorder.currentRecord != null && recorder.currentRecord.frames != null)
        {
            foreach (var frame in recorder.currentRecord.frames)
            {
                CreateVisualizationSphere(frame.position);
            }
        }
    }

    private void CreateVisualizationSphere(Vector3 position)
    {
        GameObject sphere;
        if (spherePool.Count > 0)
        {
            sphere = spherePool.Dequeue();
        }
        else
        {
            sphere = CreateSphere(activeSpheres.Count);
        }

        sphere.transform.position = position;
        sphere.SetActive(true);
        activeSpheres.Add(sphere);
    }

    private void ClearVisualization()
    {
        foreach (var sphere in activeSpheres)
        {
            sphere.SetActive(false);
            spherePool.Enqueue(sphere);
        }
        activeSpheres.Clear();
    }
     public void UpdateCanvas()
    {
        
        if (recorder.currentRecord.frames.Count > 0)
        {
            PositionCanvasWithRecordedPoints();
        }
        else
        {
            PositionCanvasWithoutPoints();
        }
    }

    private void PositionCanvasWithRecordedPoints()
    {
        if (recorder.playerToRecord == null || recorder.playerToRecord.hmd == null)
        {
            Debug.LogError("Player or HMD reference is missing!");
            return;
        }

        Transform hmdTransform = recorder.playerToRecord.hmd.transform;

        // Set canvasParent position to the first recorded point
        canvasParent.transform.position = recorder.currentRecord.frames[0].position;

        // Parent the canvasPlane to canvasParent
        canvasPlane.transform.SetParent(canvasParent.transform, true);

        // Position the parent based on HMD forward direction
        Vector3 hmdForward = hmdTransform.forward;
        Vector3 forwardProjected = Vector3.ProjectOnPlane(hmdForward, Vector3.up).normalized;
        Vector3 newPosition = hmdTransform.position + forwardProjected * distanceFromHMD;
        newPosition.y = hmdTransform.position.y;
        newPosition += offsetFromHMD;
        canvasParent.transform.position = newPosition;

        // Rotate the parent so its red arrow (forward) is parallel and opposite to HMD forward projected on XZ
        // Then apply additional rotation
        Quaternion baseRotation = Quaternion.LookRotation(-forwardProjected, Vector3.up);
        Quaternion additionalRotation = Quaternion.Euler(0, additionalRotationAngle, 0);
        canvasParent.transform.rotation = baseRotation * additionalRotation;

        // Update recorded positions
        UpdateRecordedPositions();

        // Unparent the canvasPlane
        canvasPlane.transform.SetParent(null);
        OnCanvasRepositioned?.Invoke();
        Debug.Log($"Canvas positioned at {canvasParent.transform.position} and rotated based on HMD with additional rotation");
    }

    private void PositionCanvasWithoutPoints()
    {
        if (recorder.playerToRecord != null && recorder.playerToRecord.hmd != null)
        {
            Transform hmdTransform = recorder.playerToRecord.hmd.transform;
            Vector3 hmdForward = hmdTransform.forward;
            Vector3 forwardProjected = Vector3.ProjectOnPlane(hmdForward, Vector3.up).normalized;
            
            Vector3 newPosition = hmdTransform.position + forwardProjected * distanceFromHMD;
            newPosition.y = hmdTransform.position.y;
            newPosition += offsetFromHMD;
            
            canvasPlane.transform.position = newPosition;

            Quaternion baseRotation = Quaternion.LookRotation(-forwardProjected, Vector3.up);
            Quaternion additionalRotation = Quaternion.Euler(0, additionalRotationAngle, 0);
            canvasPlane.transform.rotation = baseRotation * additionalRotation;

            Debug.Log($"Canvas positioned at {canvasPlane.transform.position} without recorded points");
        }
    }

    private void UpdateRecordedPositions()
    {
        if (activeSpheres.Count != recorder.currentRecord.frames.Count)
        {
            Debug.LogError("Mismatch between number of active spheres and recorded frames!");
            return;
        }

        for (int i = 0; i < recorder.currentRecord.frames.Count; i++)
        {
            if (activeSpheres[i] != null)
            {
                SimpleFrame frame = recorder.currentRecord.frames[i];
                frame.position = activeSpheres[i].transform.position;
                recorder.currentRecord.frames[i] = frame;
            }
            else
            {
                Debug.LogWarning($"Active sphere at index {i} is null!");
            }
        }

        Debug.Log($"Updated {recorder.currentRecord.frames.Count} frame positions based on sphere positions.");
    }

    public void UpdateActiveSpheres(List<GameObject> spheres)
    {
        activeSpheres = spheres;
    }
     private void OnDisable()
    {
        if (recorder != null)
        {
            recorder.OnRecordingStarted -= StartProcessing;
            recorder.OnRecordingStopped -= StopProcessing;
            recorder.OnRecordingStopped -= currentRecordProcessor; // Unsubscribe from the event
            recorder.OnRecordingLoaded -= CreateVisualizationForAllPoints;
        }
    }

    private void OnDestroy()
    {
        if (visualizationParent != null)
        {
            Destroy(visualizationParent);
        }
    }
    
}