using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Elements")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text countdownText;

    [Header("Goal Overlay")]
    [SerializeField] private GameObject goalOverlay;
    [SerializeField] private TMP_Text goalOverlayText;

    [Header("End Game Overlay")]
    [SerializeField] private GameObject endGameOverlay;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (playAgainButton != null) playAgainButton.onClick.AddListener(PlayAgain);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);

        if (goalOverlay != null) goalOverlay.SetActive(false);
        if (endGameOverlay != null) endGameOverlay.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        if (scoreText == null) Debug.LogWarning("[UIManager] Score Text is not assigned in the Inspector!");
        if (timerText == null) Debug.LogWarning("[UIManager] Timer Text is not assigned in the Inspector!");
        if (countdownText == null) Debug.LogWarning("[UIManager] Countdown Text is not assigned in the Inspector!");

        UpdateUI();
    }

    private void OnEnable()
    {
        MatchManager.OnStateChanged += UpdateUI;
    }

    private void OnDisable()
    {
        MatchManager.OnStateChanged -= UpdateUI;
    }

    private void Update()
    {
        UpdateTimerAndCountdown();
    }

    private void UpdateTimerAndCountdown()
    {
        if (MatchManager.Instance == null) return;

        MatchManager.MatchState state = MatchManager.Instance.CurrentState;

        if (state == MatchManager.MatchState.Warmup)
        {
            if (timerText != null) timerText.text = "03:00";
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
                float rawTime = MatchManager.Instance.CurrentTime;
                if (rawTime > 0.1f)
                {
                    countdownText.text = Mathf.CeilToInt(rawTime).ToString();
                }
                else
                {
                    countdownText.text = "GO!";
                }
            }
        }
        else
        {
            if (countdownText != null && countdownText.gameObject.activeSelf)
            {
                countdownText.gameObject.SetActive(false);
            }

            if (timerText != null && state != MatchManager.MatchState.Warmup)
            {
                float timeRemaining = MatchManager.Instance.CurrentTime;
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }
    }

    private void UpdateUI()
    {
        if (MatchManager.Instance == null) return;

        Debug.Log($"[UIManager] UpdateUI called. State: {MatchManager.Instance.CurrentState}, Red: {MatchManager.Instance.Team0Score}, Blue: {MatchManager.Instance.Team1Score}");

        // Update score
        if (scoreText != null)
        {
            scoreText.text = $"RED {MatchManager.Instance.Team0Score} - {MatchManager.Instance.Team1Score} BLUE";
        }

        MatchManager.MatchState state = MatchManager.Instance.CurrentState;

        // Manage Goal Overlay — use animated coroutine
        if (goalOverlay != null)
        {
            if (state == MatchManager.MatchState.GoalReset)
            {
                goalOverlay.SetActive(true);
                StartCoroutine(AnimateGoalText());
            }
            else
            {
                goalOverlay.SetActive(false);
            }
        }

        // Manage End Game Overlay
        if (endGameOverlay != null)
        {
            bool isFinished = (state == MatchManager.MatchState.Finished);
            endGameOverlay.SetActive(isFinished);

            if (isFinished)
            {
                int redScore = MatchManager.Instance.Team0Score;
                int blueScore = MatchManager.Instance.Team1Score;

                if (finalScoreText != null)
                {
                    finalScoreText.text = $"Final Score\nRED {redScore} - {blueScore} BLUE";
                }

                if (resultText != null)
                {
                    if (redScore > blueScore)
                    {
                        resultText.text = "RED TEAM WINS!";
                        resultText.color = Color.red;
                    }
                    else if (blueScore > redScore)
                    {
                        resultText.text = "BLUE TEAM WINS!";
                        resultText.color = new Color(0.2f, 0.3f, 1f);
                    }
                    else
                    {
                        resultText.text = "IT'S A DRAW!";
                        resultText.color = Color.white;
                    }
                }
            }
        }
    }

    private void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Animates the GOAL! text with a punch-scale effect (1.0 → 1.4 → 1.0 over 0.5s).
    /// </summary>
    private IEnumerator AnimateGoalText()
    {
        if (goalOverlayText == null) yield break;

        RectTransform rt = goalOverlayText.GetComponent<RectTransform>();
        if (rt == null) yield break;

        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 originalScale = Vector3.one;
        float peakScale = 1.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Punch curve: quickly scale up then settle back down
            // Using a sine curve: peaks at t=0.25, returns to 1.0 at t=1.0
            float scale = 1f + (peakScale - 1f) * Mathf.Sin(t * Mathf.PI);
            rt.localScale = originalScale * scale;

            yield return null;
        }

        rt.localScale = originalScale;
    }
}
