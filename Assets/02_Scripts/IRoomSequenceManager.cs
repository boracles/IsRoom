using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IRoomSequenceManager : MonoBehaviour
{
    [Header("Test Start")]
    public bool autoStartOnPlay = false;
    public TestStartRoom startRoom = TestStartRoom.Stair;

    [Header("Room Controller")]
    public RoomStepController roomStepController;

    [Header("Room Configs")]
    public RoomConfig stairRoomConfig;
    public RoomConfig waveRoomConfig;
    public RoomConfig shadowRoomConfig;

    [Header("Generated Pieces")]
    public List<GameObject> generatedPieces = new List<GameObject>();

    private List<RoomConfig> roomOrder = new List<RoomConfig>();
    private int currentRoomIndex;

    private bool waitingForRoomTarget = false;

    private void Awake()
    {
        roomOrder.Clear();
        roomOrder.Add(stairRoomConfig);
        roomOrder.Add(waveRoomConfig);
        roomOrder.Add(shadowRoomConfig);

        currentRoomIndex = GetStartIndex(startRoom);
    }

    private IEnumerator Start()
    {
        yield return null;

        if (autoStartOnPlay)
        {
            StartSequenceAfterIRoomAwakened();
        }
    }

    public void StartSequenceAfterIRoomAwakened()
    {
        if (roomStepController == null)
        {
            Debug.LogError("RoomStepController가 연결되어 있지 않습니다.");
            return;
        }

        roomStepController.PlayIRoomAwakenedSequence(() =>
        {
            WaitForCurrentRoomTarget();
        });
    }

    private void WaitForCurrentRoomTarget()
    {
        if (currentRoomIndex >= roomOrder.Count)
        {
            Debug.Log("더 이상 기다릴 방이 없습니다.");
            return;
        }

        RoomConfig expectedRoom = roomOrder[currentRoomIndex];

        if (expectedRoom == null)
        {
            Debug.LogError("기다릴 RoomConfig가 비어 있습니다.");
            return;
        }

        waitingForRoomTarget = true;

        roomStepController.ShowFindRoomGuide(expectedRoom.roomType);

        Debug.Log($"이제 {expectedRoom.roomType} 방을 기다립니다.");
    }

    public void OnRoomTargetDetected(RoomType detectedRoomType)
    {
        if (!waitingForRoomTarget)
        {
            return;
        }

        if (currentRoomIndex >= roomOrder.Count)
        {
            return;
        }

        RoomConfig expectedRoom = roomOrder[currentRoomIndex];

        if (expectedRoom == null)
        {
            Debug.LogError("현재 RoomConfig가 비어 있습니다.");
            return;
        }

        if (detectedRoomType == expectedRoom.roomType)
        {
            waitingForRoomTarget = false;

            Debug.Log($"{detectedRoomType} 방 인식됨. 방 루틴을 시작합니다.");

            StartCurrentRoom();
        }
        else
        {
            Debug.Log($"잘못된 방 인식됨: {detectedRoomType}. 기다리는 방: {expectedRoom.roomType}");

            roomStepController.ShowWrongRoomGuide(expectedRoom.roomType, detectedRoomType);
        }
    }

    public void StartCurrentRoom()
    {
        if (currentRoomIndex >= roomOrder.Count)
        {
            Debug.Log("앞의 세 방이 모두 끝났습니다.");
            return;
        }

        RoomConfig currentRoom = roomOrder[currentRoomIndex];

        if (currentRoom == null)
        {
            Debug.LogError("RoomConfig가 비어 있습니다.");
            return;
        }

        roomStepController.StartRoom(currentRoom, OnRoomFinished);
    }

    private void OnRoomFinished(GameObject createdPiece)
    {
        if (createdPiece != null)
        {
            generatedPieces.Add(createdPiece);
        }

        currentRoomIndex++;

        if (currentRoomIndex < roomOrder.Count)
        {
            WaitForCurrentRoomTarget();
        }
        else
        {
            waitingForRoomTarget = false;
            Debug.Log("계단, 파도, 그림자의 방 완료.");
        }
    }

    private int GetStartIndex(TestStartRoom room)
    {
        switch (room)
        {
            case TestStartRoom.Stair:
                return 0;

            case TestStartRoom.Wave:
                return 1;

            case TestStartRoom.Shadow:
                return 2;

            default:
                return 0;
        }
    }

    [ContextMenu("Start From Stair")]
    public void StartFromStair()
    {
        currentRoomIndex = 0;
        generatedPieces.Clear();
        waitingForRoomTarget = false;
        StartCurrentRoom();
    }

    [ContextMenu("Start From Wave")]
    public void StartFromWave()
    {
        currentRoomIndex = 1;
        generatedPieces.Clear();
        waitingForRoomTarget = false;
        StartCurrentRoom();
    }

    [ContextMenu("Start From Shadow")]
    public void StartFromShadow()
    {
        currentRoomIndex = 2;
        generatedPieces.Clear();
        waitingForRoomTarget = false;
        StartCurrentRoom();
    }
}