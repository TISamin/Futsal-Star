using UnityEngine;

public static class AIShootingEvaluator
{
    private const float SHOOTING_RANGE = 18f;

    /// <summary>
    /// Checks if a player is in range to shoot at the opponent's goal.
    /// </summary>
    public static bool IsInShootingRange(PlayerBase player)
    {
        Vector3 goalCenter = ShootingHelper.GetGoalTarget(player.TeamId, 0);
        float distance = Vector3.Distance(player.transform.position, goalCenter);
        return distance <= SHOOTING_RANGE;
    }

    /// <summary>
    /// Evaluates the best target slot (-1 near post, 0 center, 1 far post) on the opponent's goal.
    /// Returns the target slot and its score.
    /// </summary>
    public static int EvaluateBestShotSlot(PlayerBase shooter, out float bestScore)
    {
        int bestSlot = 0;
        bestScore = float.MinValue;

        int opponentTeamId = shooter.TeamId == 0 ? 1 : 0;

        // Find defending goalkeeper and other defenders to check blockages
        PlayerBase[] allPlayers = Object.FindObjectsByType<PlayerBase>(FindObjectsSortMode.None);
        PlayerBase defendingGK = null;
        System.Collections.Generic.List<PlayerBase> opposingDefenders = new System.Collections.Generic.List<PlayerBase>();

        foreach (PlayerBase p in allPlayers)
        {
            if (p.TeamId == opponentTeamId)
            {
                if (p.Role == "GK")
                {
                    defendingGK = p;
                }
                else
                {
                    opposingDefenders.Add(p);
                }
            }
        }

        // Test each of the three aim slots: -1 (near post), 0 (center), 1 (far post)
        for (int slot = -1; slot <= 1; slot++)
        {
            Vector3 targetPos = ShootingHelper.GetGoalTarget(shooter.TeamId, slot);
            Vector3 toTarget = targetPos - shooter.transform.position;
            float distToTarget = toTarget.magnitude;
            Vector3 dirToTarget = toTarget / distToTarget;

            // Start with a base score based on slot openness
            float score = 100f;

            // 1. Penalty for goalkeeper coverage:
            // If goalkeeper is close to this target slot, heavily penalize the score.
            if (defendingGK != null)
            {
                float gkDistToTarget = Vector3.Distance(defendingGK.transform.position, targetPos);
                // The closer the GK is to the target slot, the lower the score
                score += Mathf.Clamp(gkDistToTarget * 10f, 0f, 50f);
            }

            // 2. Penalty for defenders blocking the lane:
            // Raycast or check proximity of defenders to the shooting lane
            int blockCount = 0;
            foreach (PlayerBase defender in opposingDefenders)
            {
                // Project defender onto the shot line segment to find distance to the line
                Vector3 startToDef = defender.transform.position - shooter.transform.position;
                float projection = Vector3.Dot(startToDef, dirToTarget);

                if (projection > 0f && projection < distToTarget)
                {
                    Vector3 closestPoint = shooter.transform.position + dirToTarget * projection;
                    float distToLine = Vector3.Distance(defender.transform.position, closestPoint);

                    // If a defender is within 1.2 units of the direct line, it's considered a blocking hazard
                    if (distToLine < 1.2f)
                    {
                        blockCount++;
                    }
                }
            }

            score -= blockCount * 30f; // Deduct score for each blocking defender

            // 3. Angle penalty:
            // Shots from extreme angles are slightly penalized
            float angleOffset = Mathf.Abs(Vector3.Angle(shooter.FacingDirection, dirToTarget));
            score -= angleOffset * 0.5f;

            if (score > bestScore)
            {
                bestScore = score;
                bestSlot = slot;
            }
        }

        return bestSlot;
    }
}
