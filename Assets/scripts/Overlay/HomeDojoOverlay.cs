using UnityEngine;
using Valve.VR;

public class HomeDojoOverlay : MonoBehaviour
{
    public RenderTexture renderTexture; // Assign this in the Unity Editor
    private ulong overlayHandle = OpenVR.k_ulOverlayHandleInvalid;

    void Start()
    {
        var overlay = OpenVR.Overlay;

        if (overlay == null)
        {
            Debug.LogError("OpenVR.Overlay is not initialized.");
            return;
        }

        EVROverlayError error = overlay.CreateOverlay("HomeDojoOverlayKey", "HomeDojo Overlay", ref overlayHandle);
        if (error != EVROverlayError.None)
        {
            Debug.LogError("Error creating overlay: " + error.ToString());
            return;
        }

        overlay.SetOverlayWidthInMeters(overlayHandle, 1.5f);
        var texture = new Texture_t { handle = renderTexture.GetNativeTexturePtr(), eType = ETextureType.OpenGL, eColorSpace = EColorSpace.Auto };
        overlay.SetOverlayTexture(overlayHandle, ref texture);
    }

    void Update()
    {
        if (overlayHandle == OpenVR.k_ulOverlayHandleInvalid)
        {
            return;
        }

        var overlay = OpenVR.Overlay;
        if (overlay == null)
        {
            Debug.LogError("OpenVR.Overlay is not available in Update.");
            return;
        }

        overlay.ShowOverlay(overlayHandle);

        HmdMatrix34_t transform = new HmdMatrix34_t();
        transform.m0 = 1; transform.m1 = 0; transform.m2 = 0; transform.m3 = 0;
        transform.m4 = 0; transform.m5 = 1; transform.m6 = 0; transform.m7 = 0;
        transform.m8 = 0; transform.m9 = 0; transform.m10 = 1; transform.m11 = 0.5f;

        overlay.SetOverlayTransformAbsolute(overlayHandle, ETrackingUniverseOrigin.TrackingUniverseStanding, ref transform);
    }

    void OnDestroy()
    {
        if (overlayHandle == OpenVR.k_ulOverlayHandleInvalid)
        {
            return;
        }

        var overlay = OpenVR.Overlay;
        if (overlay != null)
        {
            overlay.DestroyOverlay(overlayHandle);
        }
    }
}
