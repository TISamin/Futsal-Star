using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerBase))]
public class AIController : MonoBehaviour
{
    public enum AIState { GoToFormation, ChaseBall, DribbleAndAttack, HoldBall, Support }
    
    [Header("AI State")]
    [SerializeField] private AIState currentState = AIState.GoToFormation;
    [SerializeField] private float stateEvaluationInterval = 0.2f;

    private PlayerBase playerBase;
    private Transform ballTransform;
    private float evaluationTimer;
    private Vector3 currentMovementDirection;
    private bool currentSprintSetting;

    // AI decision memory
    private float holdBallTimer = 0f;
    private Vector3 lateralDribbleDirection;

    public AIState CurrentState => currentState;

    private void Awake()
    {
        playerBase = GetComponent<PlayerBase>();
    }

    private void Start()
    {
        // Find ball
        BallPhysics ball = FindFirstObjectByType<BallPhysics>();
        if (ball != null)
        {
            ballTransform = ball.transform;
        }

        // Randomize timer slightly so AI updates don't all execute on the exact same frame
        evaluationTimer = Random.Range(0f, stateEvaluationInterval);
        ChooseNewLateralDribbleDirection();
    }

    private void Update()
    {
        // Freeze AI if the match is not currently playing
        if (MatchManager.Instance != null && !MatchManager.Instance.IsMatchPlaying)
        {
            playerBase.MoveTo(Vector3.zero, false);
            return;
        }

        // If this player is controlled by a human, do not run AI logic
        if (ControlAssignment.Instance != null && ControlAssignment.Instance.ActivePlayer == playerBase)
        {
            return;
        }

        if (ballTransform == null) return;

        // Periodic state evaluation (tick state machine)
        evaluationTimer += Time.deltaTime;
        if (evaluationTimer >= stateEvaluationInterval)
        {
            evaluationTimer = 0f;
            EvaluateState();
        }

        // Execute current state movement and actions every frame
        ExecuteStateMovement();
    }

    /// <summary>
    /// Resets state immediately upon AI takeover.
    /// </summary>
    public void OnAITakeover()
    {
        currentState = AIState.GoToFormation;
        evaluationTimer = stateEvaluationInterval; // Force immediate state evaluation next frame
    }

    private void EvaluateState()
    {
        // 1. Goalkeeper Priority Override Rule:
        if (playerBase.Role == "GK")
        {
            bool isBallLooseInGKArea = false;
            if (BallPossession.Instance != null && BallPossession.Instance.CurrentPossessor == null)
            {
                // Ball is loose, check if it's within 5.0 units of the goalkeeper
                if (Vector3.Distance(transform.position, ballTransform.position) < 5.0f)
                {
                    isBallLooseInGKArea = true;
                }
            }

            if (!playerBase.HasBall && !isBallLooseInGKArea)
            {
                // GK stays in goalkeeper mode: return/slide around goal
                currentState = AIState.GoToFormation;
                return;
            }
        }

        // 2. Ball Carrier State Machine (if this player has the ball)
        if (playerBase.HasBall)
        {
            // If this player is on the human team, NEVER let the AI auto-pass or auto-shoot!
            // They will simply dribble forward while waiting for the human player to take direct control.
            if (ControlAssignment.Instance != null && playerBase.TeamId == ControlAssignment.Instance.HumanTeamId)
            {
                currentState = AIState.DribbleAndAttack;
                return;
            }

            // Evaluate shooting first
            if (AIShootingEvaluator.IsInShootingRange(playerBase))
            {
                float bestShotScore;
                int slot = AIShootingEvaluator.EvaluateBestShotSlot(playerBase, out bestShotScore);
                
                // If the shot is reasonably open, shoot!
                if (bestShotScore > 30f)
                {
                    playerBase.ShootAtGoal(slot, 1.0f);
                    return;
                }
            }

            // Evaluate passing
            if (AIPassingEvaluator.Instance != null)
            {
                AIPassingEvaluator.PassOption bestPass = AIPassingEvaluator.Instance.EvaluateBestPass(playerBase);
                if (bestPass.TargetPlayer != null)
                {
                    // Pass the ball!
                    if (bestPass.IsBankPass)
                    {
                        // Use lob pass for bank passes to avoid catching immediate defenders
                        playerBase.ShortPass(bestPass.PassDirection, 1.0f);
                    }
                    else
                    {
                        playerBase.ShortPass(bestPass.PassDirection, 0.9f);
                    }
                    return;
                }
            }

            // If shooting is not open and passing is blocked, decide whether to Dribble or Hold
            if (AIShootingEvaluator.IsInShootingRange(playerBase))
            {
                currentState = AIState.DribbleAndAttack;
            }
            else
            {
                // Check if opponent is closing in
                float nearestOpponentDist = GetNearestOpponentDistance();
                if (nearestOpponentDist < 3.0f)
                {
                    // Opponent is close, and passing lanes are blocked. Hold the ball to protect it.
                    currentState = AIState.HoldBall;
                }
                else
                {
                    currentState = AIState.DribbleAndAttack;
                }
            }
            return;
        }

        // 3. Off-ball Player State Machine (if this player does NOT have the ball)
        
        // If my teammate has the ball: Support state
        if (BallPossession.Instance != null && BallPossession.Instance.CurrentPossessor != null && 
            BallPossession.Instance.CurrentPossessor.TeamId == playerBase.TeamId)
        {
            currentState = AIState.Support;
            return;
        }

        // If opponent has the ball or ball is loose: Defensive rules
        bool isOpponentInPossession = BallPossession.Instance != null && 
                                     BallPossession.Instance.CurrentPossessor != null && 
                                     BallPossession.Instance.CurrentPossessor.TeamId != playerBase.TeamId;

        bool isBallLoose = BallPossession.Instance != null && BallPossession.Instance.CurrentPossessor == null;

        if (isOpponentInPossession || isBallLoose)
        {
            // Determine who should press (First Defender)
            PlayerBase presser = DetermineFirstDefender();

            if (presser == playerBase)
            {
                // This player is selected to press!
                currentState = AIState.ChaseBall;
            }
            else
            {
                // Other defenders fall back to shape
                currentState = AIState.GoToFormation;
            }
        }
    }

