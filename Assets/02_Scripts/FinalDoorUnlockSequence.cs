using System.Collections;
using UnityEngine;

public class FinalDoorUnlockSequence : MonoBehaviour
{
    [Header("Key Poses")]
    public Transform keyApproachPose;
    public Transform keyInsertPose;
    public Transform keyTurnPose;

    [Header("Key Parent")]
    public Transform keyholeParent;
    public bool parentKeyToKeyholeAfterInsert = true;

    [Header("Key Insert Scale")]
    public bool scaleKeyAfterInsert = true;
    public Vector3 insertedKeyScale = new Vector3(0.65f, 0.65f, 0.65f);
    public float keyScaleDuration = 0.25f;

    [Header("Door Targets")]
    public Transform innerDoor; // Door
    public Transform innerDoorOpenPose;

    public Transform outerDoor; // Wall_Light
    public Transform outerDoorOpenPose;

    [Header("Top Target")]
    public Transform topDoor; // Top
    public Transform topDoorOpenPose;

    [Header("Guide")]
    public GameObject keyDragGuideObject;

    [Header("Timing")]
    public float keyMoveToApproachDuration = 0.35f;
    public float keyInsertDuration = 0.45f;
    public float keyTurnDuration = 0.45f;
    public float pauseAfterTurn = 0.25f;
    public float pauseAfterInsert = 0.35f;
    public float pauseBeforeDoorOpen = 0.5f;
    public float pauseAfterInnerDoorOpen = 0.6f;
    public float pauseAfterOuterAndTopOpen = 0.8f;
    public float pauseBeforeTurntableStart = 0.7f;

    [Header("Key Loop Audio")]
    public bool playKeyAudioAfterOpen = true;
    public float keyAudioFadeInDuration = 1.0f;
    public float keyAudioVolume = 0.8f;

    [Header("Key Insert SFX")]
    public AudioSource sfxSource;
    public AudioClip keyInsertClip;

    [Header("Final Room Close SFX")]
    public AudioSource closeSfxSource;
    public AudioClip finalRoomCloseClip;
    public float closeSfxVolume = 1f;

    [Header("Open Durations")]
    public float innerDoorOpenDuration = 0.8f;
    public float outerDoorOpenDuration = 1.0f;
    public float topDoorOpenDuration = 1.0f;

    [Header("Fake Interior Reveal")]
    public FakeInteriorRoomController fakeInteriorController;
    public bool revealInteriorAfterOpen = true;

    [Header("Turntable Body Motion")]
    public Transform turntableBody;
    public Transform bodyOnCirclePose;
    public bool moveBodyBeforeCircleRotation = true;
    public float bodyMoveDuration = 0.8f;
    public float bodyRotateDuration = 0.8f;

    [Header("Circle Rotation")]
    public Transform rotatingCircle;
    public bool rotateCircleAfterOpen = true;
    public Vector3 circleRotateAxis = Vector3.up;
    public float circleRotateSpeed = 120f;
    public Space circleRotateSpace = Space.Self;

    [Header("Turntable Layering")]
    public TurntableLayeringSequence turntableLayeringSequence;
    public bool startTurntableLayeringAfterOpen = true;

