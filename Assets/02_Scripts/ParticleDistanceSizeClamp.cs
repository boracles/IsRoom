using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleDistanceSizeClamp : MonoBehaviour
{
    public Camera targetCamera;

    [Header("Distance Range")]
    public float nearDistance = 2f;
    public float farDistance = 15f;

    [Header("Particle Screen Size")]
    public float nearMaxParticleSize = 0.04f;
    public float farMaxParticleSize = 0.01f;

    private ParticleSystemRenderer psRenderer;

    void Awake()
    {
        psRenderer = GetComponent<ParticleSystemRenderer>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null || psRenderer == null) return;

        float distance = Vector3.Distance(targetCamera.transform.position, transform.position);

        float t = Mathf.InverseLerp(nearDistance, farDistance, distance);

        float size = Mathf.Lerp(nearMaxParticleSize, farMaxParticleSize, t);

        psRenderer.maxParticleSize = size;
        psRenderer.minParticleSize = 0f;
    }
}