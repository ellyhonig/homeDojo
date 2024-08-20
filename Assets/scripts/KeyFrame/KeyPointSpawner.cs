using System.Collections.Generic;
using UnityEngine;
using System.IO;
using HomeDojo;
using UnityEngine.UI;
using TMPro;

namespace HomeDojo{
public class KeyPointSpawner : MonoBehaviour
{
    public GameObject hmd;
    public GameObject conR;
    public GameObject conL;
    public GameObject kneeConR;
    public GameObject kneeConL;
    public delegate void UpdateDelegate();
    public Recorder recorder; // Assigned in Unity Editor; the recorder holding the recording
    public UpdateDelegate currentUpdate;
    public List<KeyFrame> keyFrameList;
    public int publicIndex;
    public int currentlySelectedKeyFrame;
    public TraceChecker traceChecker;
    public bool rewind = false;
    private string saveFilePath = "keyFrameList.json";
    public TMP_Text scoreText; // Reference to the TextMeshPro Text element

    public int playerScore = 0;
    public float proximityThreshold = 0.5f; // Adjust as needed
    public float distanceUnit = 1.0f; // Distance unit to space out keyframes

void Start()
{
    recorder = GetComponent<Recorder>();
    keyFrameList = new List<KeyFrame>();
    if (recorder == null)
    {
        Debug.LogError("Recorder component not found on the same GameObject.");
    }
    traceChecker = new TraceChecker(this);
}

    private float updateRate = 0.01f; // Run 10x per second
    private float nextUpdateTime = 0f;


void Update()
{
    if (Time.time >= nextUpdateTime)
    {
        currentUpdate?.Invoke();
        nextUpdateTime = Time.time + updateRate;
    }
}
public void UpdateScoreText()
{
    if (scoreText != null)
    {
        scoreText.text = "Score: " + playerScore;
    }
}

 public void SaveKeyFrameList()
    {
        List<SerializableKeyFrame> serializableKeyFrameList = new List<SerializableKeyFrame>();
        keyFrameList.ForEach(keyFrame => serializableKeyFrameList.Add(keyFrame.ToSerializable()));
        string json = JsonUtility.ToJson(new SerializableKeyFrameListContainer(serializableKeyFrameList), true);
        File.WriteAllText(saveFilePath, json);
    }

    public void LoadKeyFrameList()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            SerializableKeyFrameListContainer container = JsonUtility.FromJson<SerializableKeyFrameListContainer>(json);
            keyFrameList = new List<KeyFrame>();
            container.keyFrameList.ForEach(serializableKeyFrame => keyFrameList.Add(serializableKeyFrame.ToKeyFrame(this)));
            keyFrameList.ForEach(keyFrame => keyFrame.ApplyFrameData()); // Apply frame data after loading
        }
    }

[System.Serializable]
public class SerializableKeyFrame
{
    public int indexInList;
    public int assignedFrame;

    public SerializableKeyFrame(KeyFrame keyFrame)
    {
        this.indexInList = keyFrame.indexInList;
        this.assignedFrame = keyFrame.assignedFrame;
    }

    public KeyFrame ToKeyFrame(KeyPointSpawner spawner)
    {
        KeyFrame keyFrame = new KeyFrame(spawner);
        keyFrame.indexInList = this.indexInList;
        keyFrame.assignedFrame = this.assignedFrame;
        return keyFrame;
    }
}

   public void moveKeyFrame()
{
    // Check if the currently selected key frame is within bounds
    if (currentlySelectedKeyFrame < 0 || currentlySelectedKeyFrame >= keyFrameList.Count)
    {
        Debug.LogError($"currentlySelectedKeyFrame index {currentlySelectedKeyFrame} is out of bounds.");
        return;
    }

    // Check if the recorder's frame list is empty or if the publicIndex is out of bounds
    if (recorder.currentRecord.frames == null || recorder.currentRecord.frames.Count == 0 || publicIndex < 0 || publicIndex >= recorder.currentRecord.frames.Count)
    {
        Debug.LogError($"Frame index {publicIndex} is out of bounds or frames list is empty.");
        // Consider resetting publicIndex or handling this case more gracefully
        return;
    }

    // Apply the frame to the keyframe player
    recorder.currentRecord.frames[publicIndex].ApplyToPlayer(keyFrameList[currentlySelectedKeyFrame].keyFramePlayer);
    keyFrameList[currentlySelectedKeyFrame].assignedFrame = publicIndex;
}

    public void addKeyFrame()
{
    KeyFrame newKeyFrame = new KeyFrame(this);
    keyFrameList.Add(newKeyFrame);
    keyFrameList[keyFrameList.Count - 1].indexInList = keyFrameList.Count - 1;
    currentlySelectedKeyFrame = keyFrameList.Count - 1;
    currentUpdate = moveKeyFrame;

    Debug.Log("New keyframe added; new count: " + keyFrameList.Count + ", currentlySelectedKeyFrame: " + currentlySelectedKeyFrame);
}
[System.Serializable]
    public class SerializableKeyFrameListContainer
    {
        public List<SerializableKeyFrame> keyFrameList;

        public SerializableKeyFrameListContainer(List<SerializableKeyFrame> keyFrameList)
        {
            this.keyFrameList = keyFrameList;
        }
    }
    public void Pause()
    {
        currentUpdate = null;
    }
    
