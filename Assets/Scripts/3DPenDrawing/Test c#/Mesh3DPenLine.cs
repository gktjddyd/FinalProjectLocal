﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 싱글플레이 환경에서 라인을 그리고,
/// "픽셀 단위"로 일부만 지울 수 있는 Mesh3DPenLine (일반 C#/MonoBehaviour 버전).
/// </summary>
public class Mesh3DPenLine : MonoBehaviour
{
    #region Variables

    [Tooltip("The pen associated with this line (optional)")]
    public Mesh3DPen pen;

    [Tooltip("The component that holds all the lines in the scene (optional)")]
    [SerializeField] 
    public Mesh3DPenLineHolder lineHolder;

    [Header("Line Renderers")]
    [Tooltip("The line renderer used for the current line (while drawing)")]
    [SerializeField]
    private LineRenderer currentLineRenderer;

    [Tooltip("The line renderer used for marking lines (highlighting to erase)")]
    [SerializeField]
    private LineRenderer markingLineRenderer;

    [Tooltip("The main line renderers used for displaying all drawn lines (one per color)")]
    [SerializeField]
    private LineRenderer[] mainLineRenderers;

    [Header("Drawing Settings")]
    [Tooltip("The list of colors this line can be")]
    [SerializeField] 
    private Color[] colors;

    [Tooltip("Minimum distance the pen must move before adding a new point to the line")]
    [SerializeField]
    private float minMoveDistance = 0.02f;

    [Tooltip("The tolerance used when simplifying a finished line")]
    [SerializeField]
    private float simplifyTolerance;

    [Header("Pixel Eraser Settings")]
    [Tooltip("Minimum distance a pixel eraser must move before updating line positions")]
    [SerializeField]
    private float pixelEraserMinMoveDistance = 0.1f;

    [Tooltip("The size of the shader points buffer to use when pixel erasing.")]
    [SerializeField]
    private int pixelEraseShaderPointsBufferSize = 25;

    [Tooltip("Material parameter for pixel erase points (e.g. _Points)")]
    [SerializeField]
    private string pixelErasePointsParameter = "_Points";

    [Tooltip("Material parameter for pixel eraser position (e.g. _EraserPosition)")]
    [SerializeField]
    private string pixelEraserPositionParameter = "_EraserPosition";

    [Tooltip("Material parameter for pixel erase radius (e.g. _EraseRadius)")]
    [SerializeField]
    private string pixelEraseRadiusParameter = "_EraseRadius";

    [Tooltip("Material parameter for pixel erase marking radius (e.g. _MarkRadius)")]
    [SerializeField]
    private string pixelEraseMarkRadiusParameter = "_MarkRadius";

    [Tooltip("Current color index in the colors array")]
    [SerializeField]
    private int colorIndex = 0;

    // 내부 상태
    private Transform penTip;          // 펜 팁
    private Vector3 lastPointPosition; // 마지막으로 기록한 펜 위치
    private bool isDrawing = false;    // 라인 그리는 중인지 여부

    // 픽셀 지우개 관련
    private Vector3 lastPixelEraserPosition = Vector3.positiveInfinity;
    private Vector4[] pixelEraseShaderPoints;
    private int pixelEraseShaderPointsIndex = 0;

    // 라인 데이터 (여러 개의 선)
    private List<List<Vector3>> linePositions = new List<List<Vector3>>();
    private List<int> lineColorIndices = new List<int>();
    private List<Bounds> lineBounds = new List<Bounds>();

    // 라인 렌더러에서 "새 라인" 시작 시 단절을 표시할 좌표
    private Vector3 lineBreakPosition = new Vector3(0, -10000, 0);

    #endregion

    #region Unity Methods

    private void Start()
    {
        // 펜 팁 가져오기
        if (pen != null)
        {
            penTip = pen.GetTip();
            // 펜 색상 초기화
            pen.SetPenModelColor(colors[colorIndex]);
        }

        // 메인 라인 렌더러 색상 초기화
        for (int i = 0; i < mainLineRenderers.Length; i++)
        {
            mainLineRenderers[i].startColor = colors[i];
            mainLineRenderers[i].endColor = colors[i];
        }

        // 픽셀 지우개용 셰이더 포인트 배열 초기화
        pixelEraseShaderPoints = new Vector4[pixelEraseShaderPointsBufferSize];
        for (int i = 0; i < pixelEraseShaderPoints.Length; i++)
        {
            pixelEraseShaderPoints[i] = Vector4.positiveInfinity;
        }

        // 메인 라인 렌더러 클리어
        ClearAllMainLineRenderers();
    }

