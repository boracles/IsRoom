using UnityEngine;

public class FollowWorldTargetUI : MonoBehaviour
{
    public Transform worldTarget;
    public RectTransform uiElement;
    public Camera arCamera;

    public Vector2 screenOffset = new Vector2(0f, 80f);
    public bool hideWhenBehindCamera = true;

    private void LateUpdate()
    {
        if (worldTarget == null || uiElement == null)
            return;

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (arCamera == null)
            return;

        Vector3 screenPos = arCamera.WorldToScreenPoint(worldTarget.position);

        if (hideWhenBehindCamera && screenPos.z < 0f)
        {
            uiElement.gameObject.SetActive(false);
            return;
        }

        if (!uiElement.gameObject.activeSelf)
        {
            uiElement.gameObject.SetActive(true);
        }

        uiElement.position = new Vector2(
            screenPos.x + screenOffset.x,
            screenPos.y + screenOffset.y
        );
    }
}