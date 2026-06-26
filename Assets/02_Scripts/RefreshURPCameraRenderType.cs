using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class RefreshURPCameraRenderType : MonoBehaviour
{
    private IEnumerator Start()
    {
        var cam = GetComponent<Camera>();
        var data = cam.GetUniversalAdditionalCameraData();

        // Vuforia가 VideoBackground를 생성할 시간을 한 프레임 줌
        yield return null;
        yield return null;

        // Render Type 토글로 URP camera data / renderer pass 강제 갱신
        data.renderType = CameraRenderType.Overlay;
        yield return null;

        data.renderType = CameraRenderType.Base;
    }
}