using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class PawnNavMesh : MonoBehaviour
{
    [SerializeField]
    protected PlayerData initialPlayerData;
    private PlayerData playerDataCached;
    [SerializeField]
    private PlayerData enemyDataExample;
    private PlayerData enemyDataCached;

    protected Dictionary<string, float> playerData;

    private NavMeshAgent navMeshAgent;
    protected float distanceTravelling = 0f;
    protected Vector3 targetPosition = Vector3.zero;
    protected bool isMoving = false;

    private Vector3 cachedTargetPosition = Vector3.zero;
    private Vector3[] cachedPointsAvailable = null;
    private Vector3[] cachedPointsOutOfRange = null;
    private bool cachedTargetPositionValid = false;
    [SerializeReference]
    private FormulaField formulaField = new FormulaField();

    public bool IsMoving()
    {
        return isMoving;
    }

    protected virtual void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        playerData = initialPlayerData.GetCopyOfParameters();

        navMeshAgent.stoppingDistance = 1.05f;
    }

    protected virtual void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart += ResetActionPoints;
        }
    }

    protected virtual void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= ResetActionPoints;
        }
    }

    private void ResetActionPoints()
    {
        playerData = initialPlayerData.GetCopyOfParameters();
        //Debug.Log($"Pawn {name} points reset to {playerData["availableDistance"]}");
    }

    public void TravelToPosition(Vector3 position)
    {
        NavMeshHit navHit;
        if (!NavMesh.SamplePosition(position, out navHit, initialPlayerData.maxSampleDistance, NavMesh.AllAreas))
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

                if (distanceTravelling + dist > playerData[PlayerData.AVAILABLE_DISTANCE_KEY])
                {
                    float sectionDistance = (playerData[PlayerData.AVAILABLE_DISTANCE_KEY] - distanceTravelling) / dist;
                    Vector3 pointInTheMiddleOfTheSection = Vector3.Lerp(pointPrev, pointNext, sectionDistance);
                    distanceTravelling += sectionDistance * dist;

                    navMeshAgent.SetDestination(pointInTheMiddleOfTheSection);
                    targetPosition = pointInTheMiddleOfTheSection;

                    playerData[PlayerData.AVAILABLE_DISTANCE_KEY] = 0f;
                    isMoving = true;
                    return;
                }
                distanceTravelling += dist;
            }

            navMeshAgent.SetDestination(samplePosition);
            targetPosition = samplePosition;

            playerData[PlayerData.AVAILABLE_DISTANCE_KEY] -= distanceTravelling;
            isMoving = true;
        }
    }

    protected virtual void Update()
    {
        if (isMoving)
        {
            if (!navMeshAgent.pathPending)
            {
                if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                    {
                        isMoving = false;
                        navMeshAgent.ResetPath();
                        distanceTravelling = 0f;
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
        if (!NavMesh.SamplePosition(position, out navHit, initialPlayerData.maxSampleDistance, NavMesh.AllAreas))
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
        float limit = playerData[PlayerData.AVAILABLE_DISTANCE_KEY] + distanceTravelling;

        if (limit < 0f) return (points, null);
        if (limit == 0f) return (null, points);

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

    public static float CalculateDistance(Vector3[] points)
    {
        if (points == null || points.Length == 0)
        {
            return 0f;
        }
        float distance = 0f;
        for (int i = 0; i < points.Length - 1; i++)
        {
            distance += Vector3.Distance(points[i], points[i + 1]);
        }
        return distance;
    }
    public float some_value = 0f;
    private float cache = 0f;
    void OnValidate()
    {
        if (some_value != cache)
        {
            cache = some_value;
            var dictsArr = new Dictionary<string, float>[] { initialPlayerData.GetParametersDict(), enemyDataExample.GetParametersDict() };
            // Debug.Log("dictsArr: " + dictsArr[0].Count + " " + dictsArr[1].Count);
            float res = formulaField.EvaluateFormula(dictsArr);
            // Debug.Log("Formula result: " + res);
            some_value = res;
        }
        bool doUpdate = false;
        if (playerDataCached != initialPlayerData)
        {
            playerDataCached = initialPlayerData;
            playerData = initialPlayerData.GetCopyOfParameters();
            doUpdate = true;
        }

        if (enemyDataCached != enemyDataExample)
        {
            enemyDataCached = enemyDataExample;
            doUpdate = true;
        }

        if (doUpdate)
        {
            formulaField.dataAssets.Clear();
            formulaField.dataAssets.Add(initialPlayerData);
            formulaField.dataAssets.Add(enemyDataExample);
            formulaField.names.Clear();
            formulaField.names.Add("CurrentPanw");
            formulaField.names.Add("EnemyPawn");
        }
    }

}