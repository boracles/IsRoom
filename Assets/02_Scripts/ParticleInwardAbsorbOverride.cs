using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
[DefaultExecutionOrder(10000)]
public class ParticleInwardAbsorbOverride : MonoBehaviour
{
    [Header("Target")]
    public Transform attractTarget;

    [Header("Inward Motion")]
    [Range(0f, 1f)]
    public float inwardAmount = 1f;

    public float curvePower = 1.4f;

    [Header("Optional")]
    public bool killNearTarget = false;
    public float killDistance = 0.01f;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void LateUpdate()
    {
        if (ps == null) return;

        Transform target = attractTarget != null ? attractTarget : transform;

        int count = ps.particleCount;
        if (count <= 0) return;

        if (particles == null || particles.Length < count)
        {
            particles = new ParticleSystem.Particle[count];
        }

        count = ps.GetParticles(particles);

        ParticleSystem.MainModule main = ps.main;
        bool worldSpace = main.simulationSpace == ParticleSystemSimulationSpace.World;

        Vector3 targetPosition = worldSpace
            ? target.position
            : ps.transform.InverseTransformPoint(target.position);

        for (int i = 0; i < count; i++)
        {
            float startLifetime = Mathf.Max(particles[i].startLifetime, 0.0001f);
            float age01 = 1f - Mathf.Clamp01(particles[i].remainingLifetime / startLifetime);

            float t = Mathf.Pow(age01, curvePower) * inwardAmount;
            t = Mathf.Clamp01(t);

            particles[i].position = Vector3.Lerp(
                particles[i].position,
                targetPosition,
                t
            );

            if (killNearTarget)
            {
                float distance = Vector3.Distance(particles[i].position, targetPosition);
                if (distance < killDistance)
                {
                    particles[i].remainingLifetime = Mathf.Min(particles[i].remainingLifetime, 0.05f);
                }
            }
        }

        ps.SetParticles(particles, count);
    }
}