public void EraseKeyFrameList()
    {
        foreach (KeyFrame keyFrame in keyFrameList)
        {
            if (keyFrame.keyFramePlayer != null && keyFrame.keyFramePlayer.bodyPartsParent != null)
            {
                Destroy(keyFrame.keyFramePlayer.bodyPartsParent);
            }
        }

        keyFrameList.Clear();
        Debug.Log("All keyframe players have been destroyed and the keyframe list has been cleared.");
    }
 public void addKeyFramesAtIntervals(int delta)
{
    if (keyFrameList.Count < 2)
    {
        Debug.LogWarning("Not enough keyframes to perform interval filling.");
        return;
    }

    int firstFrame = keyFrameList[0].assignedFrame;
    int finalFrame = keyFrameList[keyFrameList.Count - 1].assignedFrame;

    // Store the first and last keyframes
    KeyFrame firstKeyFrame = keyFrameList[0];
    KeyFrame lastKeyFrame = keyFrameList[keyFrameList.Count - 1];

    // Clear existing keyframes
    EraseKeyFrameList();

    // Add the first keyframe back
    keyFrameList.Add(firstKeyFrame);


    // Add keyframes at regular intervals
    for (int frame = firstFrame + delta; frame < finalFrame; frame += delta)
    {
        addKeyFrame(frame);
    }

    // Ensure the last keyframe is addedff
    if (keyFrameList[keyFrameList.Count - 1].assignedFrame != finalFrame)
    {
        keyFrameList.Add(lastKeyFrame);
    }

    Debug.Log($"Added keyframes at intervals of {delta} frames. New keyframe count: {keyFrameList.Count}");
}

    public void addKeyFrame(int frame)
{
    publicIndex = frame;
    
    KeyFrame newKeyFrame = new KeyFrame(this);
    newKeyFrame.assignedFrame = frame;
    newKeyFrame.indexInList = keyFrameList.Count;
    keyFrameList.Add(newKeyFrame);
    
    currentlySelectedKeyFrame = keyFrameList.Count - 1;
    
    moveKeyFrame();
    currentUpdate = null;

    Debug.Log($"New keyframe added at frame {frame}; new count: {keyFrameList.Count}, currentlySelectedKeyFrame: {currentlySelectedKeyFrame}");
}
 private Vector3 positionTolerance = new Vector3(0.05f, 0.05f, 0.05f); // Adjust as needed
 public int currentKeyFrame = 0;
    public void StartLetterTrace()
    {
        foreach (KeyFrame keyFrame in keyFrameList)
        {
            DisableAllPartsExceptHandR(keyFrame.keyFramePlayer);
        }
        currentKeyFrame = 1;
        currentUpdate = LetterTraceUpdate;
        Debug.Log($"Letter Trace mode started. Only right hands are visible. Total keyframes: {keyFrameList.Count}");
    }

    private void LetterTraceUpdate()
    {
        if (currentKeyFrame >= keyFrameList.Count)
        {
            Debug.Log("Finished letter tracing all keyframes.");
            Pause();
            return;
        }

        // Activate the current keyframe player
        keyFrameList[currentKeyFrame].keyFramePlayer.bodyPartsParent.SetActive(true);
        recorder.currentRecord.frames[keyFrameList[currentKeyFrame].assignedFrame].ApplyToPlayer(keyFrameList[currentKeyFrame].keyFramePlayer);

        if (CheckRightHandPosition(currentKeyFrame))
        {
            Debug.Log($"Keyframe {currentKeyFrame} passed the letter trace check.");
            currentKeyFrame++; // Move to the next keyframe only if the current one passes the check
        }
    }

    private bool CheckRightHandPosition(int keyPointIndex)
    {
        var keyFramePlayer = keyFrameList[keyPointIndex].keyFramePlayer;
        var playerToRecord = recorder.PlayerToRecord;

        string[] handNames = { "HandR", "ChestHandR" };
        
        foreach (string handName in handNames)
        {
            if (keyFramePlayer.bodyPartsDictionary.TryGetValue(handName, out GameObject keyFrameHand) &&
                playerToRecord.bodyPartsDictionary.TryGetValue(handName, out GameObject playerHand))
            {
                if (closeEnoughPos(playerHand.transform, keyFrameHand.transform, positionTolerance))
                {
                    UpdateHandVisual(playerHand, keyFrameHand, true);
                    return true;
                }
                else
                {
                    UpdateHandVisual(playerHand, keyFrameHand, false);
                    return false;
                }
            }
        }

        Debug.LogWarning("Right hand not found in either player or keyframe.");
        return false;
    }

    private void UpdateHandVisual(GameObject playerHand, GameObject keyFrameHand, bool isMatching)
    {
        Color playerColor = isMatching ? Color.green : Color.red;
        Color keyFrameColor = isMatching ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);

        UpdateRendererColor(playerHand, playerColor);
        UpdateRendererColor(keyFrameHand, keyFrameColor);
    }

    private void UpdateRendererColor(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private bool closeEnoughPos(Transform reference, Transform toBeChecked, Vector3 positionTolerance)
    {
        return (reference.position - toBeChecked.position).magnitude <= positionTolerance.magnitude;
    }

private void DisableAllPartsExceptHandR(player playerToModify)
{
    // First, activate all parts to ensure the hierarchy is fully enabled
    foreach (var bodyPart in playerToModify.bodyPartsDictionary.Values)
    {
        bodyPart.SetActive(true);
    }

    // Then, disable specific parts, keeping the hand and its parents active
    foreach (var kvp in playerToModify.bodyPartsDictionary)
    {
        string partName = kvp.Key;
        GameObject bodyPart = kvp.Value;

        // Keep these parts active
        bool shouldRemainActive = partName == "HandR" || partName == "ChestHandR" || partName == "HipHandR" ||
                                  partName == "ElbowR" || partName == "ChestElbowR" || partName == "HipElbowR" ||
                                  partName == "ShoulderR" || partName == "ChestShoulderR" || partName == "HipShoulderR" ||
                                  partName == "Chest" || partName == "Hip";

        if (!shouldRemainActive)
        {
            bodyPart.SetActive(false);
        }
        else
        {
            // For parts that should remain active, ensure their renderers are enabled
            Renderer renderer = bodyPart.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = (partName == "HandR" || partName == "ChestHandR" || partName == "HipHandR");
            }
        }
    }

    Debug.Log("Disabled all parts except right hand and its parent objects.");
}
    

