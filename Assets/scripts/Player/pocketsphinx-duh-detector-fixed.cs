using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Windows.Speech;

public class PocketSphinxDuhDetector : MonoBehaviour
{
    private SpeechRecognizerListener listener;
    public float duhScore = 0f;
    private float decayRate = 0.5f;
    
    // Configuration
    public string configPath = "psConfig";
    public string keywordPath = "keywords";
    public string dictionaryPath = "cmudict-en-us";
    public string acousticModelPath = "en-us";
    public string languageModelPath = "en-us.lm";

    void Start()
    {
        listener = new SpeechRecognizerListener();
        
        // Set up the configuration
        listener.CustomInit(configPath, keywordPath, dictionaryPath, acousticModelPath, languageModelPath);
        
        // Add the keyword we want to detect
        listener.AddKeyword("duh", 1e-40f);
        
        // Start listening
        listener.StartListening();
        
        // Subscribe to the OnPhraseRecognized event
        listener.OnPhraseRecognized += OnPhraseRecognized;
    }

    void Update()
    {
        // Decay the duh score over time
        duhScore = Mathf.Max(0, duhScore - (decayRate * Time.deltaTime));
    }

    void OnPhraseRecognized(string phrase, List<string> phraseWords, float confidence)
    {
        if (phraseWords.Contains("duh"))
        {
            duhScore = 1f;
            Debug.Log($"Duh sound detected! Confidence: {confidence}");
        }
    }

    void OnApplicationQuit()
    {
        if (listener != null)
        {
            listener.StopListening();
        }
    }
}

public class SpeechRecognizerListener
{
    public delegate void OnPhraseRecognizedDelegate(string phrase, List<string> phraseWords, float confidence);
    public event OnPhraseRecognizedDelegate OnPhraseRecognized;

    private DictationRecognizer dictationRecognizer;
    
    public void CustomInit(string configPath, string keywordPath, string dictionaryPath, string acousticModelPath, string languageModelPath)
    {
        // In a real implementation, this would initialize PocketSphinx with the given paths
        // For this example, we'll use Unity's built-in DictationRecognizer as a placeholder
        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationResult += OnDictationResult;
    }

    public void AddKeyword(string keyword, float threshold)
    {
        // In a real implementation, this would add a keyword to PocketSphinx
        Debug.Log($"Added keyword: {keyword} with threshold: {threshold}");
    }

    public void StartListening()
    {
        dictationRecognizer.Start();
    }

    public void StopListening()
    {
        dictationRecognizer.Stop();
    }

    private void OnDictationResult(string text, ConfidenceLevel confidence)
    {
        List<string> words = text.ToLower().Split(' ').ToList();
        OnPhraseRecognized?.Invoke(text, words, (float)confidence / 3f);
    }
}
