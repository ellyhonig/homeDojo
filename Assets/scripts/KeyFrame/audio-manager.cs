using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip popSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private LetterTracingSystem tracingSystem;

    private void Start()
    {
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
        }
        else
        {
            Debug.LogError("LetterTracingSystem not found. Please assign it in the inspector or ensure it's on the same GameObject.");
        }
    }

    private void PlayPopSound()
    {
        if (audioSource != null && popSound != null)
        {
            audioSource.PlayOneShot(popSound);
        }
        else
        {
            Debug.LogWarning("AudioSource or pop sound is missing. Please assign them in the inspector.");
        }
    }
    private void PlayWinSound()
    {
        if (audioSource != null && winSound != null)
        {
            audioSource.PlayOneShot(winSound);
        }
        else
        {
            Debug.LogWarning("AudioSource or winSound sound is missing. Please assign them in the inspector.");
        }
    }
    private void OnDisable()
    {
        if (tracingSystem != null)
        {
            tracingSystem.OnKeyframeReached -= PlayPopSound;
        }
    }
}
