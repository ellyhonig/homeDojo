using UnityEngine;
using HomeDojo;
using System.Collections.Generic;

public class recorderButtonInteraction : MonoBehaviour
{
    private Recorder recorder;
    private KeyPointSpawner keyPointSpawner;
    private LevelQueue levelQueue;
    private System.Action directorAddKeyFrame;
    private Dictionary<string, System.Action> buttonActions;

    void Start()
    {
        GameObject scriptObject = GameObject.Find("script");
        if (scriptObject != null)
        {
            recorder = scriptObject.GetComponent<Recorder>();
            keyPointSpawner = scriptObject.GetComponent<KeyPointSpawner>();
            levelQueue = scriptObject.GetComponent<LevelQueue>();
        }
        else
        {
            Debug.LogError("The object named 'script' was not found. Please ensure it exists and has Recorder and KeyPointSpawner components.");
        }

        buttonActions = new Dictionary<string, System.Action>
        {
            { "PlayButton", () => recorder.PlayRecording() },
            { "PauseButton", () => { recorder.PauseRecording(); keyPointSpawner.Pause(); } },
            { "SaveButton", () => { recorder.SaveRecording(); keyPointSpawner.SaveKeyFrameList(); } },
            { "RecordButton", () => recorder.StartRecording() },
            { "LoadButton", () => { recorder.LoadRecording(); keyPointSpawner.LoadKeyFrameList(); } },
            { "spawn trace", () => keyPointSpawner.addKeyFrame() },
            { "test trace", () => 
                { 
                    keyPointSpawner.Pause(); 
                    keyPointSpawner.traceChecker.startBeatSaber(); 
                    recorder.PauseRecording();  
                    recorder.PlayRecording();
                }
            },
            { "play level", () => levelQueue.playLevel() },
            { "add tutorial section", () => levelQueue.addRecord() },
            { "add test section", () => levelQueue.addTest() }, { "UndoCalibrationButton", () => 
                {
                    if (recorder?.PlayerToRecord != null)
                    {
                        recorder.PlayerToRecord.UndoCalibration();
                    }
                    else
                    {
                        Debug.LogWarning("Cannot undo calibration: Recorder or PlayerToRecord is null.");
                    }
                }
            },
            { "fillBetween", () => {keyPointSpawner.addKeyFramesAtIntervals(10);keyPointSpawner.StartLetterTrace();} }
        };

        directorAddKeyFrame = () => keyPointSpawner.currentUpdate = keyPointSpawner.addKeyFrame;
    }

    void Update()
    {
        CheckSpaceBarPress(directorAddKeyFrame);
    }

    void OnCollisionEnter(Collision collision)
    {
        ResetButtonColors();

        GetComponent<Renderer>().material.color = Color.green;

        if (buttonActions.TryGetValue(gameObject.tag, out System.Action action))
        {
            action();
        }
        else
        {
            Debug.LogWarning("No action defined for tag: " + gameObject.tag);
        }
    }

    void ResetButtonColors()
    {
        // Collect all buttons and reset their colors
        foreach (var buttonTag in buttonActions.Keys)
        {
            GameObject[] buttons = GameObject.FindGameObjectsWithTag(buttonTag);
            ResetButtons(buttons);
        }
    }

    void ResetButtons(GameObject[] buttons)
    {
        foreach (GameObject button in buttons)
        {
            if (button != this.gameObject) // Exclude the current object
            {
                button.GetComponent<Renderer>().material.color = Color.white;
            }
        }
    }
void CheckSpaceBarPress(System.Action action)
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            action?.Invoke();
            Debug.Log("Space bar pressed. Action performed.");
        }
    }


}
