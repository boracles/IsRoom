using UnityEngine;

public class AIPerformerI : MonoBehaviour
{
    [Header("Input")]
    public VoiceHoldInput voiceInput;

    [Header("Renderer")]
    public Renderer iRenderer;

    [Header("Particles")]
    public ParticleSystem absorbParticle;
    public ParticleSystem emitParticle;

    [Header("Positions")]
    public Transform stairDoorPosition;
    public Transform waveDoorPosition;
    public Transform shadowDoorPosition;
    public Transform touchPosition;
    public Transform afterAnswerPosition;

    [Header("Motion")]
    public float moveSpeed = 1.8f;
    public float smoothTime = 0.35f;
    public float floatingSpeed = 0.7f;
    public float floatingAmount = 0.008f;
    public float voiceScaleAmount = 0.35f;

    [Header("Arrival")]
    public float snapDistance = 0.001f;

    private Vector3 targetPosition;
    private Transform targetTransform;
    private Vector3 moveVelocity;
    private Vector3 baseScale;

    private bool isListening;
    private bool isSpeaking;

    private void OnEnable()
    {
        StopListening();
    }

    private void OnDisable()
    {
        StopListening();
    }

    private void Awake()
    {
        targetPosition = transform.position;
        targetTransform = null;
        baseScale = transform.localScale;

        StopListening();
    }

    private void Update()
    {
        MoveToTarget();
        UpdateReactiveScale();
    } 

    private void MoveToTarget()
    {
        Vector3 target = targetTransform != null
            ? targetTransform.position
            : targetPosition;

        // 월드 Y 기준으로 아주 약하게만 둥둥 뜨게 함
        Vector3 floatOffset = Vector3.up * Mathf.Sin(Time.time * floatingSpeed) * floatingAmount;
        Vector3 finalTarget = target + floatOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            finalTarget,
            ref moveVelocity,
            smoothTime,
            moveSpeed
        );

        if (Vector3.Distance(transform.position, finalTarget) < snapDistance)
        {
            transform.position = finalTarget;
        }
    }

    private void UpdateReactiveScale()
    {
        float reaction = 0f;

        if (isListening && voiceInput != null)
        {
            reaction = voiceInput.CurrentVolume;
        }
        else if (isSpeaking)
        {
            reaction = Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.25f;
        }

        Vector3 targetScale = baseScale * (1f + reaction * voiceScaleAmount);

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * 8f
        );

        if (iRenderer != null && iRenderer.material.HasProperty("_NoiseStrength"))
        {
            iRenderer.material.SetFloat("_NoiseStrength", reaction);
        }
    }

    public void MoveToRoomDoor(RoomType roomType)
    {
        targetTransform = null;

        if (roomType == RoomType.Stair && stairDoorPosition != null)
        {
            targetTransform = stairDoorPosition;
        }
        else if (roomType == RoomType.Wave && waveDoorPosition != null)
        {
            targetTransform = waveDoorPosition;
        }
        else if (roomType == RoomType.Shadow && shadowDoorPosition != null)
        {
            targetTransform = shadowDoorPosition;
        }

        if (targetTransform != null)
        {
            targetPosition = targetTransform.position;
            moveVelocity = Vector3.zero;
        }
        else
        {
            Debug.LogWarning($"I_Performer: {roomType} door position is not assigned.");
        }
    }

    public void MoveToTouchPosition()
    {
        if (touchPosition != null)
        {
            targetTransform = touchPosition;
            targetPosition = touchPosition.position;
            moveVelocity = Vector3.zero;
        }
        else
        {
            Debug.LogWarning("I_Performer: touchPosition is not assigned.");
        }
    }

    public void MoveToAfterAnswerPosition()
    {
        if (afterAnswerPosition != null)
        {
            targetTransform = afterAnswerPosition;
            targetPosition = afterAnswerPosition.position;
            moveVelocity = Vector3.zero;
        }
        else
        {
            Debug.LogWarning("I_Performer: afterAnswerPosition is not assigned.");
        }
    }

    public void SetIColor(Color color)
    {
        if (iRenderer != null && iRenderer.material.HasProperty("_BaseColor"))
        {
            iRenderer.material.SetColor("_BaseColor", color);
        }
    }

    public void FirstTonePulse()
    {
        transform.localScale = baseScale * 1.15f;
    }

    public void StartSpeaking()
    {
        isSpeaking = true;
    }

    public void StopSpeaking()
    {
        isSpeaking = false;
    }

    public void StartListening()
    {
        if (isListening) return;

        isListening = true;

        if (absorbParticle != null)
        {
            absorbParticle.gameObject.SetActive(true);

            absorbParticle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            absorbParticle.Play(true);
        }
    }

    public void StopListening()
    {
        isListening = false;

        if (absorbParticle != null)
        {
            absorbParticle.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            absorbParticle.gameObject.SetActive(false);
        }
    }

    public void ReleaseContraction()
    {
        transform.localScale = baseScale * 0.85f;
    }

    public void EmitToPiece(Vector3 piecePosition)
    {
        if (emitParticle == null) return;

        emitParticle.transform.LookAt(piecePosition);
        emitParticle.Play();
    }

    public bool HasArrivedAtCurrentTarget(float tolerance = 0.03f)
    {
        Vector3 target = targetTransform != null
            ? targetTransform.position
            : targetPosition;

        return Vector3.Distance(transform.position, target) <= tolerance;
    }

}