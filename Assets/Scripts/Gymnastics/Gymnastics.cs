using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
public class Gymnastics : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform hmd;
    [SerializeField] private Transform rightController;
    [SerializeField] private Transform leftController;

    [Header("Settings")]
    [Tooltip("HMD 기준 컨트롤러 상대좌표")]
    [SerializeField] private float raiseThreshold = 0.2f;

    [Tooltip("HMD 기준 컨트롤러 상대좌표")]
    [SerializeField] private float sideThreshold = 0.2f;



    void Update()
    {
        if (hmd == null || leftController == null || rightController == null) return;

        // 월드 좌표 -> HMD 상대좌표 변환.
        Vector3 headLocalPos = hmd.transform.InverseTransformPoint(hmd.position);
        Vector3 rightCtrlLocalPos = hmd.transform.InverseTransformPoint(rightController.position);
        Vector3 leftCtrlLocalPos = hmd.transform.InverseTransformPoint(leftController.position);

        // 상대 y좌표 비교
        float verticalDiff = rightCtrlLocalPos.y - headLocalPos.y;

        // 상대 x좌표 비교 
        float horizontalDiff = rightCtrlLocalPos.x - headLocalPos.x;

        // 1) 컨트롤러가 머리보다 특정 값 이상 위에 있는지
        if (verticalDiff > raiseThreshold)
        {
            Debug.Log($"[ControllerRelativePositionChecker] 컨트롤러가 HMD보다 {verticalDiff:F2}만큼 더 위에 있습니다.");
        }

        // 2) 컨트롤러가 머리 기준 특정 값 이상 오른쪽에 있는지
        //if (horizontalDiff > sideThreshold)
        //{
        //    Debug.Log($"[ControllerRelativePositionChecker] 컨트롤러가 HMD 기준 {horizontalDiff:F2}만큼 오른쪽에 있습니다.");
        //}
    }
}
