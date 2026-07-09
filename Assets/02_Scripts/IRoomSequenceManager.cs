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

    [Header("Final Room")]
    public RoomType finalRoomType = RoomType.Light;

    [Header("Light Room Window Emission")]
    public LightRoomWindowEmissionController lightRoomWindowEmission;
    public InkColorType selectedInkColor = InkColorType.Blue;

    private bool waitingForFinalRoomTarget = false;

    [Header("Generated Pieces")]
    public List<GameObject> generatedPieces = new List<GameObject>();

    [Header("Room Detection")]
    public CubeFaceRoomDetector cubeFaceRoomDetector;

    [Header("I Room Base Loop")]
    public AudioSource iRoomBaseLoopSource;
    public float iRoomBaseLoopVolume = 0.45f;
    public float iRoomBaseLoopFadeInTime = 2.0f;
    public float iRoomBaseLoopFadeOutTime = 2.0f;

    private Coroutine iRoomBaseLoopRoutine;

    private List<RoomConfig> roomOrder = new List<RoomConfig>();
    private int currentRoomIndex;

    private bool waitingForRoomTarget = false;

    private float roomTargetAcceptTime = 0f;
    public float roomTargetDetectionDelay = 1.0f;

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

        PlayIRoomBaseLoop();

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
        roomTargetAcceptTime = Time.time + roomTargetDetectionDelay;

        if (cubeFaceRoomDetector != null)
        {
            cubeFaceRoomDetector.ResetDetection();
        }

        roomStepController.ShowFindRoomGuide(expectedRoom.roomType);

        Debug.Log($"이제 {expectedRoom.roomType} 방을 기다립니다.");
    }

    public void OnRoomTargetDetected(RoomType detectedRoomType)
    {
        if (waitingForFinalRoomTarget)
        {
            if (Time.time < roomTargetAcceptTime)
            {
                return;
            }

            if (detectedRoomType == finalRoomType)
            {
                waitingForFinalRoomTarget = false;

                Debug.Log("빛이 머무는 방 인식됨. 마지막 방 시퀀스를 시작합니다.");

                if (lightRoomWindowEmission != null)
                {
                    lightRoomWindowEmission.PlayEmissionByInk(selectedInkColor);
                }

                roomStepController.PlayFinalRoomFoundSequence(() =>
                {
                    roomStepController.StartFinalLightRoom(generatedPieces);
                });
            }
            else
            {
                Debug.Log($"잘못된 방 인식됨: {detectedRoomType}. 기다리는 방: {finalRoomType}");

                roomStepController.ShowWrongFinalRoomGuide(detectedRoomType);
            }

            return;
        }

        if (!waitingForRoomTarget)
        {
            return;
        }

        if (Time.time < roomTargetAcceptTime)
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

            Debug.Log($"{detectedRoomType} 방 인식됨. 확인 메시지 후 방 루틴을 시작합니다.");

            roomStepController.PlayRoomFoundSequence(detectedRoomType, () =>
            {
                StartCurrentRoom();
            });
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

        // 파도의 방에서 생성된 잉크 조각 색 저장
        if (currentRoomIndex >= 0 && currentRoomIndex < roomOrder.Count)
        {
            RoomConfig finishedRoom = roomOrder[currentRoomIndex];

            if (finishedRoom != null && finishedRoom.roomType == RoomType.Wave)
            {
                SetSelectedInkColorFromPiece(createdPiece);
            }
        }

        currentRoomIndex++;

        if (currentRoomIndex < roomOrder.Count)
        {
            WaitForCurrentRoomTarget();
        }
        else
        {
            waitingForRoomTarget = false;

            Debug.Log("계단, 파도, 그림자의 방 완료. 이제 빛이 머무는 방을 기다립니다.");

            WaitForFinalRoomTarget();
        }
    }

    private void SetSelectedInkColorFromPiece(GameObject createdPiece)
    {
        if (createdPiece == null)
        {
            Debug.LogWarning("파도의 방 조각이 없어서 잉크 색을 기본값 Blue로 유지합니다.");
            selectedInkColor = InkColorType.Blue;
            return;
        }

        string pieceName = createdPiece.name.ToLower();

        if (pieceName.Contains("blue"))
        {
            selectedInkColor = InkColorType.Blue;
        }
        else if (pieceName.Contains("green"))
        {
            selectedInkColor = InkColorType.Green;
        }
        else if (pieceName.Contains("purple"))
        {
            selectedInkColor = InkColorType.Purple;
        }
        else
        {
            Debug.LogWarning($"잉크 색을 판단할 수 없는 조각 이름입니다: {createdPiece.name}. 기본값 Blue를 사용합니다.");
            selectedInkColor = InkColorType.Blue;
        }

        Debug.Log($"선택된 잉크 색 저장됨: {selectedInkColor} / 조각 이름: {createdPiece.name}");
    }

    private void WaitForFinalRoomTarget()
    {
        waitingForFinalRoomTarget = true;
        roomTargetAcceptTime = Time.time + roomTargetDetectionDelay;

        if (cubeFaceRoomDetector != null)
        {
            cubeFaceRoomDetector.ResetDetection();
        }

        if (roomStepController != null)
        {
            roomStepController.ShowFindFinalRoomGuide();
        }

        Debug.Log("이제 빛이 머무는 방을 기다립니다.");
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
        waitingForFinalRoomTarget = false;

        selectedInkColor = InkColorType.Blue;

        if (lightRoomWindowEmission != null)
        {
            lightRoomWindowEmission.TurnOffEmission();
        }

        if (cubeFaceRoomDetector != null)
        {
            cubeFaceRoomDetector.ResetDetection();
        }

        PlayIRoomBaseLoop();

        StartCurrentRoom();
    }

    [ContextMenu("Start From Wave")]
    public void StartFromWave()
    {
        currentRoomIndex = 1;
        generatedPieces.Clear();
        waitingForRoomTarget = false;
        waitingForFinalRoomTarget = false;

        selectedInkColor = InkColorType.Blue;

        if (lightRoomWindowEmission != null)
        {
            lightRoomWindowEmission.TurnOffEmission();
        }

        if (cubeFaceRoomDetector != null)
        {
            cubeFaceRoomDetector.ResetDetection();
        }

        PlayIRoomBaseLoop();

        StartCurrentRoom();
    }

    [ContextMenu("Start From Shadow")]
    public void StartFromShadow()
    {
        currentRoomIndex = 2;
        generatedPieces.Clear();
        waitingForRoomTarget = false;
        waitingForFinalRoomTarget = false;

        selectedInkColor = InkColorType.Blue;

        if (lightRoomWindowEmission != null)
        {
            lightRoomWindowEmission.TurnOffEmission();
        }

        if (cubeFaceRoomDetector != null)
        {
            cubeFaceRoomDetector.ResetDetection();
        }

        PlayIRoomBaseLoop();

        StartCurrentRoom();
    }

    private void PlayIRoomBaseLoop()
    {
        if (iRoomBaseLoopSource == null)
        {
            return;
        }

        if (iRoomBaseLoopRoutine != null)
        {
            StopCoroutine(iRoomBaseLoopRoutine);
        }

        iRoomBaseLoopSource.loop = true;

        if (!iRoomBaseLoopSource.isPlaying)
        {
            iRoomBaseLoopSource.volume = 0f;
            iRoomBaseLoopSource.Play();
        }

        iRoomBaseLoopRoutine = StartCoroutine(
            FadeAudio(iRoomBaseLoopSource, iRoomBaseLoopVolume, iRoomBaseLoopFadeInTime, false)
        );
    }

    private void StopIRoomBaseLoop()
    {
        if (iRoomBaseLoopSource == null)
        {
            return;
        }

        if (iRoomBaseLoopRoutine != null)
        {
            StopCoroutine(iRoomBaseLoopRoutine);
        }

        iRoomBaseLoopRoutine = StartCoroutine(
            FadeAudio(iRoomBaseLoopSource, 0f, iRoomBaseLoopFadeOutTime, true)
        );
    }

    private IEnumerator FadeAudio(AudioSource source, float targetVolume, float duration, bool stopAfterFade)
    {
        if (source == null)
        {
            yield break;
        }

        float startVolume = source.volume;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            source.volume = targetVolume;

            if (stopAfterFade)
            {
                source.Stop();
            }

            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        source.volume = targetVolume;

        if (stopAfterFade)
        {
            source.Stop();
        }
    }
}