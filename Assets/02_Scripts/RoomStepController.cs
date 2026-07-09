using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Piece Created Guide Voice")]
    public AudioClip stairPieceCreatedGuideClip;
    public AudioClip wavePieceCreatedGuideClip;
    public AudioClip shadowPieceCreatedGuideClip;

    [Header("Generated Piece Parent")]
    public Transform generatedPieceParent;

    [Header("Awake Sequence")]
    public AudioClip scanIRoomGuideClip;
    public AudioClip iRoomAwakeClip;
    public AudioClip stairRoomGuideClip;
    public float awakenedMessageDuration = 2.8f;
    public float nextGuideDuration = 2.0f;

    [Header("Scan Guide Voice Repeat")]
    public float scanGuideVoiceInitialDelay = 0.8f;
    public float scanGuideVoiceRepeatDelay = 4.0f;

    private Coroutine scanGuideVoiceRoutine;

    [Header("Awake Visual Feedback")]
    public GameObject iRoomAwakeParticleObject;
    public float particleHideDelay = 2.5f;

    [Header("Guide Text Highlight")]
    public Color guideNormalColor = Color.white;
    public Color guideHighlightColor = new Color(1f, 0.88f, 0.25f, 1f);
    public float guideHighlightDuration = 0.9f;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioSource questionVoiceSource;
    public AudioSource guideVoiceSource;

    [Header("Room Found SFX")]
    public AudioClip stairRoomFoundClip;
    public AudioClip waveRoomFoundClip;
    public AudioClip shadowRoomFoundClip;
    public AudioClip lightRoomFoundClip;

    [Header("Music Listening")]
    public float musicListenDuration = 20f;
    public float pieceRotationSpeed = 18f;

    [Header("UI")]
    public GameObject scanGuideGroup;
    public GameObject questionPanel;
    public GameObject guidePanel;
    public TMP_Text questionText;
    public TMP_Text guideText;

    [Header("Touch Guide UI")]
    public GameObject touchGuideObject;

    [Header("Room Guide Image")]
    public GameObject roomGuideImagePanel;
    public Image roomGuideImage;

    public Sprite stairGuideSprite;
    public Sprite waveGuideSprite;
    public Sprite shadowGuideSprite;
    public Sprite lightGuideSprite;

    [Header("Timing")]
    public float firstToneDelay = 0.5f;
    public float afterFirstToneDelay = 0.4f;
    public float questionFallbackDuration = 2.5f;
    public float releaseDuration = 0.8f;
    public float createPieceDelay = 0.5f;
    public float nextRoomDelay = 1.0f;

    [Header("Room Found Feedback")]
    public float roomFoundMessageDuration = 1.5f;

    [Header("Final Light Room Piece Movement")]
    public Transform[] finalPieceUpperPositions;
    public float finalPieceMoveUpDuration = 1.5f;
    public AnimationCurve finalPieceMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Final Door Keyhole Highlight")]
    public Renderer keyholeRenderer;
    public Material keyholeNormalMaterial;
    public Material keyholeEmissionMaterial;

    [Header("Final Key Guide")]
    public GameObject keyholeGuideObject;
    public AudioClip keyToDoorGuideClip;
    public AudioClip openFinalDoorGuideClip;

    [Header("Final Key Drag Guide")]
    public KeyDragGuideUI keyDragGuideUI;
    public Transform keyholeTarget;
    public Transform keySnapPose;
    public Camera arCamera;

    [Header("Final Door Unlock Sequence")]
    public FinalDoorUnlockSequence finalDoorUnlockSequence;

    private RoomConfig currentRoom;
    private Action<GameObject> onRoomFinished;

    private bool sequenceStarted = false;

    private Dictionary<RoomType, int> lastPieceIndexByRoom = new Dictionary<RoomType, int>();
    private System.Random pieceRandom;

    private void Awake()
    {
        int seed = System.Guid.NewGuid().GetHashCode();
        pieceRandom = new System.Random(seed);

        Debug.Log($"[RoomStepController] Piece random seed: {seed}");
    }

    private IEnumerator Start()
    {
        yield return null;

        yield return new WaitForSeconds(0.8f);

        if (!sequenceStarted)
        {
            ShowScanGuide();
        }
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

        StartScanGuideVoiceLoop();
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
        sequenceStarted = true;

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

        if (performerI != null)
        {
            performerI.StopListening();
        }

        currentState = RoomState.DoorFocused;

        performerI.SetIColor(currentRoom.iParticleColor);
        performerI.MoveToRoomDoor(currentRoom.roomType);

        yield return new WaitForSeconds(firstToneDelay);

        currentState = RoomState.FirstTone;

        if (currentRoom.firstToneClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(currentRoom.firstToneClip);
        }

        performerI.FirstTonePulse();

        yield return new WaitForSeconds(GetClipLength(currentRoom.firstToneClip) + afterFirstToneDelay);

        // 여기서 질문을 바로 띄우지 않음.
        // 먼저 I를 TouchPoint로 이동시킴.
        currentState = RoomState.WaitingHold;

        ClearUI();
        SetGuide("I가 당신에게 다가가고 있습니다.");

        performerI.StopListening();
        performerI.MoveToTouchPosition();

        // I가 TouchPoint에 도착할 때까지 기다림.
        yield return new WaitUntil(() => performerI.HasArrivedAtCurrentTarget(0.03f));

        // 도착한 뒤 질문 표시.
        currentState = RoomState.Question;

        SetGuide("");
        SetQuestion(currentRoom.questionText);
        SetTouchGuide(true);

        performerI.StartSpeaking();

        if (currentRoom.questionVoiceClip != null && questionVoiceSource != null)
        {
            questionVoiceSource.PlayOneShot(currentRoom.questionVoiceClip);
            yield return new WaitForSeconds(currentRoom.questionVoiceClip.length);
        }
        else
        {
            yield return new WaitForSeconds(questionFallbackDuration);
        }

        performerI.StopSpeaking();

        currentState = RoomState.WaitingHold;

        SetGuide("화면의 I를 누른 채 답해보세요.");

        // 혹시 이전 터치가 남아 있으면 먼저 완전히 떼기를 기다림.
        yield return new WaitUntil(() => voiceInput == null || !voiceInput.IsHolding);

        // 새로 I를 누를 때까지 기다림.
        yield return new WaitUntil(() => voiceInput != null && voiceInput.IsHolding);

        SetTouchGuide(false);

        currentState = RoomState.Listening;

        SetGuide("누르고 있는 동안 I가 듣고 있습니다.");

        performerI.StartListening();

        // 누르고 있는 동안만 듣기.
        yield return new WaitUntil(() => voiceInput == null || !voiceInput.IsHolding);

        currentState = RoomState.Release;

        SetGuide("I가 듣기를 멈췄습니다.");

        performerI.StopListening();
        performerI.ReleaseContraction();

        // 답변이 끝나면 I가 조각을 가리지 않는 위치로 비켜남
        performerI.MoveToAfterAnswerPosition();

        if (currentRoom.releaseClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(currentRoom.releaseClip);
        }

        // I가 어느 정도 비켜날 때까지 기다림
        yield return new WaitUntil(() => performerI.HasArrivedAtCurrentTarget(0.04f));

        yield return new WaitForSeconds(releaseDuration);

        currentState = RoomState.CreatePiece;

        GameObject createdPiece = CreateRandomPiece();

        if (createdPiece != null)
        {
            performerI.EmitToPiece(createdPiece.transform.position);
        }

        if (currentRoom.createPieceClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(currentRoom.createPieceClip);
        }

        SetQuestion("");

        // 1. 조각 생성 완료 문장
        SetGuide(GetPieceCreatedGuide(currentRoom.roomType));
        PlayGuideHighlight();

        AudioClip pieceCreatedGuideClip = GetPieceCreatedGuideClip(currentRoom.roomType);
        PlayGuideVoiceClip(pieceCreatedGuideClip);

        float pieceCreatedWaitTime = pieceCreatedGuideClip != null
            ? Mathf.Max(createPieceDelay, pieceCreatedGuideClip.length)
            : createPieceDelay;

        yield return new WaitForSeconds(pieceCreatedWaitTime);

        // 2. 음악 듣기 안내
        SetGuide("조각이 담고 있는 소리를 들어보세요.");

        // 3. 생성된 조각 프리팹 안의 AudioSource 재생 + 조각 회전
        if (createdPiece != null)
        {
            yield return StartCoroutine(PlayPieceAudioAndRotate(createdPiece));
        }
        else
        {
            yield return new WaitForSeconds(musicListenDuration);
        }

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

        int prefabCount = currentRoom.piecePrefabs.Length;

        if (pieceRandom == null)
        {
            pieceRandom = new System.Random(System.Guid.NewGuid().GetHashCode());
        }

        int randomIndex = pieceRandom.Next(prefabCount);

        // 같은 방에서 직전에 나온 프리팹은 피하기
        if (prefabCount > 1 && lastPieceIndexByRoom.TryGetValue(currentRoom.roomType, out int lastIndex))
        {
            int safety = 0;

            while (randomIndex == lastIndex && safety < 20)
            {
                randomIndex = pieceRandom.Next(prefabCount);
                safety++;
            }
        }

        lastPieceIndexByRoom[currentRoom.roomType] = randomIndex;

        GameObject selectedPrefab = currentRoom.piecePrefabs[randomIndex];

        Debug.Log(
            $"[{currentRoom.roomType}] 랜덤 선택됨: index {randomIndex}, prefab {selectedPrefab.name}, total {prefabCount}"
        );

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
            spawnRotation,
            generatedPieceParent
        );

        piece.name = selectedPrefab.name;

        return piece;
    }

    public GameObject CreateRoomPieceOnly(RoomConfig roomConfig)
    {
        if (roomConfig == null)
        {
            Debug.LogWarning("[RoomStepController] RoomConfig가 비어 있어 조각만 생성할 수 없습니다.");
            return null;
        }

        RoomConfig previousRoom = currentRoom;
        currentRoom = roomConfig;

        GameObject createdPiece = CreateRandomPiece();

        currentRoom = previousRoom;

        if (createdPiece != null)
        {
            AudioSource pieceAudio = createdPiece.GetComponent<AudioSource>();

            if (pieceAudio == null)
            {
                pieceAudio = createdPiece.GetComponentInChildren<AudioSource>(true);
            }

            if (pieceAudio != null)
            {
                pieceAudio.Stop();
                pieceAudio.playOnAwake = false;
            }

            Debug.Log($"[RoomStepController] 이전 방 조각만 생성됨: {roomConfig.roomType} / {createdPiece.name}");
        }

        return createdPiece;
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

    private IEnumerator RotatePieceDuringMusic(Transform pieceTransform)
    {
        if (pieceTransform == null)
        {
            yield return new WaitForSeconds(musicListenDuration);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < musicListenDuration)
        {
            if (pieceTransform != null)
            {
                pieceTransform.Rotate(
                    Vector3.up,
                    pieceRotationSpeed * Time.deltaTime,
                    Space.Self
                );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private string GetPieceCreatedGuide(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Stair:
                return "계단의 소리 조각이 만들어졌습니다.";

            case RoomType.Wave:
                return "파도의 소리 조각이 만들어졌습니다.";

            case RoomType.Shadow:
                return "그림자의 소리 조각이 만들어졌습니다.";

            default:
                return "소리 조각이 만들어졌습니다.";
        }
    }

    private AudioClip GetPieceCreatedGuideClip(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Stair:
                return stairPieceCreatedGuideClip;

            case RoomType.Wave:
                return wavePieceCreatedGuideClip;

            case RoomType.Shadow:
                return shadowPieceCreatedGuideClip;

            default:
                return null;
        }
    }

    private IEnumerator PlayPieceAudioAndRotate(GameObject piece)
    {
        if (piece == null)
        {
            yield return new WaitForSeconds(musicListenDuration);
            yield break;
        }

        AudioSource pieceAudio = piece.GetComponent<AudioSource>();

        if (pieceAudio == null)
        {
            pieceAudio = piece.GetComponentInChildren<AudioSource>(true);
        }

        if (pieceAudio != null)
        {
            pieceAudio.Stop();
            pieceAudio.loop = false;

            if (pieceAudio.clip != null)
            {
                pieceAudio.Play();
                Debug.Log($"[RoomStepController] 조각 사운드 재생: {piece.name} / {pieceAudio.clip.name}");
            }
            else
            {
                Debug.LogWarning($"[RoomStepController] {piece.name} AudioSource에 AudioClip이 없습니다. 음악 없이 회전만 진행합니다.");
            }
        }
        else
        {
            Debug.LogWarning($"[RoomStepController] {piece.name} 프리팹 안에서 AudioSource를 찾지 못했습니다.");
        }

        Transform pieceTransform = piece.transform;
        float elapsed = 0f;

        while (elapsed < musicListenDuration)
        {
            if (pieceTransform != null)
            {
                pieceTransform.Rotate(
                    Vector3.up,
                    pieceRotationSpeed * Time.deltaTime,
                    Space.Self
                );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (pieceAudio != null)
        {
            pieceAudio.Stop();
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

            case RoomType.Light:
                return lightGuideSprite;

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
        sequenceStarted = true;

        StopAllCoroutines();
        scanGuideVoiceRoutine = null;

        StartCoroutine(IRoomAwakenedRoutine(onFinished));
    }

    private IEnumerator IRoomAwakenedRoutine(Action onFinished)
    {
        currentState = RoomState.Idle;

        // 이전 하단 안내 음성이 끝날 때까지 기다림
        yield return WaitForGuideVoiceToFinish();

        // 1. 육면체 인식 가이드 사라짐
        SetScanGuide(false);
        SetQuestion("");
        HideRoomGuideImage();

        // 2. 파티클 한 번 재생
        PlayIRoomAwakeParticle();

        // 3. I의 방이 깨어났습니다
        SetGuide("I의 방이 깨어났습니다.");
        PlayGuideHighlight();
        PlayGuideVoiceClip(iRoomAwakeClip);

        // I의 방 깨어남 문구와 음성이 충분히 머무름
        float waitTime = iRoomAwakeClip != null
            ? Mathf.Max(awakenedMessageDuration, iRoomAwakeClip.length)
            : awakenedMessageDuration;

        yield return new WaitForSeconds(waitTime);

        // 다음 안내: 오브제를 돌려 방의 문을 찾도록 안내
        SetQuestion("");
        HideRoomGuideImage();
        SetScanGuide(false);
        SetGuide("오브제를 천천히 돌려 방의 문을 비춰보세요.");
        PlayGuideHighlight();

        PlayGuideVoiceClip(stairRoomGuideClip);

        float rotateGuideWaitTime = stairRoomGuideClip != null
            ? Mathf.Max(nextGuideDuration, stairRoomGuideClip.length)
            : nextGuideDuration;

        yield return new WaitForSeconds(rotateGuideWaitTime);

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

    private void SetTouchGuide(bool visible)
    {
        if (touchGuideObject != null)
        {
            touchGuideObject.SetActive(visible);
        }
    }

    private void ClearUI()
    {
        SetQuestion("");
        SetGuide("");
        HideRoomGuideImage();
        SetTouchGuide(false);
        SetKeyholeGuide(false);
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

    public void PlayRoomFoundSequence(RoomType roomType, Action onFinished)
    {
        StopAllCoroutines();
        StartCoroutine(RoomFoundRoutine(roomType, onFinished));
    }

    private IEnumerator RoomFoundRoutine(RoomType roomType, Action onFinished)
    {
        SetScanGuide(false);
        SetQuestion("");
        HideRoomGuideImage();

        switch (roomType)
        {
            case RoomType.Stair:
                SetGuide("계단의 방입니다.");
                break;

            case RoomType.Wave:
                SetGuide("파도의 방입니다.");
                break;

            case RoomType.Shadow:
                SetGuide("그림자의 방입니다.");
                break;

            default:
                SetGuide("방이 열렸습니다.");
                break;
        }

        PlayRoomFoundSFX(roomType);
        PlayGuideHighlight();

        yield return new WaitForSeconds(roomFoundMessageDuration);

        ClearUI();

        onFinished?.Invoke();
    }

    public void ShowFindFinalRoomGuide()
    {
        SetScanGuide(false);
        SetQuestion("");
        SetTouchGuide(false);

        Sprite guideSprite = GetGuideSprite(RoomType.Light);
        SetRoomGuideImage(guideSprite, guideSprite != null);

        SetGuide("오브제를 돌려 빛이 머무는 방을 비춰주세요.");

        if (performerI != null)
        {
            performerI.StopListening();
            performerI.MoveToRoomDoor(RoomType.Light);
            performerI.StartSpeaking();
        }
    }

    public void ShowWrongFinalRoomGuide(RoomType detectedRoomType)
    {
        SetScanGuide(false);
        SetQuestion("");
        SetTouchGuide(false);

        Sprite guideSprite = GetGuideSprite(RoomType.Light);
        SetRoomGuideImage(guideSprite, guideSprite != null);

        SetGuide("아직 마지막 방이 아닙니다. 오브제를 돌려 빛이 머무는 방을 비춰주세요.");

        if (performerI != null)
        {
            performerI.StartSpeaking();
        }
    }

    public void PlayFinalRoomFoundSequence(Action onFinished)
    {
        StopAllCoroutines();
        StartCoroutine(FinalRoomFoundRoutine(onFinished));
    }

    private IEnumerator FinalRoomFoundRoutine(Action onFinished)
    {
        SetScanGuide(false);
        SetQuestion("");
        HideRoomGuideImage();
        SetTouchGuide(false);

        SetKeyholeHighlight(true);

        SetGuide("빛이 머무는 방입니다.");
        PlayRoomFoundSFX(RoomType.Light);
        PlayGuideHighlight();

        if (performerI != null)
        {
            performerI.StopListening();
            performerI.MoveToRoomDoor(RoomType.Light);
            performerI.StartSpeaking();
        }

        float lightRoomFoundWaitTime = lightRoomFoundClip != null
            ? Mathf.Max(roomFoundMessageDuration, lightRoomFoundClip.length)
            : roomFoundMessageDuration;

        yield return new WaitForSeconds(lightRoomFoundWaitTime);

        ClearUI();

        onFinished?.Invoke();
    }

    public void StartFinalLightRoom(List<GameObject> pieces)
    {
        StopAllCoroutines();
        StartCoroutine(FinalLightRoomRoutine(pieces));
    }

    private IEnumerator FinalLightRoomRoutine(List<GameObject> pieces)
    {
        currentState = RoomState.Done;

        SetScanGuide(false);
        SetQuestion("");
        HideRoomGuideImage();
        SetTouchGuide(false);

        Debug.Log($"빛이 머무는 방 시작. 생성된 조각 수: {pieces?.Count ?? 0}");

        SetKeyholeHighlight(true);

        SetGuide("빛이 머무는 방이 열렸습니다.");

        yield return new WaitForSeconds(1f);

        if (pieces != null && pieces.Count > 0)
        {
            SetGuide("조각들이 빛의 방으로 떠오릅니다.");
            yield return StartCoroutine(MovePiecesToFinalUpperPositions(pieces));
        }

        // 1. 먼저 목적 안내
        SetGuide("마지막 문을 열어보세요.");
        PlayGuideHighlight();
        PlayGuideVoiceClip(openFinalDoorGuideClip);

        // "마지막 문을 열어보세요" 음성이 끝난 뒤 텀을 둠
        if (openFinalDoorGuideClip != null)
        {
            yield return new WaitForSeconds(openFinalDoorGuideClip.length + 1.2f);
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }

        // 2. 그 다음 조작 안내
        SetGuide("계단의 열쇠를 문으로 가져가세요.");
        PlayGuideHighlight();
        PlayGuideVoiceClip(keyToDoorGuideClip);

        SetKeyholeHighlight(true);

        // 실제 드래그 가이드/기능 연결
        BindRuntimeKeyToDragGuide(pieces);
        SetKeyholeGuide(true);
    }

    private IEnumerator MovePiecesToFinalUpperPositions(List<GameObject> pieces)
    {
        if (pieces == null || pieces.Count == 0)
            yield break;

        List<Transform> pieceTransforms = new List<Transform>();
        List<Vector3> startPositions = new List<Vector3>();
        List<Quaternion> startRotations = new List<Quaternion>();
        List<Vector3> targetPositions = new List<Vector3>();
        List<Quaternion> targetRotations = new List<Quaternion>();

        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null)
                continue;

            if (finalPieceUpperPositions == null || i >= finalPieceUpperPositions.Length)
                continue;

            Transform targetPoint = finalPieceUpperPositions[i];

            if (targetPoint == null)
                continue;

            Transform pieceTransform = pieces[i].transform;

            pieceTransforms.Add(pieceTransform);
            startPositions.Add(pieceTransform.position);
            startRotations.Add(pieceTransform.rotation);
            targetPositions.Add(targetPoint.position);
            targetRotations.Add(targetPoint.rotation);
        }

        float elapsed = 0f;

        while (elapsed < finalPieceMoveUpDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / finalPieceMoveUpDuration);
            float curvedT = finalPieceMoveCurve != null
                ? finalPieceMoveCurve.Evaluate(t)
                : t;

            for (int i = 0; i < pieceTransforms.Count; i++)
            {
                if (pieceTransforms[i] == null)
                    continue;

                pieceTransforms[i].position = Vector3.Lerp(
                    startPositions[i],
                    targetPositions[i],
                    curvedT
                );

                pieceTransforms[i].rotation = Quaternion.Slerp(
                    startRotations[i],
                    targetRotations[i],
                    curvedT
                );
            }

            yield return null;
        }

        for (int i = 0; i < pieceTransforms.Count; i++)
        {
            if (pieceTransforms[i] == null)
                continue;

            pieceTransforms[i].position = targetPositions[i];
            pieceTransforms[i].rotation = targetRotations[i];
        }
    }

    private void SetKeyholeHighlight(bool highlighted)
    {
        if (keyholeRenderer == null)
            return;

        if (highlighted)
        {
            if (keyholeEmissionMaterial != null)
            {
                keyholeRenderer.material = keyholeEmissionMaterial;
            }
        }
        else
        {
            if (keyholeNormalMaterial != null)
            {
                keyholeRenderer.material = keyholeNormalMaterial;
            }
        }
    }

    private void SetKeyholeGuide(bool visible)
    {
        if (keyholeGuideObject != null)
        {
            keyholeGuideObject.SetActive(visible);
        }
    }

    private void BindRuntimeKeyToDragGuide(List<GameObject> pieces)
    {
        if (keyDragGuideUI == null)
        {
            Debug.LogWarning("KeyDragGuideUI가 연결되어 있지 않습니다.");
            return;
        }

        Transform keyTransform = FindRuntimeKeyTransform(pieces);

        if (keyTransform == null)
        {
            Debug.LogWarning("생성된 열쇠를 찾지 못했습니다. Key_1 / Key_2 / Key_3 이름을 확인하세요.");
            keyDragGuideUI.ClearKeyTarget();
            return;
        }

        keyDragGuideUI.SetKeyTarget(keyTransform);

        if (keyholeTarget != null)
        {
            keyDragGuideUI.SetKeyholeTarget(keyholeTarget);
        }

        DraggableKeyToKeyhole draggableKey = keyTransform.GetComponent<DraggableKeyToKeyhole>();

        if (draggableKey == null)
        {
            draggableKey = keyTransform.gameObject.AddComponent<DraggableKeyToKeyhole>();
        }

        Camera targetCamera = arCamera != null ? arCamera : Camera.main;

        draggableKey.SetTarget(
            targetCamera,
            keyholeTarget,
            keySnapPose,
            keyholeGuideObject
        );

        draggableKey.unlockSequence = finalDoorUnlockSequence;
        draggableKey.enabled = true;

        Debug.Log($"드래그 가이드와 드래그 기능이 키에 연결됨: {keyTransform.name}");
    }

    private Transform FindRuntimeKeyTransform(List<GameObject> pieces)
    {
        // 1. generatedPieces 리스트 안에서 먼저 찾기
        if (pieces != null)
        {
            foreach (GameObject piece in pieces)
            {
                if (piece == null) continue;

                string name = piece.name.ToLower();

                if (name.Contains("key"))
                {
                    return piece.transform;
                }
            }
        }

        // 2. generatedPieceParent 아래에서 다시 찾기
        if (generatedPieceParent != null)
        {
            Transform[] children = generatedPieceParent.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child == null) continue;

                string name = child.name.ToLower();

                if (name.Contains("key_1") ||
                    name.Contains("key_2") ||
                    name.Contains("key_3") ||
                    name.Contains("key"))
                {
                    return child;
                }
            }
        }

        return null;
    }

    private void PlayRoomFoundSFX(RoomType roomType)
    {
        AudioClip clip = GetRoomFoundClip(roomType);

        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private AudioClip GetRoomFoundClip(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Stair:
                return stairRoomFoundClip;

            case RoomType.Wave:
                return waveRoomFoundClip;

            case RoomType.Shadow:
                return shadowRoomFoundClip;

            case RoomType.Light:
                return lightRoomFoundClip;

            default:
                return null;
        }
    }

    private void StartScanGuideVoiceLoop()
    {
        if (scanGuideVoiceRoutine != null)
        {
            StopCoroutine(scanGuideVoiceRoutine);
        }

        scanGuideVoiceRoutine = StartCoroutine(ScanGuideVoiceLoopRoutine());
    }

    private IEnumerator ScanGuideVoiceLoopRoutine()
    {
        yield return new WaitForSeconds(scanGuideVoiceInitialDelay);

        while (currentState == RoomState.Idle)
        {
            PlayGuideVoiceClip(scanIRoomGuideClip);

            float clipLength = scanIRoomGuideClip != null ? scanIRoomGuideClip.length : 0f;
            float waitTime = Mathf.Max(scanGuideVoiceRepeatDelay, clipLength + 0.5f);

            yield return new WaitForSeconds(waitTime);
        }

        scanGuideVoiceRoutine = null;
    }

    private void StopScanGuideVoiceLoop()
    {
        if (scanGuideVoiceRoutine != null)
        {
            StopCoroutine(scanGuideVoiceRoutine);
            scanGuideVoiceRoutine = null;
        }
    }

    private void PlayGuideVoiceClip(AudioClip clip)
    {
        if (clip == null || guideVoiceSource == null)
        {
            return;
        }

        guideVoiceSource.Stop();
        guideVoiceSource.PlayOneShot(clip);
    }

    private IEnumerator WaitForGuideVoiceToFinish()
    {
        if (guideVoiceSource == null)
        {
            yield break;
        }

        while (guideVoiceSource.isPlaying)
        {
            yield return null;
        }
    }

}