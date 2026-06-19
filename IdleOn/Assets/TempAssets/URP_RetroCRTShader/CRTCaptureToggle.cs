using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Disables the CRT full-screen feature only while captureCamera (the low-res
// scene-capture camera) is rendering, so the captured frame stays clean and
// the CRT pass applies once, on the final blit camera.
public class CRTCaptureToggle : MonoBehaviour
{
    [SerializeField] private Camera captureCamera;
    [SerializeField] private ScriptableRendererFeature crtFeature;

    void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    void OnDisable() => RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

    void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (crtFeature == null) return;
        crtFeature.SetActive(cam != captureCamera);
    }
}