    private void Update()
    {
        // 라인 그리는 중이면, 펜이 일정 거리 이상 이동 시 새 점 추가
        if (isDrawing && penTip != null)
        {
            float sqrDist = (penTip.position - lastPointPosition).sqrMagnitude;
            if (sqrDist > (minMoveDistance * minMoveDistance))
            {
                int posCount = currentLineRenderer.positionCount;
                currentLineRenderer.positionCount = posCount + 1;
                currentLineRenderer.SetPosition(posCount, penTip.position);

                lastPointPosition = penTip.position;
            }
        }
    }

    #endregion

    #region Public Methods (라인 그리기 / 통째 지우기 / 색상 설정 등)

    /// <summary>
    /// 라인 그리기 시작
    /// </summary>
    public void StartDrawing()
    {
        if (!penTip) return;
        isDrawing = true;

        // 라인 시작점 2개
        lastPointPosition = penTip.position;
        currentLineRenderer.positionCount = 2;
        currentLineRenderer.SetPosition(0, penTip.position);
        currentLineRenderer.SetPosition(1, penTip.position);

        // 라인 렌더러 색상
        currentLineRenderer.startColor = colors[colorIndex];
        currentLineRenderer.endColor = colors[colorIndex];
    }

    /// <summary>
    /// 라인 그리기 중단 → 확정
    /// </summary>
    public void StopDrawing()
    {
        if (!isDrawing) return;
        isDrawing = false;

        // 라인 Simplify
        currentLineRenderer.Simplify(simplifyTolerance);

        // 라인을 메인 라인 렌더러에 합치고, 내부 데이터 저장
        AddLineToMain(currentLineRenderer, colorIndex);

        // 현재 라인 렌더러 초기화
        currentLineRenderer.positionCount = 0;
    }

    /// <summary>
    /// 전체 라인 삭제
    /// </summary>
    public void Clear()
    {
        // 내부 데이터 삭제
        linePositions.Clear();
        lineColorIndices.Clear();
        lineBounds.Clear();

        // 라인 렌더러 초기화
        ClearAllMainLineRenderers();
        currentLineRenderer.positionCount = 0;
    }

    /// <summary>
    /// 색상 인덱스 하나 증가 (loop)
    /// </summary>
    public void IncrementColor()
    {
        colorIndex = (colorIndex + 1) % colors.Length;
        if (pen != null)
            pen.SetPenModelColor(colors[colorIndex]);
    }

    /// <summary>
    /// 색상 인덱스 직접 설정
    /// </summary>
    public void SetColor(int value)
    {
        colorIndex = Mathf.Clamp(value, 0, colors.Length - 1);
        if (pen != null)
            pen.SetPenModelColor(colors[colorIndex]);
    }

    /// <summary>
    /// 현재 그리고 있는 라인을 지움
    /// </summary>
    public void ClearCurrentLine()
    {
        currentLineRenderer.positionCount = 0;
    }

    /// <summary>
    /// 지우개(구)와 닿은 라인을 통째로 삭제
    /// </summary>
    public void CheckEraseLine(Vector3 position, float radius)
    {
        int index = GetIntersectingLineIndex(position, radius);
        if (index >= 0)
        {
            EraseLine(index);
        }
    }

    /// <summary>
    /// 지우개가 닿은 라인이 존재하면 전체 Clear
    /// </summary>
    public void CheckClearLines(Vector3 position, float radius)
    {
        int idx = GetIntersectingLineIndex(position, radius);
        if (idx >= 0)
        {
            Clear();
        }
    }

    /// <summary>
    /// 지우개(구)와 닿은 라인 하이라이트
    /// </summary>
    public void CheckMarkLine(Vector3 position, float radius)
    {
        ClearMark();
        int idx = GetIntersectingLineIndex(position, radius);
        if (idx >= 0)
        {
            MarkLine(idx);
        }
    }

    public void ClearMark()
    {
        if (markingLineRenderer)
            markingLineRenderer.positionCount = 0;
    }

    #endregion

    #region 픽셀 단위 부분 지우기 (Pixel Erase)

