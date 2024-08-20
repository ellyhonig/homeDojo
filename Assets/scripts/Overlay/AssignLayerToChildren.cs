using UnityEngine;

public class AssignLayerToChildren : MonoBehaviour
{
    public string layerName = "Overlay";

    void Start()
    {
        int layer = LayerMask.NameToLayer(layerName);
        SetLayerRecursively(gameObject, layer);
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null)
        {
            return;
        }

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child == null)
            {
                continue;
            }
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
