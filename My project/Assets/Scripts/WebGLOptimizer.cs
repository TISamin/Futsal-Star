using UnityEngine;

/// <summary>
/// Applies WebGL-specific optimizations on startup.
/// Attach to a persistent GameObject in the first scene (MainMenu).
/// </summary>
public class WebGLOptimizer : MonoBehaviour
{
    private void Awake()
    {
        // Lock frame rate to 60 FPS across all platforms
        Application.targetFrameRate = 60;

        // Disable VSync so targetFrameRate takes effect (especially on WebGL)
        QualitySettings.vSyncCount = 0;

        Debug.Log($"[WebGLOptimizer] Platform: {Application.platform}, TargetFPS: {Application.targetFrameRate}");
    }
}
