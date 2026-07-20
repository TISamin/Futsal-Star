using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BallPhysics))]
public class BallPossession : MonoBehaviour
{
    public static BallPossession Instance { get; private set; }

    [Header("Dribble Position Offset")]
    [SerializeField] private float dribbleDistance = 0.6f;
    [SerializeField] private float dribbleHeight = 0.1f;

    private PlayerBase currentPossessor;
    private PlayerBase lastPossessor;
    private float possessionCooldown;
    private Rigidbody ballRb;
    private Collider ballCollider;

    public PlayerBase CurrentPossessor => currentPossessor;

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

        ballRb = GetComponent<Rigidbody>();
        ballCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (MatchManager.Instance != null && !MatchManager.Instance.IsMatchPlaying)
        {
            // If someone has the ball, keep it at their feet, but don't do anything else (no pickup, no timer tick)
            if (currentPossessor != null)
            {
                Vector3 targetPosition = currentPossessor.transform.position + currentPossessor.FacingDirection * dribbleDistance;
                targetPosition.y = currentPossessor.transform.position.y - 0.4f + dribbleHeight;
                ballRb.MovePosition(targetPosition);
            }
            return;
        }

        // Decrement the possession cooldown timer
        if (possessionCooldown > 0f)
        {
            possessionCooldown -= Time.deltaTime;
        }

        if (currentPossessor != null)
        {
            // Position the ball at the feet of the player in their facing direction
            Vector3 targetPosition = currentPossessor.transform.position + currentPossessor.FacingDirection * dribbleDistance;
            
            // Adjust the height to align with ground contact (assuming player transform center is at pivot)
            targetPosition.y = currentPossessor.transform.position.y - 0.4f + dribbleHeight;

            ballRb.MovePosition(targetPosition);
        }
        else
        {
            // Don't do proximity pickup if the ball is a powerful shot flying through (speed > 45)
            if (ballRb.linearVelocity.magnitude > 45f) return;

            // Proximity claim check: if a loose ball is close to any player, they claim it immediately (makes passes snap to targets)
            PlayerBase[] players = TeamManager.AllPlayers;
            if (players == null) return;
            PlayerBase bestPlayer = null;
            float minDistance = 1.1f; // Pick up within 1.1 units (capsule radius 0.4 + ball radius 0.2 + small buffer)

            foreach (PlayerBase p in players)
            {
                // Ignore the kicker during possession cooldown
                if (p == lastPossessor && possessionCooldown > 0f) continue;

                // For intercepting/claiming, check flat XZ distance
                Vector3 playerPos = p.transform.position;
                Vector3 ballPos = transform.position;
                playerPos.y = 0;
                ballPos.y = 0;

                float dist = Vector3.Distance(playerPos, ballPos);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestPlayer = p;
                }
            }

            if (bestPlayer != null)
            {
                AssignPossession(bestPlayer);
            }
        }
    }

    /// <summary>
    /// Grants possession of the ball to a specific player.
    /// </summary>
    public void AssignPossession(PlayerBase player)
    {
        if (currentPossessor != null)
        {
            currentPossessor.HasBall = false;
        }

        currentPossessor = player;

        if (currentPossessor != null)
        {
            currentPossessor.HasBall = true;
            ballRb.isKinematic = true; // Turn off physics while dribbling
            if (ballCollider != null)
            {
                ballCollider.isTrigger = true; // Prevent the ball from physically pushing the player
            }

            // Instantly trigger control switch evaluation to prevent frame delays causing auto-passes
            if (ControlAssignment.Instance != null)
            {
                ControlAssignment.Instance.EvaluateControlSwitch(true);
            }
        }
        else
        {
            ballRb.isKinematic = false;
            if (ballCollider != null)
            {
                ballCollider.isTrigger = false;
            }
        }
    }

    /// <summary>
    /// Releases the ball back to physics control with an optional force.
    /// </summary>
    public void ReleaseBall(Vector3 impulseForce)
    {
        if (currentPossessor != null)
        {
            lastPossessor = currentPossessor;
            possessionCooldown = 0.25f; // Prevent the kicker from interacting with the ball for a split second

            currentPossessor.HasBall = false;
            currentPossessor = null;
        }

        ballRb.isKinematic = false;
        if (ballCollider != null)
        {
            ballCollider.isTrigger = false; // Restore physical collisions
        }

        if (impulseForce.sqrMagnitude > 0.001f)
        {
            ballRb.AddForce(impulseForce, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryClaimPossession(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryClaimPossession(collision);
    }

    private void TryClaimPossession(Collision collision)
    {
        if (MatchManager.Instance != null && !MatchManager.Instance.IsMatchPlaying) return;

        // Only grab ball if it's currently free
        if (currentPossessor != null) return;

        PlayerBase player = collision.gameObject.GetComponent<PlayerBase>();
        if (player != null)
        {
            // Ignore the kicker during the possession cooldown
            if (player == lastPossessor && possessionCooldown > 0f)
            {
                return;
            }

            // Always claim possession on contact (no recoil)
            AssignPossession(player);
        }
    }
}
