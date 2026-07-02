using System.Collections.Generic;
using UnityEngine;

public class IRoomSequenceManager : MonoBehaviour
{
    [Header("Test Start")]
    public bool autoStartOnPlay = true;
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

    private void Awake()
    {
        roomOrder.Clear();
        roomOrder.Add(stairRoomConfig);
        roomOrder.Add(waveRoomConfig);
        roomOrder.Add(shadowRoomConfig);

        currentRoomIndex = GetStartIndex(startRoom);
    }

    private void Start()
    {
        if (autoStartOnPlay)
        {
            StartCurrentRoom();
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
            StartCurrentRoom();
        }
        else
        {
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
        StartCurrentRoom();
    }

    [ContextMenu("Start From Wave")]
    public void StartFromWave()
    {
        currentRoomIndex = 1;
        generatedPieces.Clear();
        StartCurrentRoom();
    }

    [ContextMenu("Start From Shadow")]
    public void StartFromShadow()
    {
        currentRoomIndex = 2;
        generatedPieces.Clear();
        StartCurrentRoom();
    }
}