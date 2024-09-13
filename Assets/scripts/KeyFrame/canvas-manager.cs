using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;


public class CanvasManager : MonoBehaviour
{
    [SerializeField] public GameObject canvasPlane;
    [SerializeField] private SimpleRecorder recorder;
    [SerializeField] private float proximityThreshold = 0.1f;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.white;

    [Header("Visualization")]
    [SerializeField] private float sphereRadius = 0.01f;
    [SerializeField] private Color sphereColor = Color.red;
    [SerializeField, Range(0, 31)] private int visualizationLayer = 0;
    [SerializeField] private int initialPoolSize = 2000;
    [SerializeField] private GameObject spherePrefab; // Add this field

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
    public float distanceFromHMD = .5f;
    public Vector3 offsetFromHMD = new Vector3(0f, -0.2f, 0f);
    public float additionalRotationAngle = 116; // Adjust this value to rotate more or less
    public GameObject dictationCanvas;
    private List<GameObject> dictSpheres = new List<GameObject>();
    public float dictationDistance = 0.1f;
    public float dictationSpacing = 0.05f;
    public event Action OnCanvasRepositioned;
     public event Action OnDictationStart;
    public event Action OnDictationComplete;
    public delegate void UpdateDelegate();
    public UpdateDelegate currentUpdate;
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
            recorder.OnRecordingLoaded += UpdateCanvas;
        }
        else
        {
            Debug.LogError("SimpleRecorder reference is missing in CanvasManager.");
        }

        CreateVisualizationParent();
        InitializeSpherePool();
        canvasParent = new GameObject("CanvasParent");
        UpdateCanvasPlaneGeometry();
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
            Vector3 lastKeptPosition = spheresToKeep[spheresToKeep.Count - 1].transform.localPosition;
            Vector3 currentPosition = activeSpheres[i].transform.localPosition;

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
            Vector3 worldPosition = canvasPlane.transform.TransformPoint(sphere.transform.localPosition);
            SimpleFrame newFrame = new SimpleFrame(worldPosition);
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
    private GameObject CreateColoredSphere(Vector3 position, Color color)
    {
        // Create the sphere as a child of the canvas
        GameObject sphere = Instantiate(spherePrefab, position, Quaternion.identity, canvasPlane.transform);
        
        // Convert the world position to a local position relative to the canvas
        sphere.transform.localPosition = canvasPlane.transform.InverseTransformPoint(position);
        
        sphere.transform.localScale = Vector3.one * (sphereRadius * 2);
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
        else
        {
            Debug.LogWarning("Sphere prefab does not have a Renderer component.");
        }
        return sphere;
    }

    private float updateRate = 0.01f; // Run 100x per second
    private float nextUpdateTime = 0f;

    private void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            currentUpdate?.Invoke();
            if (!isInitialized || !isRecording) return;

            UpdateHmdSide();
            CheckHandProximity();
            UpdateCanvasColor();
            nextUpdateTime = Time.time + updateRate;
           
        }
         
    }
    private IEnumerator RemoveSphereAfterDelay(GameObject sphere, float delay)
    {
        yield return new WaitForSeconds(delay);
        dictSpheres.Remove(sphere);
        Destroy(sphere);
    }
