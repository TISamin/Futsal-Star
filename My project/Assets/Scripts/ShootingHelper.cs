using UnityEngine;

/// <summary>
/// Utility class for shooting calculations: goal positions, accuracy curves, and aim deviation.
/// </summary>
public static class ShootingHelper
{
    // Goal X positions (court is 28 units wide, goals at the short ends)
    // Red team (teamId 0) attacks the +X goal (Blue's goal at x=14)
    // Blue team (teamId 1) attacks the -X goal (Red's goal at x=-14)
    // Goal X positions (court length is 42, goal wall faces at x = ±20.5)
    private static readonly float goalXPositive = 20.6f;
    private static readonly float goalXNegative = -20.6f;

    // Goal post Z positions (posts are at ±3.0f, aim inside at ±2.2f to score cleanly in the corners)
    private static readonly float nearPostZ = -2.2f;
    private static readonly float farPostZ = 2.2f;
    private static readonly float centerZ = 0f;
    private static readonly float goalY = 0.5f; // Ball height for shots

    /// <summary>
    /// Returns the world position of the target point on the opponent's goal.
    /// </summary>
    /// <param name="attackingTeamId">The team doing the shooting.</param>
    /// <param name="aimSlot">-1 = near post (left), 0 = center, 1 = far post (right)</param>
    public static Vector3 GetGoalTarget(int attackingTeamId, int aimSlot)
    {
        float goalX = (attackingTeamId == 0) ? goalXPositive : goalXNegative;

        float targetZ;
        if (aimSlot < 0)
            targetZ = nearPostZ;
        else if (aimSlot > 0)
            targetZ = farPostZ;
        else
            targetZ = centerZ;

        return new Vector3(goalX, goalY, targetZ);
    }

    /// <summary>
    /// Calculates shot accuracy (0.0 to 1.0) based on distance to the opponent's goal.
    /// Distance thresholds scaled 1.5 times.
    /// </summary>
    public static float CalculateAccuracy(float distanceToGoal)
    {
        if (distanceToGoal <= 10.5f)
        {
            // Very close range: 100% accuracy
            return 1.0f;
        }
        else if (distanceToGoal <= 15.0f)
        {
            // Close range: 100% to 90%
            float t = (distanceToGoal - 10.5f) / 4.5f;
            return Mathf.Lerp(1.0f, 0.90f, t);
        }
        else if (distanceToGoal <= 21.0f)
        {
            // Mid range: 90% to 50%
            float t = (distanceToGoal - 15.0f) / 6.0f;
            return Mathf.Lerp(0.90f, 0.50f, t);
        }
        else
        {
            // Long range: 50% to 20%
            float t = Mathf.Clamp01((distanceToGoal - 21.0f) / 21.0f);
            return Mathf.Lerp(0.50f, 0.20f, t);
        }
    }

    /// <summary>
    /// Applies accuracy-based deviation to a shot direction.
    /// Higher accuracy = less deviation. Returns the deviated direction.
    /// </summary>
    public static Vector3 ApplyAccuracyDeviation(Vector3 perfectDirection, float accuracy)
    {
        // Maximum deviation angle in degrees when accuracy is 0
        float maxDeviationDegrees = 25f;

        // Deviation scales with inaccuracy
        float deviationRange = maxDeviationDegrees * (1f - accuracy);

        // Random deviation on the horizontal plane
        float deviationAngle = Random.Range(-deviationRange, deviationRange);

        // Rotate the direction around Y axis
        Quaternion deviation = Quaternion.AngleAxis(deviationAngle, Vector3.up);
        return deviation * perfectDirection;
    }
}
