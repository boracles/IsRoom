using UnityEngine;
using UnityEngine.UI;

public class KeyDragGuideUI : MonoBehaviour
{
    [Header("World Targets")]
    public Transform keyTarget;
    public Transform keyholeTarget;

    [Header("Camera")]
    public Camera arCamera;

    [Header("UI")]
    public RectTransform touchIcon;
    public RectTransform dragLine;
    public Canvas canvas;

    [Header("Animation")]
    public float loopDuration = 1.4f;
    public float pauseDuration = 0.35f;
    public Vector2 iconOffset = new Vector2(0f, 35f);

    [Header("Visibility")]
    public bool hideWhenBehindCamera = true;

    [Header("Icon Rotation")]
    public float touchIconRotationOffset = -135f;

    private float timer;

    private void OnEnable()
    {
        timer = 0f;

        if (keyTarget == null)
        {
            SetVisible(false);
        }
    }

    private void LateUpdate()
    {
        if (keyTarget == null || keyholeTarget == null || touchIcon == null)
        {
            SetVisible(false);
            return;
        }

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (arCamera == null)
        {
            SetVisible(false);
            return;
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (canvas == null)
        {
            SetVisible(false);
            return;
        }

        Vector3 keyScreen = arCamera.WorldToScreenPoint(keyTarget.position);
        Vector3 holeScreen = arCamera.WorldToScreenPoint(keyholeTarget.position);

        if (hideWhenBehindCamera && (keyScreen.z < 0f || holeScreen.z < 0f))
        {
            SetVisible(false);
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;

        if (canvasRect == null)
        {
            SetVisible(false);
            return;
        }

        Vector2 keyCanvasPos;
        Vector2 holeCanvasPos;

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : arCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            keyScreen,
            uiCamera,
            out keyCanvasPos
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            holeScreen,
            uiCamera,
            out holeCanvasPos
        );

        SetVisible(true);

        UpdateLine(keyCanvasPos, holeCanvasPos);
        UpdateTouchIcon(keyCanvasPos, holeCanvasPos);
    }

    public void SetKeyTarget(Transform key)
    {
        keyTarget = key;
        timer = 0f;

        if (keyTarget == null)
        {
            SetVisible(false);
        }
    }

    public void ClearKeyTarget()
    {
        keyTarget = null;
        timer = 0f;
        SetVisible(false);
    }

    public void SetKeyholeTarget(Transform target)
    {
        keyholeTarget = target;
    }

    private void UpdateTouchIcon(Vector2 start, Vector2 end)
    {
        float totalDuration = loopDuration + pauseDuration;

        timer += Time.deltaTime;

        if (timer > totalDuration)
        {
            timer = 0f;
        }

        float t = Mathf.Clamp01(timer / loopDuration);

        // 부드럽게 출발하고 도착
        float smoothT = t * t * (3f - 2f * t);

        Vector2 pos = Vector2.Lerp(start, end, smoothT) + iconOffset;
        touchIcon.anchoredPosition = pos;

        // 손가락이 이동 방향을 약간 향하게 회전
        Vector2 dir = end - start;

        if (dir.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            touchIcon.localRotation = Quaternion.Euler(0f, 0f, angle + touchIconRotationOffset);
        }
    }

    private void UpdateLine(Vector2 start, Vector2 end)
    {
        if (dragLine == null)
        {
            return;
        }

        Vector2 center = (start + end) * 0.5f;
        Vector2 dir = end - start;
        float length = dir.magnitude;

        dragLine.anchoredPosition = center;
        dragLine.sizeDelta = new Vector2(length, dragLine.sizeDelta.y);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        dragLine.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void SetVisible(bool visible)
    {
        if (touchIcon != null && touchIcon.gameObject.activeSelf != visible)
        {
            touchIcon.gameObject.SetActive(visible);
        }

        if (dragLine != null && dragLine.gameObject.activeSelf != visible)
        {
            dragLine.gameObject.SetActive(visible);
        }
    }
}