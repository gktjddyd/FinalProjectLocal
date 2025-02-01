using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using PDollarGestureRecognizer;
using System.IO;
using UnityEngine.Events;

public class MovementRecognizer : MonoBehaviour
{
    public XRNode inputSource;
    public UnityEngine.XR.Interaction.Toolkit.InputHelpers.Button inputButton;
    public float inputThreshold = 0.1f;
    public Transform movementSource;

    public float newPositionThresholdDistance = 0.05f;
    public GameObject debugCubePrefab;
    public bool creationMode = true;
    public string newGestureName;

    public float recognitionThreshold = 0.9f;

    [System.Serializable]
    public class UnityStringEvent : UnityEvent<string> { }
    public UnityStringEvent OnRecognized;

    private List<Gesture> trainingSet = new List<Gesture>();
    private bool isMoving = false;
    private List<Vector3> positionList = new List<Vector3>();

    void Start()
    {
        // (1) "Assets/GestureData" 폴더 경로
        string gesturePath = Application.dataPath + "/GestureData";

        // 폴더가 없으면 생성
        if (!Directory.Exists(gesturePath))
        {
            Directory.CreateDirectory(gesturePath);
        }

        // (2) 제스처 파일(.xml) 로드
        string[] gestureFiles = Directory.GetFiles(gesturePath, "*.xml");
        foreach (var item in gestureFiles)
        {
            trainingSet.Add(GestureIO.ReadGestureFromFile(item));
        }
    }

    void Update()
    {
        UnityEngine.XR.Interaction.Toolkit.InputHelpers.IsPressed(
            InputDevices.GetDeviceAtXRNode(inputSource),
            inputButton,
            out bool isPressed,
            inputThreshold
        );
        
        if (!isMoving && isPressed)
        {
            StartMovement();
        }
        else if (isMoving && !isPressed)
        {
            EndMovement();
        }
        else if (isMoving && isPressed)
        {
            UpdateMovement();
        }
    }

    void StartMovement()
    {
        isMoving = true;
        positionList.Clear();
        positionList.Add(movementSource.position);

        if (debugCubePrefab)
        {
            Destroy(Instantiate(debugCubePrefab, movementSource.position, Quaternion.identity), 3);
        }
    }

    void EndMovement()
    {
        isMoving = false;

        // positionList -> Point[]
        Point[] pointArray = new Point[positionList.Count];
        for (int i = 0; i < positionList.Count; i++)
        {
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(positionList[i]);
            pointArray[i] = new Point(screenPoint.x, screenPoint.y, 0);
        }

        Gesture newGesture = new Gesture(pointArray);

        if (creationMode)
        {
            // (3) 제스처 저장
            newGesture.Name = newGestureName;
            trainingSet.Add(newGesture);

            // "Assets/GestureData/[제스처이름].xml" 로 저장
            string fileName = Application.dataPath + "/GestureData/" + newGestureName + ".xml";
            GestureIO.WriteGesture(pointArray, newGestureName, fileName);

            Debug.Log("Gesture Saved: " + fileName);
        }
        else
        {
            // (4) 인식
            Result result = PointCloudRecognizer.Classify(newGesture, trainingSet.ToArray());

            if (result.Score > recognitionThreshold)
            {
                Debug.Log(result.GestureClass + " " + result.Score);
                OnRecognized.Invoke(result.GestureClass);
            }
        }
    }

    void UpdateMovement()
    {
        Vector3 lastPosition = positionList[positionList.Count - 1];
        if (Vector3.Distance(movementSource.position, lastPosition) > newPositionThresholdDistance)
        {
            positionList.Add(movementSource.position);
            if (debugCubePrefab)
            {
                Destroy(Instantiate(debugCubePrefab, movementSource.position, Quaternion.identity), 3);
            }
        }
    }
}
