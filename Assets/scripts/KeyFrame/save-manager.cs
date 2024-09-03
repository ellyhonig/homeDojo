using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class SaveManager : MonoBehaviour
{
    private string saveFilePath;
    private string saveFilePathFormat;

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "simple_recording.json");
        saveFilePathFormat = Path.Combine(Application.persistentDataPath, "simple_recording{0}.json");
    }

    // Existing methods remain unchanged
    public void SaveRecording(SimpleRecord recordToSave)
    {
        try
        {
            string json = JsonUtility.ToJson(recordToSave, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"Recording saved to {saveFilePath}. Frame count: {recordToSave.frames.Count}");
            
            // Log the first coordinate
            if (recordToSave.frames.Count > 0)
            {
                Debug.Log($"First saved coordinate: {recordToSave.frames[0].position}");
            }
            else
            {
                Debug.Log("No frames saved in the recording.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving recording: {e.Message}\nStack Trace: {e.StackTrace}");
        }
    }

    public SimpleRecord LoadRecording()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                SimpleRecord loadedRecord = JsonUtility.FromJson<SimpleRecord>(json);
                
                if (loadedRecord == null || loadedRecord.frames == null)
                {
                    Debug.LogError("Failed to deserialize the recording or frames are null.");
                    return null;
                }

                Debug.Log($"Recording loaded from {saveFilePath}. Frame count: {loadedRecord.frames.Count}");
                
                // Log the first coordinate
                if (loadedRecord.frames.Count > 0)
                {
                    Debug.Log($"First loaded coordinate: {loadedRecord.frames[0].position}");
                }
                else
                {
                    Debug.Log("No frames in the loaded recording.");
                }

                return loadedRecord;
            }
            else
            {
                Debug.LogWarning("No saved recording found at path: " + saveFilePath);
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading recording: {e.Message}\nStack Trace: {e.StackTrace}");
            return null;
        }
    }

    // New methods for level-specific saving and loading
    public void SaveRecording(SimpleRecord recordToSave, int level)
    {
        try
        {
            string json = JsonUtility.ToJson(recordToSave, true);
            string filePath = string.Format(saveFilePathFormat, level);
            File.WriteAllText(filePath, json);
            Debug.Log($"Recording saved to {filePath}. Frame count: {recordToSave.frames.Count}");
            
            if (recordToSave.frames.Count > 0)
            {
                Debug.Log($"First saved coordinate: {recordToSave.frames[0].position}");
            }
            else
            {
                Debug.Log("No frames saved in the recording.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving recording: {e.Message}\nStack Trace: {e.StackTrace}");
        }
    }

    public SimpleRecord LoadRecording(int level)
    {
        try
        {
            string filePath = string.Format(saveFilePathFormat, level);
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                SimpleRecord loadedRecord = JsonUtility.FromJson<SimpleRecord>(json);
                
                if (loadedRecord == null || loadedRecord.frames == null)
                {
                    Debug.LogError("Failed to deserialize the recording or frames are null.");
                    return null;
                }

                Debug.Log($"Recording loaded from {filePath}. Frame count: {loadedRecord.frames.Count}");
                
                if (loadedRecord.frames.Count > 0)
                {
                    Debug.Log($"First loaded coordinate: {loadedRecord.frames[0].position}");
                }
                else
                {
                    Debug.Log("No frames in the loaded recording.");
                }

                return loadedRecord;
            }
            else
            {
                Debug.LogWarning("No saved recording found at path: " + filePath);
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading recording: {e.Message}\nStack Trace: {e.StackTrace}");
            return null;
        }
    }
}