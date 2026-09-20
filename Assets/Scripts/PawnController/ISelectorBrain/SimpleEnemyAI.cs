using System.Collections.Generic;
using UnityEngine;

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
    public static SimpleEnemyAI Instance { get; private set; }

    [System.Serializable]
    public class StageRule
    {
        [Tooltip("While first non-Done main task index < this value, use these profiles")]
        public int untilMainTaskIndex = 999;
        public EnemyAiProfile meleeProfile;
        public EnemyAiProfile shooterProfile;
        public EnemyAiProfile tankProfile;
    }

    class DetailedScenarioElement
    {
        public DetailedScenarioElementType type;
        public IControlableSelectable controlledPawn;
        public IControlableSelectable targetPawn;
        public Vector3 position;
    }

    [SerializeField]
    private IPawnState meleeState;
    [SerializeField]
    private IPawnState walkState;
    [SerializeField]
    private IPawnState shootState;
    [Header("AI profiles")]
    [SerializeField]
    private EnemyAiProfile defaultProfile;
    [SerializeField]
    private List<StageRule> stageRules = new List<StageRule>();
    private List<DetailedScenarioElement> detailedScenario = new List<DetailedScenarioElement>();
    private int currentScenarioIndex = -1;
    private int completedScenarioIndex = -2;
    private int currentScenarioIndexBeforeUpdate = -1;
    public override bool SyncUI => false;
    private const string UNIQUE_ID = "EnemyAI";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Debug.LogError("Constructor met second SimpleEnemyAI instance");
        meleeState = GetComponent<MeleeState>();
        walkState = GetComponent<WalkState>();
        shootState = GetComponent<ShootState>();
    }

    void Start()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnEnemyTurnStart += OnEnemyTurnStart;
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnSave += OnSaveData;
            SaveHub.Instance.OnLoad += OnLoadData;
        }
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnEnemyTurnStart -= OnEnemyTurnStart;
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnLoad -= OnLoadData;
            SaveHub.Instance.OnSave -= OnSaveData;
        }
    }

    private float timeStack = 0.0f;
    private bool attackStepSawBusy;
    private float attackStepElapsed;
    private float moveWaitElapsed;
    [SerializeField]
    private float moveWaitTimeout = 8f;
    void Update()
    {
        if (UILayersController.Instance == null
            || UILayersController.Instance.overlayStack.Count == 0
            || UILayersController.Instance.overlayStack.Peek() != UILayersController.UILayer.GameUI)
            return;
        currentScenarioIndexBeforeUpdate = currentScenarioIndex;

        if (completedScenarioIndex == -2) return;
        if (currentScenarioIndex >= detailedScenario.Count)
        {
            completedScenarioIndex = -2;
            currentScenarioIndex = -1;
            if (TurnManager.Instance != null)
                TurnManager.Instance.EndEnemyTurn();
            return;
        }
        if (completedScenarioIndex + 1 == currentScenarioIndex)
        {
            if (detailedScenario[currentScenarioIndex].type != DetailedScenarioElementType.WaitMovement)
                timeStack = 0.0f;
            switch (detailedScenario[currentScenarioIndex].type)
            {
                case DetailedScenarioElementType.MovePawn:
                    completedScenarioIndex++;
                    moveWaitElapsed = 0f;
                    break;
                case DetailedScenarioElementType.WaitMovement:
                    WaitMovement(detailedScenario[currentScenarioIndex]);
                    break;
                case DetailedScenarioElementType.AttackPawn:
                    bool busy = PhysicalDiceRoller.IsBusy
                        || (CombatAttackRunner.Instance != null && CombatAttackRunner.Instance.IsBusy);
                    if (busy)
                    {
                        attackStepSawBusy = true;
                        attackStepElapsed = 0f;
                        break;
                    }
                    attackStepElapsed += Time.deltaTime;
                    if (!attackStepSawBusy && attackStepElapsed < 0.2f)
                        break;
                    attackStepSawBusy = false;
                    attackStepElapsed = 0f;
                    completedScenarioIndex++;
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
                Debug.LogError("Enemy AI step timeout, skipping");
                currentScenarioIndex++;
            }
        }
    }

    public override IControlableSelectable PollSelectPawn(IControlableSelectable defaultPawn)
    {
        if (currentScenarioIndex == completedScenarioIndex && currentScenarioIndex >= 0 && currentScenarioIndex < detailedScenario.Count)
        {
            switch (detailedScenario[currentScenarioIndex].type)
            {
                case DetailedScenarioElementType.SelectPawn:
                    currentScenarioIndex++;
                    return detailedScenario[currentScenarioIndex - 1].controlledPawn;
                case DetailedScenarioElementType.DeselectPawn:
                    currentScenarioIndex++;
                    return null;
                default:
                    return defaultPawn;
            }
        }
        return defaultPawn;
    }

    public override (ISelectable selectable, Vector3 worldPoint) PollSelectPosForState()
    {
        if (currentScenarioIndex != completedScenarioIndex
            || currentScenarioIndex < 0
            || currentScenarioIndex >= detailedScenario.Count)
            return (null, Vector3.zero);

        var el = detailedScenario[currentScenarioIndex];
        switch (el.type)
        {
            case DetailedScenarioElementType.MovePawn:
                currentScenarioIndex++;
                if (el.controlledPawn == null || !el.controlledPawn.IsAlive)
                    return (null, Vector3.zero);
                if (el.position != Vector3.zero)
                    return (null, el.position);
                if (el.targetPawn != null && el.targetPawn.IsAlive)
                    return (null, el.targetPawn.GetTransform().position);
                return (null, Vector3.zero);
            case DetailedScenarioElementType.AttackPawn:
                currentScenarioIndex++;
                if (el.controlledPawn == null || !el.controlledPawn.IsAlive)
                    return (null, Vector3.zero);
                IControlableSelectable target = el.targetPawn;
                if (target == null || !target.IsAlive)
                {
                    EnemyAiProfile profile = ResolveProfile(el.controlledPawn);
                    target = EnemyAiDecide.PickAlternateAttackTarget(el.controlledPawn, el.targetPawn, profile);
                }
                if (target != null && target.IsAlive)
                    return (target, target.GetTransform().position);
                return (null, Vector3.zero);
            default:
                return (null, Vector3.zero);
        }
    }

    public override IPawnState PollChangeState()
    {
        if (currentScenarioIndex != completedScenarioIndex
            || currentScenarioIndex < 0
            || currentScenarioIndex >= detailedScenario.Count)
            return null;

        switch (detailedScenario[currentScenarioIndex].type)
        {
            case DetailedScenarioElementType.SetMoveState:
                currentScenarioIndex++;
                return walkState;
            case DetailedScenarioElementType.SetAttackState:
                currentScenarioIndex++;
                return shootState != null ? shootState : GetComponent<ShootState>();
            default:
                return null;
        }
    }

    public override void SetClickAsUnhandled()
    {
        currentScenarioIndex = currentScenarioIndexBeforeUpdate;
    }

    void OnEnemyTurnStart()
    {
        BuildDetailedScenario();
        attackStepSawBusy = false;
        attackStepElapsed = 0f;
        moveWaitElapsed = 0f;
        if (detailedScenario.Count == 0)
        {
            completedScenarioIndex = -2;
            currentScenarioIndex = -1;
            TurnManager.Instance.EndEnemyTurn();
            return;
        }
        currentScenarioIndex = 0;
        completedScenarioIndex = -1;
    }

    private void OnSaveData(System.Action<SaveRecord[], string> addSaveData)
    {
        addSaveData(new SaveRecord[] {
            new SaveRecord() { recordName = "CompletedScenarioIndex", recordType = SaveRecordType.integerNumber, intValue = completedScenarioIndex },
            new SaveRecord() { recordName = "CurrentScenarioIndex", recordType = SaveRecordType.integerNumber, intValue = currentScenarioIndex }
        }, UNIQUE_ID);
    }

    private void OnLoadData(LoadedData data)
    {
        completedScenarioIndex = data.GetData("CompletedScenarioIndex", UNIQUE_ID, -2);
        currentScenarioIndex = data.GetData("CurrentScenarioIndex", UNIQUE_ID, -1);
    }

    public EnemyAiProfile ResolveProfile(IControlableSelectable actor)
    {
        PawnDataController data = actor != null ? actor.GetComponent<PawnDataController>() : null;
        if (data != null && data.AiProfileOverride != null)
            return data.AiProfileOverride;

        EnemyAiRole role = EnemyAiDecide.InferRole(data, defaultProfile);
        int frontier = GetMainTaskFrontier();
        if (stageRules != null)
        {
            StageRule best = null;
            for (int i = 0; i < stageRules.Count; i++)
            {
                StageRule rule = stageRules[i];
                if (rule == null) continue;
                if (frontier < rule.untilMainTaskIndex)
                {
                    if (best == null || rule.untilMainTaskIndex < best.untilMainTaskIndex)
                        best = rule;
                }
            }
            if (best != null)
            {
                EnemyAiProfile picked = PickStageProfile(best, role);
                if (picked != null) return picked;
            }
        }
        return defaultProfile;
    }

    static EnemyAiProfile PickStageProfile(StageRule rule, EnemyAiRole role)
    {
        if (role == EnemyAiRole.Tank && rule.tankProfile != null) return rule.tankProfile;
        if (role == EnemyAiRole.Shooter && rule.shooterProfile != null) return rule.shooterProfile;
        if (role == EnemyAiRole.Melee && rule.meleeProfile != null) return rule.meleeProfile;
        if (rule.meleeProfile != null) return rule.meleeProfile;
        if (rule.shooterProfile != null) return rule.shooterProfile;
        return rule.tankProfile;
    }

    public static int GetMainTaskFrontier()
    {
        if (ClickableItemsController.Instance == null || ClickableItemsController.Instance.mainTaskScenario == null)
            return 0;
        var main = ClickableItemsController.Instance.mainTaskScenario;
        for (int i = 0; i < main.Count; i++)
        {
            if (main[i].status != ClickableItemsController.TaskItem.TaskItemStatus.Done)
                return i;
        }
        return main.Count;
    }

    private void BuildDetailedScenario()
    {
        detailedScenario.Clear();
        IControlableSelectable actor = TurnManager.Instance != null ? TurnManager.Instance.CurrentActor : null;
        if (actor == null || actor.GetSelectableType() != SelectableType.Enemy)
            return;
        if (!actor.IsInActiveTriggerZone())
            return;

        EnemyAiProfile profile = ResolveProfile(actor);
        EnemyAiDecide.Decision decision = EnemyAiDecide.Decide(actor, profile);

        AddStep(DetailedScenarioElementType.SelectPawn, actor, decision.target, decision.moveTo);
        switch (decision.intent)
        {
            case EnemyAiDecide.Intent.Move:
                AddStep(DetailedScenarioElementType.SetMoveState, actor, decision.target, decision.moveTo);
                AddStep(DetailedScenarioElementType.MovePawn, actor, decision.target, decision.moveTo);
                AddStep(DetailedScenarioElementType.WaitMovement, actor, decision.target, decision.moveTo);
                break;
            case EnemyAiDecide.Intent.Attack:
                if (decision.target != null)
                {
                    int attacks = Mathf.Max(1, decision.attackCount);
                    AddStep(DetailedScenarioElementType.SetAttackState, actor, decision.target, Vector3.zero);
                    for (int i = 0; i < attacks; i++)
                        AddStep(DetailedScenarioElementType.AttackPawn, actor, decision.target, Vector3.zero);
                    if (decision.retreatAfterAttack && decision.retreatTo != Vector3.zero)
                    {
                        AddStep(DetailedScenarioElementType.SetMoveState, actor, decision.target, decision.retreatTo);
                        AddStep(DetailedScenarioElementType.MovePawn, actor, decision.target, decision.retreatTo);
                        AddStep(DetailedScenarioElementType.WaitMovement, actor, decision.target, decision.retreatTo);
                    }
                }
                break;
            default:
                break;
        }
        AddStep(DetailedScenarioElementType.DeselectPawn, actor, null, Vector3.zero);
    }

    void AddStep(DetailedScenarioElementType type, IControlableSelectable controlled, IControlableSelectable target, Vector3 position)
    {
        detailedScenario.Add(new DetailedScenarioElement
        {
            type = type,
            controlledPawn = controlled,
            targetPawn = target,
            position = position
        });
    }

    private void WaitMovement(DetailedScenarioElement element)
    {
        if (element.controlledPawn == null)
        {
            moveWaitElapsed = 0f;
            completedScenarioIndex++;
            currentScenarioIndex++;
            return;
        }

        moveWaitElapsed += Time.deltaTime;

        if (element.controlledPawn.IsMoving()
            && element.targetPawn != null
            && element.targetPawn.IsAlive)
        {
            PawnDataController data = element.controlledPawn.PawnData;
            if (data != null)
            {
                float reach = data.MeleeReach;
                float stopAt = Mathf.Max(0.4f, reach * 0.5f);
                float dist = Vector3.Distance(
                    element.controlledPawn.GetTransform().position,
                    element.targetPawn.GetTransform().position);
                if (dist <= stopAt + 0.05f)
                    element.controlledPawn.StopMove();
            }
        }

        bool done = !element.controlledPawn.IsMoving();
        if (!done && moveWaitElapsed >= moveWaitTimeout)
        {
            Debug.LogWarning(
                "[EnemyAI] move timeout "
                + moveWaitElapsed.ToString("F1")
                + "s, force stop "
                + element.controlledPawn.name);
            element.controlledPawn.StopMove();
            done = true;
        }

        if (done)
        {
            moveWaitElapsed = 0f;
            completedScenarioIndex++;
            currentScenarioIndex++;
        }
    }
}
