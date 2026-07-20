using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class ControlAssignment : MonoBehaviour
{
    public static ControlAssignment Instance { get; private set; }

    [Header("Switch Settings")]
    [SerializeField] private int humanTeamId = 0; // 0: Red, 1: Blue
    [SerializeField] private float hysteresisMargin = 0.5f; // Reduced for snappier switching
    [SerializeField] private float autoSwitchInterval = 0.3f; // Faster checks

    [Header("Visuals/Materials")]
    [SerializeField] private Material highlightMaterial;

    private PlayerBase activePlayer;
    private Transform ballTransform;
    private float autoSwitchTimer;
    private float manualSwitchCooldown;

    // Visual indicator for the controlled player
    private GameObject controlIndicator;

    public PlayerBase ActivePlayer => activePlayer;
    public int HumanTeamId => humanTeamId;

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
        // Find ball physics component to get the ball's transform
        BallPhysics ball = FindFirstObjectByType<BallPhysics>();
        if (ball != null)
        {
            ballTransform = ball.transform;
        }

        // Create the visual indicator
        CreateControlIndicator();

        // Initial assignment
        FindAndAssignInitialPlayer();
    }

    private void Update()
    {
        if (ballTransform == null) return;

        if (MatchManager.Instance != null && !MatchManager.Instance.IsMatchPlaying) return;

        // Manual switch check (J key)
        if (manualSwitchCooldown > 0f)
        {
            manualSwitchCooldown -= Time.deltaTime;
        }

        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame && manualSwitchCooldown <= 0f)
        {
            // Manual override forces switch check immediately
            manualSwitchCooldown = 0.3f;
            EvaluateControlSwitch(true);
        }

        // Auto-switch periodic check
        autoSwitchTimer += Time.deltaTime;
        if (autoSwitchTimer >= autoSwitchInterval)
        {
            autoSwitchTimer = 0f;
            EvaluateControlSwitch(false);
        }
    }

    private void LateUpdate()
    {
        // Keep the indicator hovering above the active player's head with a subtle pulse
        if (controlIndicator != null && activePlayer != null)
        {
            controlIndicator.transform.position = activePlayer.transform.position + Vector3.up * 1.4f;

            // Subtle scale pulse (0.4 → 0.55 over time)
            float pulse = 0.475f + Mathf.Sin(Time.time * 4f) * 0.075f;
            controlIndicator.transform.localScale = new Vector3(pulse, 0.04f, pulse);

            controlIndicator.SetActive(true);
        }
        else if (controlIndicator != null)
        {
            controlIndicator.SetActive(false);
        }
    }

    private void CreateControlIndicator()
    {
        // Create a bright flat disc hovering above the controlled player
        controlIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        controlIndicator.name = "ControlIndicator";
        controlIndicator.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f); // Flat disc

        // Remove the collider so it doesn't interfere with gameplay physics
        Collider col = controlIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Set a bright green-yellow emissive color for maximum visibility
        MeshRenderer mr = controlIndicator.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Material mat = null;
            if (highlightMaterial != null)
            {
                mat = new Material(highlightMaterial);
            }
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader != null ? shader : Shader.Find("Hidden/InternalErrorShader"));
            }
            mat.color = new Color(0.3f, 1f, 0.3f, 0.9f); // Bright green
            mr.material = mat;
        }
    }

    private void FindAndAssignInitialPlayer()
    {
        PlayerBase[] players = TeamManager.AllPlayers;
        if (players == null)
        {
            // Fallback if TeamManager hasn't spawned yet
            players = FindObjectsByType<PlayerBase>(FindObjectsSortMode.None);
        }
        float minDistance = float.MaxValue;
        PlayerBase nearest = null;

        Vector3 targetPos = ballTransform != null ? ballTransform.position : Vector3.zero;

        foreach (PlayerBase p in players)
        {
            if (p.TeamId == humanTeamId)
            {
                float dist = Vector3.Distance(p.transform.position, targetPos);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = p;
                }
            }
        }

        if (nearest != null)
        {
            AssignActivePlayer(nearest);
        }
    }

    public void AssignActivePlayer(PlayerBase newPlayer)
    {
        if (activePlayer != null && activePlayer != newPlayer)
        {
            // Handle AI Takeover: old player immediately reverts to AI state evaluation
            AIController oldAI = activePlayer.GetComponent<AIController>();
            if (oldAI != null)
            {
                oldAI.OnAITakeover();
            }
        }

        activePlayer = newPlayer;
    }

    /// <summary>
    /// Evaluates if control should be switched to a closer teammate.
    /// No proximity buffer — always picks the nearest teammate to the ball.
    /// </summary>
    public void EvaluateControlSwitch(bool force)
    {
        if (ballTransform == null) return;

        // If your team currently has the ball, control follows the possessor
        if (BallPossession.Instance != null && BallPossession.Instance.CurrentPossessor != null)
        {
            PlayerBase possessor = BallPossession.Instance.CurrentPossessor;
            if (possessor.TeamId == humanTeamId)
            {
                if (activePlayer != possessor)
                {
                    AssignActivePlayer(possessor);
                }
                return;
            }
        }

        // Defense or Loose Ball: always switch to nearest teammate to ball
        PlayerBase[] players = TeamManager.AllPlayers;
        if (players == null) return;
        PlayerBase bestCandidate = null;
        float bestDist = float.MaxValue;

        foreach (PlayerBase p in players)
        {
            if (p.TeamId != humanTeamId) continue;

            float dist = Vector3.Distance(p.transform.position, ballTransform.position);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestCandidate = p;
            }
        }

        // Apply hysteresis only when not forced (prevent rapid flickering)
        if (!force && activePlayer != null && bestCandidate != activePlayer)
        {
            float activeDist = Vector3.Distance(activePlayer.transform.position, ballTransform.position);
            if (bestDist >= activeDist - hysteresisMargin)
            {
                return; // Not significantly closer, don't switch
            }
        }

        if (bestCandidate != null && bestCandidate != activePlayer)
        {
            AssignActivePlayer(bestCandidate);
        }
    }
}