public void StartDictation()
    {
        if (recorder.currentRecord == null || recorder.currentRecord.frames.Count == 0)
        {
            Debug.LogWarning("No current record available for dictation.");
            return;
        }

        ClearDictationSpheres();

        // Create initial spheres for the record points
        foreach (var frame in recorder.currentRecord.frames)
        {
            GameObject sphere = CreateColoredSphere(frame.position, Color.red);
            sphere.transform.SetParent(canvasPlane.transform, true);
            sphere.SetActive(false);  // Hide the sphere initially
            dictSpheres.Add(sphere);
        }

        if (dictationCanvas != null)
        {
            dictationCanvas.SetActive(true);
        }

        OnDictationStart?.Invoke();
        currentUpdate = DictationUpdate;
    }

    private void DictationUpdate()
    {
        ConstrainedHandUpdate();
        Vector3 handPosition = recorder.playerToRecord.righthand.transform.position;

        if (dictSpheres.Count == 0 || Vector3.Distance(handPosition, dictSpheres[dictSpheres.Count - 1].transform.position) >= dictationSpacing)
        {
            GameObject newSphere = CreateColoredSphere(handPosition, Color.red);
            newSphere.transform.SetParent(canvasPlane.transform, true);
            dictSpheres.Add(newSphere);

            CheckSphereProximity(newSphere);
            StartCoroutine(RemoveSphereAfterDelay(newSphere, 1.5f));
        }

        if (AllPointsMapped())
        {
            OnDictationComplete?.Invoke();
            currentUpdate = null;
        }
    }

    private void CheckSphereProximity(GameObject sphere)
    {
        for (int i = 0; i < recorder.currentRecord.frames.Count; i++)
        {
            if (Vector3.Distance(sphere.transform.position, recorder.currentRecord.frames[i].position) <= dictationDistance)
            {
                sphere.GetComponent<Renderer>().material.color = Color.green;
                dictSpheres[i].SetActive(true);  // Show the corresponding record sphere
                break;
            }
        }
    }

    private bool AllPointsMapped()
    {
        return dictSpheres.Take(recorder.currentRecord.frames.Count).All(sphere => sphere.activeSelf);
    }

    private void ClearDictationSpheres()
    {
        foreach (var sphere in dictSpheres)
        {
            Destroy(sphere);
        }
        dictSpheres.Clear();
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

    private void CreateVisualizationSphere(Vector3 worldPosition)
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

        // Convert world position to local position relative to the canvas
        Vector3 localPosition = canvasPlane.transform.InverseTransformPoint(worldPosition);
        sphere.transform.localPosition = localPosition;
        sphere.SetActive(true);
        activeSpheres.Add(sphere);
    }

    public void ClearVisualization()
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
        if (recorder.playerToRecord == null || recorder.playerToRecord.hmd == null)
        {
            Debug.LogError("Player or HMD reference is missing!");
            return;
        }

        Transform hmdTransform = recorder.playerToRecord.hmd.transform;
        
        // Get the HMD's forward vector
        Vector3 hmdForward = hmdTransform.forward;
        
        // Project the forward vector onto the XZ plane
        Vector3 forwardProjected = Vector3.ProjectOnPlane(hmdForward, Vector3.up).normalized;
        
        // Calculate the new position
        Vector3 newPosition = hmdTransform.position + forwardProjected * distanceFromHMD;
        
        // Keep the Y position the same as the HMD
        newPosition.y = hmdTransform.position.y;
        
        // Apply the offset
        newPosition += offsetFromHMD;
        
        // Set the position of the canvas plane
        canvasPlane.transform.position = newPosition;

        // Calculate the rotation
        Vector3 right = Vector3.Cross(Vector3.up, forwardProjected).normalized;
        Vector3 up = Vector3.Cross(forwardProjected, right).normalized;
        
        // Create a rotation that makes the canvas face the HMD
        Quaternion targetRotation = Quaternion.LookRotation(forwardProjected, up);
        
        // Apply the -90 degree rotation around the local X axis
        targetRotation *= Quaternion.Euler(-90, 0, 0);
        
        // Apply the rotation
        canvasPlane.transform.rotation = targetRotation;

        UpdateCanvasPlaneGeometry();
        
        OnCanvasRepositioned?.Invoke();
    }
     public void UpdateCanvasPlaneGeometry()
    {
        if (canvasPlane != null)
        {
            canvasPlaneGeometry = new Plane(canvasPlane.transform.up, canvasPlane.transform.position);
            Debug.Log("Canvas plane geometry updated");
        }
    }

    private void UpdateSpherePositions()
    {
        // Assuming the spheres are child objects of the canvas plane
        for (int i = 0; i < activeSpheres.Count; i++)
        {
            activeSpheres[i].transform.position = canvasPlane.transform.TransformPoint(activeSpheres[i].transform.localPosition);
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