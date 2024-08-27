using UnityEngine;
using System.Collections;
using System;

public class ObjectOfInterestManager : MonoBehaviour
{
    public enum ObjectState
    {
        Opaque,
        Locked,
        Held,
        Thrown,
        Collected
    }

    public GameObject objectOfInterest;
    public GameObject containerObject;
    public float maxDistance = 2f;
    public float proximityThreshold = 0.1f;
    public float throwThreshold = 5f;
    public float throwDuration = 0.4f;
    public float resetDelay = 3f;
    public float hmdProximityThreshold = 0.3f;
    public GameObject spherePrefab;
    public int sphereCount = 3;
    public float sphereEjectionForce = 5f;
    public float sphereEjectionOffset = 0.2f;
    public float containerProximityThreshold = 0.5f;

    public event Action<ObjectState> OnStateChanged;
    public event Action OnHMDProximity;
    public event Action OnObjectCollected;

    private LetterTracingSystem letterTracingSystem;
    private SimpleRecorder recorder;
    private Vector3 initialPosition;
    private ObjectState currentState = ObjectState.Opaque;
    private Rigidbody rb;
    private Vector3 handVelocity;
    private Vector3 lastHandPosition;
    private float handTrackTimer;

    private void Start()
    {
        letterTracingSystem = GetComponent<LetterTracingSystem>();
        recorder = GetComponent<SimpleRecorder>();

        if (letterTracingSystem == null || recorder == null)
        {
            Debug.LogError("Required components are missing!");
            return;
        }

        letterTracingSystem.OnTraceCompleted += OnTraceCompleted;
        recorder.OnRecordingLoaded += SetInitialPosition;

        if (objectOfInterest == null)
        {
            CreateDefaultObject();
        }

        if (spherePrefab == null)
        {
            CreateDefaultSpherePrefab();
        }

        if (containerObject == null)
        {
            CreateDefaultContainerObject();
        }

        SetInitialPosition();
        SetState(ObjectState.Opaque);
    }


    private void CreateDefaultContainerObject()
    {
        containerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        containerObject.transform.localScale = Vector3.one;
        containerObject.name = "ContainerObject";
        containerObject.transform.position = new Vector3(1f, 1f, 1f); // Set a default position
    }

    private void CreateDefaultObject()
    {
        objectOfInterest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objectOfInterest.transform.localScale = Vector3.one * 0.1f;
        objectOfInterest.name = "ObjectOfInterest";
    }

    private void CreateDefaultSpherePrefab()
    {
        spherePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spherePrefab.transform.localScale = Vector3.one * 0.05f;
        spherePrefab.name = "SpherePrefab";
        spherePrefab.SetActive(false);
    }

    private void SetInitialPosition()
    {
        if (recorder.currentRecord.frames.Count > 0)
        {
            Vector3 lastPosition = recorder.currentRecord.frames[recorder.currentRecord.frames.Count - 1].position;
            initialPosition = lastPosition - Vector3.up * 0.1f;
            objectOfInterest.transform.position = initialPosition;
        }
        else
        {
            Debug.LogWarning("No recorded frames found. Using current position.");
            initialPosition = objectOfInterest.transform.position;
        }
    }

