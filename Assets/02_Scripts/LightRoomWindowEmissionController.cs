using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InkColorType
{
    Blue,
    Green,
    Purple
}

public class LightRoomWindowEmissionController : MonoBehaviour
{
    [System.Serializable]
    public class EmissionTarget
    {
        public Renderer renderer;
        public int materialIndex = 0;
    }

    [Header("Targets")]
    public EmissionTarget[] targets;

    [Header("Animation")]
    public float fadeDuration = 1.2f;
    public float targetIntensity = 5.5f;

    [Header("Ink Colors")]
    public Color darkBlue = new Color(0.08f, 0.16f, 0.45f);
    public Color darkGreen = new Color(0.08f, 0.30f, 0.16f);
    public Color darkPurple = new Color(0.28f, 0.08f, 0.32f);

    private readonly List<Material> runtimeMaterials = new List<Material>();
    private Coroutine emissionCoroutine;

    private void Awake()
    {
        CacheMaterials();
        SetEmissionImmediate(Color.black);
    }

    private void OnEnable()
    {
        CacheMaterials();
        SetEmissionImmediate(Color.black);
    }

    private void CacheMaterials()
    {
        runtimeMaterials.Clear();

        if (targets == null) return;

        foreach (var t in targets)
        {
            if (t == null || t.renderer == null) continue;

            Material[] mats = t.renderer.materials;

            if (t.materialIndex < 0 || t.materialIndex >= mats.Length)
            {
                Debug.LogWarning($"[LightRoomWindowEmissionController] Material index out of range: {t.renderer.name}");
                continue;
            }

            Material mat = mats[t.materialIndex];
            if (mat == null) continue;

            mat.EnableKeyword("_EMISSION");

            if (!runtimeMaterials.Contains(mat))
            {
                runtimeMaterials.Add(mat);
            }
        }
    }

    public void TurnOffEmission()
    {
        if (emissionCoroutine != null)
        {
            StopCoroutine(emissionCoroutine);
            emissionCoroutine = null;
        }

        SetEmissionImmediate(Color.black);
    }

    public void PlayEmissionByInk(InkColorType inkType)
    {
        Color targetBaseColor = GetColorByInk(inkType);
        Color targetEmission = targetBaseColor * targetIntensity;

        if (emissionCoroutine != null)
        {
            StopCoroutine(emissionCoroutine);
        }

        emissionCoroutine = StartCoroutine(AnimateEmission(targetEmission));
    }

    private Color GetColorByInk(InkColorType inkType)
    {
        switch (inkType)
        {
            case InkColorType.Blue:
                return darkBlue;

            case InkColorType.Green:
                return darkGreen;

            case InkColorType.Purple:
                return darkPurple;
        }

        return darkBlue;
    }

    private IEnumerator AnimateEmission(Color targetEmission)
    {
        Color start = GetCurrentEmissionColor();
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            Color c = Color.Lerp(start, targetEmission, t);
            SetEmissionImmediate(c);

            yield return null;
        }

        SetEmissionImmediate(targetEmission);
        emissionCoroutine = null;
    }

    private Color GetCurrentEmissionColor()
    {
        if (runtimeMaterials.Count == 0) return Color.black;

        if (runtimeMaterials[0].HasProperty("_EmissionColor"))
        {
            return runtimeMaterials[0].GetColor("_EmissionColor");
        }

        return Color.black;
    }

    private void SetEmissionImmediate(Color emissionColor)
    {
        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            Material mat = runtimeMaterials[i];
            if (mat == null) continue;

            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor);
        }
    }
}