using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    public Camera targetCamera;

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null) return;

        transform.rotation = Quaternion.LookRotation(
            transform.position - targetCamera.transform.position,
            targetCamera.transform.up
        );
    }
}