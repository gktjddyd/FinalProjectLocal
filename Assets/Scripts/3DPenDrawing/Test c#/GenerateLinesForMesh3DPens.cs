using UnityEngine;
using System.Collections.Generic;

/// <summary>
///       펜마다 라인(프리팹)을 생성해 주는 스크립트.
/// 변환: 일반 Unity 환경에서는 "Pool" 대신, 단순 배열/리스트로 대체 가능.
/// </summary>
public class GenerateLinesForMesh3DPens : MonoBehaviour
{
    #region Variables

    [Tooltip("The pens to generate lines for (replacing VRCObjectPool)")]
    [SerializeField] 
    private List<Mesh3DPen> pens = new List<Mesh3DPen>();

    [Tooltip("The Mesh 3D Pen Line Holder to hold the lines")]
    [SerializeField] 
    private Mesh3DPenLineHolder lineHolder;

    [Tooltip("The Mesh 3D Pen Line prefab")]
    [SerializeField] 
    private GameObject linePrefab;

    #endregion

    #region Private Functions

    /// <summary>
    /// Generates lines for a list of 3D pens
    /// (원본은 VRCObjectPool.Pool.Length를 기반으로 생성했음)
    /// </summary>
    [ContextMenu("Generate")]
    private void Generate()
    {
        if (lineHolder == null)
        {
            Debug.LogError("[GenerateLinesForMesh3DPens] lineHolder is not assigned.");
            return;
        }
        if (linePrefab == null)
        {
            Debug.LogError("[GenerateLinesForMesh3DPens] linePrefab is not assigned.");
            return;
        }

        //Clear previous lines
        if (lineHolder.mesh3DPenLines != null)
        {
            foreach (var line in lineHolder.mesh3DPenLines)
            {
                if (line != null && line.gameObject != null)
                {
#if UNITY_EDITOR
                    // 에디터에서 즉시 삭제
                    DestroyImmediate(line.gameObject);
#else
                    // 런타임에서는 Destroy 사용
                    Destroy(line.gameObject);
#endif
                }
            }
        }

        //Initialize pen holder array
        lineHolder.mesh3DPenLines = new Mesh3DPenLine[pens.Count];

        //Generate new ones
        for (int i = 0; i < pens.Count; i++)
        {
            var pen = pens[i];
            if (pen == null) continue;

            GameObject lineGO = Instantiate(linePrefab, lineHolder.transform);
            lineGO.name = linePrefab.name + " (" + i + ")";

            Mesh3DPenLine line = lineGO.GetComponentInChildren<Mesh3DPenLine>();
            if (line == null)
            {
                Debug.LogError("[GenerateLinesForMesh3DPens] The line prefab has no Mesh3DPenLine component!");
                continue;
            }

            // pen - line 연결
            pen.line = line;
            line.pen = pen;
            line.lineHolder = lineHolder;

            lineHolder.mesh3DPenLines[i] = line;
        }
    }

    #endregion
}
