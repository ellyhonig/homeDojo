using UnityEngine;
using Valve.VR;
public class HomeDojoOverlayCamera : MonoBehaviour
{
    public Camera overlayCamera;
    public RenderTexture renderTexture;

    void Start()
    {
        // Set the camera to render only the "Overlay" layer
        overlayCamera.cullingMask = LayerMask.GetMask("Overlay");

        // Set the camera's target texture to the Render Texture
        overlayCamera.targetTexture = renderTexture;
    }
}