public class KeyFrame
{
    public KeyPointSpawner spawner;
    public player keyFramePlayer;
    public int indexInList;
    public int assignedFrame;
    public KeyFrame(KeyPointSpawner Spawner)
    {
        spawner = Spawner;
        keyFramePlayer = new player(spawner.hmd.transform, spawner.conR.transform, spawner.conL.transform, spawner.kneeConR.transform, spawner.kneeConL.transform);
        keyFramePlayer.Calibrate();
        keyFramePlayer.DisableColliders();
        MakePlayerWhiteAndTransparent(keyFramePlayer);       // keyFramePlayer.Calibrate();
    }
    public SerializableKeyFrame ToSerializable()
    {
        return new SerializableKeyFrame(this);
    }
    public void ApplyFrameData()
    {
        if (spawner.recorder.currentRecord != null && assignedFrame >= 0 && assignedFrame < spawner.recorder.currentRecord.frames.Count)
        {
            spawner.recorder.currentRecord.frames[assignedFrame].ApplyToPlayer(keyFramePlayer);
        }
    }
   public void MakePlayerWhiteAndTransparent(player playerToModify)
{
    foreach (GameObject bodyPart in playerToModify.bodyPartsDictionary.Values)
    {
        Renderer renderer = bodyPart.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Adjust the shader to one that supports transparency
            renderer.material.shader = Shader.Find("Transparent/Diffuse");
            // Adjust the RGBA values to make it white with some transparency
            Color currentColor = renderer.material.color;
            renderer.material.color = new Color(1f, 1f, 1f, 0.3f); // White with 70% transparency
        }
    }
}
}



}

