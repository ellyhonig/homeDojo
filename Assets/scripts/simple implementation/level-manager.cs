using UnityEngine;
using System;

public class LevelManager : MonoBehaviour
{
    public enum GameMode
    {
        VolumeChecking,
        TraceChecking,
        ObjectPlacing
    }

    public GameMode currentMode { get; set; }
    public int currentLevel { get; set; }

    public event Action OnVolumeCheckStart;

    [SerializeField] private SimpleRecorder recorder;
    [SerializeField] private LetterTracingSystem tracingSystem;
    [SerializeField] private ObjectOfInterestManager objectManager;
    [SerializeField] private SimpleMicVolumeChecker volumeChecker;

    private void Start()
    {
        if (recorder == null) recorder = GetComponent<SimpleRecorder>();
        if (tracingSystem == null) tracingSystem = GetComponent<LetterTracingSystem>();
        if (objectManager == null) objectManager = GetComponent<ObjectOfInterestManager>();
        if (volumeChecker == null) volumeChecker = GetComponent<SimpleMicVolumeChecker>();

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

    private void SetGameMode(GameMode newMode)
    {
        currentMode = newMode;
        UpdateComponentStates();

        if (currentMode == GameMode.VolumeChecking)
        {
            OnVolumeCheckStart?.Invoke();
        }
    }

    private void LoadLevel(int level)
    {
        currentLevel = level;
        recorder.LoadRecording(currentLevel);
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