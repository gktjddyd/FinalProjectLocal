using UnityEngine;
using System.Collections;

public class MeshPenPixelEraser : MonoBehaviour
{
    [Tooltip("The mesh 3D pen line holder in the scene")]
    [SerializeField] 
    private Mesh3DPenLineHolder lineHolder;

    [Tooltip("The radius around this eraser to erase")]
    [SerializeField] 
    private float eraseRadius = 0.1f;

    private bool erasing = false;
    private bool isHeld = false;

    private void Update()
    {
        // 만약 "erasing" 상태라면, 각 라인에 픽셀 단위 지우기 CheckPixelEraseLine
        if (erasing && lineHolder != null && lineHolder.mesh3DPenLines != null)
        {
            foreach (Mesh3DPenLine line in lineHolder.mesh3DPenLines)
            {
                // 원본: line.CheckPixelEraseLine(transform.position, eraseRadius);
                // 여기서는 픽셀 지우기 로직을 제대로 구현하려면, Mesh3DPenLine에 
                // PixelEraseLine()을 추가해야 합니다. (코드 길이상 생략)
                // 일단 "일반 EraseLine" 비슷한 호출만 예시:
                line.CheckEraseLine(transform.position, eraseRadius);
            }
        }

        // isHeld 상태일 때, 라인에 MarkPixelEraseLine() 호출 가능(마킹)
        // 여기서는 생략
    }

    #region Public XR / Interaction Methods

    public void OnEraserPickedUp()
    {
        isHeld = true;
    }

    public void OnEraserDropped()
    {
        isHeld = false;
        StopErasing();
    }

    public void StartErasing()
    {
        if (!erasing)
        {
            erasing = true;
        }
    }

    public void StopErasing()
    {
        if (erasing)
        {
            erasing = false;
        }
    }

    #endregion
}