    [Header("Motion")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 innerDoorClosedLocalPosition;
    private Quaternion innerDoorClosedLocalRotation;

    private Vector3 outerDoorClosedLocalPosition;
    private Quaternion outerDoorClosedLocalRotation;

    private Vector3 topDoorClosedLocalPosition;
    private Quaternion topDoorClosedLocalRotation;

    private bool closedPoseCached;

    private bool isPlaying;
    private Coroutine circleRotateCoroutine;

    private void Awake()
    {
        CacheClosedPoses();
    }

    private void CacheClosedPoses()
    {
        if (closedPoseCached) return;

        if (innerDoor != null)
        {
            innerDoorClosedLocalPosition = innerDoor.localPosition;
            innerDoorClosedLocalRotation = innerDoor.localRotation;
        }

        if (outerDoor != null)
        {
            outerDoorClosedLocalPosition = outerDoor.localPosition;
            outerDoorClosedLocalRotation = outerDoor.localRotation;
        }

        if (topDoor != null)
        {
            topDoorClosedLocalPosition = topDoor.localPosition;
            topDoorClosedLocalRotation = topDoor.localRotation;
        }

        closedPoseCached = true;
    }

    public void PlayUnlockSequence(Transform keyTransform)
    {
        if (isPlaying)
            return;

        if (keyTransform == null)
        {
            Debug.LogWarning("[FinalDoorUnlockSequence] keyTransform이 없습니다.");
            return;
        }

        StartCoroutine(UnlockRoutine(keyTransform));
    }

    private IEnumerator UnlockRoutine(Transform keyTransform)
    {
        isPlaying = true;

        if (keyDragGuideObject != null)
        {
            keyDragGuideObject.SetActive(false);
        }

        DraggableKeyToKeyhole draggable = keyTransform.GetComponent<DraggableKeyToKeyhole>();
        if (draggable != null)
        {
            draggable.enabled = false;
        }

        Collider[] colliders = keyTransform.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // 1. 열쇠가 키홀 앞 위치로 이동
        if (keyApproachPose != null)
        {
            yield return MoveTransformWorld(
                keyTransform,
                keyApproachPose.position,
                keyApproachPose.rotation,
                keyMoveToApproachDuration
            );
        }

        // 2. 열쇠 삽입
        if (keyInsertPose != null)
        {
            yield return MoveTransformWorld(
                keyTransform,
                keyInsertPose.position,
                keyInsertPose.rotation,
                keyInsertDuration
            );
        }

        PlayKeyInsertSFX();

        // 2-1. 꽂힌 순간부터 키홀의 자식으로 붙임
        if (parentKeyToKeyholeAfterInsert && keyholeParent != null)
        {
            keyTransform.SetParent(keyholeParent, true);
        }

        // 2-2. 꽂힌 뒤 열쇠 스케일 축소
        if (scaleKeyAfterInsert)
        {
            yield return ScaleTransformLocal(
                keyTransform,
                insertedKeyScale,
                keyScaleDuration
            );
        }

        yield return new WaitForSeconds(pauseAfterInsert);

        // 3. 열쇠 회전
        if (keyTurnPose != null)
        {
            yield return RotateTransformWorld(
                keyTransform,
                keyTurnPose.rotation,
                keyTurnDuration
            );
        }

        yield return new WaitForSeconds(pauseAfterTurn);
        yield return new WaitForSeconds(pauseBeforeDoorOpen);

        // 4. 안쪽 문 먼저 열기
        if (innerDoor != null && innerDoorOpenPose != null)
        {
            yield return RotateTransformWorld(
                innerDoor,
                innerDoorOpenPose.rotation,
                innerDoorOpenDuration
            );
        }

        yield return new WaitForSeconds(pauseAfterInnerDoorOpen);

        // 5. 바깥문 + Top 동시에 열기
        bool hasOuterDoor = outerDoor != null && outerDoorOpenPose != null;
        bool hasTopDoor = topDoor != null && topDoorOpenPose != null;

        if (hasOuterDoor || hasTopDoor)
        {
            yield return OpenOuterAndTopTogether(hasOuterDoor, hasTopDoor);
        }

        yield return new WaitForSeconds(pauseAfterOuterAndTopOpen);

        // 6. 문이 다 열린 직후 동시에 실행
        // 6-1. Fake Interior reveal
        if (revealInteriorAfterOpen && fakeInteriorController != null)
        {
            fakeInteriorController.RevealRandomInterior();
        }

        yield return new WaitForSeconds(pauseBeforeTurntableStart);

        // 6-2. Body가 먼저 제자리에서 회전
        if (moveBodyBeforeCircleRotation && turntableBody != null && bodyOnCirclePose != null)
        {
            yield return RotateTransformWorld(
                turntableBody,
                bodyOnCirclePose.rotation,
                bodyRotateDuration
            );

            // 6-3. 회전 후 Circle 위로 내려감
            yield return MovePositionWorld(
                turntableBody,
                bodyOnCirclePose.position,
                bodyMoveDuration
            );
        }

        // 6-4. Body가 자리 잡은 뒤 Circle 회전 시작
        if (rotateCircleAfterOpen && rotatingCircle != null)
        {
            StartCircleRotation();
        }

        // 6-5. 잉크 → 편지지 순서로 턴테이블 레이어링 시작
        if (startTurntableLayeringAfterOpen && turntableLayeringSequence != null)
        {
            turntableLayeringSequence.BeginSequence();
        }

        // 6-3. 열쇠 배경음 루프 시작
        if (playKeyAudioAfterOpen)
        {
            StartCoroutine(PlayKeyLoopAudio(keyTransform));
        }

        Debug.Log("[FinalDoorUnlockSequence] 마지막 문 열림 시퀀스 완료.");
        isPlaying = false;
    }

    private IEnumerator OpenOuterAndTopTogether(bool hasOuterDoor, bool hasTopDoor)
    {
        Vector3 outerStartPos = Vector3.zero;
        Quaternion outerStartRot = Quaternion.identity;

        Vector3 topStartPos = Vector3.zero;
        Quaternion topStartRot = Quaternion.identity;

        if (hasOuterDoor)
        {
            outerStartPos = outerDoor.position;
            outerStartRot = outerDoor.rotation;
        }

        if (hasTopDoor)
        {
            topStartPos = topDoor.position;
            topStartRot = topDoor.rotation;
        }

        float duration = Mathf.Max(
            hasOuterDoor ? outerDoorOpenDuration : 0f,
            hasTopDoor ? topDoorOpenDuration : 0f
        );

        if (duration <= 0.001f)
        {
            if (hasOuterDoor)
            {
                outerDoor.position = outerDoorOpenPose.position;
                outerDoor.rotation = outerDoorOpenPose.rotation;
            }

            if (hasTopDoor)
            {
                topDoor.position = topDoorOpenPose.position;
                topDoor.rotation = topDoorOpenPose.rotation;
            }

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (hasOuterDoor)
            {
                float tOuter = Mathf.Clamp01(elapsed / outerDoorOpenDuration);
                float cOuter = moveCurve != null ? moveCurve.Evaluate(tOuter) : tOuter;

                outerDoor.position = Vector3.Lerp(
                    outerStartPos,
                    outerDoorOpenPose.position,
                    cOuter
                );

                outerDoor.rotation = Quaternion.Slerp(
                    outerStartRot,
                    outerDoorOpenPose.rotation,
                    cOuter
                );
            }

            if (hasTopDoor)
            {
                float tTop = Mathf.Clamp01(elapsed / topDoorOpenDuration);
                float cTop = moveCurve != null ? moveCurve.Evaluate(tTop) : tTop;

                topDoor.position = Vector3.Lerp(
                    topStartPos,
                    topDoorOpenPose.position,
                    cTop
                );

                topDoor.rotation = Quaternion.Slerp(
                    topStartRot,
                    topDoorOpenPose.rotation,
                    cTop
                );
            }

            yield return null;
        }

        if (hasOuterDoor)
        {
            outerDoor.position = outerDoorOpenPose.position;
            outerDoor.rotation = outerDoorOpenPose.rotation;
        }

        if (hasTopDoor)
        {
            topDoor.position = topDoorOpenPose.position;
            topDoor.rotation = topDoorOpenPose.rotation;
        }
    }

    private IEnumerator MoveTransformWorld(
        Transform target,
        Vector3 targetPosition,
        Quaternion targetRotation,
        float duration
    )
    {
        if (target == null)
            yield break;

        Vector3 startPosition = target.position;
        Quaternion startRotation = target.rotation;

        if (duration <= 0.001f)
        {
            target.position = targetPosition;
            target.rotation = targetRotation;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = moveCurve != null ? moveCurve.Evaluate(t) : t;

            target.position = Vector3.Lerp(startPosition, targetPosition, curvedT);
            target.rotation = Quaternion.Slerp(startRotation, targetRotation, curvedT);

            yield return null;
        }

        target.position = targetPosition;
        target.rotation = targetRotation;
    }

    private IEnumerator ScaleTransformLocal(
    Transform target,
    Vector3 targetScale,
    float duration
    )
    {
        if (target == null)
            yield break;

        Vector3 startScale = target.localScale;

        if (duration <= 0.001f)
        {
            target.localScale = targetScale;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = moveCurve != null ? moveCurve.Evaluate(t) : t;

            target.localScale = Vector3.Lerp(
                startScale,
                targetScale,
                curvedT
            );

            yield return null;
        }

        target.localScale = targetScale;
    }

    private IEnumerator RotateTransformWorld(
    Transform target,
    Quaternion targetRotation,
    float duration
    )
    {
        if (target == null)
            yield break;

        Quaternion startRotation = target.rotation;

        if (duration <= 0.001f)
        {
            target.rotation = targetRotation;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = moveCurve != null ? moveCurve.Evaluate(t) : t;

            target.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                curvedT
            );

            yield return null;
        }

        target.rotation = targetRotation;
    }

