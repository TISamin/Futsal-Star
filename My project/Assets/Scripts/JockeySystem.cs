using UnityEngine;

[RequireComponent(typeof(PlayerBase))]
public class JockeySystem : MonoBehaviour
{
    [Header("Jockey Settings")]
    [SerializeField] private float dislodgeImpulse = 8f;

    private PlayerBase playerBase;

    private void Awake()
    {
        playerBase = GetComponent<PlayerBase>();
    }

    [SerializeField] private float jockeyProximityRange = 1.3f; // Proximity tackle range

    private float tackleCooldown = 0f;

    private void Update()
    {
        if (MatchManager.Instance != null && !MatchManager.Instance.IsMatchPlaying) return;

        if (tackleCooldown > 0f)
        {
            tackleCooldown -= Time.deltaTime;
            return;
        }

        // Proximity jockey check: if we are near an opponent holding the ball, try to tackle them
        PlayerBase[] players = FindObjectsByType<PlayerBase>(FindObjectsSortMode.None);
        foreach (PlayerBase opponent in players)
        {
            if (opponent.TeamId != playerBase.TeamId && opponent.HasBall)
            {
                // Check flat XZ distance
                Vector3 myPos = transform.position;
                Vector3 oppPos = opponent.transform.position;
                myPos.y = 0;
                oppPos.y = 0;

                float dist = Vector3.Distance(myPos, oppPos);
                if (dist <= jockeyProximityRange)
                {
                    // Walking tackles are 100% successful. Sprinting tackles have a 30% chance per frame.
                    bool canSteal = !playerBase.IsSprinting || (Random.value < 0.3f);
                    if (canSteal)
                    {
                        // Dislodge the ball in the direction of the opponent
                        Vector3 dislodgeDir = (opponent.transform.position - transform.position).normalized;
                        dislodgeDir.y = 0.1f;
                        Vector3 impulse = dislodgeDir * dislodgeImpulse;

                        Debug.Log($"JOCKEY: Player {playerBase.name} dislodged the ball from opponent {opponent.name} via proximity!");
                        BallPossession.Instance.ReleaseBall(impulse);
                        tackleCooldown = 0.4f; // Prevent double-stealing instant feedback loop
                        break;
                    }
                }
            }
        }
    }
}
