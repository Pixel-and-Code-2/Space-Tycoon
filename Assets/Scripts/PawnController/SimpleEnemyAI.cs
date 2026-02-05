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
    [SerializeField]
    private float checkInterval = 2.0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        dataController = GetComponent<PawnDataController>();
        agent.autoBraking = true;
        agent.stoppingDistance = checkInterval;
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
        // 1. Ищем игрока и получаем сразу дистанцию пути до него (out pathDistance)
        PawnNavMesh closestPlayer = FindClosestPlayer(out float pathDistance);

        if (closestPlayer != null)
        {
            // 2. Сравниваем aggressionRange с реальным путём, а не радиусом
            if (pathDistance <= aggressionRange)
            {
                agent.SetDestination(closestPlayer.transform.position);
            }
        }
    }

    // Метод теперь возвращает и Пешку, и длину пути до неё
    private PawnNavMesh FindClosestPlayer(out float shortestDistance)
    {
        PawnNavMesh[] players = FindObjectsByType<PawnNavMesh>(FindObjectsSortMode.None)
            .Where(p => p.CompareTag("Player")).ToArray();

        shortestDistance = float.MaxValue;

        if (players.Length == 0) return null;

        PawnNavMesh closestPlayer = null;
        NavMeshPath path = new NavMeshPath(); // Создаем один раз, чтобы не мусорить

        foreach (var player in players)
        {
            // Считаем путь до конкретного игрока
            if (agent.CalculatePath(player.transform.position, path))
            {
                // (Опционально) Игнорируем тех, до кого нельзя дойти физически
                if (path.status != NavMeshPathStatus.PathComplete) continue;

                // Считаем длину по углам пути
                float distance = CalculatePathLength(path);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestPlayer = player;
                }
            }
        }
        return closestPlayer;
    }

    // Вспомогательный метод для точного расчета длины NavMesh пути
    private float CalculatePathLength(NavMeshPath path)
    {
        float length = 0f;
        if (path.corners.Length < 2) return 0f;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        return length;
    }

    // 05.02 AlbionVisual: ISelectable implementation
    public Transform GetTransform()
    {
        return transform;
    }

    public void OnDealDamage(float damage)
    {
        float newHealth = dataController.GetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY) - damage;
        if (newHealth <= 0f)
        {
            // Debug.Log("Dying" + name + " " + damage);
            selectableType = SelectableType.Dead;
            newHealth = 0f;
        }
        dataController.SetParameterValue(
            PawnDataController.AVAILABLE_HEALTH_KEY,
            newHealth
        );
    }

    public bool IsShootable => true;
    private SelectableType selectableType = SelectableType.Enemy;
    public SelectableType GetSelectableType() => selectableType;
    public string GetHPText()
    {
        return $"{dataController.GetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY)} / {dataController.GetParameterValue(PawnDataController.INITIAL_HP_KEY)}";
    }

    public IFormulaData GetFormulaData()
    {
        return dataController;
    }
}