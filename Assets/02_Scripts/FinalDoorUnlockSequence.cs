using System.Collections;
using UnityEngine;

public class FinalDoorUnlockSequence : MonoBehaviour
{
    [Header("Key Poses")]
    public Transform keyApproachPose;
    public Transform keyInsertPose;
    public Transform keyTurnPose;

    [Header("Door Targets")]
    public Transform innerDoor; // Door_LightRoom
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

    public float innerDoorOpenDuration = 0.8f;
    public float outerDoorOpenDuration = 1.0f;
    public float topDoorOpenDuration = 1.0f;

    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isPlaying;

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

        // 2. 열쇠가 키홀에 삽입됨
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

        // 4. 안쪽 문 먼저 열림
        if (innerDoor != null && innerDoorOpenPose != null)
        {
            yield return MoveTransformWorld(
                innerDoor,
                innerDoorOpenPose.position,
                innerDoorOpenPose.rotation,
                innerDoorOpenDuration
            );
        }

        // 5. Wall_Light와 Top이 동시에 열림
        bool hasOuterDoor = outerDoor != null && outerDoorOpenPose != null;
        bool hasTopDoor = topDoor != null && topDoorOpenPose != null;

        if (hasOuterDoor || hasTopDoor)
        {
            yield return OpenOuterAndTopTogether(hasOuterDoor, hasTopDoor);
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
}