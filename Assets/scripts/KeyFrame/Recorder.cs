using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO; // For file operations
using Newtonsoft.Json; // Make sure to have this namespace available

public class Recorder : MonoBehaviour
{
    public player PlayerToRecord;
    public MirroredPlayer mirroredPlayer;
    public Record currentRecord;
    public delegate void UpdateDelegate();
    public UpdateDelegate currentUpdate;
    private string saveFilePath;
    void Start()
    {
        var personComponent = GetComponent<person>();
        if (personComponent != null)
        {
            PlayerToRecord = personComponent.player1;
            mirroredPlayer = personComponent.mirroredPlayer;
        }

        currentRecord = new Record();
        currentUpdate = null;
        saveFilePath = Path.Combine(Application.persistentDataPath, "recording.json");
    }

    private float updateRate = 0.01f; // Run 100x per second
    private float nextUpdateTime = 0f;

    void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            currentUpdate?.Invoke();
            nextUpdateTime = Time.time + updateRate;
        }
    }

    public void StartRecording()
    {
        currentRecord = new Record();
        currentUpdate = RecordFrame;
    }

    public void PauseRecording()
    {
        currentUpdate = null;
    }

    public void PlayRecording()
    {
        mirroredPlayer.EnableMirroredPlayerParent();
        currentRecord.currentFrame = 0;
        currentUpdate = PlayFrame;
    }

    private void RecordFrame()
    {
        var frame = new Frame();
        frame.CapturePlayerState(PlayerToRecord);
        currentRecord.frames.Add(frame);
    }

    private void PlayFrame()
    {
        if (currentRecord.currentFrame >= currentRecord.frames.Count)
        {
            currentUpdate = PlayRecording; // Stop playback when done
            return;
        }

        var frame = currentRecord.frames[currentRecord.currentFrame];
        frame.ApplyToPlayer(mirroredPlayer.mirroredPlayer);

        currentRecord.currentFrame++;
    }
    public void SaveRecording()
    {
        string json = JsonUtility.ToJson(currentRecord, true);
        File.WriteAllText(saveFilePath, json);
    }
    
     public void LoadRecording()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            currentRecord = JsonUtility.FromJson<Record>(json);
        }
    }
    
}
[System.Serializable]
public class Frame
{
    public List<FrameData> data = new List<FrameData>();

    public void CapturePlayerState(player playerToCapture)
    {
        data.Clear();
        if (playerToCapture == null)
        {
            Debug.LogError("playerToCapture is null");
            return;
        }

        foreach (var item in playerToCapture.bodyPartsDictionary)
        {
            Transform partTransform = item.Value.transform;
            data.Add(new FrameData(item.Key, partTransform.localPosition, partTransform.localRotation));
        }
    }

    public void ApplyToPlayer(player playerToApply)
    {
        foreach (FrameData frameData in data)
        {
            Transform targetTransform = playerToApply.GetTransformByName(frameData.partName);
            if (targetTransform != null)
            {
                targetTransform.localPosition = frameData.position;
                targetTransform.localRotation = frameData.rotation;
            }
        }
    }
}
[System.Serializable]
public class FrameData
{
    public string partName;
    public Vector3 position;
    public Quaternion rotation;

    public FrameData(string name, Vector3 pos, Quaternion rot)
    {
        partName = name;
        position = pos;
        rotation = rot;
    }
}
[System.Serializable]
public class Record
{
    public List<Frame> frames = new List<Frame>();
    public int currentFrame;
    public Record()
    {
        currentFrame = 0;
    }
    
}
