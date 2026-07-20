using UnityEngine;

/// <summary>
/// Adds a motion trail behind the ball when it moves fast.
/// Automatically clears the trail on goal resets to prevent teleport streaks.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    [SerializeField] private float speedThreshold = 15f;
    [SerializeField] private float trailTime = 0.15f;
    [SerializeField] private float trailStartWidth = 0.12f;
    [SerializeField] private float trailEndWidth = 0f;
    [SerializeField] private Color trailColor = new Color(1f, 1f, 1f, 0.7f);

    private TrailRenderer trailRenderer;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        CreateTrailRenderer();
    }

    private void OnEnable()
    {
        // Subscribe to goal events to clear the trail before position reset
        GoalDetector.OnGoalScored += OnGoalScored;
    }

    private void OnDisable()
    {
        GoalDetector.OnGoalScored -= OnGoalScored;
    }

    private void CreateTrailRenderer()
    {
        trailRenderer = gameObject.AddComponent<TrailRenderer>();
        trailRenderer.time = trailTime;
        trailRenderer.startWidth = trailStartWidth;
        trailRenderer.endWidth = trailEndWidth;
        trailRenderer.numCapVertices = 3;
        trailRenderer.numCornerVertices = 3;
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;
        trailRenderer.allowOcclusionWhenDynamic = false;

        // Use an unlit material for consistent trail appearance
        Material trailMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (trailMat == null)
        {
            trailMat = new Material(Shader.Find("Unlit/Color"));
        }
        trailMat.color = trailColor;
        trailRenderer.material = trailMat;

        // Gradient: white at start, transparent at end
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trailRenderer.colorGradient = gradient;

        // Start disabled — will enable when ball is fast
        trailRenderer.emitting = false;
    }

    private void Update()
    {
        if (trailRenderer == null || rb == null) return;

        // Only emit trail when the ball is moving fast enough
        float speed = rb.linearVelocity.magnitude;
        trailRenderer.emitting = speed > speedThreshold;
    }

    /// <summary>
    /// Clears the trail immediately. Call this before any ball teleport/reset
    /// to prevent a visible streak across the court.
    /// </summary>
    public void ClearTrail()
    {
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }

    private void OnGoalScored(int scoringTeamId)
    {
        // Clear trail immediately when a goal is scored, before the ball gets reset
        ClearTrail();
    }
}
