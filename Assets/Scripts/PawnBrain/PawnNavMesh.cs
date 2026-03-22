using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PawnDataController))]
public class PawnNavMesh : MonoBehaviour
{
    private PawnDataController dataController;
    public NavMeshAgent navMeshAgent;
    private float distanceTravelling = 0f;
    [HideInInspector]
    public Vector3 targetPosition { get; private set; } = Vector3.zero;
    private bool isMoving = false;
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
        if (isMyTeamsTurn != -1) this.isMyTeamsTurn = isMyTeamsTurn;
        if (isSelected != -1) this.isSelected = isSelected;
        if (isDeath != -1) this.isDeath = isDeath;
        foreach (var scriptEnabler in scriptEnablers)
        {
            bool newVal = scriptEnabler.CheckEnabled(this.isMyTeamsTurn, this.isSelected, isMoving ? 1 : 0, this.isDeath);
            if (scriptEnabler.script != null) scriptEnabler.script.enabled = newVal;
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
    }

    void Start()
    {
        navMeshAgent.stoppingDistance = 1.05f;
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

    private float GetAvDist()
    {
        return dataController.GetParameterValue(PawnDataController.AVAILABLE_DISTANCE_KEY);
    }

    public void TravelToPosition(Vector3 position)
    {
        NavMeshHit navHit;
        if (!NavMesh.SamplePosition(position, out navHit, dataController.maxSampleDistance, NavMesh.AllAreas))
        {
            return;
        }
        Vector3 samplePosition = navHit.position;

        distanceTravelling = 0f;
        NavMeshPath path = new NavMeshPath();

        if (navMeshAgent.CalculatePath(samplePosition, path))
        {
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Vector3 pointPrev = path.corners[i];
                Vector3 pointNext = path.corners[i + 1];
                float dist = Vector3.Distance(pointPrev, pointNext);

                if (distanceTravelling + dist > GetAvDist())
                {
                    float sectionDistance = (GetAvDist() - distanceTravelling) / dist;
                    Vector3 pointInTheMiddleOfTheSection = Vector3.Lerp(pointPrev, pointNext, sectionDistance);
                    distanceTravelling += sectionDistance * dist;

                    navMeshAgent.SetDestination(pointInTheMiddleOfTheSection);
                    targetPosition = pointInTheMiddleOfTheSection;

                    AddWalkedDistance(GetAvDist());
                    isMoving = true;
                    TurnManager.Instance.RegisterMovingPawn(gameObject);
                    return;
                }
                distanceTravelling += dist;
            }

            navMeshAgent.SetDestination(samplePosition);
            targetPosition = samplePosition;

            AddWalkedDistance(distanceTravelling);
            isMoving = true;
            TurnManager.Instance.RegisterMovingPawn(gameObject);
        }
    }

    private void AddWalkedDistance(float distance)
    {
        dataController.SetParameterValue(PawnDataController.AVAILABLE_DISTANCE_KEY, GetAvDist() - distance);
        dataController.SetParameterValue(
            PawnDataController.WALKED_KEY,
            dataController.GetParameterValue(PawnDataController.WALKED_KEY) + distance
        );
    }
    protected virtual void Update()
    {
        if (MainMenu.Instance.isMainMenuVisible) return;
        if (isMoving)
        {
            if (!navMeshAgent.pathPending)
            {
                if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                    {
                        isMoving = false;
                        SetTypeOfModifierVolumes(-1, -1);
                        navMeshAgent.ResetPath();
                        distanceTravelling = 0f;
                        TurnManager.Instance.UnregisterMovingPawn(gameObject);
                    }
                }
            }
        }
    }

    public (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) GetPathPointsTo(Vector3 position)
    {
        if (cachedTargetPositionValid && cachedTargetPosition == position)
        {
            return (cachedPointsAvailable, cachedPointsOutOfRange);
        }
        else
        {
            cachedTargetPositionValid = false;
        }

        NavMeshHit navHit;
        if (!NavMesh.SamplePosition(position, out navHit, dataController.maxSampleDistance, NavMesh.AllAreas))
        {
            return (null, null);
        }
        Vector3 samplePosition = navHit.position;

        NavMeshPath path = new NavMeshPath();
        if (navMeshAgent.CalculatePath(samplePosition, path))
        {
            (cachedPointsAvailable, cachedPointsOutOfRange) = DividePath(path.corners);
            cachedTargetPosition = position;
            cachedTargetPositionValid = true;
            return (cachedPointsAvailable, cachedPointsOutOfRange);
        }
        return (null, null);
    }

    (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) DividePath(Vector3[] points)
    {
        float limit = GetAvDist() + distanceTravelling;
        if (Mathf.Abs(limit) < 0.001f) return (null, points);
        if (limit < 0f) return (points, null);

        float distCalc = 0f;
        for (int i = 0; i < points.Length - 1; i++)
        {
            float segmentDist = Vector3.Distance(points[i], points[i + 1]);

            if (distCalc + segmentDist > limit)
            {
                float remaining = limit - distCalc;
                float ratio = remaining / segmentDist;

                Vector3 splitPoint = Vector3.Lerp(points[i], points[i + 1], ratio);

                Vector3[] pointsAvailable = new Vector3[i + 2];
                System.Array.Copy(points, pointsAvailable, i + 1);
                pointsAvailable[^1] = splitPoint;

                Vector3[] pointsOutOfRange = new Vector3[points.Length - i];
                pointsOutOfRange[0] = splitPoint;
                System.Array.Copy(points, i + 1, pointsOutOfRange, 1, points.Length - i - 1);

                return (pointsAvailable, pointsOutOfRange);
            }
            distCalc += segmentDist;
        }
        return (points, null);
    }

    public void ResetMovement()
    {
        if (navMeshAgent.enabled)
        {
            navMeshAgent.ResetPath();
        }
        distanceTravelling = 0f;
        targetPosition = Vector3.zero;
        isMoving = false;
        cachedTargetPosition = Vector3.zero;
        cachedPointsAvailable = null;
        cachedPointsOutOfRange = null;
        cachedTargetPositionValid = false;
    }
}