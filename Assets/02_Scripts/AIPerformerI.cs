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

    [Header("Motion")]
    public float moveSpeed = 5f;
    public float floatingSpeed = 1.5f;
    public float floatingAmount = 0.03f;
    public float voiceScaleAmount = 0.35f;

    private Vector3 targetPosition;
    private Vector3 baseScale;

    private bool isListening;
    private bool isSpeaking;

    private void Awake()
    {
        targetPosition = transform.position;
        baseScale = transform.localScale;
    }

    private void Update()
    {
        MoveToTarget();
        UpdateReactiveScale();
    }

    private void MoveToTarget()
    {
        Vector3 floatOffset = Vector3.up * Mathf.Sin(Time.time * floatingSpeed) * floatingAmount;
        Vector3 finalTarget = targetPosition + floatOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            finalTarget,
            Time.deltaTime * moveSpeed
        );
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
        if (roomType == RoomType.Stair && stairDoorPosition != null)
            targetPosition = stairDoorPosition.position;

        if (roomType == RoomType.Wave && waveDoorPosition != null)
            targetPosition = waveDoorPosition.position;

        if (roomType == RoomType.Shadow && shadowDoorPosition != null)
            targetPosition = shadowDoorPosition.position;
    }

    public void MoveToTouchPosition()
    {
        if (touchPosition != null)
        {
            targetPosition = touchPosition.position;
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
        transform.localScale = baseScale * 1.25f;
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
        isListening = true;

        if (absorbParticle != null)
        {
            absorbParticle.Play();
        }
    }

    public void StopListening()
    {
        isListening = false;

        if (absorbParticle != null)
        {
            absorbParticle.Stop();
        }
    }

    public void ReleaseContraction()
    {
        transform.localScale = baseScale * 0.7f;
    }

    public void EmitToPiece(Vector3 piecePosition)
    {
        if (emitParticle == null) return;

        emitParticle.transform.LookAt(piecePosition);
        emitParticle.Play();
    }
}