using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "Cutscene";

    public void LoadNextScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
