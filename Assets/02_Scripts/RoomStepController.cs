using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomStepController : MonoBehaviour
{
    [Header("State")]
    public RoomState currentState = RoomState.Idle;

    [Header("References")]
    public AIPerformerI performerI;
    public VoiceHoldInput voiceInput;

    [Header("Piece Spawn Points")]
    public Transform stairPieceSpawn;
    public Transform wavePieceSpawn;
    public Transform shadowPieceSpawn;

    [Header("Awake Sequence")]
    public AudioClip iRoomAwakeClip;
    public AudioClip stairRoomGuideClip;
    public float awakenedMessageDuration = 2.8f;
    public float nextGuideDuration = 2.0f;

    [Header("Awake Visual Feedback")]
    public GameObject iRoomAwakeParticleObject;
    public float particleHideDelay = 2.5f;

    [Header("Guide Text Highlight")]
    public Color guideNormalColor = Color.white;
    public Color guideHighlightColor = new Color(1f, 0.88f, 0.25f, 1f);
    public float guideHighlightDuration = 0.9f;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    [Header("UI")]
    public GameObject scanGuideGroup;
    public GameObject questionPanel;
    public GameObject guidePanel;
    public TMP_Text questionText;
    public TMP_Text guideText;

    [Header("Room Guide Image")]
    public GameObject roomGuideImagePanel;
    public Image roomGuideImage;

    public Sprite stairGuideSprite;
    public Sprite waveGuideSprite;
    public Sprite shadowGuideSprite;

    [Header("Timing")]
    public float firstToneDelay = 0.5f;
    public float afterFirstToneDelay = 0.4f;
    public float questionFallbackDuration = 2.5f;
    public float releaseDuration = 0.8f;
    public float createPieceDelay = 0.5f;
    public float nextRoomDelay = 1.0f;

    private RoomConfig currentRoom;
    private Action<GameObject> onRoomFinished;

    private void Start()
    {
        ShowScanGuide();
    }

    private void SetScanGuide(bool visible)
    {
        if (scanGuideGroup != null)
        {
            scanGuideGroup.SetActive(visible);
        }
    }

    public void ShowScanGuide()
    {
        currentState = RoomState.Idle;

        StopAllCoroutines();

        HideRoomGuideImage();

        SetScanGuide(true);
        SetQuestion("");
        SetGuide("오브제를 비추어 I의 방을 깨워주세요.");
    }

    public void ShowRotateGuide()
    {
        currentState = RoomState.Idle;

        StopAllCoroutines();

        HideRoomGuideImage();

        SetScanGuide(false);
        SetQuestion("");
        SetGuide("오브제를 천천히 돌려 방의 문을 비춰보세요.");
    }

    public void StartRoom(RoomConfig roomConfig, Action<GameObject> finishedCallback)
    {
        currentRoom = roomConfig;
        onRoomFinished = finishedCallback;

        StopAllCoroutines();
        HideIRoomAwakeParticleImmediately();
        HideRoomGuideImage();

        StartCoroutine(RoomRoutine());
    }

    private void HideIRoomAwakeParticleImmediately()
    {
        if (iRoomAwakeParticleObject == null) return;

        ParticleSystem[] particles = iRoomAwakeParticleObject.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        iRoomAwakeParticleObject.SetActive(false);
    }

    private IEnumerator RoomRoutine()
    {
        SetScanGuide(false);
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

        SetQuestion(currentRoom.questionText);
        SetGuide("");

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

        SetGuide("화면의 I를 누른 채 답해보세요.");

        performerI.MoveToTouchPosition();

        yield return new WaitUntil(() => voiceInput.IsHolding);

        currentState = RoomState.Listening;

        SetGuide("누르고 있는 동안 I가 듣고 있습니다.");

        performerI.StartListening();

        yield return new WaitUntil(() => !voiceInput.IsHolding);

        currentState = RoomState.Release;

        SetGuide("I가 듣기를 멈췄습니다.");

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

        Transform spawnPoint = GetSpawnPoint(currentRoom.roomType);

        Vector3 spawnPosition = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        Quaternion spawnRotation = spawnPoint != null
            ? spawnPoint.rotation
            : Quaternion.identity;

        GameObject piece = Instantiate(
            selectedPrefab,
            spawnPosition,
            spawnRotation
        );

        piece.name = $"{currentRoom.roomType}_Piece_{randomIndex}";

        return piece;
    }

    private Transform GetSpawnPoint(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Stair:
                return stairPieceSpawn;

            case RoomType.Wave:
                return wavePieceSpawn;

            case RoomType.Shadow:
                return shadowPieceSpawn;

            default:
                return null;
        }
    }

    private float GetClipLength(AudioClip clip)
    {
        if (clip == null) return 0.5f;
        return clip.length;
    }

    private void SetQuestion(string text)
    {
        bool hasText = !string.IsNullOrWhiteSpace(text);

        if (questionPanel != null)
        {
            questionPanel.SetActive(hasText);
        }

        if (questionText != null)
        {
            questionText.text = hasText ? text : "";
        }
    }

    private void SetGuide(string text)
    {
        bool hasText = !string.IsNullOrWhiteSpace(text);

        if (guidePanel != null)
        {
            guidePanel.SetActive(hasText);
        }

        if (guideText != null)
        {
            guideText.text = hasText ? text : "";
            guideText.color = guideNormalColor;
        }
    }

    private void SetRoomGuideImage(Sprite sprite, bool visible)
    {
        if (roomGuideImagePanel != null)
        {
            roomGuideImagePanel.SetActive(visible);
        }

        if (roomGuideImage != null)
        {
            roomGuideImage.sprite = sprite;
            roomGuideImage.preserveAspect = true;
        }
    }

    private void HideRoomGuideImage()
    {
        if (roomGuideImagePanel != null)
        {
            roomGuideImagePanel.SetActive(false);
        }
    }

    private Sprite GetGuideSprite(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Stair:
                return stairGuideSprite;

            case RoomType.Wave:
                return waveGuideSprite;

            case RoomType.Shadow:
                return shadowGuideSprite;

            default:
                return null;
        }
    }

    private Coroutine guideHighlightRoutine;

    private void PlayGuideHighlight()
    {
        if (guideText == null) return;

        if (guideHighlightRoutine != null)
        {
            StopCoroutine(guideHighlightRoutine);
        }

        guideHighlightRoutine = StartCoroutine(GuideHighlightRoutine());
    }

    private IEnumerator GuideHighlightRoutine()
    {
        guideText.color = guideHighlightColor;

        float halfDuration = guideHighlightDuration * 0.5f;
        float t = 0f;

        // 노란색으로 밝아졌다가
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // 다시 원래 흰색으로 돌아오기
        t = 0f;
        Color startColor = guideHighlightColor;

        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float lerp = t / halfDuration;
            guideText.color = Color.Lerp(startColor, guideNormalColor, lerp);
            yield return null;
        }

        guideText.color = guideNormalColor;
        guideHighlightRoutine = null;
    }

    public void PlayIRoomAwakenedSequence(Action onFinished)
    {
        StopAllCoroutines();
        StartCoroutine(IRoomAwakenedRoutine(onFinished));
    }

    private IEnumerator IRoomAwakenedRoutine(Action onFinished)
    {
        currentState = RoomState.Idle;

        // 1. 육면체 인식 가이드 사라짐
        SetScanGuide(false);
        SetQuestion("");

        // 2. 파티클 한 번 재생
        PlayIRoomAwakeParticle();

        // 3. I의 방이 깨어났습니다
        SetGuide("I의 방이 깨어났습니다.");
        PlayGuideHighlight();

        if (iRoomAwakeClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(iRoomAwakeClip);
        }

        // 좀 더 오래 머무르게
        yield return new WaitForSeconds(awakenedMessageDuration);

        // 4. 다음 안내
        SetGuide("오브제를 돌려 계단의 방을 비춰주세요.");

        if (stairRoomGuideClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(stairRoomGuideClip);
        }

        yield return new WaitForSeconds(nextGuideDuration);

        // 5. 이후 실제 계단 방 루틴 시작
        ClearUI();

        onFinished?.Invoke();
    }

    private void PlayIRoomAwakeParticle()
    {
        if (iRoomAwakeParticleObject == null) return;

        iRoomAwakeParticleObject.SetActive(true);

        ParticleSystem[] particles = iRoomAwakeParticleObject.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }

        StartCoroutine(HideIRoomAwakeParticleAfterDelay());
    }

    private IEnumerator HideIRoomAwakeParticleAfterDelay()
    {
        yield return new WaitForSeconds(particleHideDelay);

        if (iRoomAwakeParticleObject == null) yield break;

        ParticleSystem[] particles = iRoomAwakeParticleObject.GetComponentsInChildren<ParticleSystem>(true);

        // 새 입자 생성만 멈춤. 이미 나온 입자는 자연스럽게 lifetime 끝까지 사라짐.
        foreach (ParticleSystem ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // 남은 입자들이 사라질 시간을 줌
        yield return new WaitForSeconds(1.2f);

        iRoomAwakeParticleObject.SetActive(false);
    }

    private void ClearUI()
    {
        SetQuestion("");
        SetGuide("");
        HideRoomGuideImage();
    }

    public void ShowFindRoomGuide(RoomType expectedRoomType)
    {
        SetScanGuide(false);
        SetQuestion("");

        Sprite guideSprite = GetGuideSprite(expectedRoomType);
        SetRoomGuideImage(guideSprite, guideSprite != null);

        switch (expectedRoomType)
        {
            case RoomType.Stair:
                SetGuide("오브제를 돌려 계단의 방을 비춰주세요.");
                break;

            case RoomType.Wave:
                SetGuide("오브제를 돌려 파도의 방을 비춰주세요.");
                break;

            case RoomType.Shadow:
                SetGuide("오브제를 돌려 그림자의 방을 비춰주세요.");
                break;

            default:
                SetGuide("오브제를 돌려 다음 방을 비춰주세요.");
                break;
        }

        if (performerI != null)
        {
            performerI.StartSpeaking();
        }
    }

    public void ShowWrongRoomGuide(RoomType expectedRoomType, RoomType detectedRoomType)
    {
        SetScanGuide(false);
        SetQuestion("");

        Sprite guideSprite = GetGuideSprite(expectedRoomType);
        SetRoomGuideImage(guideSprite, guideSprite != null);

        switch (expectedRoomType)
        {
            case RoomType.Stair:
                SetGuide("여기는 계단의 방이 아닙니다. 오브제를 돌려 계단의 방을 비춰주세요.");
                break;

            case RoomType.Wave:
                SetGuide("여기는 파도의 방이 아닙니다. 오브제를 돌려 파도의 방을 비춰주세요.");
                break;

            case RoomType.Shadow:
                SetGuide("여기는 그림자의 방이 아닙니다. 오브제를 돌려 그림자의 방을 비춰주세요.");
                break;

            default:
                SetGuide("아직 다음 방이 아닙니다. 오브제를 천천히 돌려주세요.");
                break;
        }

        if (performerI != null)
        {
            performerI.StartSpeaking();
        }
    }        
}