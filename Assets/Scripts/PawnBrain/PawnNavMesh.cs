using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PawnDataController))]
[RequireComponent(typeof(Collider))]
public class PawnNavMesh : MonoBehaviour
{
    private PawnDataController dataController;
    public NavMeshAgent navMeshAgent;
    public Collider col;
    private float pendingPathMeters = 0f;
    private Vector3 moveStartPos;
    private bool trackStaminaSpend = false;
    private Vector3 lastStaminaPos;
    [HideInInspector]
    public Vector3 targetPosition { get; private set; } = Vector3.zero;
    private bool isMoving = false;
    private bool softStopping = false;
    public event System.Action OnMoveStopped;
    public const float ArriveStoppingDistance = 0.08f;
    private Vector3 cachedTargetPosition = Vector3.zero;
    private Vector3[] cachedPointsAvailable = null;
    private Vector3[] cachedPointsOutOfRange = null;
    private bool cachedTargetPositionValid = false;
    private string UNIQUE_ID => "PawnNavMesh_" + gameObject.name;
    [System.Serializable]
    private class ScriptEnabler
    {
        public MonoBehaviour script;
        [Range(-1, 1)]
        public int onlyMyTurn = -1;
        [Range(-1, 1)]
        public int onlySelected = -1;
        [Range(-1, 1)]
        public int onlyOnMove = -1;
        [Range(-1, 1)]
        public int onlyOnDeath = -1;
        public bool CheckEnabled(int isMyTeamsTurn, int isSelected, int isMoving, int isDeath)
        {
            if (onlyMyTurn != -1 && isMyTeamsTurn != onlyMyTurn) return false;
            if (onlySelected != -1 && isSelected != onlySelected) return false;
            if (onlyOnMove != -1 && isMoving != onlyOnMove) return false;
            if (onlyOnDeath != -1 && isDeath != onlyOnDeath) return false;
            return true;
        }
    }
    [SerializeField]
    private ScriptEnabler[] scriptEnablers;

    private int isMyTeamsTurn = -1;
    private int isSelected = -1;
    private int isDeath = -1;
    public void SetTypeOfModifierVolumes(int isMyTeamsTurn = -1, int isSelected = -1, int isDeath = -1)
    {
        if (isDeath == 1)
        {
            if (navMeshAgent != null) navMeshAgent.enabled = false;
            gameObject.layer = LayerMask.NameToLayer("DeadPawn");
        }
        if (isDeath == 0)
        {
            if (isActiveAndEnabled)
                StartCoroutine(EnableNavAgentDeferred());
            else if (navMeshAgent != null)
                navMeshAgent.enabled = true;
        }
        if (isMyTeamsTurn != -1) this.isMyTeamsTurn = isMyTeamsTurn;
        if (isSelected != -1) this.isSelected = isSelected;
        if (isDeath != -1) this.isDeath = isDeath;
        foreach (var scriptEnabler in scriptEnablers)
        {
            bool newVal = scriptEnabler.CheckEnabled(this.isMyTeamsTurn, this.isSelected, isMoving ? 1 : 0, this.isDeath);
            if (scriptEnabler.script != null) scriptEnabler.script.enabled = newVal;
        }
    }

    private IEnumerator EnableNavAgentDeferred()
    {
        yield return null;
        if (navMeshAgent == null) yield break;
        Vector3 pos = transform.position;
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            pos = hit.position;
        navMeshAgent.enabled = true;
        if (navMeshAgent.isOnNavMesh)
            navMeshAgent.Warp(pos);
        else
        {
            navMeshAgent.enabled = false;
            transform.position = pos;
            navMeshAgent.enabled = true;
            if (navMeshAgent.isOnNavMesh)
                navMeshAgent.Warp(pos);
        }
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        dataController = GetComponent<PawnDataController>();
        col = GetComponent<Collider>();
    }

    void Start()
    {
        navMeshAgent.stoppingDistance = ArriveStoppingDistance;
        navMeshAgent.autoBraking = true;
        SaveHub.Instance.OnSave += OnSaveData;
        SaveHub.Instance.OnLoad += OnLoadData;
    }
    private void OnSaveData(System.Action<SaveRecord[], string> addSaveData)
    {
        SaveRecord[] records = new SaveRecord[isMoving ? 3 : 2];
        records[0] = new SaveRecord()
        {
            recordName = "Pos",
            recordType = SaveRecordType.vector,
            vecValue = transform.position
        };
        records[1] = new SaveRecord()
        {
            recordName = "Rot",
            recordType = SaveRecordType.quaternion,
            quatValue = transform.rotation
        };
        if (isMoving)
        {
            records[2] = new SaveRecord()
            {
                recordName = "Destination",
                recordType = SaveRecordType.vector,
                vecValue = navMeshAgent.destination
            };
        }
        addSaveData(records, UNIQUE_ID);
    }

