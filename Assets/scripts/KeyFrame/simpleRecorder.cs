using UnityEngine;
using System;
using System.Collections.Generic;

public class SimpleRecorder : MonoBehaviour
{
    public simplePlayer playerToRecord;
    public SimpleRecord currentRecord;
    
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

    private float recordingInterval = 400f;
    private float timer = 0f;
    private SaveManager saveManager;

    void Start()
    {
        playerToRecord = GetComponent<simplePlayer>();
        currentRecord = new SimpleRecord();
        Debug.Log("SimpleRecorder started.");
        LoadRecording();
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


    private float updateRate = 0.1f; // Run 100x per second
    private float nextUpdateTime = 0f;

    private void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            if (_isRecording)
            {
                timer += Time.deltaTime;
                if (timer >= recordingInterval)
                {
                    RecordFrame();
                    timer = 0f;
                }
            }
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
        var frame = new SimpleFrame(playerToRecord.righthand.transform.position);
        currentRecord.frames.Add(frame);
    }

    

    public void SaveRecording()
    {
        saveManager.SaveRecording(currentRecord);
        
    }

    public void LoadRecording()
    {
        if (saveManager == null)
        {
            Debug.LogError("SaveManager is null. Initializing SaveManager.");
            InitializeSaveManager();
        }

        try
        {
            SimpleRecord loadedRecord = saveManager.LoadRecording();
            if (loadedRecord != null && loadedRecord.frames != null && loadedRecord.frames.Count > 0)
            {
                currentRecord = loadedRecord;
                OnRecordingLoaded?.Invoke();
                Debug.Log($"Recording loaded successfully. Frame count: {currentRecord.frames.Count}");
            }
            else
            {
                Debug.LogWarning("Loaded record is null or empty. Creating a new empty record.");
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

    
}

[System.Serializable]
public class SimpleRecord
{
    public List<SimpleFrame> frames = new List<SimpleFrame>();
}

[System.Serializable]
public class SimpleFrame
{
    public Vector3 position;

    public SimpleFrame(Vector3 pos)
    {
        position = pos;
    }
}