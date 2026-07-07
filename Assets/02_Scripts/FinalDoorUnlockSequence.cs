using System.Collections;
using UnityEngine;

public class FinalDoorUnlockSequence : MonoBehaviour
{
    [Header("Key Poses")]
    public Transform keyApproachPose;
    public Transform keyInsertPose;
    public Transform keyTurnPose;

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

    [Header("Key Loop Audio")]
    public bool playKeyAudioAfterOpen = true;
    public float keyAudioFadeInDuration = 1.0f;
    public float keyAudioVolume = 0.8f;

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

    [Header("Motion")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isPlaying;
    private Coroutine circleRotateCoroutine;

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

        // 3. 열쇠 회전
        if (keyTurnPose != null)
        {
            yield return MoveTransformWorld(
                keyTransform,
                keyTurnPose.position,
                keyTurnPose.rotation,
                keyTurnDuration
            );
        }

        yield return new WaitForSeconds(pauseAfterTurn);

        // 4. 안쪽 문 먼저 열기
        if (innerDoor != null && innerDoorOpenPose != null)
        {
            yield return MoveTransformWorld(
                innerDoor,
                innerDoorOpenPose.position,
                innerDoorOpenPose.rotation,
                innerDoorOpenDuration
            );
        }

        // 5. 바깥문 + Top 동시에 열기
        bool hasOuterDoor = outerDoor != null && outerDoorOpenPose != null;
        bool hasTopDoor = topDoor != null && topDoorOpenPose != null;

        if (hasOuterDoor || hasTopDoor)
        {
            yield return OpenOuterAndTopTogether(hasOuterDoor, hasTopDoor);
        }

        // 6. 문이 다 열린 직후 동시에 실행
        // 6-1. Fake Interior reveal
        if (revealInteriorAfterOpen && fakeInteriorController != null)
        {
            fakeInteriorController.RevealRandomInterior();
        }

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
}