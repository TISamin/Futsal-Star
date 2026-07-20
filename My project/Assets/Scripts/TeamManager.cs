using UnityEngine;
using System.Collections.Generic;

public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance { get; private set; }

    [Header("Team Colors")]
    [SerializeField] private Color team0Color = Color.red;
    [SerializeField] private Color team1Color = new Color(0.2f, 0.3f, 1f); // Brighter blue

    [Header("Player Sizing")]
    [SerializeField] private float playerRadius = 0.4f;
    [SerializeField] private float playerHeight = 1.2f;

    private List<PlayerBase> spawnedPlayers = new List<PlayerBase>();
    private bool isResetting = false;

    /// <summary>
    /// Cached array of all 10 spawned players. Use this instead of FindObjectsByType.
    /// </summary>
    public static PlayerBase[] AllPlayers { get; private set; }

    public int Team0Score => MatchManager.Instance != null ? MatchManager.Instance.Team0Score : 0;
    public int Team1Score => MatchManager.Instance != null ? MatchManager.Instance.Team1Score : 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Automatically add AIPassingEvaluator if not present
            if (GetComponent<AIPassingEvaluator>() == null)
            {
                gameObject.AddComponent<AIPassingEvaluator>();
            }

            SpawnTeams();
            AllPlayers = spawnedPlayers.ToArray();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        GoalDetector.OnGoalScored += HandleGoalScored;
    }

    private void OnDisable()
    {
        GoalDetector.OnGoalScored -= HandleGoalScored;
    }

    private void Start()
    {
        // Force evaluation of control assignment now that all players are spawned
        if (ControlAssignment.Instance != null)
        {
            ControlAssignment.Instance.EvaluateControlSwitch(true);
        }
    }

    private void SpawnTeams()
    {
        string[] roles = { "GK", "Defender", "LeftMid", "RightMid", "Forward" };

        // Spawn Team 0 (Red)
        for (int i = 0; i < roles.Length; i++)
        {
            SpawnPlayer(0, roles[i]);
        }

        // Spawn Team 1 (Blue)
        for (int i = 0; i < roles.Length; i++)
        {
            SpawnPlayer(1, roles[i]);
        }
    }

    private void SpawnPlayer(int teamId, string role)
    {
        // Calculate spawn position from formation data
        Vector3 spawnPos = FormationData.GetTargetPosition(teamId, role, Vector3.zero);

        // === BUILD PLAYER FROM CODE ===

        GameObject playerObj = new GameObject($"{(teamId == 0 ? "Red" : "Blue")}_{role}");
        playerObj.transform.position = spawnPos;

        Rigidbody rb = playerObj.AddComponent<Rigidbody>();
        rb.mass = 2f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezePositionY
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        CapsuleCollider cc = playerObj.AddComponent<CapsuleCollider>();
        cc.radius = playerRadius;
        cc.height = playerHeight;
        cc.center = new Vector3(0f, 0f, 0f); // Centered at pivot (player sits on ground)

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Visual";
        visual.transform.SetParent(playerObj.transform);
        visual.transform.localPosition = new Vector3(0f, 0f, 0f); // Sits on ground
        visual.transform.localScale = new Vector3(playerRadius * 2f, playerHeight / 2f, playerRadius * 2f);

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null) Object.Destroy(visualCollider);

        Color teamColor = (teamId == 0) ? team0Color : team1Color;
        MeshRenderer mr = visual.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.material.color = teamColor;
        }

        PlayerBase pb = playerObj.AddComponent<PlayerBase>();
        pb.Initialize(teamId, role);

        playerObj.AddComponent<PlayerController>();
        playerObj.AddComponent<AIController>();
        playerObj.AddComponent<JockeySystem>();

        // Track spawned players
        spawnedPlayers.Add(pb);
    }

    private void HandleGoalScored(int scoringTeamId)
    {
        if (isResetting) return;

        Debug.Log($"GOAL SCORED! Resetting positions...");

        // Kickoff starts with the team that conceded (the losing team of that point)
        int kickoffTeamId = (scoringTeamId == 0) ? 1 : 0;
        StartCoroutine(ResetKickoffCoroutine(kickoffTeamId));
    }

    private System.Collections.IEnumerator ResetKickoffCoroutine(int kickoffTeamId)
    {
        isResetting = true;

        // Briefly pause all players
        foreach (PlayerBase p in spawnedPlayers)
        {
            p.MoveTo(Vector3.zero, false);
            Rigidbody rb = p.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }

        yield return new WaitForSeconds(1.5f);

        // Find ball
        BallPhysics ball = FindFirstObjectByType<BallPhysics>();
        Rigidbody ballRb = ball != null ? ball.GetComponent<Rigidbody>() : null;

        // Reset ball position & physics
        if (ball != null)
        {
            // Clear ball trail before teleporting to prevent streak across the court
            BallTrail ballTrail = ball.GetComponent<BallTrail>();
            if (ballTrail != null)
            {
                ballTrail.ClearTrail();
            }

            if (BallPossession.Instance != null)
            {
                BallPossession.Instance.ReleaseBall(Vector3.zero);
            }
            ball.transform.position = new Vector3(0f, 0.2f, 0f);
            if (ballRb != null)
            {
                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
            }
        }

        // Reset players to kickoff spots
        foreach (PlayerBase p in spawnedPlayers)
        {
            Vector3 startPos;
            if (p.Role == "Forward")
            {
                if (p.TeamId == kickoffTeamId)
                {
                    // Kickoff forward stands at center circle
                    startPos = (kickoffTeamId == 0) ? new Vector3(-0.5f, 0f, 0f) : new Vector3(0.5f, 0f, 0f);
                }
                else
                {
                    // Defending forward stands back on their half
                    startPos = (p.TeamId == 0) ? new Vector3(-2f, 0f, 0f) : new Vector3(2f, 0f, 0f);
                }
            }
            else
            {
                // Formations align to center
                startPos = FormationData.GetTargetPosition(p.TeamId, p.Role, Vector3.zero);
            }

            p.ResetToPosition(startPos);
        }

        yield return new WaitForEndOfFrame();

        // Give ball to kickoff forward
        PlayerBase kickoffForward = spawnedPlayers.Find(p => p.TeamId == kickoffTeamId && p.Role == "Forward");
        if (kickoffForward != null && BallPossession.Instance != null)
        {
            BallPossession.Instance.AssignPossession(kickoffForward);
        }

        // Reset manual control targets
        if (ControlAssignment.Instance != null)
        {
            ControlAssignment.Instance.EvaluateControlSwitch(true);
        }

        isResetting = false;
    }
}
