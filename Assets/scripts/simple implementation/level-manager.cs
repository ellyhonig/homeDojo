using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class LevelManager : MonoBehaviour
{
    public enum GameMode
    {
        PhonemeChecking,
        TraceChecking,
        ObjectPlacing
    }

    public GameMode currentMode { get; private set; }

    private int _currentLevel;
    public int currentLevel
    {
        get => _currentLevel;
        set
        {
            if (LevelExists(value))
            {
                _currentLevel = value;
                currentSound = levelPlan[value].Phoneme;
                Debug.Log($"Current Level: {_currentLevel}, Letter: {levelPlan[value].Letter}, Phoneme: {currentSound}");
            }
            else
            {
                Debug.LogWarning($"Attempted to set currentLevel to {value}, but this level does not exist.");
            }
        }
    }

    public string currentSound { get; private set; } = "CUH";

    public void SetLevel(int level)
    {
        if (LevelExists(level))
        {
            currentLevel = level;
            LoadLevel(currentLevel);
        }
        else
        {
            Debug.LogWarning($"Attempted to set level to {level}, but it does not exist.");
        }
    }
    
    public event Action OnPhonemeCheckStart;

    [SerializeField] private SimpleRecorder recorder;
    [SerializeField] private LetterTracingSystem tracingSystem;
    [SerializeField] private ObjectOfInterestManager objectManager;
    [SerializeField] private PocketSphinxPhonemeRecognition phonemeRecognizer;
    private SaveManager saveManager;

    private List<LevelData> levelPlan = new List<LevelData>();

    private void Start()
    {
        if (recorder == null) recorder = GetComponent<SimpleRecorder>();
        if (tracingSystem == null) tracingSystem = GetComponent<LetterTracingSystem>();
        if (objectManager == null) objectManager = GetComponent<ObjectOfInterestManager>();
        if (phonemeRecognizer == null) phonemeRecognizer = GetComponent<PocketSphinxPhonemeRecognition>();
        saveManager = GetComponent<SaveManager>();
        phonemeRecognizer.OnPhonemeDetected += OnPhonemeDetected;
        tracingSystem.OnTraceCompleted += OnTraceCompleted;
        objectManager.OnObjectCollected += OnObjectCollected;

        LoadLevelPlan();
        //Restart();
    }

    private void LoadLevelPlan()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "LevelPlan.json");
        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            levelPlan = JsonConvert.DeserializeObject<List<LevelData>>(jsonContent);
        }
        else
        {
            Debug.LogError("LevelPlan.json not found!");
        }
    }

    public void Restart()
    {
        currentLevel = 0;
        LoadLevel(currentLevel);
        SetGameMode(GameMode.PhonemeChecking);
    }

    private bool LevelExists(int level)
    {
        return level >= 0 && level < levelPlan.Count;
    }

    private void SetGameMode(GameMode newMode)
    {
        currentMode = newMode;
        UpdateComponentStates();

        if (currentMode == GameMode.PhonemeChecking)
        {
            OnPhonemeCheckStart?.Invoke();
            phonemeRecognizer.StartPhonemeRecognition();
        }
    }

    private void LoadLevel(int level)
    {
        if (LevelExists(level))
        {
            currentLevel = level;
            currentSound = levelPlan[level].Phoneme;
            recorder.LoadRecording(currentLevel);
        }
        else
        {
            Debug.LogWarning($"Attempted to set level to {level}, but it does not exist.");
        }
    }

    private void UpdateComponentStates()
    {
        phonemeRecognizer.enabled = (currentMode == GameMode.PhonemeChecking);
        tracingSystem.enabled = (currentMode == GameMode.TraceChecking);
        objectManager.enabled = (currentMode == GameMode.ObjectPlacing);
    }

    private void OnPhonemeDetected(string detectedPhoneme)
    {
        Debug.Log($"Phoneme detected: {detectedPhoneme}");
        if (currentMode == GameMode.PhonemeChecking && detectedPhoneme == currentSound)
        {
            SetGameMode(GameMode.TraceChecking);
            tracingSystem.StartTracing();
            Debug.Log("Correct phoneme detected, starting tracing");
        }
    }

    private void OnTraceCompleted()
    {
        if (currentMode == GameMode.TraceChecking)
        {
            SetGameMode(GameMode.ObjectPlacing);
        }
    }

    private void OnObjectCollected()
    {
        if (currentMode == GameMode.ObjectPlacing)
        {
            currentLevel++;
            LoadLevel(currentLevel);
            SetGameMode(GameMode.PhonemeChecking);
        }
    }

    private void OnDestroy()
    {
        if (phonemeRecognizer != null) phonemeRecognizer.OnPhonemeDetected -= OnPhonemeDetected;
        if (tracingSystem != null) tracingSystem.OnTraceCompleted -= OnTraceCompleted;
        if (objectManager != null) objectManager.OnObjectCollected -= OnObjectCollected;
    }
}

[System.Serializable]
public class LevelData
{
    public string Letter;
    public string Phoneme;
}