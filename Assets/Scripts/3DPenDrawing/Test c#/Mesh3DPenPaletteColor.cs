using UnityEngine;

/// <summary>
/// 원본: OnTriggerEnter -> 펜과 충돌하면 펜 색상 변경.
/// 변환: 싱글플레이, Collider/Trigger 이용 예시.
/// </summary>
public class Mesh3DPenPaletteColor : MonoBehaviour
{
    [Tooltip("The color index this pen palette color applies")]
    public int colorIndex = 0;

    private void OnTriggerEnter(Collider other)
    {
        Mesh3DPen pen = other.GetComponentInParent<Mesh3DPen>();
        if (pen != null)
        {
            pen.SetColor(colorIndex);
        }
    }
}
