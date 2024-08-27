using UnityEngine;
using System;
using System.Collections.Generic;

public class LetterTracingSystem : MonoBehaviour
{
    public enum TracingState
    {
        Idle,
        Tracing,
        Completed
    }

    public TracingState CurrentState { get; private set; }

    public event Action OnTraceStarted;
    public event Action OnKeyframeReached;
    public event Action OnTraceCompleted;

    [SerializeField] private SimpleRecorder recorder;
    [SerializeField] private GameObject handObject;
    [SerializeField] private float proximityThreshold = 0.1f;
    [SerializeField] private float pointDistance = 0.5f; // Distance to determine end of first letter
    [SerializeField] private bool firstLetter = true; // Whether to trace only the first letter

    private int currentKeyframeIndex;
    public int lastLetterKeyframeIndex;

    public int CurrentKeyframeIndex => currentKeyframeIndex;

    private void Start()
    {
        if (recorder == null)
        {
            recorder = GetComponent<SimpleRecorder>();
        }
        
        CurrentState = TracingState.Idle;
    }

    private void Update()
    {
        if (CurrentState == TracingState.Tracing)
        {
            CheckKeyframeProximity();
        }
    }

    private void CheckKeyframeProximity()
    {
        if (currentKeyframeIndex >= recorder.currentRecord.frames.Count || 
            (firstLetter && currentKeyframeIndex > lastLetterKeyframeIndex))
        {
            return;
        }

        Vector3 currentKeyframePosition = recorder.currentRecord.frames[currentKeyframeIndex].position;
        
        if (Vector3.Distance(handObject.transform.position, currentKeyframePosition) < proximityThreshold)
        {
            OnKeyframeReached?.Invoke();
            currentKeyframeIndex++;
            Debug.Log("hit a point");

            if (currentKeyframeIndex >= recorder.currentRecord.frames.Count || 
                (firstLetter && currentKeyframeIndex > lastLetterKeyframeIndex))
            {
                CurrentState = TracingState.Completed;
                OnTraceCompleted?.Invoke();
            }
        }
    }

    public void StartTracing()
    {
        if (recorder.currentRecord.frames.Count == 0)
        {
            Debug.LogWarning("No keyframes to trace. Make sure to record some frames first.");
            return;
        }

        CurrentState = TracingState.Tracing;
        currentKeyframeIndex = 0;
        
        if (firstLetter)
        {
            DetermineLastLetterKeyframe();
        }
        else
        {
            lastLetterKeyframeIndex = recorder.currentRecord.frames.Count - 1;
        }

        OnTraceStarted?.Invoke();
    }

    private void DetermineLastLetterKeyframe()
    {
        lastLetterKeyframeIndex = 0;
        for (int i = 0; i < recorder.currentRecord.frames.Count - 1; i++)
        {
            if (Vector3.Distance(recorder.currentRecord.frames[i].position, 
                                 recorder.currentRecord.frames[i + 1].position) > pointDistance)
            {
                lastLetterKeyframeIndex = i;
                break;
            }
        }
        
        // If no break is found, set to the last frame
        if (lastLetterKeyframeIndex == 0)
        {
            lastLetterKeyframeIndex = recorder.currentRecord.frames.Count - 1;
        }

        Debug.Log($"Last keyframe of first letter: {lastLetterKeyframeIndex}");
    }
}