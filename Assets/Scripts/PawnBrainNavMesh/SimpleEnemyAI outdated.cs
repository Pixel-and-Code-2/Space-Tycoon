using UnityEngine;
using UnityEngine.AI;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PawnDataController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AnimatorBrainEnemy))]
public class SimpleEnemyAI : MonoBehaviour, IAttackableSelectable
{
    public bool IsAttackable => true;
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

        // 06.02 AlbionVisual: EnemyAnimations
        anim = GetComponent<Animator>();
        animator = GetComponent<AnimatorBrainEnemy>();
        animator.Initialize(1, EnemyAnimations.IDLE, anim, (layer) => animator.Play(EnemyAnimations.IDLE, layer, false, true));
        animator.Play(EnemyAnimations.IDLE, 0, false, false);

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
        // 1. ���� ������ � �������� ����� ��������� ���� �� ���� (out pathDistance)
        PawnNavMesh closestPlayer = FindClosestPlayer(out float pathDistance);

        if (closestPlayer != null)
        {
            // 2. ���������� aggressionRange � �������� ����, � �� ��������
            if (pathDistance <= aggressionRange)
            {
                agent.SetDestination(closestPlayer.transform.position);
                animator.Play(EnemyAnimations.WALK, 0, false, false);
            }
        }
    }

    // ����� ������ ���������� � �����, � ����� ���� �� ��
    private PawnNavMesh FindClosestPlayer(out float shortestDistance)
    {
        PawnNavMesh[] players = FindObjectsByType<PawnNavMesh>(FindObjectsSortMode.None)
            .Where(p => p.CompareTag("Player")).ToArray();

        shortestDistance = float.MaxValue;

        if (players.Length == 0) return null;

        PawnNavMesh closestPlayer = null;
        NavMeshPath path = new NavMeshPath(); // ������� ���� ���, ����� �� ��������

        foreach (var player in players)
        {
            ISelectable pl = player.gameObject.GetComponent<ISelectable>();
            if (pl == null || pl.GetSelectableType() != SelectableType.Player) continue;
            // ������� ���� �� ����������� ������
            if (agent.CalculatePath(player.transform.position, path))
            {
                // (�����������) ���������� ���, �� ���� ������ ����� ���������
                if (path.status != NavMeshPathStatus.PathComplete) continue;

                // ������� ����� �� ����� ����
                float distance = CalculatePathLength(path);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestPlayer = player;
                }
            }
        }
        attackTarget = closestPlayer.gameObject.GetComponent<IAttackableSelectable>();
        return closestPlayer;
    }

    // ��������������� ����� ��� ������� ������� ����� NavMesh ����
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

    public void OnGetHit(float damage)
    {
        float newHealth = dataController.GetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY) - damage;
        if (newHealth <= 0f)
        {
            // Debug.Log("Dying" + name + " " + damage);
            animator.Play(EnemyAnimations.DEATH, 0, true, true);
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

    // 06.02 AlbionVisual: EnemyAnimations
    private AnimatorBrainEnemy animator;
    private Animator anim;
    private IAttackableSelectable attackTarget;

    void Update()
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    HandleDestinationReached();
                }
            }
        }
    }

    void HandleDestinationReached()
    {
        if (attackTarget != null)
        {
            animator.Play(EnemyAnimations.ATTACK, 0, true, false);
            float randomValue = Random.value;
            if (randomValue < 0.5)
            {
                attackTarget.OnGetHit(6.66f);
            }
        }
        attackTarget = null;
    }


}