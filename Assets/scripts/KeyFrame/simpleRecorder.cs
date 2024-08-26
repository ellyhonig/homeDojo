using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class SimpleRecorder : MonoBehaviour
{
    public simplePlayer playerToRecord;
    private string saveFilePath;
    public SimpleRecord currentRecord;
    
    public event Action OnRecordingStarted;
    public event Action OnRecordingStopped;

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

    private float recordingInterval = 0.3f;
    private float timer = 0f;

    private void Start()
    {
        playerToRecord = GetComponent<simplePlayer>();
        saveFilePath = Path.Combine(Application.persistentDataPath, "simple_recording.json");
        currentRecord = new SimpleRecord();
        Debug.Log("SimpleRecorder started.");
    }
    private float updateRate = 0.01f; // Run 100x per second
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

    // This method is called when the script is loaded or a value is changed in the Inspector.
    private void OnValidate()
    {
        // Ensure that changes made in the inspector trigger the appropriate methods
        if (Application.isPlaying)
        {
            IsRecording = _isRecording;
        }
    }

    public void SaveRecording()
    {
        string json = JsonConvert.SerializeObject(currentRecord, Formatting.Indented);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"Recording saved to {saveFilePath}");
    }

    public void LoadRecording()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            currentRecord = JsonConvert.DeserializeObject<SimpleRecord>(json);
            Debug.Log($"Recording loaded from {saveFilePath}");
        }
        else
        {
            Debug.LogWarning("No saved recording found.");
        }
    }
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

[System.Serializable]
public class SimpleRecord
{
    public List<SimpleFrame> frames = new List<SimpleFrame>();
}