public class TraceChecker
{
    public int playerScore = 0;
    private float proximityThreshold = 0.5f; // Adjust as needed
    private float distanceUnit = .08f; // Distance unit to space out keyframes
    public KeyPointSpawner spawner;
    public int currentKeyFrame = 0;
    public bool loop = true; // Loop boolean to reset the test at the end
    public TraceChecker(KeyPointSpawner spawner)
    {
        this.spawner = spawner;
    }

    public void startTestTrace()
    {
        currentKeyFrame = 1;
        spawner.currentUpdate = testTrace;
    }

    public void testTrace()
    {
        if (currentKeyFrame >= spawner.keyFrameList.Count)
        {
            Debug.Log("Finished testing all keyframes.");
            //DeactivateAllKeyframePlayers();
            spawner.Pause();
            return;
        }

        // Ensure all keyframe players are deactivated
        //DeactivateAllKeyframePlayers();

        // Activate the current keyframe player
        spawner.keyFrameList[currentKeyFrame].keyFramePlayer.bodyPartsParent.SetActive(true);
        spawner.recorder.currentRecord.frames[spawner.keyFrameList[currentKeyFrame].assignedFrame].ApplyToPlayer(spawner.keyFrameList[currentKeyFrame].keyFramePlayer);

        if (CheckAllLimbsAtKeyPoint(currentKeyFrame))
        {
            Debug.Log($"Keyframe {currentKeyFrame} passed the trace check.");
            currentKeyFrame++; // Move to the next keyframe only if the current one passes the check

            
        }
    }
      public void startBeatSaber()
    {
        // Calculate initial positions and activate all keyframe players
        float playerYPosition = spawner.hmd.transform.position.y;

        for (int i = 0; i < spawner.keyFrameList.Count; i++)
        {
            KeyPointSpawner.KeyFrame keyFrame = spawner.keyFrameList[i];
            if (keyFrame.keyFramePlayer != null && keyFrame.keyFramePlayer.bodyPartsParent != null)
            {
                keyFrame.keyFramePlayer.bodyPartsParent.SetActive(true);

                // Calculate initial position based on frame number
                float initialDistance = keyFrame.assignedFrame * spawner.distanceUnit;
                Vector3 initialPosition = spawner.recorder.PlayerToRecord.bodyPartsParent.transform.position + 
                    spawner.recorder.PlayerToRecord.bodyPartsParent.transform.forward * initialDistance;
                keyFrame.keyFramePlayer.bodyPartsParent.transform.position = initialPosition;
            }
            else
            {
                Debug.LogWarning($"KeyFrame at index {i} has null keyFramePlayer or bodyPartsParent");
            }
        }

        // Start the beatSaber method
        currentKeyFrame = 0;
        spawner.currentUpdate = beatSaber;
    }

    public void beatSaber()
    {
        // Define the speed calculation variables
        float speed = spawner.distanceUnit;

        // Move all keyframe players towards the player in the XZ plane
        for (int i = 0; i < spawner.keyFrameList.Count; i++)
        {
            KeyPointSpawner.KeyFrame keyFrame = spawner.keyFrameList[i];
            if (keyFrame.keyFramePlayer != null && keyFrame.keyFramePlayer.bodyPartsParent != null)
            {
                var keyFramePlayer = keyFrame.keyFramePlayer;
                Vector3 direction = (spawner.recorder.PlayerToRecord.bodyPartsParent.transform.position - 
                    keyFramePlayer.bodyPartsParent.transform.position).normalized;
                keyFramePlayer.bodyPartsParent.transform.position += direction * speed;
            }
        }

        // Check proximity for the current keyframe player
        if (currentKeyFrame < spawner.keyFrameList.Count)
        {
            var currentKeyFramePlayer = spawner.keyFrameList[currentKeyFrame].keyFramePlayer;
            if (currentKeyFramePlayer != null && currentKeyFramePlayer.bodyPartsParent != null)
            {
                float distanceToPlayer = Vector3.Distance(spawner.recorder.PlayerToRecord.bodyPartsParent.transform.position,
                                                          currentKeyFramePlayer.bodyPartsParent.transform.position);

                // Perform trace test if within proximity threshold
                if (distanceToPlayer <= spawner.proximityThreshold)
                {
                    if (CheckAllLimbsAtKeyPoint(currentKeyFrame))
                    {
                        spawner.playerScore++;
                        spawner.UpdateScoreText();
                        Debug.Log($"Keyframe {currentKeyFrame} passed the trace check. Player score: {spawner.playerScore}");

                        // Deactivate the current keyframe player and move to the next keyframe
                        currentKeyFramePlayer.bodyPartsParent.SetActive(false);
                        currentKeyFrame++;
                    }
                }

                // Move to the next keyframe if the player is very close to the current keyframe player
                if (distanceToPlayer < 0.05f)
                {
                    currentKeyFramePlayer.bodyPartsParent.SetActive(false);
                    currentKeyFrame++;
                }
            }
        }

        // End of beatSaber, all keyframes processed
        if (currentKeyFrame >= spawner.keyFrameList.Count)
        {
            Debug.Log("Finished all keyframes in beatSaber.");
            DeactivateAllKeyframePlayers();
            spawner.Pause();

            if (loop)
            {
                Debug.Log("Restarting the test due to loop being true.");
                currentKeyFrame = 0;
                spawner.currentUpdate = startBeatSaber;
                spawner.recorder.currentUpdate = spawner.recorder.PlayRecording;
            }
        }
    }



