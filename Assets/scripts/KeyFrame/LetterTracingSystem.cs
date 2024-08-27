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

    private int currentKeyframeIndex;

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
        if (currentKeyframeIndex >= recorder.currentRecord.frames.Count)
        {
            return;
        }

        Vector3 currentKeyframePosition = recorder.currentRecord.frames[currentKeyframeIndex].position;
        
        if (Vector3.Distance(handObject.transform.position, currentKeyframePosition) < proximityThreshold)
        {
            OnKeyframeReached?.Invoke();
            currentKeyframeIndex++;
            Debug.Log("hit a point");

            if (currentKeyframeIndex >= recorder.currentRecord.frames.Count)
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
        OnTraceStarted?.Invoke();
    }
}