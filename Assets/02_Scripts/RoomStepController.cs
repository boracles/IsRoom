using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class RoomStepController : MonoBehaviour
{
    [Header("State")]
    public RoomState currentState = RoomState.Idle;

    [Header("References")]
    public AIPerformerI performerI;
    public VoiceHoldInput voiceInput;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    [Header("UI")]
    public TMP_Text questionText;
    public TMP_Text guideText;

    [Header("Timing")]
    public float firstToneDelay = 0.5f;
    public float afterFirstToneDelay = 0.4f;
    public float questionFallbackDuration = 2.5f;
    public float releaseDuration = 0.8f;
    public float createPieceDelay = 0.5f;
    public float nextRoomDelay = 1.0f;

    private RoomConfig currentRoom;
    private Action<GameObject> onRoomFinished;

    public void StartRoom(RoomConfig roomConfig, Action<GameObject> finishedCallback)
    {
        currentRoom = roomConfig;
        onRoomFinished = finishedCallback;

        StopAllCoroutines();
        StartCoroutine(RoomRoutine());
    }

    private IEnumerator RoomRoutine()
    {
        ClearUI();

        currentState = RoomState.DoorFocused;

        performerI.SetIColor(currentRoom.iParticleColor);
        performerI.MoveToRoomDoor(currentRoom.roomType);

        yield return new WaitForSeconds(firstToneDelay);

        currentState = RoomState.FirstTone;

        if (currentRoom.firstToneClip != null)
        {
            sfxSource.PlayOneShot(currentRoom.firstToneClip);
        }

        performerI.FirstTonePulse();

        yield return new WaitForSeconds(GetClipLength(currentRoom.firstToneClip) + afterFirstToneDelay);

        currentState = RoomState.Question;

        if (questionText != null)
        {
            questionText.text = currentRoom.questionText;
        }

        performerI.StartSpeaking();

        if (currentRoom.questionVoiceClip != null)
        {
            voiceSource.PlayOneShot(currentRoom.questionVoiceClip);
            yield return new WaitForSeconds(currentRoom.questionVoiceClip.length);
        }
        else
        {
            yield return new WaitForSeconds(questionFallbackDuration);
        }

        performerI.StopSpeaking();

        currentState = RoomState.WaitingHold;

        if (guideText != null)
        {
            guideText.text = "화면의 I를 누른 채 답해보세요.";
        }

        performerI.MoveToTouchPosition();

        yield return new WaitUntil(() => voiceInput.IsHolding);

        currentState = RoomState.Listening;

        if (guideText != null)
        {
            guideText.text = "누르고 있는 동안 I가 듣고 있습니다.";
        }

        performerI.StartListening();

        yield return new WaitUntil(() => !voiceInput.IsHolding);

        currentState = RoomState.Release;

        if (guideText != null)
        {
            guideText.text = "I가 듣기를 멈췄습니다.";
        }

        performerI.StopListening();
        performerI.ReleaseContraction();

        if (currentRoom.releaseClip != null)
        {
            sfxSource.PlayOneShot(currentRoom.releaseClip);
        }

        yield return new WaitForSeconds(releaseDuration);

        currentState = RoomState.CreatePiece;

        GameObject createdPiece = CreateRandomPiece();

        if (createdPiece != null)
        {
            performerI.EmitToPiece(createdPiece.transform.position);
        }

        if (currentRoom.createPieceClip != null)
        {
            sfxSource.PlayOneShot(currentRoom.createPieceClip);
        }

        yield return new WaitForSeconds(createPieceDelay);

        currentState = RoomState.Done;

        ClearUI();

        yield return new WaitForSeconds(nextRoomDelay);

        onRoomFinished?.Invoke(createdPiece);
    }

    private GameObject CreateRandomPiece()
    {
        if (currentRoom.piecePrefabs == null || currentRoom.piecePrefabs.Length == 0)
        {
            Debug.LogWarning($"{currentRoom.roomType} room has no piece prefabs.");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, currentRoom.piecePrefabs.Length);
        GameObject selectedPrefab = currentRoom.piecePrefabs[randomIndex];

        Vector3 spawnPosition = transform.position;

        if (currentRoom.pieceSpawnPoint != null)
        {
            spawnPosition = currentRoom.pieceSpawnPoint.position;
        }

        GameObject piece = Instantiate(
            selectedPrefab,
            spawnPosition,
            Quaternion.identity
        );

        piece.name = $"{currentRoom.roomType}_Piece_{randomIndex}";

        return piece;
    }

    private float GetClipLength(AudioClip clip)
    {
        if (clip == null) return 0.5f;
        return clip.length;
    }

    private void ClearUI()
    {
        if (questionText != null)
        {
            questionText.text = "";
        }

        if (guideText != null)
        {
            guideText.text = "";
        }
    }
}