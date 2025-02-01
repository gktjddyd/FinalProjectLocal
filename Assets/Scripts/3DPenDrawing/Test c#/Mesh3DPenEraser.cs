using UnityEngine;

/// <summary>

///      (지우개) 로직.

/// </summary>
public class Mesh3DPenEraser : MonoBehaviour
{
    #region Variables

    [Tooltip("The Mesh 3D Pen Line Holder in the scene")]
    [SerializeField] 
    private Mesh3DPenLineHolder penLineHolder;

    [Tooltip("The radius around this GameObject to erase")]
    [SerializeField] 
    private float eraseRadius = 0.2f;

    [Tooltip("How frequently to check which line is being targeted for erase")]
    [SerializeField] 
    private float checkEraseFrequency = 0.5f;

    [Tooltip("Whether this eraser clears all lines created by the same pen as the selected line (unused in single-play sample)")]
    [SerializeField] 
    private bool clearAll = false;

    private float lastCheckEraseTime = -Mathf.Infinity; 
    private bool isHeld = false; 
    private bool clearedMarks = false;

    #endregion

    #region Unity Methods

    private void Update()
    {
        if (!penLineHolder)
            return;

        // 에러서가 잡혀있다면, 일정 간격으로 라인에 "mark" 체크
        if (isHeld)
        {
            if (Time.time - lastCheckEraseTime > checkEraseFrequency)
            {
                Mark();
                lastCheckEraseTime = Time.time;
                clearedMarks = false;
            }
        }
        else if (!clearedMarks)
        {
            // 잡고 있지 않으면, 이전에 마킹된 라인을 해제
            clearedMarks = true;
            ClearMarks();
        }
    }

    #endregion

    #region Public Methods (XR Interaction 대체)

    /// <summary>
    /// XR Grab Interactable에서 'Select Entered' 등으로 이 에러서를 집었을 때.
    /// </summary>
    public void OnEraserPickedUp()
    {
        isHeld = true;
    }

    /// <summary>
    /// XR Grab Interactable에서 'Select Exited' 등으로 이 에러서를 놓았을 때.
    /// </summary>
    public void OnEraserDropped()
    {
        isHeld = false;
        ClearMarks();
    }

    /// <summary>
    /// XR Grab Interactable에서 'Activate'를 눌렀을 때(원본 OnPickupUseDown).
    /// </summary>
    public void OnEraserUseDown()
    {
        Erase();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Marks the line that is currently targeted for erasal
    /// </summary>
    private void Mark()
    {
        if (penLineHolder == null || penLineHolder.mesh3DPenLines == null)
            return;

        foreach (Mesh3DPenLine penLine in penLineHolder.mesh3DPenLines)
        {
            penLine?.CheckMarkLine(transform.position, eraseRadius);
        }
    }

    /// <summary>
    /// Clear all marks currently on lines
    /// </summary>
    private void ClearMarks()
    {
        if (penLineHolder == null || penLineHolder.mesh3DPenLines == null)
            return;

        foreach (Mesh3DPenLine penLine in penLineHolder.mesh3DPenLines)
        {
            penLine?.ClearMark();
        }
    }

    /// <summary>
    /// Actually performs the erase operation on lines (원본: SendCustomNetworkEvent, owner check 등)
    /// 싱글플레이이므로 그냥 모든 라인에 대해 Erase 시도
    /// </summary>
    private void Erase()
    {
        if (penLineHolder == null || penLineHolder.mesh3DPenLines == null)
            return;

        foreach (Mesh3DPenLine penLine in penLineHolder.mesh3DPenLines)
        {
            // clearAll이 true면 penLine.CheckClearLines, 아니면 penLine.CheckEraseLine
            if (clearAll)
                penLine.CheckClearLines(transform.position, eraseRadius);
            else
                penLine.CheckEraseLine(transform.position, eraseRadius);
        }
    }

    #endregion
}
