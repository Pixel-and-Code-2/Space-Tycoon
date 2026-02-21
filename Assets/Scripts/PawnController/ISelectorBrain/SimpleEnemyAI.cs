using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EnemyCapabilities
{
    // you can use all whenever you want, if the enemy is too far you just won't shoot or attack. If you want to go further, than pawn can move, but you'll go only available part of path.
    Move,
    Melee,
    WaitMovement
}

public enum DetailedScenarioElementType
{
    SelectPawn,
    DeselectPawn,
    MovePawn,
    SetMoveState,
    AttackPawn,
    SetAttackState,
    WaitMovement
}

public class SimpleEnemyAI : ISelectorBrain
{
    // controlledPawn not null
    // targetPawn null when you want to hardcoded vector instead or to find closest target
    // position Vector3.zero when you want to use "closest target" feature instead, otherwise hardcode position
    [System.Serializable]
    public class ScenarioElement
    {
        [SerializeField]
        public IControlableSelectable controlledPawn;
        [SerializeField]
        public IControlableSelectable targetPawn;
        [SerializeField]
        public Vector3 position;
    }
    [System.Serializable]
    public class EnemyScenarioElement : ScenarioElement
    {
        [SerializeField]
        public EnemyCapabilities capability;
    }
    public class DetailedScenarioElement : ScenarioElement
    {
        [SerializeField]
        public DetailedScenarioElementType type;
    }

    [SerializeField]
    private IPawnState meleeState;
    [SerializeField]
    private IPawnState walkState;
    [SerializeField]
    private List<EnemyScenarioElement> scenario = new List<EnemyScenarioElement>();
    private List<DetailedScenarioElement> detailedScenario = new List<DetailedScenarioElement>();
    private int currentScenarioIndex = -1; // -1 means no scenario is running, if this var is bigger than completedScenarioIndex, then local methods (not polls ones) are waiting for sth, otherwise scenario is stopped to make a poll return
    private int completedScenarioIndex = -2;

    void Awake()
    {
        meleeState = GetComponent<MeleeState>();
        walkState = GetComponent<WalkState>();
    }

