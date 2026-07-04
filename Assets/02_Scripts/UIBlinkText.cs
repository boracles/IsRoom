using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class UIBlinkText : MonoBehaviour
{
    [Header("Alpha")]
    public float minAlpha = 0.35f;
    public float maxAlpha = 1f;

    [Header("Timing")]
    public float speed = 2.2f;

    private TMP_Text text;
    private Color baseColor;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        baseColor = text.color;
    }

    private void OnEnable()
    {
        if (text == null)
        {
            text = GetComponent<TMP_Text>();
        }

        baseColor = text.color;

        Color c = baseColor;
        c.a = maxAlpha;
        text.color = c;
    }

    private void Update()
    {
        if (text == null) return;

        float t = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        Color c = baseColor;
        c.a = alpha;
        text.color = c;
    }

    private void OnDisable()
    {
        if (text == null) return;

        Color c = baseColor;
        c.a = maxAlpha;
        text.color = c;
    }
}