    public void DeactivateAllKeyframePlayers()
    {
        foreach (var keyFrame in spawner.keyFrameList)
        {
            if (keyFrame.keyFramePlayer.bodyPartsParent != null)
            {
                keyFrame.keyFramePlayer.bodyPartsParent.SetActive(false);
            }
        }
    }

  private bool CheckAllLimbsAtKeyPoint(int keyPointIndex)
{
    
    bool allLimbsCloseEnough = true;

    var keyFramePlayer = spawner.keyFrameList[keyPointIndex].keyFramePlayer;
    var playerToRecord = spawner.recorder.PlayerToRecord;

    // Iterate through the playerToRecord's limbs
    foreach (var limbName in playerToRecord.testPartsDictionary.Keys)
    {
        if (!keyFramePlayer.testPartsDictionary.ContainsKey(limbName))
        {
            Debug.LogWarning($"Limb {limbName} not found in keyFramePlayer's testPartsDictionary.");
            continue; // Skip this iteration if the limb doesn't exist in keyFramePlayer
        }

        // Fetch corresponding Transforms to be checked
        Transform recordLimbTransform = playerToRecord.testPartsDictionary[limbName].transform;
        Transform keyFrameLimbTransform = keyFramePlayer.testPartsDictionary[limbName].transform;

        Renderer recordLimbRenderer = playerToRecord.testPartsDictionary[limbName].GetComponent<Renderer>();
        Renderer keyFrameLimbRenderer = keyFramePlayer.testPartsDictionary[limbName].GetComponent<Renderer>();

        // Use some predefined tolerances or calculate them based on your needs
        Vector3 positionTolerance = new Vector3(0.5f, 0.5f, 0.5f);
        float rotationToleranceDegrees = 30f;

        if (!closeEnoughRot(recordLimbTransform, keyFrameLimbTransform, rotationToleranceDegrees))
        {
            // If not close enough, turn both limbs red and keyFramePlayer transparent
            if (recordLimbRenderer != null)
            {
                recordLimbRenderer.material.color = Color.red;
            }
            if (keyFrameLimbRenderer != null)
            {
                keyFrameLimbRenderer.material.color = new Color(1, 0, 0, 0.5f); // Red with 50% transparency
            }
            allLimbsCloseEnough = false; // At least one limb is not close enough
        }
        else
        {
           
            if (recordLimbRenderer != null)
            {
                recordLimbRenderer.material.color = Color.green;
            }
            if (keyFrameLimbRenderer != null)
            {
                keyFrameLimbRenderer.material.color = new Color(0, 1, 0, 0.5f); // Green with 50% transparency
            }
        }
    }

    return allLimbsCloseEnough;
}



    public bool closeEnoughRot(Transform reference, Transform toBeChecked, float rotToleranceDegrees)
    {
        // Check rotation tolerance. Quaternion.Angle returns the angle in degrees between two rotations.
        bool isRotationCloseEnough = Quaternion.Angle(reference.rotation, toBeChecked.rotation) <= rotToleranceDegrees;

        return isRotationCloseEnough;
    }
    public bool closeEnoughPos(Transform reference, Transform toBeChecked, Vector3 positionTolerance)
    {
        // Check position tolerance
        bool isPositionCloseEnough = (reference.position - toBeChecked.position).magnitude <= positionTolerance.magnitude;

        return isPositionCloseEnough;
    }
}
}
