using UnityEngine;
using System;
using System.Collections.Generic;

public class SimpleRecorder : MonoBehaviour
{
    public simplePlayer playerToRecord;
    public SimpleRecord currentRecord;
    [SerializeField] private CanvasManager canvasManager;

    public event Action OnRecordingStarted;
    public event Action OnRecordingStopped;
    public event Action OnRecordingLoaded;
    [SerializeField] private bool _isRecording = false;
    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            _isRecording = value;
            if (_isRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }
    }

    [SerializeField] private GameObject pointPrefab;
    [SerializeField] private Transform pointParent;

    private float timer = 0f;
    private SaveManager saveManager;

    private float updateRate = 0.1f; // Run 10x per second
    private float nextUpdateTime = 0f;

    void Start()
    {
        playerToRecord = GetComponent<simplePlayer>();
        currentRecord = new SimpleRecord();
        Debug.Log("SimpleRecorder started.");
        canvasManager = GetComponent<CanvasManager>();
    }

    void Awake()
    {
        InitializeSaveManager();
    }

    private void InitializeSaveManager()
    {
        saveManager = GetComponent<SaveManager>();
        if (saveManager == null)
        {
            Debug.Log("SaveManager not found. Adding a new one.");
            saveManager = gameObject.AddComponent<SaveManager>();
        }
    }

    private void Update()
    {
        if (_isRecording && Time.time >= nextUpdateTime)
        {
            RecordFrame();
            nextUpdateTime = Time.time + updateRate;
        }
    }

    private void StartRecording()
    {
        currentRecord = new SimpleRecord();
        OnRecordingStarted?.Invoke();
        Debug.Log("Recording started");
    }

    private void StopRecording()
    {
        Debug.Log("StopRecording called. Subscriber count: " + (OnRecordingStopped?.GetInvocationList().Length ?? 0));
        OnRecordingStopped?.Invoke();
        Debug.Log("Recording stopped");
    }

    private void RecordFrame()
    {
        Vector3 localPosition = canvasManager.canvasPlane.transform.InverseTransformPoint(playerToRecord.righthand.transform.position);
        var frame = new SimpleFrame(localPosition);
        currentRecord.frames.Add(frame);
    }

    public void SaveRecording()
    {
        SaveRecording(0);  // Default to level 0 for backward compatibility
    }

    public void SaveRecording(int level)
    {
        currentRecord.canvasTransform = new SerializableTransform(canvasManager.canvasPlane.transform);
        saveManager.SaveRecording(currentRecord, level);
        Debug.Log($"Recording saved for level {level}. Frame count: {currentRecord.frames.Count}");
    }

    public void LoadRecording()
    {
        LoadRecording(0);  // Default to level 0 for backward compatibility
    }

    public void LoadRecording(int level)
    {
        if (saveManager == null)
        {
            Debug.LogError("SaveManager is null. Initializing SaveManager.");
            InitializeSaveManager();
        }

        try
        {
            SimpleRecord loadedRecord = saveManager.LoadRecording(level);
            if (loadedRecord != null && loadedRecord.frames != null && loadedRecord.frames.Count > 0)
            {
                currentRecord = loadedRecord;
                loadedRecord.canvasTransform.ApplyTo(canvasManager.canvasPlane.transform);
                OnRecordingLoaded?.Invoke();
                Debug.Log($"Level {level} loaded successfully. Frame count: {currentRecord.frames.Count}");
            }
            else
            {
                Debug.LogWarning($"Failed to load level {level} or loaded record is empty. Creating a new empty record.");
                currentRecord = new SimpleRecord();
                OnRecordingLoaded?.Invoke();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading recording: {e.Message}\n{e.StackTrace}");
            currentRecord = new SimpleRecord();
            OnRecordingLoaded?.Invoke();
        }
    }

    private void ApplyCanvasTransform(SerializableTransform canvasTransform)
    {
        if (canvasManager != null && canvasManager.canvasPlane != null)
        {
            canvasTransform.ApplyTo(canvasManager.canvasPlane.transform);
            canvasManager.UpdateCanvasPlaneGeometry();
            Debug.Log("Canvas transform applied and geometry updated.");
        }
        else
        {
            Debug.LogError("CanvasManager or canvasPlane is null. Cannot apply canvas transform.");
        }
    }
}

[System.Serializable]
public class SimpleRecord
{
    public List<SimpleFrame> frames = new List<SimpleFrame>();
    public SerializableTransform canvasTransform;
}
[System.Serializable]
public class SimpleFrame
{
    public Vector3 position; // Changed from position to localPosition

    public SimpleFrame(Vector3 pos)
    {
        position = pos;
    }
}


[System.Serializable]
public class SerializableTransform
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public SerializableTransform(Transform transform)
    {
        position = transform.position;
        rotation = transform.rotation;
        scale = transform.localScale;
    }

    public void ApplyTo(Transform transform)
    {
        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;
    }
}