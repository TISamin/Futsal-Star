using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerBase : MonoBehaviour
{
    [Header("Team & Role Settings")]
    [SerializeField] private int teamId; // 0 for Red, 1 for Blue
    [SerializeField] private string role = "Midfielder"; // GK, Defender, Midfielder, Forward

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3.2f; // Reduced speed (was 5f)
    [SerializeField] private float sprintSpeed = 5.2f; // Reduced speed (was 8f)
    [SerializeField] private float carrySpeedMultiplier = 0.85f;

    [Header("Kicking Settings")]
    [SerializeField] private float shootPower = 90f; // Super powerful shooting
    [SerializeField] private float passPower = 26f; // Faster passing (was 18f)
    [SerializeField] private float lobHorizontalPower = 18f; // Faster lob (was 12f)
    [SerializeField] private float lobVerticalPower = 10f; // Faster vertical lift (was 7f)

    private Rigidbody rb;
    private Vector3 facingDirection = Vector3.forward;
    private bool isSprinting;

    public int TeamId => teamId;
    public string Role => role;
    public bool IsSprinting => isSprinting;
    public Vector3 FacingDirection => facingDirection;
    public bool HasBall { get; set; } // Will be set by BallPossession system

    /// <summary>
    /// Programmatically initializes the player's team and role.
    /// </summary>
    public void Initialize(int team, string playerRole)
    {
        teamId = team;
        role = playerRole;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Lock rotation and Y-position on Rigidbody to prevent the player from falling or bouncing up
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    private Vector3 currentMoveDir;
    private float currentSpeed;

    /// <summary>
    /// Commands the player to move in a given XZ direction.
    /// Movement is applied in FixedUpdate to respect physics.
    /// </summary>
    public void MoveTo(Vector3 direction, bool sprint)
    {
        isSprinting = sprint;

        Vector3 moveDir = new Vector3(direction.x, 0f, direction.z);

        if (moveDir.sqrMagnitude > 0.001f)
        {
            moveDir.Normalize();
            facingDirection = moveDir;

            currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
            if (HasBall)
            {
                currentSpeed *= carrySpeedMultiplier;
            }

            currentMoveDir = moveDir;
        }
        else
        {
            currentMoveDir = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        if (currentMoveDir.sqrMagnitude > 0.001f)
        {
            // MovePosition handles wall sliding and collisions much better than setting velocity directly
            Vector3 targetPos = rb.position + currentMoveDir * currentSpeed * Time.fixedDeltaTime;
            
            // Hard clamp targets to keep players strictly inside the 1.5x expanded court bounds (limit ±21 and ±12)
            targetPos.x = Mathf.Clamp(targetPos.x, -20.4f, 20.4f);
            targetPos.z = Mathf.Clamp(targetPos.z, -11.4f, 11.4f);
            
            rb.MovePosition(targetPos);
        }
        else
        {
            // Stop horizontal momentum quickly, but keep falling
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    /// <summary>
    /// Teleports the player to a target position and resets physics velocity.
    /// </summary>
    public void ResetToPosition(Vector3 position)
    {
        transform.position = position;
        rb.position = position;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        currentMoveDir = Vector3.zero;
        facingDirection = (teamId == 0) ? Vector3.right : Vector3.left;
    }

    /// <summary>
    /// Shoots the ball toward the opponent's goal. Always perfectly accurate.
    /// </summary>
    /// <param name="aimSlot">-1 = near post, 0 = center, 1 = far post</param>
    /// <param name="powerPercent">Multiplier for shoot power</param>
    public void ShootAtGoal(int aimSlot, float powerPercent)
    {
        if (!HasBall || BallPossession.Instance == null) return;

        BallPhysics ballPhys = BallPossession.Instance.GetComponent<BallPhysics>();
        if (ballPhys == null) return;

        // Get the target point on the opponent's goal
        Vector3 goalTarget = ShootingHelper.GetGoalTarget(teamId, aimSlot);

        // Calculate perfect direction from player to goal target (no deviation)
        Vector3 shotDir = (goalTarget - transform.position).normalized;

        BallPossession.Instance.ReleaseBall(Vector3.zero);
        ballPhys.Kick(shotDir, shootPower * powerPercent);
    }

    /// <summary>
    /// Passes the ball in a specified direction with variable power.
    /// </summary>
    public void ShortPass(Vector3 aimDirection, float powerPercent)
    {
        if (!HasBall || BallPossession.Instance == null) return;

        BallPhysics ballPhys = BallPossession.Instance.GetComponent<BallPhysics>();
        if (ballPhys != null)
        {
            Vector3 dir = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : facingDirection;
            BallPossession.Instance.ReleaseBall(Vector3.zero);
            ballPhys.Pass(dir, passPower * powerPercent);
        }
    }

    /// <summary>
    /// Lobs the ball with vertical lift in a specified direction (no power scaling).
    /// </summary>
    public void LobPass(Vector3 aimDirection)
    {
        if (!HasBall || BallPossession.Instance == null) return;

        BallPhysics ballPhys = BallPossession.Instance.GetComponent<BallPhysics>();
        if (ballPhys != null)
        {
            Vector3 dir = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : facingDirection;
            BallPossession.Instance.ReleaseBall(Vector3.zero);
            ballPhys.Lob(dir, lobHorizontalPower, lobVerticalPower);
        }
    }
}
