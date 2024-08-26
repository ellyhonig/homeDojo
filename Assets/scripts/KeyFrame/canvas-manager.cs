using UnityEngine;
using System.Collections.Generic;

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

    private List<GameObject> activeSpheres = new List<GameObject>();
    private Queue<GameObject> spherePool = new Queue<GameObject>();
    private Renderer canvasRenderer;
    private Plane canvasPlaneGeometry;
    private bool isInitialized = false;
    private bool isRecording = false;
    private bool isHandConstrained = false;
    private int hmdSide = 1; // 1 for positive side, -1 for negative side

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
        
        recorder.OnRecordingStarted += StartProcessing;
        recorder.OnRecordingStopped += StopProcessing;

        InitializeSpherePool();

        isInitialized = true;
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
        sphere.transform.localScale = Vector3.one * (sphereRadius * 2);
        sphere.GetComponent<Renderer>().material.color = sphereColor;
        sphere.layer = visualizationLayer;
        Destroy(sphere.GetComponent<Collider>());
        return sphere;
    }

    private void Update()
    {
        if (!isInitialized || !isRecording) return;

        UpdateHmdSide();
        CheckHandProximity();
        UpdateCanvasColor();
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

    private void CreateVisualizationSphere(Vector3 position)
    {
        GameObject sphere;
        if (spherePool.Count > 0)
        {
            sphere = spherePool.Dequeue();
            sphere.SetActive(true);
        }
        else
        {
            sphere = CreateSphere(activeSpheres.Count);
        }

        sphere.transform.position = position;
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

    private void OnDisable()
    {
        if (recorder != null)
        {
            recorder.OnRecordingStarted -= StartProcessing;
            recorder.OnRecordingStopped -= StopProcessing;
        }
    }
}