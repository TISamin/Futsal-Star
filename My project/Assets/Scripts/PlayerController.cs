using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerBase))]
public class PlayerController : MonoBehaviour
{

    private PlayerBase playerBase;

    // Aim window state
    private enum AimState { None, Shooting, Passing, Lobbing }
    private AimState currentAimState = AimState.None;
    private Vector3 preAimMovement;
    private bool preAimSprint;
    private Vector3 aimDirection;
    private int shootAimSlot;

    private void Awake()
    {
        playerBase = GetComponent<PlayerBase>();
    }

    private void Update()
    {
        // Check if the match is currently playing
        if (MatchManager.Instance != null && !MatchManager.Instance.IsMatchPlaying)
        {
            playerBase.MoveTo(Vector3.zero, false);
            ResetAimWindow();
            return;
        }

        // If this player is not the currently assigned human-controlled player, ignore input
        if (ControlAssignment.Instance != null && ControlAssignment.Instance.ActivePlayer != playerBase)
        {
            playerBase.MoveTo(Vector3.zero, false);
            ResetAimWindow();
            return;
        }

        if (Keyboard.current == null) return;

        // Read raw WASD input
        float moveX = 0f;
        float moveZ = 0f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ = 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ = -1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;

        Vector3 rawInput = new Vector3(moveX, 0f, moveZ);
        bool sprintInput = Keyboard.current.leftShiftKey.isPressed;

        // ========== 'K' AUTO-SPRINT TO BALL ==========
        if (Keyboard.current.kKey.isPressed && !playerBase.HasBall)
        {
            Transform ballTransform = BallPossession.Instance != null ? BallPossession.Instance.transform : null;
            if (ballTransform == null)
            {
                BallPhysics bp = FindFirstObjectByType<BallPhysics>();
                if (bp != null) ballTransform = bp.transform;
            }

            if (ballTransform != null)
            {
                Vector3 toBall = ballTransform.position - transform.position;
                toBall.y = 0;
                if (toBall.sqrMagnitude > 0.01f)
                {
                    rawInput = toBall.normalized;
                    sprintInput = true;
                }
            }
        }

        // ========== AIM WINDOW ACTIVE ==========
        if (currentAimState != AimState.None)
        {
            // Player keeps moving with pre-aim momentum
            playerBase.MoveTo(preAimMovement, preAimSprint);

            // WASD now controls aim direction
            if (currentAimState == AimState.Shooting)
            {
                if (moveX < 0f) shootAimSlot = -1;
                else if (moveX > 0f) shootAimSlot = 1;
            }
            else
            {
                if (rawInput.sqrMagnitude > 0.001f)
                {
                    aimDirection = rawInput.normalized;
                }
            }

            // Check if the aiming key is still held down
            bool stillHeld = false;
            if (currentAimState == AimState.Shooting && Keyboard.current.spaceKey.isPressed) stillHeld = true;
            else if (currentAimState == AimState.Passing && Keyboard.current.lKey.isPressed) stillHeld = true;
            else if (currentAimState == AimState.Lobbing && Keyboard.current.iKey.isPressed) stillHeld = true;

            // If the key is released, execute the kick immediately
            if (!stillHeld)
            {
                ExecuteKick();
                ResetAimWindow();
            }
        }
        // ========== NORMAL MODE ==========
        else
        {
            playerBase.MoveTo(rawInput, sprintInput);

            if (playerBase.HasBall)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    OpenAimWindow(AimState.Shooting, rawInput, sprintInput);
                }
                else if (Keyboard.current.lKey.wasPressedThisFrame)
                {
                    OpenAimWindow(AimState.Passing, rawInput, sprintInput);
                }
                else if (Keyboard.current.iKey.wasPressedThisFrame)
                {
                    OpenAimWindow(AimState.Lobbing, rawInput, sprintInput);
                }
            }
        }
    }

    private void OpenAimWindow(AimState state, Vector3 currentMovement, bool currentSprint)
    {
        currentAimState = state;
        preAimMovement = currentMovement;
        preAimSprint = currentSprint;
        shootAimSlot = 0;
        aimDirection = playerBase.FacingDirection;
    }

    private void ExecuteKick()
    {
        switch (currentAimState)
        {
            case AimState.Shooting:
                playerBase.ShootAtGoal(shootAimSlot, 1.0f); // Always full power
                break;
            case AimState.Passing:
                {
                    PlayerBase target = FindBestPassTarget(aimDirection);
                    if (target != null)
                    {
                        Vector3 toTarget = (target.transform.position - transform.position);
                        toTarget.y = 0f;
                        playerBase.ShortPass(toTarget.normalized, 1.0f); // Always full power
                    }
                    else
                    {
                        // No teammate in that direction — just kick raw
                        playerBase.ShortPass(aimDirection, 1.0f);
                    }
                }
                break;
            case AimState.Lobbing:
                {
                    PlayerBase target = FindBestPassTarget(aimDirection);
                    if (target != null)
                    {
                        Vector3 toTarget = (target.transform.position - transform.position);
                        toTarget.y = 0f;
                        playerBase.LobPass(toTarget.normalized);
                    }
                    else
                    {
                        playerBase.LobPass(aimDirection);
                    }
                }
                break;
        }
    }

    private void ResetAimWindow()
    {
        currentAimState = AimState.None;
        shootAimSlot = 0;
    }

    /// <summary>
    /// Finds the best teammate to pass to based on the aimed direction.
    /// Picks the teammate closest to the intended direction within a 90° cone.
    /// If two are at similar angles, prefers the closer one.
    /// </summary>
    private PlayerBase FindBestPassTarget(Vector3 aimDir)
    {
        PlayerBase[] allPlayers = FindObjectsByType<PlayerBase>(FindObjectsSortMode.None);
        PlayerBase bestTarget = null;
        float bestScore = float.MinValue;

        foreach (PlayerBase p in allPlayers)
        {
            // Skip self and opponents
            if (p == playerBase || p.TeamId != playerBase.TeamId) continue;

            Vector3 toTeammate = p.transform.position - transform.position;
            toTeammate.y = 0f;
            float dist = toTeammate.magnitude;

            if (dist < 0.5f) continue; // Too close, skip

            float angle = Vector3.Angle(aimDir, toTeammate.normalized);

            // Only consider teammates within a 90 degree cone of the aim direction
            if (angle > 90f) continue;

            // Score: strongly prefer smaller angle, slightly prefer closer distance
            float score = 100f - (angle * 2f) - (dist * 0.3f);

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = p;
            }
        }

        return bestTarget;
    }
}

