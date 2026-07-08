using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TurntableLayeringSequence : MonoBehaviour
{
    [Header("References")]
    public Transform generatedPieceParent;
    public Camera arCamera;

    [Header("Guide UI")]
    public KeyDragGuideUI dragGuideUI;
    public GameObject dragGuideObject;
    public TMP_Text guideText;

    [Header("Turntable Poses")]
    public Transform inkTurntablePose;
    public Transform paperTurntablePose;

    [Header("Scale")]
    public Vector3 inkDragScale = new Vector3(0.45f, 0.45f, 0.45f);
    public Vector3 inkPlacedScale = new Vector3(0.45f, 0.45f, 0.45f);

    public Vector3 paperDragScale = new Vector3(0.45f, 0.45f, 0.45f);
    public Vector3 paperPlacedScale = new Vector3(0.45f, 0.45f, 0.45f);

    [Header("Snap")]
    public float screenSnapDistance = 130f;
    public float worldSnapDistance = 0.12f;

    [Header("Final Sequence")]
    public FinalSequenceController finalSequenceController;

    private GameObject inkPiece;
    private GameObject paperPiece;

    public void BeginSequence()
    {
        BeginSequence(null);
    }

    public void BeginSequence(List<GameObject> pieces)
    {
        inkPiece = FindPiece(pieces, "ink");
        paperPiece = FindPiece(pieces, "paper");

        if (inkPiece == null)
        {
            Debug.LogWarning("[TurntableLayeringSequence] Ink 오브제를 찾지 못했습니다.");
            return;
        }

        if (paperPiece == null)
        {
            Debug.LogWarning("[TurntableLayeringSequence] Paper 오브제를 찾지 못했습니다.");
        }

        StartInkStep();
    }

    private void StartInkStep()
    {
        SetGuideText("잉크를 턴테이블 위에 올려보세요.");

        SetupDraggablePiece(
            inkPiece,
            inkTurntablePose,
            inkDragScale,
            inkPlacedScale,
            OnInkPlaced
        );
    }

    private void OnInkPlaced(DraggablePieceToTurntable placed)
    {
        Debug.Log("Ink 배치 완료. Paper 단계 시작.");

        if (paperPiece == null)
        {
            SetGuideText("");
            if (dragGuideObject != null) dragGuideObject.SetActive(false);
            return;
        }

        StartPaperStep();
    }

    private void StartPaperStep()
    {
        SetGuideText("편지지를 턴테이블 위에 올려보세요.");

        SetupDraggablePiece(
            paperPiece,
            paperTurntablePose,
            paperDragScale,
            paperPlacedScale,
            OnPaperPlaced
        );
    }

    private void OnPaperPlaced(DraggablePieceToTurntable placed)
    {
        Debug.Log("Paper 배치 완료. 턴테이블 레이어링 완료.");

        SetGuideText("");

        if (dragGuideObject != null)
        {
            dragGuideObject.SetActive(false);
        }

        if (dragGuideUI != null)
        {
            dragGuideUI.ClearKeyTarget();
        }

        if (finalSequenceController != null)
        {
            finalSequenceController.StartFinalSequence();
        }
    }

    private void SetupDraggablePiece(
        GameObject piece,
        Transform targetPose,
        Vector3 dragScale,
        Vector3 placedScale,
        System.Action<DraggablePieceToTurntable> onPlaced
    )
    {
        if (piece == null || targetPose == null)
            return;

        DraggablePieceToTurntable draggable = piece.GetComponent<DraggablePieceToTurntable>();

        if (draggable == null)
        {
            draggable = piece.AddComponent<DraggablePieceToTurntable>();
        }

        draggable.screenSnapDistance = screenSnapDistance;
        draggable.worldSnapDistance = worldSnapDistance;

        draggable.SetTarget(
            arCamera != null ? arCamera : Camera.main,
            targetPose,
            dragGuideObject,
            dragScale,
            placedScale,
            targetPose
        );

        draggable.onPlaced = onPlaced;
        draggable.enabled = true;

        if (dragGuideUI != null)
        {
            dragGuideUI.SetKeyTarget(piece.transform);
            dragGuideUI.SetKeyholeTarget(targetPose);
        }

        if (dragGuideObject != null)
        {
            dragGuideObject.SetActive(true);
        }

        EnsureCollider(piece);

        Debug.Log($"드래그 대상 설정됨: {piece.name} → {targetPose.name}");
    }

    private void EnsureCollider(GameObject piece)
    {
        Collider existing = piece.GetComponentInChildren<Collider>(true);

        if (existing != null)
            return;

        BoxCollider box = piece.AddComponent<BoxCollider>();
        box.isTrigger = false;
        box.size = Vector3.one * 0.25f;

        Debug.LogWarning($"[{piece.name}] Collider가 없어 BoxCollider를 임시로 추가했습니다.");
    }

    private GameObject FindPiece(List<GameObject> pieces, string keyword)
    {
        if (pieces != null)
        {
            foreach (GameObject piece in pieces)
            {
                if (piece == null) continue;

                string name = piece.name.ToLower();

                if (name.Contains(keyword))
                    return piece;
            }
        }

        if (generatedPieceParent != null)
        {
            Transform[] children = generatedPieceParent.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                string name = child.name.ToLower();

                if (name.Contains(keyword))
                    return child.gameObject;
            }
        }

        return null;
    }

    private void SetGuideText(string text)
    {
        if (guideText != null)
        {
            guideText.text = text;
            guideText.transform.parent.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
        }
    }
}