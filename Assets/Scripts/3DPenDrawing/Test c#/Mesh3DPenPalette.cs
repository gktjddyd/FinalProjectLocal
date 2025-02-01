using UnityEngine;

/// <summary>
/// </summary>
public class Mesh3DPenPalette : MonoBehaviour
{
    [Tooltip("The root gameObject of the palette (for enabling/disabling)")]
    [SerializeField] 
    private GameObject paletteRoot;

    // 팔레트를 팔목에 따라 위치시켜주는 로직(FollowWrist)은
    // XR Interaction Toolkit에서 직접 구현해야 함.

    /// <summary>
    /// Called when pen is picked up
    /// </summary>
    public void PenPickedUp()
    {
        if (paletteRoot != null)
            paletteRoot.SetActive(true);
    }

    /// <summary>
    /// Called when pen is dropped
    /// </summary>
    public void PenDropped()
    {
        if (paletteRoot != null)
            paletteRoot.SetActive(false);
    }
}
