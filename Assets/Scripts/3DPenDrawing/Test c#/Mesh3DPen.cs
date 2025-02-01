using UnityEngine;

/// <summary>
/// 변환: 일반 Unity + XR Interaction Toolkit을 가정한 단일 플레이 펜 스크립트.
/// </summary>
public class Mesh3DPen : MonoBehaviour
{
    #region Variables

    [Tooltip("This pen's line (drawn line)")]
    public Mesh3DPenLine line;

    [Tooltip("The pen palette in the scene (optional)")]
    [SerializeField] 
    private Mesh3DPenPalette palette;

    [Tooltip("The pen tip transform (where lines are drawn from)")]
    [SerializeField] 
    private Transform tip;

    [Tooltip("The MeshRenderer of this pen (to show paint color)")]
    [SerializeField] 
    private MeshRenderer meshRenderer;

    [Tooltip("The color property on the pen's material (e.g. _Color)")]
    [SerializeField] 
    private string colorProperty = "_Color";

    [Tooltip("If using XR Interaction, link the XR grab script here (optional)")]
    [SerializeField] 
    private MonoBehaviour xrGrabInteractable; 
    // ↑ 실제로는 XRGrabInteractable 참조하면 됩니다.

    #endregion

    #region Unity Methods

    private void Start()
    {
        // 싱글플레이이므로, 네트워크 Ownership 등은 제거
        UpdateLineOwnership();
    }

    private void Update()
    {
        // 원본 코드: if pen is held && local player pressed F -> line.IncrementColor();
        // XR 환경에서는 "F 키"를 누르는 대신, XR 컨트롤러 이벤트를 받아야 합니다.
        // 여기서는 예시로 PC에서 테스트할 때 F 누르면 색상 변경.

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (line != null)
            {
                Debug.Log("[Mesh3DPen] Changing color (IncrementColor).");
                line.IncrementColor();
            }
        }
    }

    #endregion

    #region Public XR / Interaction Methods

    /// <summary>
    /// XR Grab Interactable에서 'Select Entered'(= 펜 집기) 시 호출한다고 가정.
    /// </summary>
    public void OnPenPickedUp()
    {
        // 원본 OnPickup() 이벤트 대체
        if (palette != null)
        {
            // palette에서 "PenPickedUp" 호출
            palette.PenPickedUp();
        }
    }

    /// <summary>
    /// XR Grab Interactable에서 'Select Exited'(= 펜 놓기) 시 호출한다고 가정.
    /// </summary>
    public void OnPenDropped()
    {
        // 원본 OnDrop() 이벤트 대체
        if (palette != null)
        {
            palette.PenDropped();
        }
    }

    /// <summary>
    /// XR Grab Interactable에서 'Activate'(= 트리거/버튼) 시 호출한다고 가정.
    /// </summary>
    public void OnPenUseDown()
    {
        // 원본 OnPickupUseDown() 이벤트 대체
        if (line != null)
        {
            line.StartDrawing();
        }
    }

    /// <summary>
    /// XR Grab Interactable에서 'Deactivate'(= 트리거/버튼 해제) 시 호출한다고 가정.
    /// </summary>
    public void OnPenUseUp()
    {
        // 원본 OnPickupUseUp() 이벤트 대체
        if (line != null)
        {
            line.StopDrawing();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the color (index) the pen is currently set to draw.
    /// </summary>
    /// <param name="value">Color index</param>
    public void SetColor(int value)
    {
        if (line != null)
        {
            line.SetColor(value);
        }
    }

    /// <summary>
    /// Returns the transform marking the tip of the pen
    /// </summary>
    public Transform GetTip()
    {
        return tip;
    }

    /// <summary>
    /// Sets the color of the paint on the tip of the pen's model
    /// </summary>
    public void SetPenModelColor(Color color)
    {
        if (meshRenderer != null && !string.IsNullOrEmpty(colorProperty))
        {
            meshRenderer.material.SetColor(colorProperty, color);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 싱글플레이이므로, line과 pen이 같은 '주인' 개념을 가진다 가정.
    /// </summary>
    private void UpdateLineOwnership()
    {
        if (line != null)
        {
            // 원본: line.UpdateOwner(Networking.LocalPlayer);
            // 여기서는 아무것도 안 해도 됩니다. (싱글플레이)
        }
    }

    #endregion
}
