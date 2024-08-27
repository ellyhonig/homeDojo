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

    private void Start()
    {
        playerToRecord = GetComponent<simplePlayer>();
        currentRecord = new SimpleRecord();
        saveManager = GetComponent<SaveManager>();
        if (saveManager == null)
        {
            saveManager = gameObject.AddComponent<SaveManager>();
        }
        Debug.Log("SimpleRecorder started.");
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
        SimpleRecord loadedRecord = saveManager.LoadRecording();
        if (loadedRecord != null)
        {
            currentRecord = loadedRecord;
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