   private void SetState(ObjectState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(currentState);

            switch (currentState)
            {
                case ObjectState.Opaque:
                    SetOpacity(1f);
                    RemoveRigidbody();
                    break;
                case ObjectState.Locked:
                    SetOpacity(0.5f);
                    AddLockedRigidbody();
                    break;
                case ObjectState.Held:
                    SetOpacity(1f);
                    if (rb != null) rb.isKinematic = true;
                    break;
                case ObjectState.Thrown:
                    SetOpacity(1f);
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.velocity = handVelocity;
                    }
                    StartCoroutine(ResetAfterDelay());
                    break;
                case ObjectState.Collected:
                    // The object is already set to inactive in CollectObject method
                    break;
            }
        }
    }
    private void SetOpacity(float opacity)
    {
        Renderer renderer = objectOfInterest.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            Color color = renderer.material.color;
            color.a = opacity;
            renderer.material.color = color;
        }
    }

    private void AddLockedRigidbody()
    {
        if (rb == null)
            rb = objectOfInterest.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void RemoveRigidbody()
    {
        if (rb != null)
        {
            Destroy(rb);
            rb = null;
        }
    }

    private void OnTraceCompleted()
    {
        SetState(ObjectState.Locked);
    }

     private void Update()
    {
        switch (currentState)
        {
            case ObjectState.Locked:
                CheckProximity();
                break;
            case ObjectState.Held:
                UpdateHandVelocity();
                CheckThrow();
                CheckContainerProximity();
                break;
        }

        CheckHMDProximity();
    }

    private void CheckContainerProximity()
    {
        if (Vector3.Distance(objectOfInterest.transform.position, containerObject.transform.position) <= containerProximityThreshold)
        {
            CollectObject();
        }
    }

    private void CollectObject()
    {
        SetState(ObjectState.Collected);
        OnObjectCollected?.Invoke();
        objectOfInterest.SetActive(false);
    }
    private void CheckProximity()
    {
        Vector3 rightHandPos = recorder.playerToRecord.righthand.transform.position;
        Vector3 leftHandPos = recorder.playerToRecord.lefthand.transform.position;

        if (Vector3.Distance(objectOfInterest.transform.position, rightHandPos) <= proximityThreshold ||
            Vector3.Distance(objectOfInterest.transform.position, leftHandPos) <= proximityThreshold)
        {
            SetState(ObjectState.Held);
            lastHandPosition = Vector3.Distance(objectOfInterest.transform.position, rightHandPos) <= proximityThreshold ? rightHandPos : leftHandPos;
        }
    }

    private void UpdateHandVelocity()
    {
        Vector3 rightHandPos = recorder.playerToRecord.righthand.transform.position;
        Vector3 leftHandPos = recorder.playerToRecord.lefthand.transform.position;
        Vector3 closestHand = Vector3.Distance(objectOfInterest.transform.position, rightHandPos) <= Vector3.Distance(objectOfInterest.transform.position, leftHandPos) ? rightHandPos : leftHandPos;

        handVelocity = (closestHand - lastHandPosition) / Time.deltaTime;
        lastHandPosition = closestHand;

        objectOfInterest.transform.position = closestHand;
    }

    private void CheckThrow()
    {
        if (handVelocity.magnitude >= throwThreshold)
        {
            handTrackTimer += Time.deltaTime;
            if (handTrackTimer >= throwDuration)
            {
                SetState(ObjectState.Thrown);
                handTrackTimer = 0f;
            }
        }
        else
        {
            handTrackTimer = 0f;
        }
    }

    private void CheckHMDProximity()
    {
        Vector3 hmdPosition = recorder.playerToRecord.hmd.transform.position;
        if (Vector3.Distance(objectOfInterest.transform.position, hmdPosition) <= hmdProximityThreshold)
        {
            OnHMDProximity?.Invoke();
            EjectSpheres();
            ResetToInitialPosition();
        }
    }

    private void ResetToInitialPosition()
    {
        objectOfInterest.transform.position = initialPosition;
        SetState(ObjectState.Locked);
    }
    private void EjectSpheres()
    {
        Vector3 hmdForward = recorder.playerToRecord.hmd.transform.forward;
        Vector3 hmdPosition = recorder.playerToRecord.hmd.transform.position;
        Vector3 hmdUp = recorder.playerToRecord.hmd.transform.up;

        // Calculate the ejection start position
        Vector3 ejectionStartPosition = hmdPosition + hmdForward * 0.2f - hmdUp * sphereEjectionOffset;

        for (int i = 0; i < sphereCount; i++)
        {
            GameObject sphere = Instantiate(spherePrefab, ejectionStartPosition, Quaternion.identity);
            sphere.SetActive(true);

            Rigidbody sphereRb = sphere.AddComponent<Rigidbody>();
            
            // Calculate ejection direction with a downward angle
            Vector3 ejectionDirection = Quaternion.Euler(
                UnityEngine.Random.Range(-15f, -45f), // Downward angle
                UnityEngine.Random.Range(-30f, 30f),  // Horizontal spread
                0
            ) * hmdForward;

            sphereRb.AddForce(ejectionDirection * sphereEjectionForce, ForceMode.Impulse);

            StartCoroutine(DestroySphereAfterDelay(sphere, 2f));
        }
    }

    private IEnumerator DestroySphereAfterDelay(GameObject sphere, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(sphere);
    }

    private IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);
        objectOfInterest.transform.position = initialPosition;
        SetState(ObjectState.Locked);
    }

    private void OnDestroy()
    {
        if (letterTracingSystem != null)
        {
            letterTracingSystem.OnTraceCompleted -= OnTraceCompleted;
        }
        if (recorder != null)
        {
            recorder.OnRecordingLoaded -= SetInitialPosition;
        }
    }
}