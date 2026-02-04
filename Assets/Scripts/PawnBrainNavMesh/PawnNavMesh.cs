using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PawnDataController))]
public class PawnNavMesh : MonoBehaviour
{
    private PawnDataController dataController;
    private NavMeshAgent navMeshAgent;
    private float distanceTravelling = 0f;
    [HideInInspector]
    public Vector3 targetPosition { get; private set; } = Vector3.zero;
    private bool isMoving = false;
    private Vector3 cachedTargetPosition = Vector3.zero;
    private Vector3[] cachedPointsAvailable = null;
    private Vector3[] cachedPointsOutOfRange = null;
    private bool cachedTargetPositionValid = false;

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

                    dataController.SetParameterValue(PawnDataController.AVAILABLE_DISTANCE_KEY, 0f);
                    isMoving = true;
                    return;
                }
                distanceTravelling += dist;
            }

            navMeshAgent.SetDestination(samplePosition);
            targetPosition = samplePosition;

            dataController.SetParameterValue(PawnDataController.AVAILABLE_DISTANCE_KEY, GetAvDist() - distanceTravelling);
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
}