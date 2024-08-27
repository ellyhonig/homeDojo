using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip popSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip munchSound;

    [SerializeField] private LetterTracingSystem tracingSystem;
    [SerializeField] private ObjectOfInterestManager objManager;
    private void Start()
    {
        objManager = GetComponent<ObjectOfInterestManager>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (tracingSystem == null)
        {
            tracingSystem = GetComponent<LetterTracingSystem>();
        }

        if (tracingSystem != null)
        {
            tracingSystem.OnKeyframeReached += PlayPopSound;
            tracingSystem.OnTraceCompleted += PlayWinSound;
            objManager.OnHMDProximity += PlayMunchSound;

        }
        else
        {
            Debug.LogError("LetterTracingSystem not found. Please assign it in the inspector or ensure it's on the same GameObject.");
        }
    }
    private void PlaySound(AudioClip clip)
    {
       if (audioSource != null && popSound != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("AudioSource or clip sound is missing. Please assign them in the inspector.");
        } 
    }
    private void PlayPopSound()
    {
        PlaySound(popSound);
    }
    private void PlayWinSound()
    {
        PlaySound(winSound);

    }
    private void PlayMunchSound()
    {
        PlaySound(munchSound);

    }
    private void OnDisable()
    {
        if (tracingSystem != null)
        {
            tracingSystem.OnKeyframeReached -= PlayPopSound;
        }
    }
}