    /// <summary>
    /// 매 프레임 호출: "픽셀 단위 지우기" 로직
    /// </summary>
    public void CheckPixelEraseLine(Vector3 eraserPosition, float eraseRadius)
    {
        // 1) 셰이더 파라미터 업데이트 → 시각적 마스킹
        foreach (LineRenderer lr in mainLineRenderers)
        {
            lr.material.SetFloat(pixelEraseRadiusParameter, eraseRadius);
            lr.material.SetVector(pixelEraserPositionParameter, eraserPosition);
        }

        // 2) 지우개가 일정 거리 이상 움직였으면, 실제 라인 데이터 수정
        float distSqr = (eraserPosition - lastPixelEraserPosition).sqrMagnitude;
        if (distSqr > (pixelEraserMinMoveDistance * pixelEraserMinMoveDistance))
        {
            lastPixelEraserPosition = eraserPosition;

            // 셰이더 포인트 배열 업데이트
            pixelEraseShaderPoints[pixelEraseShaderPointsIndex] = eraserPosition;
            foreach (LineRenderer lr in mainLineRenderers)
            {
                lr.material.SetVectorArray(pixelErasePointsParameter, pixelEraseShaderPoints);
            }
            pixelEraseShaderPointsIndex++;
            if (pixelEraseShaderPointsIndex >= pixelEraseShaderPointsBufferSize)
            {
                // 버퍼 초과 시 리셋
                pixelEraseShaderPointsIndex = 0;
            }

            // 실제 라인 부분 삭제
            PixelEraseUpdateLinePositions(eraserPosition, eraseRadius);
        }
    }

    /// <summary>
    /// linePositions 전체를 검사해, 구(eraserPosition, eraseRadius)와 겹치는 부분만 부분 삭제
    /// </summary>
    private void PixelEraseUpdateLinePositions(Vector3 position, float radius)
    {
        // 1) mainLineRenderers bounds와 교차 체크
        bool[] lineRendererBoundsChecks = new bool[mainLineRenderers.Length];
        bool intersectedAny = false;
        for (int i = 0; i < mainLineRenderers.Length; i++)
        {
            Bounds lrBounds = mainLineRenderers[i].bounds;
            if (SphereAABBIntersectionCheck(position, radius, lrBounds.min, lrBounds.max))
            {
                lineRendererBoundsChecks[i] = true;
                intersectedAny = true;
            }
            else
            {
                lineRendererBoundsChecks[i] = false;
            }
        }
        if (!intersectedAny) return;

        // 2) linePositions 순회
        bool erasedAny = false;
        for (int i = 0; i < linePositions.Count; i++)
        {
            int cIdx = lineColorIndices[i];
            if (!lineRendererBoundsChecks[cIdx]) continue;

            var b = lineBounds[i];
            if (!SphereAABBIntersectionCheck(position, radius, b.min, b.max))
                continue;

            var pts = linePositions[i];
            if (pts.Count < 2) continue;

            // 실제 부분 지우기
            bool changedLine = EraseSegmentsInLine(ref pts, position, radius);
            if (changedLine)
            {
                erasedAny = true;
                // 바운즈 재계산
                if (pts.Count >= 2)
                {
                    Bounds newB = new Bounds(pts[0], Vector3.zero);
                    for (int p = 1; p < pts.Count; p++)
                        newB.Encapsulate(pts[p]);
                    lineBounds[i] = newB;
                }
                else
                {
                    // 완전히 지워져서 2점 미만
                    lineBounds[i] = new Bounds();
                }
            }

            linePositions[i] = pts;
        }

        if (erasedAny)
        {
            // null(2점 미만) 라인 제거
            ClearNullLines();
            // 라인 렌더러 갱신
            RebuildMainLineRenderers();
        }
    }

    /// <summary>
    /// 한 라인(pts)에서, 구와 교차하는 구간만 삭제
    /// </summary>
    private bool EraseSegmentsInLine(ref List<Vector3> pts, Vector3 center, float radius)
    {
        bool removed = false;
        List<Vector3> newLine = new List<Vector3>();
        newLine.Add(pts[0]);
        bool removing = false;

        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 a = pts[i];
            Vector3 b = pts[i + 1];

            bool intersects = SegmentSphereIntersectionCheck(center, radius, a, b);
            if (!intersects)
            {
                if (!removing)
                    newLine.Add(b);
            }
            else
            {
                removing = !removing;
                removed = true;
            }
        }