    private void ExecuteStateMovement()
    {
        // Goalkeeper custom sliding logic takes precedence if GK in GoToFormation
        if (playerBase.Role == "GK" && currentState == AIState.GoToFormation)
        {
            ExecuteGKSliding();
            return;
        }

        switch (currentState)
        {
            case AIState.GoToFormation:
                Vector3 targetFormationPos = FormationData.GetTargetPosition(playerBase.TeamId, playerBase.Role, ballTransform.position);
                Vector3 toTarget = targetFormationPos - transform.position;
                toTarget.y = 0;
                
                // Return to formation (walk back to save stamina/keep shape, unless ball is close)
                float distToBall = Vector3.Distance(transform.position, ballTransform.position);
                bool shouldSprint = distToBall < 5f; 
                
                if (toTarget.sqrMagnitude > 0.1f)
                {
                    playerBase.MoveTo(toTarget.normalized, shouldSprint);
                }
                else
                {
                    playerBase.MoveTo(Vector3.zero, false);
                }
                break;

            case AIState.ChaseBall:
                Vector3 toBall = ballTransform.position - transform.position;
                toBall.y = 0;
                
                // If ball is loose, sprint to claim it.
                // If pressing an opponent ball carrier (First Defender), walk to jockey.
                bool opponentPossesses = BallPossession.Instance != null && 
                                         BallPossession.Instance.CurrentPossessor != null && 
                                         BallPossession.Instance.CurrentPossessor.TeamId != playerBase.TeamId;

                playerBase.MoveTo(toBall.normalized, !opponentPossesses);
                break;

            case AIState.DribbleAndAttack:
                // Move towards opponent's goal
                Vector3 opponentGoalPos = ShootingHelper.GetGoalTarget(playerBase.TeamId, 0);
                Vector3 toGoal = opponentGoalPos - transform.position;
                toGoal.y = 0;
                
                // Dribble forward (slightly angled towards the goal) steered away from walls
                Vector3 attackDir = SteerAwayFromWalls(toGoal.normalized);
                playerBase.MoveTo(attackDir, false);
                break;

            case AIState.HoldBall:
                // Dribble laterally/slowly or away from the nearest opponent
                holdBallTimer += Time.deltaTime;
                if (holdBallTimer > 1.0f)
                {
                    holdBallTimer = 0f;
                    ChooseNewLateralDribbleDirection();
                }

                Vector3 holdDir = SteerAwayFromWalls(lateralDribbleDirection);
                playerBase.MoveTo(holdDir, false);
                break;

            case AIState.Support:
                // Support teammate: Move to formation with anti-clustering repulsion
                Vector3 formationTarget = FormationData.GetTargetPosition(playerBase.TeamId, playerBase.Role, ballTransform.position);
                
                Vector3 repulsionVector = Vector3.zero;
                PlayerBase[] allPlayers = TeamManager.AllPlayers;
                if (allPlayers == null) break;
                
                foreach (PlayerBase p in allPlayers)
                {
                    if (p == playerBase) continue;
                    if (p.TeamId == playerBase.TeamId)
                    {
                        Vector3 toTeammate = p.transform.position - transform.position;
                        toTeammate.y = 0;
                        float dist = toTeammate.magnitude;
                        
                        // Active repulsion if teammate is within 3.5 units
                        if (dist < 3.5f && dist > 0.05f)
                        {
                            repulsionVector -= toTeammate.normalized * (3.5f - dist) * 1.5f;
                        }
                    }
                }

                Vector3 supportDir = (formationTarget - transform.position).normalized + repulsionVector;
                supportDir.y = 0;
                supportDir.Normalize();

                playerBase.MoveTo(supportDir, false);
                break;
        }
    }

