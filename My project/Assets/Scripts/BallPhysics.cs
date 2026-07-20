using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class BallPhysics : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 80f;
    [SerializeField] private float groundDrag = 0.5f;
    [SerializeField] private float airDrag = 0.1f;

    [Header("Shadow Projection")]
    [SerializeField] private Transform shadowTransform;
    [SerializeField] private float shadowScaleFactor = 0.8f;

    private Rigidbody rb;
    private SphereCollider ballCollider;
    private float initialShadowScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ballCollider = GetComponent<SphereCollider>();

        // Ensure collision detection is set to continuous to prevent wall tunneling
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (shadowTransform != null)
        {
            initialShadowScale = shadowTransform.localScale.x;
        }
    }

    private void FixedUpdate()
    {
        if (rb.isKinematic) return;

        // Apply custom drag based on whether the ball is grounded
        bool isGrounded = CheckGrounded();
        rb.linearDamping = isGrounded ? groundDrag : airDrag;

        // Cap maximum speed
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        // --- ROBUST BOUNDS CHECK (NEVER OUT OF BOUNDS) ---
        Vector3 pos = transform.position;
        Vector3 vel = rb.linearVelocity;
        bool clamped = false;
        float limitX = 20.6f;
        float limitZ = 11.6f;

        if (pos.x > limitX) { pos.x = limitX; vel.x = -Mathf.Abs(vel.x) * 0.7f; clamped = true; }
        if (pos.x < -limitX) { pos.x = -limitX; vel.x = Mathf.Abs(vel.x) * 0.7f; clamped = true; }
        if (pos.z > limitZ) { pos.z = limitZ; vel.z = -Mathf.Abs(vel.z) * 0.7f; clamped = true; }
        if (pos.z < -limitZ) { pos.z = -limitZ; vel.z = Mathf.Abs(vel.z) * 0.7f; clamped = true; }
        if (pos.y < 0.15f) { pos.y = 0.15f; if (vel.y < 0f) vel.y = -vel.y * 0.5f; clamped = true; }
        if (pos.y > 5.0f) { pos.y = 5.0f; if (vel.y > 0f) vel.y = -vel.y * 0.5f; clamped = true; }

        if (clamped)
        {
            transform.position = pos;
            rb.linearVelocity = vel;
        }

        // Update shadow position and size
        UpdateShadow(isGrounded);
    }

    /// <summary>
    /// Kicks the ball in a given direction at ground level (XZ plane).
    /// </summary>
    public void Kick(Vector3 direction, float power)
    {
        // Project direction to XZ plane and normalize
        Vector3 forceDirection = new Vector3(direction.x, 0f, direction.z).normalized;
        rb.linearVelocity = Vector3.zero; // Reset current momentum for crisp control
        rb.AddForce(forceDirection * power, ForceMode.Impulse);
    }

    /// <summary>
    /// Passes the ball at ground level (softer than a kick).
    /// </summary>
    public void Pass(Vector3 direction, float power)
    {
        Kick(direction, power);
    }

    /// <summary>
    /// Launches the ball in a vertical arc (3D Y-axis lift).
    /// </summary>
    public void Lob(Vector3 direction, float horizontalPower, float verticalPower)
    {
        Vector3 forceDirection = new Vector3(direction.x, 0f, direction.z).normalized;
        // Combine horizontal push with vertical lift
        Vector3 impulseVector = (forceDirection * horizontalPower) + (Vector3.up * verticalPower);
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(impulseVector, ForceMode.Impulse);
    }

    private bool CheckGrounded()
    {
        // Check if the ball is touching the floor (assuming floor has a collider on y = 0)
        // Adjust raycast distance slightly larger than the sphere radius
        float rayLength = ballCollider.radius + 0.1f;
        return Physics.Raycast(transform.position, Vector3.down, rayLength);
    }

    private void UpdateShadow(bool isGrounded)
    {
        if (shadowTransform == null) return;

        // Shadow always sits on the ground floor (y = 0 or slightly above to prevent z-fighting)
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 100f))
        {
            shadowTransform.position = hit.point + new Vector3(0f, 0.01f, 0f);
        }
        else
        {
            shadowTransform.position = new Vector3(transform.position.x, 0.01f, transform.position.z);
        }

        // Shrink the shadow as the ball gets higher
        float height = Mathf.Max(0f, transform.position.y - shadowTransform.position.y);
        float scaleMultiplier = Mathf.Max(0.2f, 1f - (height * shadowScaleFactor * 0.1f));
        shadowTransform.localScale = new Vector3(initialShadowScale * scaleMultiplier, 0.01f, initialShadowScale * scaleMultiplier);
    }


}
