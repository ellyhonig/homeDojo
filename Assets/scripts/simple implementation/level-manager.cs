using UnityEngine;
using System;
using System.IO;

public class LevelManager : MonoBehaviour
{
    public enum GameMode
    {
        VolumeChecking,
        TraceChecking,
        ObjectPlacing
    }

    public GameMode currentMode { get; set; }

    private int _currentLevel;
    public int currentLevel
    {
        get => _currentLevel;
        set
        {
            if (LevelExists(value))
            {
                _currentLevel = value;
            }
            else
            {
                Debug.LogWarning($"Attempted to set currentLevel to {value}, but this level does not exist.");
            }
        }
    }
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
    public event Action OnVolumeCheckStart;

    [SerializeField] private SimpleRecorder recorder;
    [SerializeField] private LetterTracingSystem tracingSystem;
    [SerializeField] private ObjectOfInterestManager objectManager;
    [SerializeField] private SimpleMicVolumeChecker volumeChecker;
    private SaveManager saveManager;

    private void Start()
    {
        if (recorder == null) recorder = GetComponent<SimpleRecorder>();
        if (tracingSystem == null) tracingSystem = GetComponent<LetterTracingSystem>();
        if (objectManager == null) objectManager = GetComponent<ObjectOfInterestManager>();
        if (volumeChecker == null) volumeChecker = GetComponent<SimpleMicVolumeChecker>();
        saveManager = GetComponent<SaveManager>();
        volumeChecker.onVoiceDetected.AddListener(OnVoiceDetected);
        tracingSystem.OnTraceCompleted += OnTraceCompleted;
        objectManager.OnObjectCollected += OnObjectCollected;

        //Restart();
    }

    public void Restart()
    {
        currentLevel = 0;
        LoadLevel(currentLevel);
        SetGameMode(GameMode.VolumeChecking);
    }
    private bool LevelExists(int level)
    {
        string filePath = Path.Combine(Application.persistentDataPath, $"simple_recording{level}.json");
        return File.Exists(filePath);
    }
    private void SetGameMode(GameMode newMode)
    {
        currentMode = newMode;
        UpdateComponentStates();

        if (currentMode == GameMode.VolumeChecking)
        {
            OnVolumeCheckStart?.Invoke();
            StartCoroutine(volumeChecker.MicrophoneCheck());
        }
    }

    private void LoadLevel(int level)
    {
        if (LevelExists(level))
        {
            currentLevel = level;
            recorder.LoadRecording(currentLevel);
        }
        else
        {
            Debug.LogWarning($"Attempted to set level to {level}, but it does not exist.");
        }
        
    }

    private void UpdateComponentStates()
    {
        volumeChecker.enabled = (currentMode == GameMode.VolumeChecking);
        tracingSystem.enabled = (currentMode == GameMode.TraceChecking);
        objectManager.enabled = (currentMode == GameMode.ObjectPlacing);
    }

    private void OnVoiceDetected()
    {
        if (currentMode == GameMode.VolumeChecking)
        {
            SetGameMode(GameMode.TraceChecking);
            tracingSystem.StartTracing();
            Debug.Log("voice complete");

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
            SetGameMode(GameMode.VolumeChecking);
        }
    }

    private void OnDestroy()
    {
        if (volumeChecker != null) volumeChecker.onVoiceDetected.RemoveListener(OnVoiceDetected);
        if (tracingSystem != null) tracingSystem.OnTraceCompleted -= OnTraceCompleted;
        if (objectManager != null) objectManager.OnObjectCollected -= OnObjectCollected;
    }
}