    private void OnLoadData(LoadedData data)
    {
        SelectableType selectableType = (SelectableType)data.GetData("SelectableType", dataController.UNIQUE_ID, (int)dataController.selectableType);
        string selectableTypeKey = DataCompressor.GetRecordName("SelectableType", dataController.UNIQUE_ID);
        bool hasSelectableTypeInSave = data.intData != null && data.intData.ContainsKey(selectableTypeKey);
        if (dataController.StartDead && !hasSelectableTypeInSave)
        {
            selectableType = SelectableType.Dead;
        }

        if (selectableType != SelectableType.Dead)
        {
            SetTypeOfModifierVolumes(-1, -1, 0);
            if (gameObject.layer != LayerMask.NameToLayer("WarFog"))
            {
                gameObject.layer = selectableType == SelectableType.Player
                    ? LayerMask.NameToLayer("Player")
                    : LayerMask.NameToLayer("Hitable");
            }
        }
        else
        {
            SetTypeOfModifierVolumes(-1, -1, 1);
            gameObject.layer = LayerMask.NameToLayer("DeadPawn");
        }
        ResetMovement();
        navMeshAgent.Warp(data.GetData("Pos", UNIQUE_ID, transform.position));
        transform.rotation = data.GetData("Rot", UNIQUE_ID, transform.rotation);
        Vector3 defaultVal = new Vector3(0f, 1000f, 0f);
        Vector3 destination = data.GetData("Destination", UNIQUE_ID, defaultVal);
        if (destination != defaultVal)
        {
            targetPosition = destination;
            navMeshAgent.SetDestination(targetPosition);
            isMoving = true;
            TurnManager.Instance.RegisterMovingPawn(gameObject);
        }
        else
        {
            TurnManager.Instance.UnregisterMovingPawn(gameObject);
            isMoving = false;
        }
    }

    float GetMaxMoveMeters(bool ignoreStamina)
    {
        return ignoreStamina ? 99999f : dataController.MaxMoveMetersFromStamina;
    }

    public bool TravelToPosition(Vector3 position)
    {
        return TravelToPosition(position, false);
    }

    public bool TravelToPosition(Vector3 position, bool ignoreStamina)
    {
        NavMeshPathCost.PathPlan plan = NavMeshPathCost.Plan(navMeshAgent, position, dataController.maxSampleDistance);
        if (!plan.valid)
        {
            if (ignoreStamina && NavMeshPathCost.TrySample(position, dataController.maxSampleDistance, out Vector3 sampled))
            {
                navMeshAgent.SetDestination(sampled);
                targetPosition = sampled;
                cachedTargetPositionValid = false;
                isMoving = true;
                trackStaminaSpend = false;
                TurnManager.Instance.RegisterMovingPawn(gameObject);
                return true;
            }
            return false;
        }

        if (!ignoreStamina && !dataController.HasUsefulMoveBudget)
        {
            dataController.ClearUselessMoveStamina();
            return false;
        }

        plan = NavMeshPathCost.ClampMeters(plan, GetMaxMoveMeters(ignoreStamina));
        float minMove = ignoreStamina ? 0.001f : PawnDataController.MinUsefulMoveMeters;
        if (!plan.valid || plan.pathMeters < minMove - 0.001f) return false;

        if (!isMoving)
            lastStaminaPos = transform.position;
        moveStartPos = transform.position;
        trackStaminaSpend = !ignoreStamina;
        softStopping = false;

        navMeshAgent.SetDestination(plan.destination);
        targetPosition = plan.destination;
        cachedTargetPositionValid = false;
        pendingPathMeters = plan.pathMeters;

        isMoving = true;
        TurnManager.Instance.RegisterMovingPawn(gameObject);
        return true;
    }

    public void StopIfNoMoveBudget()
    {
        if (!isMoving || softStopping || !trackStaminaSpend || dataController == null) return;
        if (dataController.Stamina <= 0.001f)
            BeginSoftStop();
    }

    void BeginSoftStop()
    {
        trackStaminaSpend = false;
        softStopping = true;
        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            navMeshAgent.SetDestination(transform.position);
    }

