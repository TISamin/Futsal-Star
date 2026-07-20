using System;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    public enum MatchState { Warmup, Playing, GoalReset, Finished }
    
    [Header("Match State")]
    [SerializeField] private MatchState currentState = MatchState.Warmup;
    [SerializeField] private float warmupDuration = 3f;
    [SerializeField] private float goalResetDuration = 1.5f;

    private float matchTimer;
    private float warmupTimer;
    private int team0Score = 0; // Red
    private int team1Score = 0; // Blue

    public MatchState CurrentState => currentState;
    public float CurrentTime => currentState == MatchState.Warmup ? warmupTimer : matchTimer;
    public bool IsMatchPlaying => currentState == MatchState.Playing;
    
    public int Team0Score => team0Score;
    public int Team1Score => team1Score;

    public static event Action OnMatchStart;
    public static event Action OnMatchEnd;
    public static event Action OnStateChanged;

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
        matchTimer = GameSettings.MatchDuration;
        warmupTimer = warmupDuration;
        currentState = MatchState.Warmup;
        OnStateChanged?.Invoke();
    }

    private void OnEnable()
    {
        GoalDetector.OnGoalScored += HandleGoalScored;
    }

    private void OnDisable()
    {
        GoalDetector.OnGoalScored -= HandleGoalScored;
    }

    private void Update()
    {
        switch (currentState)
        {
            case MatchState.Warmup:
                warmupTimer -= Time.deltaTime;
                if (warmupTimer <= 0f)
                {
                    currentState = MatchState.Playing;
                    OnMatchStart?.Invoke();
                    OnStateChanged?.Invoke();
                }
                break;

            case MatchState.Playing:
                matchTimer -= Time.deltaTime;
                if (matchTimer <= 0f)
                {
                    matchTimer = 0f;
                    EndMatch();
                }
                break;
        }
    }

    private void HandleGoalScored(int scoringTeamId)
    {
        if (currentState != MatchState.Playing) return;

        if (scoringTeamId == 0)
        {
            team0Score++;
        }
        else
        {
            team1Score++;
        }

        currentState = MatchState.GoalReset;
        OnStateChanged?.Invoke();

        // Wait for reset to complete before resuming play
        Invoke(nameof(ResumeAfterGoal), goalResetDuration);
    }

    private void ResumeAfterGoal()
    {
        if (currentState == MatchState.Finished) return;
        
        currentState = MatchState.Playing;
        OnStateChanged?.Invoke();
    }

    private void EndMatch()
    {
        currentState = MatchState.Finished;
        OnMatchEnd?.Invoke();
        OnStateChanged?.Invoke();
        Debug.Log($"Match ended! Red {team0Score} - Blue {team1Score}");
    }
}