        if (newLine.Count < 2)
        {
            pts.Clear();
        }
        else
        {
            pts = newLine;
        }
        return removed;
    }

    private void ClearNullLines()
    {
        for (int i = linePositions.Count - 1; i >= 0; i--)
        {
            if (linePositions[i].Count < 2)
            {
                linePositions.RemoveAt(i);
                lineColorIndices.RemoveAt(i);
                lineBounds.RemoveAt(i);
            }
        }
    }

    #endregion

    #region Private Methods (라인 추가/재생성 등)

    /// <summary>
    /// 현재 라인을 메인 라인 렌더러에 합치고, 내부 데이터에 저장
    /// </summary>
    private void AddLineToMain(LineRenderer lineRenderer, int colorIdx)
    {
        if (lineRenderer.positionCount < 2) return;
        Vector3[] finishedLinePoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(finishedLinePoints);

        // 메인 라인 렌더러에 연결
        var lr = mainLineRenderers[colorIdx];
        int oldCount = lr.positionCount;
        lr.positionCount = oldCount + finishedLinePoints.Length + 1;
        lr.SetPosition(oldCount, lineBreakPosition);
        for (int i = 0; i < finishedLinePoints.Length; i++)
        {
            lr.SetPosition(oldCount + i + 1, finishedLinePoints[i]);
        }

        // 내부 데이터에 저장
        linePositions.Add(finishedLinePoints.ToList());
        lineColorIndices.Add(colorIdx);

        // 바운즈 계산
        Bounds b = new Bounds(finishedLinePoints[0], Vector3.zero);
        for (int i = 1; i < finishedLinePoints.Length; i++)
            b.Encapsulate(finishedLinePoints[i]);
        lineBounds.Add(b);
    }

    private void EraseLine(int index)
    {
        if (index < 0 || index >= linePositions.Count) return;
        int cIdx = lineColorIndices[index];
        linePositions.RemoveAt(index);
        lineColorIndices.RemoveAt(index);
        lineBounds.RemoveAt(index);

        RebuildMainLineRenderers();
    }

    private void RebuildMainLineRenderers()
    {
        ClearAllMainLineRenderers();
        for (int i = 0; i < linePositions.Count; i++)
        {
            var pts = linePositions[i];
            if (pts.Count < 2) continue;

            int cIdx = lineColorIndices[i];
            var lr = mainLineRenderers[cIdx];

            int oldCount = lr.positionCount;
            lr.positionCount = oldCount + pts.Count + 1;
            lr.SetPosition(oldCount, lineBreakPosition);
            for (int p = 0; p < pts.Count; p++)
            {
                lr.SetPosition(oldCount + p + 1, pts[p]);
            }
        }
    }

    private void MarkLine(int index)
    {
        if (!markingLineRenderer) return;
        List<Vector3> pts = linePositions[index];
        markingLineRenderer.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++)
        {
            markingLineRenderer.SetPosition(i, pts[i]);
        }
    }

    private void ClearAllMainLineRenderers()
    {
        foreach (var lr in mainLineRenderers)
        {
            if (lr)
                lr.positionCount = 0;
        }
    }

    #endregion

    #region Intersection / Utility

    /// <summary>
    /// position, radius의 구와 바운즈(AABB) 교차 여부 확인
    /// </summary>
    private bool SphereAABBIntersectionCheck(Vector3 center, float radius, Vector3 boxMin, Vector3 boxMax)
    {
        // X축 검사
        if (center.x + radius < boxMin.x) return false;
        if (center.x - radius > boxMax.x) return false;

        // Y축 검사
        if (center.y + radius < boxMin.y) return false;
        if (center.y - radius > boxMax.y) return false;

        // Z축 검사
        if (center.z + radius < boxMin.z) return false;
        if (center.z - radius > boxMax.z) return false;

        return true;
    }

    /// <summary>
    /// 선분-구 충돌 체크
    /// </summary>
    private bool SegmentSphereIntersectionCheck(Vector3 c, float r, Vector3 a, Vector3 b)
    {
        Vector3 AB = b - a;
        Vector3 AC = c - a;
        float t = Vector3.Dot(AC, AB) / AB.sqrMagnitude;
        t = Mathf.Clamp01(t);
        Vector3 closest = a + AB * t;
        return (closest - c).sqrMagnitude <= r * r;
    }

    private int GetIntersectingLineIndex(Vector3 position, float radius)
    {
        for (int i = 0; i < linePositions.Count; i++)
        {
            var bounds = lineBounds[i];
            if (!SphereAABBIntersectionCheck(position, radius, bounds.min, bounds.max))
                continue;

            var pts = linePositions[i];
            for (int p = 0; p < pts.Count - 1; p++)
            {
                if (SegmentSphereIntersectionCheck(position, radius, pts[p], pts[p + 1]))
                    return i;
            }
        }
        return -1;
    }

    #endregion
}