    void TickMoveStamina()
    {
        if (!trackStaminaSpend || dataController == null) return;
        float delta = 0f;
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            Vector3 v = navMeshAgent.velocity;
            v.y = 0f;
            delta = v.magnitude * Time.deltaTime;
        }
        if (delta < 0.0005f)
            delta = HorizontalDistance(lastStaminaPos, transform.position);
        float budget = dataController.MaxMoveMetersFromStamina;
        if (delta > budget) delta = budget;
        if (delta > 0.0005f)
        {
            dataController.SpendMoveMeters(delta);
            lastStaminaPos = transform.position;
        }
        else
            lastStaminaPos = transform.position;
        StopIfNoMoveBudget();
    }

    void FinishMove()
    {
        trackStaminaSpend = false;
        softStopping = false;
        isMoving = false;
        SetTypeOfModifierVolumes(-1, -1);
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.ResetPath();
            navMeshAgent.velocity = Vector3.zero;
        }
        pendingPathMeters = 0f;
        if (dataController != null)
            dataController.ClearUselessMoveStamina();
        if (TurnManager.Instance != null)
            TurnManager.Instance.UnregisterMovingPawn(gameObject);
        OnMoveStopped?.Invoke();
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    protected virtual void Update()
    {
        if (!isMoving) return;
        TickMoveStamina();
        if (!isMoving) return;
        if (navMeshAgent != null && navMeshAgent.enabled && !navMeshAgent.pathPending)
        {
            float stopDist = softStopping ? ArriveStoppingDistance : navMeshAgent.stoppingDistance;
            if (navMeshAgent.remainingDistance <= stopDist)
            {
                if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude < 0.05f)
                    FinishMove();
            }
        }
    }

    public (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) GetPathPointsTo(Vector3 position)
    {
        if (cachedTargetPositionValid && cachedTargetPosition == position)
        {
            return (cachedPointsAvailable, cachedPointsOutOfRange);
        }
        cachedTargetPositionValid = false;

        NavMeshPathCost.PathPlan plan = NavMeshPathCost.Plan(navMeshAgent, position, dataController.maxSampleDistance);
        if (!plan.valid)
            return (null, null);

        float budgetMeters = UsesStaminaBudget()
            ? dataController.MaxMoveMetersFromStamina
            : 99999f;
        (cachedPointsAvailable, cachedPointsOutOfRange) = DividePath(plan.corners, plan.pathMeters, budgetMeters);
        cachedTargetPosition = position;
        cachedTargetPositionValid = true;
        return (cachedPointsAvailable, cachedPointsOutOfRange);
    }

    (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) DividePath(Vector3[] corners, float fullMeters, float budgetMeters)
    {
        if (corners == null || corners.Length < 2) return (null, null);
        if (budgetMeters <= 0.001f) return (null, corners);
        if (fullMeters <= budgetMeters + 0.001f) return (corners, null);

        Vector3 splitPoint = NavMeshPathCost.PointAtDistance(corners, budgetMeters, out _);
        float walked = 0f;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            float segmentDist = Vector3.Distance(corners[i], corners[i + 1]);
            if (walked + segmentDist >= budgetMeters - 0.001f)
            {
                Vector3[] pointsAvailable = new Vector3[i + 2];
                System.Array.Copy(corners, pointsAvailable, i + 1);
                pointsAvailable[i + 1] = splitPoint;

                Vector3[] pointsOutOfRange = new Vector3[corners.Length - i];
                pointsOutOfRange[0] = splitPoint;
                System.Array.Copy(corners, i + 1, pointsOutOfRange, 1, corners.Length - i - 1);
                return (pointsAvailable, pointsOutOfRange);
            }
            walked += segmentDist;
        }
        return (corners, null);
    }

    bool UsesStaminaBudget()
    {
        if (dataController.selectableType != SelectableType.Player) return false;
        if (PawnController.Instance == null) return false;
        return PawnController.Instance.IsInCombat();
    }

    public void ResetMovement()
    {
        if (trackStaminaSpend && dataController != null)
        {
            float delta = HorizontalDistance(lastStaminaPos, transform.position);
            if (delta > 0.0005f)
                dataController.SpendMoveMeters(delta);
            lastStaminaPos = transform.position;
        }
        trackStaminaSpend = false;
        softStopping = false;
        if (navMeshAgent.enabled)
        {
            navMeshAgent.ResetPath();
        }
        pendingPathMeters = 0f;
        targetPosition = Vector3.zero;
        bool wasMoving = isMoving;
        isMoving = false;
        if (dataController != null)
            dataController.ClearUselessMoveStamina();
        cachedTargetPosition = Vector3.zero;
        cachedPointsAvailable = null;
        cachedPointsOutOfRange = null;
        cachedTargetPositionValid = false;
        if (wasMoving && TurnManager.Instance != null)
            TurnManager.Instance.UnregisterMovingPawn(gameObject);
    }

    void OnDestroy()
    {
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnLoad -= OnLoadData;
            SaveHub.Instance.OnSave -= OnSaveData;
        }
    }
}
