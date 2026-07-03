using UnityEngine;
using Vuforia;

public class IRoomVuforiaTracker : MonoBehaviour
{
    [Header("Sequence")]
    public IRoomSequenceManager sequenceManager;

    private ObserverBehaviour observerBehaviour;
    private bool hasStarted = false;

    private void Awake()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();

        if (observerBehaviour != null)
        {
            observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
        }
        else
        {
            Debug.LogError("ObserverBehaviour를 찾을 수 없습니다. 이 스크립트를 Vuforia Target 오브젝트에 붙였는지 확인하세요.");
        }
    }

    private void OnDestroy()
    {
        if (observerBehaviour != null)
        {
            observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        if (hasStarted) return;

        if (status.Status == Status.TRACKED ||
            status.Status == Status.EXTENDED_TRACKED)
        {
            hasStarted = true;

            Debug.Log("I의 방 인식됨. 시퀀스를 시작합니다.");

            if (sequenceManager != null)
            {
                sequenceManager.StartSequenceAfterIRoomAwakened();
            }
            else
            {
                Debug.LogError("IRoomSequenceManager가 연결되어 있지 않습니다.");
            }
        }
    }
}