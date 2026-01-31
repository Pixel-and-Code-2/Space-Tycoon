using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class SimpleEnemyAI : MonoBehaviour
{
    private NavMeshAgent agent;

    [SerializeField]
    private float aggressionRange = 20.0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnEnemyTurnStart += ExecuteTurn;
        }
    }
    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnEnemyTurnStart -= ExecuteTurn;
        }
    }

    private void ExecuteTurn()
    {
        PawnNavMesh closestPlayer = FindClosestPlayer();
        if(closestPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, closestPlayer.transform.position);
            if (distanceToPlayer <= aggressionRange)
            {
                agent.SetDestination(closestPlayer.transform.position);
            }
        }
    }

    private PawnNavMesh FindClosestPlayer()
    {
        PawnNavMesh[] players = FindObjectsByType<PawnNavMesh>(FindObjectsSortMode.None)
            .Where(p => p.CompareTag("Player")).ToArray();
        if(players.Length == 0) return null;
        PawnNavMesh closestPlayer = null;
        float closestDistance = float.MaxValue;
        foreach (var player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }
        return closestPlayer; 
    }
}
