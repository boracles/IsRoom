using UnityEngine;

public class AudioFollowTarget : MonoBehaviour
{
    public Transform target;
    public bool followOnlyWhenTargetActive = false;
    public float followSpeed = 20f;

    private Vector3 lastKnownPosition;

    void Start()
    {
        if (target != null)
        {
            lastKnownPosition = target.position;
            transform.position = target.position;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 타깃이 꺼져도 마지막 위치에 사운드를 남기고 싶으면 false
        if (followOnlyWhenTargetActive && !target.gameObject.activeInHierarchy)
            return;

        lastKnownPosition = target.position;

        transform.position = Vector3.Lerp(
            transform.position,
            lastKnownPosition,
            followSpeed * Time.deltaTime
        );
    }
}