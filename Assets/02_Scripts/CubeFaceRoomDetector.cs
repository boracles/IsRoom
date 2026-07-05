using UnityEngine;

public class CubeFaceRoomDetector : MonoBehaviour
{
    [Header("Sequence")]
    public IRoomSequenceManager sequenceManager;

    [Header("Camera")]
    public Transform arCamera;

    [Header("Room Face Transforms")]
    public Transform stairFace;
    public Transform waveFace;
    public Transform shadowFace;
    public Transform lightFace;

    [Header("Detection")]
    public float checkInterval = 0.3f;
    public float faceDotThreshold = 0.55f;
    public bool invertFaceNormal = false;

    private float timer;
    private RoomType lastDetectedRoomType;
    private bool hasDetectedOnce = false;

    private void Update()
    {
        if (sequenceManager == null || arCamera == null)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer < checkInterval)
        {
            return;
        }

        timer = 0f;

        DetectVisibleRoomFace();
    }

    private void DetectVisibleRoomFace()
    {
        float bestDot = -1f;
        RoomType bestRoomType = default;
        bool found = false;

        CheckFace(stairFace, RoomType.Stair, ref bestDot, ref bestRoomType, ref found);
        CheckFace(waveFace, RoomType.Wave, ref bestDot, ref bestRoomType, ref found);
        CheckFace(shadowFace, RoomType.Shadow, ref bestDot, ref bestRoomType, ref found);
        CheckFace(lightFace, RoomType.Light, ref bestDot, ref bestRoomType, ref found);

        if (!found)
        {
            return;
        }

        if (bestDot < faceDotThreshold)
        {
            return;
        }

        if (hasDetectedOnce && lastDetectedRoomType == bestRoomType)
        {
            return;
        }

        hasDetectedOnce = true;
        lastDetectedRoomType = bestRoomType;

        Debug.Log($"현재 보이는 방 면: {bestRoomType}, dot: {bestDot}");

        sequenceManager.OnRoomTargetDetected(bestRoomType);
    }

    private void CheckFace(
        Transform face,
        RoomType roomType,
        ref float bestDot,
        ref RoomType bestRoomType,
        ref bool found
    )
    {
        if (face == null) return;

        Vector3 faceNormal = invertFaceNormal ? -face.forward : face.forward;
        Vector3 directionToCamera = (arCamera.position - face.position).normalized;

        float dot = Vector3.Dot(faceNormal, directionToCamera);

        if (dot > bestDot)
        {
            bestDot = dot;
            bestRoomType = roomType;
            found = true;
        }
    }

    public void ResetDetection()
    {
        hasDetectedOnce = false;
        lastDetectedRoomType = default;
    }
}