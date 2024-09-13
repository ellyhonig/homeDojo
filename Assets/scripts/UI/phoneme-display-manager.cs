using UnityEngine;
using TMPro;
using System.Collections;

public class PhonemeDisplayManager : MonoBehaviour
{
    [SerializeField] private TextMeshPro phonemeText;
    public Color correctColor = Color.green;
    public Color incorrectColor = Color.red;
    public float displayDuration = 1.5f;
    public float checkInterval = 0.2f;

    private string currentPhoneme;
    private Coroutine clearTextCoroutine;
    private LevelManager levelManager;

    private void Awake()
    {
        // If phonemeText is not assigned in the inspector, try to find it
        if (phonemeText == null)
        {
            phonemeText = GetComponent<TextMeshPro>();
            if (phonemeText == null)
            {
                Debug.LogError("PhonemeDisplayManager: TextMeshPro component not found!");
                enabled = false;
                return;
            }
        }
    }

    private void Start()
    {
        // Get references to necessary components
        PocketSphinxPhonemeRecognition phonemeRecognizer = GetComponent<PocketSphinxPhonemeRecognition>();
        levelManager = GetComponent<LevelManager>();

        if (phonemeRecognizer != null)
        {
            phonemeRecognizer.OnPhonemeDetected += DisplayPhoneme;
        }
        else
        {
            Debug.LogError("PhonemeDisplayManager: PocketSphinxPhonemeRecognition component not found!");
        }

        if (levelManager != null)
        {
            currentPhoneme = levelManager.currentSound;
            StartCoroutine(CheckForPhonemeChanges());
        }
        else
        {
            Debug.LogError("PhonemeDisplayManager: LevelManager component not found!");
        }

        // Initially clear the text
        phonemeText.text = "";
    }

    private IEnumerator CheckForPhonemeChanges()
    {
        while (true)
        {
            if (currentPhoneme != levelManager.currentSound)
            {
                currentPhoneme = levelManager.currentSound;
                Debug.Log($"Current phoneme updated to: {currentPhoneme}");
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }

    public void DisplayPhoneme(string detectedPhoneme)
    {
        if (phonemeText == null) return;

        // Stop any ongoing clear text coroutine
        if (clearTextCoroutine != null)
        {
            StopCoroutine(clearTextCoroutine);
        }

        // Display the detected phoneme
        phonemeText.text = detectedPhoneme;

        // Set color based on whether it matches the current phoneme
        phonemeText.color = (detectedPhoneme == currentPhoneme) ? correctColor : incorrectColor;

        // Start the coroutine to clear the text after the specified duration
        clearTextCoroutine = StartCoroutine(ClearTextAfterDelay());
    }

    private IEnumerator ClearTextAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        phonemeText.text = "";
        clearTextCoroutine = null;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        PocketSphinxPhonemeRecognition phonemeRecognizer = GetComponent<PocketSphinxPhonemeRecognition>();
        if (phonemeRecognizer != null)
        {
            phonemeRecognizer.OnPhonemeDetected -= DisplayPhoneme;
        }

        StopAllCoroutines();
    }
}