    private IEnumerator MovePositionWorld(
        Transform target,
        Vector3 targetPosition,
        float duration
    )
    {
        if (target == null)
            yield break;

        Vector3 startPosition = target.position;

        if (duration <= 0.001f)
        {
            target.position = targetPosition;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = moveCurve != null ? moveCurve.Evaluate(t) : t;

            target.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                curvedT
            );

            yield return null;
        }

        target.position = targetPosition;
    }


    private void StartCircleRotation()
    {
        if (circleRotateCoroutine != null)
            return;

        circleRotateCoroutine = StartCoroutine(RotateCircleLoop());
    }

    private IEnumerator RotateCircleLoop()
    {
        while (rotatingCircle != null)
        {
            rotatingCircle.Rotate(
                circleRotateAxis.normalized * circleRotateSpeed * Time.deltaTime,
                circleRotateSpace
            );

            yield return null;
        }

        circleRotateCoroutine = null;
    }

    private IEnumerator PlayKeyLoopAudio(Transform keyTransform)
    {
        if (keyTransform == null)
            yield break;

        AudioSource keyAudio = keyTransform.GetComponentInChildren<AudioSource>(true);

        if (keyAudio == null)
        {
            Debug.LogWarning("[FinalDoorUnlockSequence] 열쇠에서 AudioSource를 찾지 못했습니다.");
            yield break;
        }

        if (keyAudio.clip == null)
        {
            Debug.LogWarning("[FinalDoorUnlockSequence] 열쇠 AudioSource에 AudioClip이 없습니다.");
            yield break;
        }

        keyAudio.loop = true;
        keyAudio.playOnAwake = false;
        keyAudio.volume = 0f;
        keyAudio.spatialBlend = 0f; // 배경음처럼 2D

        if (!keyAudio.isPlaying)
        {
            keyAudio.Play();
        }

        float elapsed = 0f;

        while (elapsed < keyAudioFadeInDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / keyAudioFadeInDuration);
            keyAudio.volume = Mathf.Lerp(0f, keyAudioVolume, t);

            yield return null;
        }

        keyAudio.volume = keyAudioVolume;
    }

    public IEnumerator CloseFinalRoomRoutine()
    {
        CacheClosedPoses();

        StopCircleRotation();

        // 앞벽 + 윗벽 + 문이 닫히기 시작하는 순간 효과음 재생
        PlayFinalRoomCloseSFX();

        bool hasOuterDoor = outerDoor != null;
        bool hasTopDoor = topDoor != null;

        // 1. 뚜껑 + 벽 먼저 닫힘
        if (hasOuterDoor || hasTopDoor)
        {
            yield return CloseOuterAndTopTogether(hasOuterDoor, hasTopDoor);
        }

        // 2. 마지막으로 안쪽 문 닫힘
        if (innerDoor != null)
        {
            yield return RotateTransformLocal(
                innerDoor,
                innerDoorClosedLocalRotation,
                innerDoorOpenDuration
            );
        }

        Debug.Log("[FinalDoorUnlockSequence] 마지막 방 닫힘 완료.");
    }

    private IEnumerator RotateTransformLocal(
    Transform target,
    Quaternion targetLocalRotation,
    float duration
    )
    {
        if (target == null)
            yield break;

        Quaternion startRotation = target.localRotation;

        if (duration <= 0.001f)
        {
            target.localRotation = targetLocalRotation;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = moveCurve != null ? moveCurve.Evaluate(t) : t;

            target.localRotation = Quaternion.Slerp(
                startRotation,
                targetLocalRotation,
                curvedT
            );

            yield return null;
        }

        target.localRotation = targetLocalRotation;
    }

    private IEnumerator CloseOuterAndTopTogether(bool hasOuterDoor, bool hasTopDoor)
    {
        Vector3 outerStartLocalPos = Vector3.zero;
        Quaternion outerStartLocalRot = Quaternion.identity;

        Vector3 topStartLocalPos = Vector3.zero;
        Quaternion topStartLocalRot = Quaternion.identity;

        if (hasOuterDoor)
        {
            outerStartLocalPos = outerDoor.localPosition;
            outerStartLocalRot = outerDoor.localRotation;
        }

        if (hasTopDoor)
        {
            topStartLocalPos = topDoor.localPosition;
            topStartLocalRot = topDoor.localRotation;
        }

        float duration = Mathf.Max(
            hasOuterDoor ? outerDoorOpenDuration : 0f,
            hasTopDoor ? topDoorOpenDuration : 0f
        );

        if (duration <= 0.001f)
        {
            if (hasOuterDoor)
            {
                outerDoor.localPosition = outerDoorClosedLocalPosition;
                outerDoor.localRotation = outerDoorClosedLocalRotation;
            }

            if (hasTopDoor)
            {
                topDoor.localPosition = topDoorClosedLocalPosition;
                topDoor.localRotation = topDoorClosedLocalRotation;
            }

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (hasOuterDoor)
            {
                float tOuter = Mathf.Clamp01(elapsed / outerDoorOpenDuration);
                float cOuter = moveCurve != null ? moveCurve.Evaluate(tOuter) : tOuter;

                outerDoor.localPosition = Vector3.Lerp(
                    outerStartLocalPos,
                    outerDoorClosedLocalPosition,
                    cOuter
                );

                outerDoor.localRotation = Quaternion.Slerp(
                    outerStartLocalRot,
                    outerDoorClosedLocalRotation,
                    cOuter
                );
            }

            if (hasTopDoor)
            {
                float tTop = Mathf.Clamp01(elapsed / topDoorOpenDuration);
                float cTop = moveCurve != null ? moveCurve.Evaluate(tTop) : tTop;

                topDoor.localPosition = Vector3.Lerp(
                    topStartLocalPos,
                    topDoorClosedLocalPosition,
                    cTop
                );

                topDoor.localRotation = Quaternion.Slerp(
                    topStartLocalRot,
                    topDoorClosedLocalRotation,
                    cTop
                );
            }

            yield return null;
        }

        if (hasOuterDoor)
        {
            outerDoor.localPosition = outerDoorClosedLocalPosition;
            outerDoor.localRotation = outerDoorClosedLocalRotation;
        }

        if (hasTopDoor)
        {
            topDoor.localPosition = topDoorClosedLocalPosition;
            topDoor.localRotation = topDoorClosedLocalRotation;
        }
    }

    public void StopCircleRotation()
    {
        if (circleRotateCoroutine != null)
        {
            StopCoroutine(circleRotateCoroutine);
            circleRotateCoroutine = null;
        }
    }

    private void PlayKeyInsertSFX()
    {
        if (sfxSource != null && keyInsertClip != null)
        {
            sfxSource.PlayOneShot(keyInsertClip);
        }
    }

    private void PlayFinalRoomCloseSFX()
    {
        if (finalRoomCloseClip == null)
        {
            return;
        }

        AudioSource source = closeSfxSource != null ? closeSfxSource : sfxSource;

        if (source == null)
        {
            return;
        }

        source.volume = closeSfxVolume;
        source.PlayOneShot(finalRoomCloseClip);
    }
}