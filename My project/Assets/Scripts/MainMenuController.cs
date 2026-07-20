using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_InputField durationInput;
    [SerializeField] private Button playButton;

    private void Start()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(StartGame);
        }

        if (durationInput != null)
        {
            durationInput.text = "3"; // Default 3 minutes
        }
    }

    private void StartGame()
    {
        float minutes = 3f;
        if (durationInput != null && float.TryParse(durationInput.text, out float parsedMinutes))
        {
            // Floor clamp to prevent negative or zero duration
            minutes = Mathf.Max(0.5f, parsedMinutes);
        }

        GameSettings.MatchDuration = minutes * 60f;
        SceneManager.LoadScene("Match");
    }
}
