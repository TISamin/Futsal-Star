using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "Cutscene";

    private void Update()
    {
        // Detect click or screen tap anywhere using the new Input System
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            LoadNextScene();
        }
    }

    public void LoadNextScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
