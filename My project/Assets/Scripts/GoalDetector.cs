using System;
using UnityEngine;

public class GoalDetector : MonoBehaviour
{
    [Header("Goal Configuration")]
    [Tooltip("The ID of the team that defends this goal (e.g., 0 for Red Team, 1 for Blue Team). Scoring here awards points to the opposing team.")]
    [SerializeField] private int defendingTeamId;

    // Static event so MatchManager or UIManager can easily subscribe to goal scoring events
    public static event Action<int> OnGoalScored; // Parameter: teamId that scored the goal

    private void OnTriggerEnter(Collider other)
    {
        // Diagnostic log to see if the trigger is working at all and which detector fired it
        Debug.Log($"[GoalDetector on '{gameObject.name}'] Trigger entered by: '{other.name}', Tag: '{other.tag}', Layer: '{LayerMask.LayerToName(other.gameObject.layer)}'");

        // Check if the colliding object is the ball (checking self, parent, and attached Rigidbody)
        bool isBall = other.CompareTag("Ball") || 
                      other.GetComponent<BallPhysics>() != null || 
                      other.GetComponentInParent<BallPhysics>() != null ||
                      (other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<BallPhysics>() != null);

        if (isBall)
        {
            // If defendingTeamId is 0 (Red), then Blue (1) scored. If defendingTeamId is 1 (Blue), then Red (0) scored.
            int scoringTeamId = (defendingTeamId == 0) ? 1 : 0;
            
            Debug.Log($"GOAL! Team {scoringTeamId} scored in Team {defendingTeamId}'s goal!");
            
            // Fire event
            OnGoalScored?.Invoke(scoringTeamId);
        }
    }
}
