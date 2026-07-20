using UnityEngine;
using System.Collections.Generic;

public class AIPassingEvaluator : MonoBehaviour
{
    public static AIPassingEvaluator Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Struct containing evaluation results for a passing candidate.
    /// </summary>
    public struct PassOption
    {
        public PlayerBase TargetPlayer;
        public float Score;
        public Vector3 PassDirection; // Direction to kick (either direct or toward wall reflection point)
        public bool IsBankPass;
    }

    /// <summary>
    /// Evaluates all teammates for a passer and returns the best pass option.
    /// If no viable options exist (all scores below threshold), returns option with TargetPlayer = null.
    /// </summary>
    public PassOption EvaluateBestPass(PlayerBase passer, float minScoreThreshold = -100f)
    {
        PlayerBase[] allPlayers = FindObjectsByType<PlayerBase>(FindObjectsSortMode.None);
        List<PlayerBase> teammates = new List<PlayerBase>();
        List<PlayerBase> opponents = new List<PlayerBase>();

        foreach (PlayerBase p in allPlayers)
        {
            if (p == passer) continue;

            if (p.TeamId == passer.TeamId)
            {
                // GK is a valid passing target, but we should penalize passing to GK slightly to avoid playing too defensively
                teammates.Add(p);
            }
            else
            {
                opponents.Add(p);
            }
        }

        PassOption bestOption = new PassOption { TargetPlayer = null, Score = float.MinValue };

        Vector3 goalCenter = ShootingHelper.GetGoalTarget(passer.TeamId, 0);

        foreach (PlayerBase teammate in teammates)
        {
            float score = 100f;

            // 1. Proximity to opponent's goal
            float distToGoal = Vector3.Distance(teammate.transform.position, goalCenter);
            score += (28f - distToGoal) * 2f; // Closer to goal = higher score (field width is 28)

            // 2. Openness (distance to nearest defender)
            float minOpponentDist = float.MaxValue;
            foreach (PlayerBase opp in opponents)
            {
                float d = Vector3.Distance(teammate.transform.position, opp.transform.position);
                if (d < minOpponentDist) minOpponentDist = d;
            }
            score += Mathf.Min(minOpponentDist * 5f, 40f); // Reward openness up to 40 points

            // 3. Backward Pass Penalty
            float passerDistToGoal = Vector3.Distance(passer.transform.position, goalCenter);
            if (distToGoal > passerDistToGoal)
            {
                score -= 35f; // Penalize passing backwards
            }

            // 4. Goalkeeper pass penalty (avoid spamming GK passes)
            if (teammate.Role == "GK")
            {
                score -= 20f;
            }

            // 5. Lane Clearness & Bank Pass Check
            Vector3 passDir = (teammate.transform.position - passer.transform.position).normalized;
            bool directClear = IsLaneClear(passer.transform.position, teammate.transform.position, opponents);
            bool isBank = false;

            if (!directClear)
            {
                // Direct lane is blocked, check bank pass off top/bottom walls (z = 8 and z = -8)
                Vector3 bankDir = Vector3.zero;
                bool bankClear = EvaluateBankPass(passer.transform.position, teammate.transform.position, opponents, out bankDir);
                if (bankClear)
                {
                    passDir = bankDir;
                    score -= 15f; // Small penalty for bank pass complexity
                    isBank = true;
                }
                else
                {
                    // Disqualify candidate if both direct and bank lanes are blocked
                    score = -999f;
                }
            }

            if (score > bestOption.Score)
            {
                bestOption.TargetPlayer = teammate;
                bestOption.Score = score;
                bestOption.PassDirection = passDir;
                bestOption.IsBankPass = isBank;
            }
        }

        if (bestOption.Score < minScoreThreshold)
        {
            bestOption.TargetPlayer = null;
        }

        return bestOption;
    }

    /// <summary>
    /// Checks if a straight path between two points is clear of opponent defenders.
    /// </summary>
    private bool IsLaneClear(Vector3 start, Vector3 end, List<PlayerBase> opponents, float clearanceRadius = 1.0f)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        Vector3 normalizedDir = direction / distance;

        foreach (PlayerBase opp in opponents)
        {
            Vector3 startToOpp = opp.transform.position - start;
            float projection = Vector3.Dot(startToOpp, normalizedDir);

            if (projection > 0f && projection < distance)
            {
                Vector3 closestPoint = start + normalizedDir * projection;
                float distToLine = Vector3.Distance(opp.transform.position, closestPoint);

                if (distToLine < clearanceRadius)
                {
                    return false; // Blocked
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Evaluates if a bank pass off the top or bottom wall is clear of opponents.
    /// Returns true if a clear bank path is found, and outputs the initial kick direction.
    /// </summary>
    private bool EvaluateBankPass(Vector3 start, Vector3 end, List<PlayerBase> opponents, out Vector3 kickDirection)
    {
        kickDirection = Vector3.zero;

        // Wall Z coordinates
        float[] wallZs = new float[] { 7.6f, -7.6f }; // Slight offset inward from 8.0f to avoid ball sticking to wall

        foreach (float wallZ in wallZs)
        {
            float dStart = Mathf.Abs(start.z - wallZ);
            float dEnd = Mathf.Abs(end.z - wallZ);
            
            // Calculate reflection X coordinate
            float t = dStart / (dStart + dEnd);
            float reflectX = start.x + t * (end.x - start.x);

            Vector3 reflectPoint = new Vector3(reflectX, start.y, wallZ);

            // Check if reflection point is within court boundaries along the X axis
            if (reflectX > -13.5f && reflectX < 13.5f)
            {
                // Verify path from start to reflection point, and reflection point to end is clear
                bool segment1Clear = IsLaneClear(start, reflectPoint, opponents, 0.85f);
                bool segment2Clear = IsLaneClear(reflectPoint, end, opponents, 0.85f);

                if (segment1Clear && segment2Clear)
                {
                    kickDirection = (reflectPoint - start).normalized;
                    return true;
                }
            }
        }

        return false;
    }
}
