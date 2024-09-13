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
        ObjectPlacing,
        Dictation
    }

    public enum GamePhase
    {
        Trace,
        Dictate
    }

    public GameMode currentMode { get; private set; }
    public GamePhase currentPhase { get; private set; }

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
    public event Action OnDictationStart;
    public event Action OnAllLevelsCompleted;

    [SerializeField] private SimpleRecorder recorder;
    [SerializeField] private LetterTracingSystem tracingSystem;
    [SerializeField] private ObjectOfInterestManager objectManager;
    [SerializeField] private PocketSphinxPhonemeRecognition phonemeRecognizer;
    [SerializeField] private CanvasManager canvasManager;
    private SaveManager saveManager;

    private List<LevelData> levelPlan = new List<LevelData>();

    private void Start()
    {
        if (recorder == null) recorder = GetComponent<SimpleRecorder>();
        if (tracingSystem == null) tracingSystem = GetComponent<LetterTracingSystem>();
        if (objectManager == null) objectManager = GetComponent<ObjectOfInterestManager>();
        if (phonemeRecognizer == null) phonemeRecognizer = GetComponent<PocketSphinxPhonemeRecognition>();
        if (canvasManager == null) canvasManager = GetComponent<CanvasManager>();
        saveManager = GetComponent<SaveManager>();
        phonemeRecognizer.OnPhonemeDetected += OnPhonemeDetected;
        tracingSystem.OnTraceCompleted += OnTraceCompleted;
        objectManager.OnObjectCollected += OnObjectCollected;
        canvasManager.OnDictationComplete += OnDictationCompleted;

        LoadLevelPlan();
        Restart();
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
        currentPhase = GamePhase.Trace;
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
        else if (currentMode == GameMode.Dictation)
        {
            OnDictationStart?.Invoke();
            canvasManager.StartDictation();
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
        //tracingSystem.enabled = (currentMode == GameMode.TraceChecking);
        //objectManager.enabled = (currentMode == GameMode.ObjectPlacing);
       // canvasManager.enabled = (currentMode == GameMode.Dictation);
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

    private void OnDictationCompleted()
    {
        if (currentMode == GameMode.Dictation)
        {
            SetGameMode(GameMode.ObjectPlacing);
        }
    }

    private void OnObjectCollected()
    {
        if (currentMode == GameMode.ObjectPlacing)
        {
            currentLevel++;
            
            if (currentLevel >= levelPlan.Count)
            {
                if (currentPhase == GamePhase.Trace)
                {
                    // All levels completed in Trace phase, move to Dictate phase
                    currentPhase = GamePhase.Dictate;
                    currentLevel = 0;
                    LoadLevel(currentLevel);
                    SetGameMode(GameMode.Dictation);
                    Debug.Log("All levels completed in Trace phase. Moving to Dictate phase.");
                }
                else
                {
                    // All levels completed in both phases
                    OnAllLevelsCompleted?.Invoke();
                    Debug.Log("All levels completed in both phases. Game finished!");
                    // You can add any game completion logic here
                    return;
                }
            }
            else
            {
                LoadLevel(currentLevel);
                
                if (currentPhase == GamePhase.Trace)
                {
                    SetGameMode(GameMode.PhonemeChecking);
                }
                else
                {
                    SetGameMode(GameMode.Dictation);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (phonemeRecognizer != null) phonemeRecognizer.OnPhonemeDetected -= OnPhonemeDetected;
        if (tracingSystem != null) tracingSystem.OnTraceCompleted -= OnTraceCompleted;
        if (objectManager != null) objectManager.OnObjectCollected -= OnObjectCollected;
        if (canvasManager != null) canvasManager.OnDictationComplete -= OnDictationCompleted;
    }
}

[System.Serializable]
public class LevelData
{
    public string Letter;
    public string Phoneme;
}