using UnityEngine;
using System.Collections.Generic;

public class VisualEffectManager : MonoBehaviour
{
    [SerializeField] private LetterTracingSystem tracingSystem;
    [SerializeField] private SimpleRecorder recorder;
    [SerializeField] private CanvasManager canvasManager;
    [SerializeField] private Color hitColor = Color.green;
    [SerializeField] private Color currentColor = Color.red;
    [SerializeField] private Color unhitColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float currentScale = 1.5f;
    [SerializeField] private Color lineColor = Color.green;
    [SerializeField] private float lineWidth = 0.05f;

    private List<LineRenderer> lines = new List<LineRenderer>();
    private GameObject lastHitSphere;

    private void Start()
    {
        if (tracingSystem == null) tracingSystem = GetComponent<LetterTracingSystem>();
        if (recorder == null) recorder = GetComponent<SimpleRecorder>();
        if (canvasManager == null) canvasManager = GetComponent<CanvasManager>();

        tracingSystem.OnTraceStarted += InitializeVisuals;
        tracingSystem.OnKeyframeReached += UpdateVisuals;
        tracingSystem.OnTraceCompleted += FinalizeVisuals;
        recorder.OnRecordingLoaded += ClearLines;
    }

    private void InitializeVisuals()
    {
        ClearLines();
        lastHitSphere = null;
        for (int i = 0; i < canvasManager.activeSpheres.Count; i++)
        {
            GameObject sphere = canvasManager.activeSpheres[i];
            SetSphereProperties(sphere, i == 0 ? currentColor : unhitColor, i == 0 ? currentScale : normalScale);
        }
    }

    private void UpdateVisuals()
    {
        int hitIndex = tracingSystem.CurrentKeyframeIndex - 1;
        int currentIndex = tracingSystem.CurrentKeyframeIndex;

        for (int i = 0; i < canvasManager.activeSpheres.Count; i++)
        {
            GameObject sphere = canvasManager.activeSpheres[i];
            if (i < hitIndex)
            {
                SetSphereProperties(sphere, hitColor, normalScale);
            }
            else if (i == currentIndex)
            {
                SetSphereProperties(sphere, currentColor, currentScale);
                if (lastHitSphere != null && sphere != lastHitSphere)
                {
                    CreateLine(lastHitSphere, sphere);
                }
                lastHitSphere = sphere;
            }
            else
            {
                SetSphereProperties(sphere, unhitColor, normalScale);
            }
        }
    }

    private void FinalizeVisuals()
    {
        for (int i = 0; i < canvasManager.activeSpheres.Count; i++)
        {
            GameObject sphere = canvasManager.activeSpheres[i];
            SetSphereProperties(sphere, hitColor, normalScale);
        }
    }

    private void SetSphereProperties(GameObject sphere, Color color, float scale)
    {
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
        sphere.transform.localScale = sphere.transform.localScale * scale;
    }

    private void CreateLine(GameObject start, GameObject end)
    {
        GameObject lineObject = new GameObject("TraceLine");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start.transform.position);
        lineRenderer.SetPosition(1, end.transform.position);
        lines.Add(lineRenderer);
    }

    private void ClearLines()
    {
        foreach (LineRenderer line in lines)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }
        lines.Clear();
    }

    private void OnDisable()
    {
        if (tracingSystem != null)
        {
            tracingSystem.OnTraceStarted -= InitializeVisuals;
            tracingSystem.OnKeyframeReached -= UpdateVisuals;
            tracingSystem.OnTraceCompleted -= FinalizeVisuals;
        }
        ClearLines();
    }
}