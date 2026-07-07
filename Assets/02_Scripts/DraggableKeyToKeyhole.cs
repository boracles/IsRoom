using UnityEngine;

public class DraggableKeyToKeyhole : MonoBehaviour
{
    [Header("Camera")]
    public Camera arCamera;

    [Header("Target")]
    public Transform keyholeTarget;
    public Transform keySnapPose;

    [Header("Drag")]
    public float followSpeed = 18f;
    public float dragPlaneOffset = 0f;

    [Header("Snap")]
    public float screenSnapDistance = 110f;
    public float worldSnapDistance = 0.08f;
    public bool disableAfterSnap = true;

    [Header("Guide")]
    public GameObject keyholeGuideObject;

    [Header("Unlock Sequence")]
    public FinalDoorUnlockSequence unlockSequence;

    private bool isDragging;
    private bool isSnapped;

    private Plane dragPlane;
    private Vector3 targetPosition;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void Awake()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        targetPosition = transform.position;
    }

    private void Update()
    {
        if (isSnapped)
            return;

        if (arCamera == null)
        {
            arCamera = Camera.main;
            if (arCamera == null) return;
        }

#if UNITY_EDITOR
        HandleMouseInput();
#else
        HandleTouchInput();
#endif

        if (isDragging)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                Time.deltaTime * followSpeed
            );

            CheckSnap();
        }
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            UpdateDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            TryBeginDrag(touch.position);
        }
        else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
        {
            UpdateDrag(touch.position);
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            EndDrag();
        }
    }

    private void TryBeginDrag(Vector2 screenPosition)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                isDragging = true;

                Vector3 planeNormal = -arCamera.transform.forward;
                Vector3 planePoint = transform.position + arCamera.transform.forward * dragPlaneOffset;

                dragPlane = new Plane(planeNormal, planePoint);
                targetPosition = transform.position;

                if (keyholeGuideObject != null)
                {
                    keyholeGuideObject.SetActive(true);
                }
            }
        }
    }

    private void UpdateDrag(Vector2 screenPosition)
    {
        if (!isDragging)
            return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (dragPlane.Raycast(ray, out float enter))
        {
            targetPosition = ray.GetPoint(enter);
        }
    }

    private void EndDrag()
    {
        if (!isDragging)
            return;

        isDragging = false;
        CheckSnap();
    }

    private void CheckSnap()
    {
        if (keyholeTarget == null || arCamera == null)
            return;

        Vector3 keyScreen = arCamera.WorldToScreenPoint(transform.position);
        Vector3 holeScreen = arCamera.WorldToScreenPoint(keyholeTarget.position);

        float screenDistance = Vector2.Distance(
            new Vector2(keyScreen.x, keyScreen.y),
            new Vector2(holeScreen.x, holeScreen.y)
        );

        float worldDistance = Vector3.Distance(transform.position, keyholeTarget.position);

        bool closeEnough =
            screenDistance <= screenSnapDistance ||
            worldDistance <= worldSnapDistance;

        if (closeEnough)
        {
            SnapToKeyhole();
        }
    }

    private void SnapToKeyhole()
    {
        isSnapped = true;
        isDragging = false;

        if (keyholeGuideObject != null)
        {
            keyholeGuideObject.SetActive(false);
        }

        Debug.Log($"열쇠가 키홀 근처에 도착했습니다: {gameObject.name}");

        if (unlockSequence != null)
        {
            unlockSequence.PlayUnlockSequence(transform);
        }
        else
        {
            // unlockSequence가 없을 때만 기존 방식으로 즉시 스냅
            if (keySnapPose != null)
            {
                transform.position = keySnapPose.position;
                transform.rotation = keySnapPose.rotation;
            }
            else if (keyholeTarget != null)
            {
                transform.position = keyholeTarget.position;
                transform.rotation = keyholeTarget.rotation;
            }
        }

        if (disableAfterSnap)
        {
            enabled = false;
        }
    }

    public void SetTarget(Camera camera, Transform keyhole, Transform snapPose, GameObject guideObject)
    {
        arCamera = camera;
        keyholeTarget = keyhole;
        keySnapPose = snapPose;
        keyholeGuideObject = guideObject;
    }

    public void ResetKey()
    {
        isDragging = false;
        isSnapped = false;

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        targetPosition = originalPosition;

        enabled = true;
    }
}