using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(VideoPlayer))]
public class CutsceneController : MonoBehaviour
{
    [SerializeField] private string videoFileName = "cutscene.mp4";
    [SerializeField] private string nextSceneName = "MainMenu";

    private VideoPlayer videoPlayer;
    private bool isTransitioning = false;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Start()
    {
        // Use streamingAssetsPath for WebGL and local compatibility
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
        
        // Correct path format for WebGL vs standalone
        #if UNITY_WEBGL && !UNITY_EDITOR
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoPath;
        #else
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = "file://" + videoPath;
        #endif

        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Play();
    }

    private void Update()
    {
        // Listen for skip inputs using the new Input System
        if (!isTransitioning)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            {
                TransitionToNextScene();
            }
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        TransitionToNextScene();
    }

    private void TransitionToNextScene()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        videoPlayer.Stop();
        SceneManager.LoadScene(nextSceneName);
    }
}
