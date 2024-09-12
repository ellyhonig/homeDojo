using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Collections;

public class PocketSphinxPhonemeRecognition : MonoBehaviour
{
    private Process pythonProcess;
    private bool isRunning = false;
    private string logFilePath;
    private long lastPosition = 0;
    private Coroutine readLogCoroutine;

    public event System.Action<string> OnPhonemeDetected;

    private LevelManager levelManager;

    void Start()
    {
        logFilePath = Path.Combine(Application.dataPath, "Scripts", "phoneme_recognition_log.txt");
        File.WriteAllText(logFilePath, string.Empty); // Clear the log file at the start
        levelManager = GetComponent<LevelManager>();
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
        }
    }

    public void StartPhonemeRecognition()
    {
        if (!isRunning)
        {
            RunPythonScript();
            readLogCoroutine = StartCoroutine(ReadLogFile());
        }
    }

    public void StopPhonemeRecognition()
    {
        if (isRunning)
        {
            isRunning = false;
            if (readLogCoroutine != null)
            {
                StopCoroutine(readLogCoroutine);
            }
            if (pythonProcess != null && !pythonProcess.HasExited)
            {
                pythonProcess.Kill();
                pythonProcess.Dispose();
            }
        }
    }

    void RunPythonScript()
    {
        string scriptPath = Path.Combine(Application.dataPath, "Scripts", "focused_phoneme_recognition.py");

        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogError("Python script not found.");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            pythonProcess = new Process { StartInfo = startInfo };
            pythonProcess.Start();
            isRunning = true;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Error starting Python process: {ex.Message}");
        }
    }

    IEnumerator ReadLogFile()
    {
        while (isRunning)
        {
            if (File.Exists(logFilePath))
            {
                string[] newLines = File.ReadAllLines(logFilePath);
                for (int i = (int)lastPosition; i < newLines.Length; i++)
                {
                    ProcessLogLine(newLines[i]);
                }
                lastPosition = newLines.Length;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void ProcessLogLine(string line)
    {
        if (line.StartsWith("Recognized:"))
        {
            string phoneme = line.Substring("Recognized:".Length).Trim();
            UnityEngine.Debug.Log($"Phoneme detected: {phoneme}");
            OnPhonemeDetected?.Invoke(phoneme);
        }
    }

    void OnDestroy()
    {
        StopPhonemeRecognition();
    }
}