    private void ExecuteGKSliding()
    {
        // Goalkeeper lateral and depth sliding logic scaled 1.5 times
        float ballX = ballTransform.position.x;
        float targetX;

        if (playerBase.TeamId == 0) // Red (defending X = -21)
        {
            // Split court into thirds along length (length is 42, bounds at ±21, thirds at ±7)
            if (ballX < -7.0f) // Defensive third
            {
                targetX = -20.2f;
            }
            else if (ballX <= 7.0f) // Middle third
            {
                targetX = -19.2f;
            }
            else // Attacking third
            {
                targetX = -18.0f;
            }
        }
        else // Blue (defending X = 21)
        {
            if (ballX > 7.0f) // Defensive third
            {
                targetX = 20.2f;
            }
            else if (ballX >= -7.0f) // Middle third
            {
                targetX = 19.2f;
            }
            else // Attacking third
            {
                targetX = 18.0f;
            }
        }

        // Position goalkeeper in the lateral third of the goal width that the ball is at (Goalposts are at Z = ±3.0)
        float targetZ;
        float ballZ = ballTransform.position.z;
        if (ballZ > 1.0f)
        {
            targetZ = 2.0f; // Top third
        }
        else if (ballZ < -1.0f)
        {
            targetZ = -2.0f; // Bottom third
        }
        else
        {
            targetZ = 0.0f; // Center third
        }

        Vector3 gkTarget = new Vector3(targetX, transform.position.y, targetZ);
        Vector3 toGkTarget = gkTarget - transform.position;
        toGkTarget.y = 0;

        // If further than 5.0 units from goal vicinity, sprint back. Otherwise slide at walk speed.
        float distToGoalCenter = Vector3.Distance(transform.position, ShootingHelper.GetGoalTarget(playerBase.TeamId, 0));
        bool mustSprintBack = distToGoalCenter > 5.0f;

        if (toGkTarget.sqrMagnitude > 0.05f)
        {
            playerBase.MoveTo(toGkTarget.normalized, mustSprintBack);
        }
        else
        {
            playerBase.MoveTo(Vector3.zero, false);
        }
    }

    private PlayerBase DetermineFirstDefender()
    {
        PlayerBase[] allPlayers = TeamManager.AllPlayers;
        if (allPlayers == null) return null;
        PlayerBase bestCandidate = null;
        float minDistance = float.MaxValue;

        PlayerBase humanControlled = ControlAssignment.Instance != null ? ControlAssignment.Instance.ActivePlayer : null;

        foreach (PlayerBase p in allPlayers)
        {
            // Only teammates, excluding the human player, and goalkeeper doesn't leave goalpost area
            if (p.TeamId == playerBase.TeamId && p != humanControlled && p.Role != "GK")
            {
                float dist = Vector3.Distance(p.transform.position, ballTransform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestCandidate = p;
                }
            }
        }

        return bestCandidate;
    }

    private float GetNearestOpponentDistance()
    {
        PlayerBase[] allPlayers = TeamManager.AllPlayers;
        if (allPlayers == null) return float.MaxValue;
        float minDistance = float.MaxValue;

        foreach (PlayerBase p in allPlayers)
        {
            if (p.TeamId != playerBase.TeamId)
            {
                float dist = Vector3.Distance(p.transform.position, transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                }
            }
        }

        return minDistance;
    }

    private void ChooseNewLateralDribbleDirection()
    {
        // Choose to move left/right along the Z axis, or slightly backward
        float zDir = Random.value > 0.5f ? 1f : -1f;
        float xDir = playerBase.TeamId == 0 ? -0.2f : 0.2f; // Back away slightly from opponent goal

        lateralDribbleDirection = new Vector3(xDir, 0f, zDir).normalized;
    }

    /// <summary>
    /// Steers the movement direction vector away from boundary walls if the player is too close.
    /// </summary>
    private Vector3 SteerAwayFromWalls(Vector3 desiredDir)
    {
        Vector3 pos = transform.position;

        // Side walls are at Z = ±12 (scaled). If close, push away from the wall.
        if (pos.z > 10.8f && desiredDir.z > 0f)
        {
            desiredDir.z = -0.3f;
        }
        else if (pos.z < -10.8f && desiredDir.z < 0f)
        {
            desiredDir.z = 0.3f;
        }

        // End/goal walls are at X = ±21 (scaled). If close, push away from the wall.
        if (pos.x > 19.8f && desiredDir.x > 0f)
        {
            desiredDir.x = -0.3f;
        }
        else if (pos.x < -19.8f && desiredDir.x < 0f)
        {
            desiredDir.x = 0.3f;
        }

        return desiredDir.normalized;
    }
}
