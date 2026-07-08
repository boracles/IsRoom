using System;
using System.Collections;
using UnityEngine;

public class DraggablePieceToTurntable : MonoBehaviour
{
    [Header("Camera")]
    public Camera arCamera;

    [Header("Target")]
    public Transform targetPose;

    [Header("Parent After Placed")]
    public Transform placedParent;
    public bool parentToPlacedParent = true;

    [Header("Guide")]
    public GameObject guideObject;

    [Header("Raycast")]
    public LayerMask draggableLayerMask;

    [Header("Drag")]
    public float followSpeed = 18f;
    public float dragPlaneOffset = 0f;

    [Header("Scale")]
    public bool scaleWhileDragging = true;
    public Vector3 dragScale = new Vector3(0.55f, 0.55f, 0.55f);
    public Vector3 placedScale = new Vector3(0.55f, 0.55f, 0.55f);
    public float scaleDuration = 0.25f;

    [Header("Snap")]
    public float screenSnapDistance = 130f;
    public float worldSnapDistance = 0.12f;

    [Header("Audio")]
    public bool playAudioOnPlaced = true;
    public float audioFadeInDuration = 1.0f;
    public float audioVolume = 0.75f;

    private bool isDragging;
    private bool isPlaced;

    private Plane dragPlane;
    private Vector3 targetDragPosition;

    private Vector3 originalScale;
    private Coroutine scaleRoutine;

    public Action<DraggablePieceToTurntable> onPlaced;

    private void Awake()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        originalScale = transform.localScale;
        targetDragPosition = transform.position;

        // 인스펙터에서 LayerMask를 안 넣었을 때 자동으로 DraggablePiece 레이어를 찾음
        if (draggableLayerMask.value == 0)
        {
            int layer = LayerMask.NameToLayer("DraggablePiece");

            if (layer >= 0)
            {
                draggableLayerMask = 1 << layer;
            }
            else
            {
                Debug.LogWarning("[DraggablePieceToTurntable] DraggablePiece 레이어를 찾지 못했습니다. Tags & Layers 이름을 확인하세요.");
                draggableLayerMask = ~0;
            }
        }
    }

    private void Update()
    {
        if (isPlaced)
            return;

        if (arCamera == null)
        {
            arCamera = Camera.main;
            if (arCamera == null) return;
        }

        HandleMouseInput();
        HandleTouchInput();

        if (isDragging)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetDragPosition,
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
        if (isDragging || isPlaced)
            return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            100f,
            draggableLayerMask,
            QueryTriggerInteraction.Collide
        );

        if (hits.Length == 0)
        {
            Debug.LogWarning($"[Drag Failed] DraggablePiece 레이어에서 아무것도 맞지 않음: {name}");
            return;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Debug.Log($"[Drag Raycast Hit] {hit.transform.name} / layer: {LayerMask.LayerToName(hit.transform.gameObject.layer)}");

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                BeginDrag();
                return;
            }
        }

        Debug.LogWarning($"[Drag Failed] Raycast는 DraggablePiece를 맞췄지만, 현재 오브제 {name}은 아님");
    }

    private void BeginDrag()
    {
        Debug.Log($"[Drag Start] {name}");

        isDragging = true;

        Vector3 planeNormal = -arCamera.transform.forward;
        Vector3 planePoint = transform.position + arCamera.transform.forward * dragPlaneOffset;

        dragPlane = new Plane(planeNormal, planePoint);
        targetDragPosition = transform.position;

        if (guideObject != null)
        {
            guideObject.SetActive(true);
        }

        if (scaleWhileDragging)
        {
            StartScaleTo(dragScale);
        }
    }

    private void UpdateDrag(Vector2 screenPosition)
    {
        if (!isDragging)
            return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (dragPlane.Raycast(ray, out float enter))
        {
            targetDragPosition = ray.GetPoint(enter);
        }
    }

    private void EndDrag()
    {
        if (!isDragging)
            return;

        isDragging = false;
        CheckSnap();

        if (!isPlaced && scaleWhileDragging)
        {
            StartScaleTo(originalScale);
        }
    }

    private void CheckSnap()
    {
        if (targetPose == null || arCamera == null)
            return;

        Vector3 pieceScreen = arCamera.WorldToScreenPoint(transform.position);
        Vector3 targetScreen = arCamera.WorldToScreenPoint(targetPose.position);

        float screenDistance = Vector2.Distance(
            new Vector2(pieceScreen.x, pieceScreen.y),
            new Vector2(targetScreen.x, targetScreen.y)
        );

        float worldDistance = Vector3.Distance(transform.position, targetPose.position);

        if (screenDistance <= screenSnapDistance || worldDistance <= worldSnapDistance)
        {
            PlaceOnTarget();
        }
    }

    private void PlaceOnTarget()
    {
        if (isPlaced)
            return;

        isPlaced = true;
        isDragging = false;

        transform.position = targetPose.position;
        transform.rotation = targetPose.rotation;

        if (parentToPlacedParent && placedParent != null)
        {
            transform.SetParent(placedParent, true); // 월드 위치/회전 유지
            Debug.Log($"[{gameObject.name}] {placedParent.name}의 자식이 되었습니다.");
        }

        StartScaleTo(placedScale);

        if (guideObject != null)
        {
            guideObject.SetActive(false);
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        if (playAudioOnPlaced)
        {
            StartCoroutine(PlayLayerAudio());
        }

        Debug.Log($"턴테이블에 오브제가 놓였습니다: {gameObject.name}");

        onPlaced?.Invoke(this);

        enabled = false;
    }

    private void StartScaleTo(Vector3 targetScale)
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
        }

        scaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < scaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scaleDuration);

            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
        scaleRoutine = null;
    }

    private IEnumerator PlayLayerAudio()
    {
        AudioSource audio = GetComponentInChildren<AudioSource>(true);

        if (audio == null)
        {
            Debug.LogWarning($"[{name}] AudioSource가 없습니다.");
            yield break;
        }

        if (audio.clip == null)
        {
            Debug.LogWarning($"[{name}] AudioClip이 없습니다.");
            yield break;
        }

        audio.playOnAwake = false;
        audio.loop = true;
        audio.spatialBlend = 0f;
        audio.volume = 0f;

        audio.Play();

        float elapsed = 0f;

        while (elapsed < audioFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / audioFadeInDuration);

            audio.volume = Mathf.Lerp(0f, audioVolume, t);
            yield return null;
        }

        audio.volume = audioVolume;
    }

    public void SetTarget(
    Camera camera,
    Transform pose,
    GameObject guide,
    Vector3 dragScaleValue,
    Vector3 placedScaleValue,
    Transform parentAfterPlaced
    )
    {
        arCamera = camera;
        targetPose = pose;
        guideObject = guide;
        dragScale = dragScaleValue;
        placedScale = placedScaleValue;
        placedParent = parentAfterPlaced;

        if (draggableLayerMask.value == 0)
        {
            int layer = LayerMask.NameToLayer("DraggablePiece");
            if (layer >= 0)
            {
                draggableLayerMask = 1 << layer;
            }
        }
    }
}