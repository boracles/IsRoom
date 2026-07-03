using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UISoftPulse : MonoBehaviour
{
    [Header("Scale")]
    public float minScale = 0.92f;
    public float maxScale = 1.04f;
    public float duration = 1.4f;

    [Header("Alpha (Optional)")]
    public bool animateAlpha = true;
    public float minAlpha = 0.55f;
    public float maxAlpha = 0.9f;

    [Header("Feel")]
    public AnimationCurve curve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(0.5f, 1f, 0f, 0f),
        new Keyframe(1f, 0f, -2f, 0f)
    );

    private RectTransform rectTransform;
    private Vector3 baseScale;
    private Graphic graphic;
    private float timeOffset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        baseScale = rectTransform.localScale;
        graphic = GetComponent<Graphic>();
        timeOffset = Random.Range(0f, 1f);
    }

    private void OnEnable()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale;
        }

        if (graphic != null && animateAlpha)
        {
            Color c = graphic.color;
            c.a = maxAlpha;
            graphic.color = c;
        }
    }

    private void Update()
    {
        if (duration <= 0.01f) return;

        float t = Mathf.Repeat((Time.unscaledTime / duration) + timeOffset, 1f);
        float eased = curve.Evaluate(t);

        float scale = Mathf.Lerp(minScale, maxScale, eased);
        rectTransform.localScale = baseScale * scale;

        if (graphic != null && animateAlpha)
        {
            Color c = graphic.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, eased);
            graphic.color = c;
        }
    }
}