    void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnEnemyTurnStart += OnEnemyTurnStart;
        }
    }
    void OnDestroy()
    {
        TurnManager.Instance.OnEnemyTurnStart -= OnEnemyTurnStart;
    }


    private float timeStack = 0.0f;
    void Update()
    {
        if (completedScenarioIndex == -2) return;
        if (currentScenarioIndex >= detailedScenario.Count)
        {
            completedScenarioIndex = -2;
            currentScenarioIndex = -1;
            TurnManager.Instance.EndEnemyTurn();
            return;
        }
        Debug.Log("SimpleEnemyAI Update " + currentScenarioIndex + " >= " + completedScenarioIndex);
        if (completedScenarioIndex + 1 == currentScenarioIndex)
        {
            timeStack = 0.0f;
            switch (detailedScenario[currentScenarioIndex].type)
            {
                case DetailedScenarioElementType.MovePawn:
                    MovePawn(detailedScenario[currentScenarioIndex]);
                    break;
                case DetailedScenarioElementType.WaitMovement:
                    WaitMovement(detailedScenario[currentScenarioIndex]);
                    break;
                case DetailedScenarioElementType.AttackPawn:
                    AttackPawn(detailedScenario[currentScenarioIndex]);
                    break;
                default:
                    completedScenarioIndex++;
                    break;
            }
        }
        if (completedScenarioIndex == currentScenarioIndex)
        {
            timeStack += Time.deltaTime;
            if (timeStack >= 3f)
            {
                Debug.LogError("Time stack is greater than 3 seconds, it's " + timeStack + " seconds... Sth wrong, skipping");
                currentScenarioIndex++;
            }
        }
        if (currentScenarioIndex != completedScenarioIndex + 1 && currentScenarioIndex != completedScenarioIndex)
        {
            Debug.LogError("Current scenario index is not valid, it's " + currentScenarioIndex + " when completed scenario index is " + completedScenarioIndex);
        }
    }

    public override IControlableSelectable PollSelectPawn()
    {
        if (currentScenarioIndex == completedScenarioIndex)
        {
            switch (detailedScenario[currentScenarioIndex].type)
            {
                case DetailedScenarioElementType.SelectPawn:
                    currentScenarioIndex++;
                    return detailedScenario[currentScenarioIndex].controlledPawn;
                case DetailedScenarioElementType.DeselectPawn:
                    currentScenarioIndex++;
                    return null;
                default:
                    return PawnController.Instance.currentSelectedPawn;
            }
        }
        else
        {
            return PawnController.Instance.currentSelectedPawn;
        }
    }

    public override (ISelectable selectable, Vector3 worldPoint) PollSelectPosForState()
    {
        if (currentScenarioIndex == completedScenarioIndex)
        {
            var el = detailedScenario[currentScenarioIndex];
            switch (el.type)
            {
                case DetailedScenarioElementType.MovePawn:
                    currentScenarioIndex++;
                    Vector3 answer = Vector3.zero;
                    if (el.targetPawn == null)
                    {
                        if (el.position == Vector3.zero)
                        {
                            answer = FindClosestPlayer(out float shortestDistance).GetTransform().position;
                        }
                        else
                        {
                            answer = el.position;
                        }
                    }
                    else
                    {
                        answer = el.targetPawn.GetTransform().position;
                    }
                    return (null, answer);
                case DetailedScenarioElementType.AttackPawn:
                    IControlableSelectable answer2;
                    currentScenarioIndex++;
                    if (el.targetPawn == null)
                    {
                        answer2 = FindClosestPlayer(out float shortestDistance);
                    }
                    else
                    {
                        answer2 = el.targetPawn;
                    }
                    return (answer2, answer2.GetTransform().position);
                default:
                    return (null, Vector3.zero);
            }
        }
        else
        {
            return (null, Vector3.zero);
        }
    }

    public override IPawnState PollChangeState()
    {
        if (currentScenarioIndex == completedScenarioIndex)
        {
            switch (detailedScenario[currentScenarioIndex].type)
            {
                case DetailedScenarioElementType.SetMoveState:
                    currentScenarioIndex++;
                    return walkState;
                case DetailedScenarioElementType.SetAttackState:
                    currentScenarioIndex++;
                    return meleeState;
                default:
                    return null;
            }
        }
        else
        {
            return null;
        }
    }

    void OnEnemyTurnStart()
    {
        currentScenarioIndex = 0;
        completedScenarioIndex = -1;
        BuildDetailedScenario();
        Debug.Log("SimpleEnemyAI OnEnemyTurnStart " + detailedScenario.Count);
    }

    private void BuildDetailedScenario()
    {
        detailedScenario.Clear();
        foreach (var element in scenario)
        {
            if (element.controlledPawn.GetSelectableType() != SelectableType.Enemy) continue;
            AddDetailedScenarioElement(DetailedScenarioElementType.SelectPawn, element);
            switch (element.capability)
            {
                case EnemyCapabilities.Move:
                    AddDetailedScenarioElement(DetailedScenarioElementType.SetMoveState, element);
                    AddDetailedScenarioElement(DetailedScenarioElementType.MovePawn, element);
                    break;
                case EnemyCapabilities.Melee:
                    AddDetailedScenarioElement(DetailedScenarioElementType.SetAttackState, element);
                    AddDetailedScenarioElement(DetailedScenarioElementType.AttackPawn, element);
                    break;
                case EnemyCapabilities.WaitMovement:
                    AddDetailedScenarioElement(DetailedScenarioElementType.WaitMovement, element);
                    break;
            }
            AddDetailedScenarioElement(DetailedScenarioElementType.DeselectPawn, element);
        }
    }

    private void AddDetailedScenarioElement(DetailedScenarioElementType type, ScenarioElement element)
    {
        if (element.controlledPawn == null)
        {
            Debug.LogError("Controlled pawn is null, make sure you select one everywhere");
            return;
        }
        DetailedScenarioElement detailedElement = new DetailedScenarioElement();
        detailedElement.type = type;
        detailedElement.controlledPawn = element.controlledPawn;
        detailedElement.targetPawn = element.targetPawn;
        detailedElement.position = element.position;
        detailedScenario.Add(detailedElement);
    }

    private void MovePawn(ScenarioElement element)
    {
        completedScenarioIndex++;
    }

    private void WaitMovement(ScenarioElement element)
    {
        if (element.targetPawn == null || !element.targetPawn.IsMoving()) completedScenarioIndex++;
    }

    private void AttackPawn(ScenarioElement element)
    {
        completedScenarioIndex++;
    }

    private IControlableSelectable FindClosestPlayer(out float shortestDistance)
    {
        shortestDistance = float.MaxValue;

        IControlableSelectable controlledPawn = PawnController.Instance.currentSelectedPawn;
        if (controlledPawn == null)
        {
            return null;
        }

        PawnBrain[] pawns = FindObjectsByType<PawnBrain>(FindObjectsSortMode.None);
        PawnBrain[] players = pawns.Where(p => p.GetSelectableType() == SelectableType.Player).ToArray();
        IControlableSelectable[] controlablePlayers = players.Select(p => p as IControlableSelectable).ToArray();

        IControlableSelectable closestPlayer = null;
        foreach (var player in controlablePlayers)
        {
            (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) = controlledPawn.GetPathPointsTo(player.GetTransform().position);
            float distance = PawnDataController.CalculateLineStringDistance(pointsAvailable) + PawnDataController.CalculateLineStringDistance(pointsOutOfRange);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestPlayer = player;
            }
        }
        return closestPlayer;
    }
}