using UnityEngine;

public static class FormationData
{
    // Court bounds limits scaled 1.5 times (Court becomes 42x24, X in [-21, 21], Z in [-12, 12])
    public const float COURT_MAX_X = 21f;
    public const float COURT_MAX_Z = 12f;
    
    // Base positions for Team 0 (Red, defending X = -21, attacking X = +21)
    public static Vector3 GetBasePosition(string role)
    {
        switch (role)
        {
            case "GK":
                return new Vector3(-20f, 0f, 0f);
            case "Defender":
                return new Vector3(-10.5f, 0f, 0f);
            case "LeftMid":
                return new Vector3(-4.5f, 0f, -5.25f);
            case "RightMid":
                return new Vector3(-4.5f, 0f, 5.25f);
            case "Forward":
                return new Vector3(3.0f, 0f, 0f);
            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    /// Gets the dynamically shifted formation target position for a given team, role, and current ball position.
    /// </summary>
    public static Vector3 GetTargetPosition(int teamId, string role, Vector3 ballPosition)
    {
        // Get base position (as defined for Team 0)
        Vector3 basePos = GetBasePosition(role);

        // If Team 1, mirror the base position (both X and Z)
        if (teamId == 1)
        {
            basePos.x = -basePos.x;
            basePos.z = -basePos.z;
        }

        // GK does not shift with outfield players (GK has custom depth/lateral sliding rules in AIController)
        if (role == "GK")
        {
            return basePos;
        }

        // Calculate dynamic shift based on ball's X position
        // Outfield players shift forward/backward as the ball moves
        float shiftFactor = 0.45f; // Outfield players shift up/down court by 45% of ball position
        float shiftX = ballPosition.x * shiftFactor;

        // Apply shift
        Vector3 shiftedPos = basePos;
        shiftedPos.x += shiftX;

        // Clamping to keep players in bounds and in sensible zones (scaled 1.5 times)
        if (teamId == 0) // Red
        {
            // Don't shift too deep into own half or too deep into opponent half
            shiftedPos.x = Mathf.Clamp(shiftedPos.x, -19.5f, 18.5f);
        }
        else // Blue
        {
            shiftedPos.x = Mathf.Clamp(shiftedPos.x, -18.5f, 19.5f);
        }

        // Keep Z in court bounds with a margin
        shiftedPos.z = Mathf.Clamp(shiftedPos.z, -COURT_MAX_Z + 1.5f, COURT_MAX_Z - 1.5f);

        return shiftedPos;
    }
}
