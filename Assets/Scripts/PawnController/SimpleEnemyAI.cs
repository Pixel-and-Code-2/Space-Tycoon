using UnityEngine;
using UnityEngine.AI;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PawnDataController))]
public class SimpleEnemyAI : MonoBehaviour, ISelectable
{
    private NavMeshAgent agent;
    private PawnDataController dataController;
    [SerializeField]
    private float aggressionRange = 20.0f;

    public bool IsShootable => true;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        dataController = GetComponent<PawnDataController>();
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
        if (closestPlayer != null)
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
        if (players.Length == 0) return null;
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

    public Transform GetTransform()
    {
        return transform;
    }

    public void OnDealDamage(float damage)
    {
        float newHealth = dataController.GetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY) - damage;
        if (newHealth <= 0f)
        {
            Debug.Log("Dying" + name + " " + damage);
            newHealth = 0f;
        }
        dataController.SetParameterValue(
            PawnDataController.AVAILABLE_HEALTH_KEY,
            newHealth
        );
    }

    public string GetHPText()
    {
        return $"{dataController.GetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY)} / {dataController.GetParameterValue(PawnDataController.INITIAL_HP_KEY)}";
    }

    public IFormulaData GetFormulaData()
    {
        return dataController;
    }
}
