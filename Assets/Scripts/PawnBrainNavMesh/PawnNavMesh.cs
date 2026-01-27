using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PawnNavMesh : MonoBehaviour
{
    [SerializeField]
    private PlayerData initialPlayerData;
    protected PlayerData playerData;
    private NavMeshAgent navMeshAgent;
    protected float distanceTravelling = 0f;
    protected float maxSampleDistance = 5f;
    protected Vector3 targetPosition = Vector3.zero;

    protected bool isMoving = false;

    public bool IsMoving()
    {
        return isMoving;
    }

    protected virtual void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        playerData = Instantiate(initialPlayerData);
        // Add some margin to stopping distance to avoid getting stuck just before reaching the target
        navMeshAgent.stoppingDistance = 1.05f;
    }

    public void TravelToPosition(Vector3 position)
    {
        NavMeshHit navHit;
        if (!NavMesh.SamplePosition(position, out navHit, playerData.maxSampleDistance, NavMesh.AllAreas))
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
                if (distanceTravelling + dist > playerData.availableDistance)
                {
                    float sectionDistance = (playerData.availableDistance - distanceTravelling) / dist;
                    Vector3 pointInTheMiddleOfTheSection = Vector3.Lerp(pointPrev, pointNext, sectionDistance);
                    navMeshAgent.SetDestination(pointInTheMiddleOfTheSection);
                    targetPosition = pointInTheMiddleOfTheSection;
                    distanceTravelling += sectionDistance * dist;
                    isMoving = true;
                    playerData.availableDistance = 0f;
                    // Debug.Log("Setting Moving to True: " + isMoving);
                    return;
                }
                distanceTravelling += dist;
            }
            navMeshAgent.SetDestination(samplePosition);
            targetPosition = samplePosition;
            isMoving = true;
            playerData.availableDistance -= distanceTravelling;
            // Debug.Log("Setting Moving to True: " + isMoving);
        }
    }

    protected virtual void Update()
    {
        // Debug.Log("Update: " + isMoving);
        // ToDo: isMoving here somehow turns again true, even without calling TravelToPosition, which is the only place to have isMoving = true;
        if (isMoving)
        {
            // More reliable way to check if the agent has reached the destination
            if (!navMeshAgent.pathPending)
            {
                if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                    {
                        isMoving = false;
                        navMeshAgent.ResetPath(); // avoid residual movement
                        distanceTravelling = 0f;
                    }
                }
            }
        }
    }
    private Vector3 cachedTargetPosition = Vector3.zero;
    private Vector3[] cachedPointsAvailable = null;
    private Vector3[] cachedPointsOutOfRange = null;
    private bool cachedTargetPositionValid = false;
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
        if (!NavMesh.SamplePosition(position, out navHit, playerData.maxSampleDistance, NavMesh.AllAreas))
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
        float testDist = distanceTravelling + playerData.availableDistance;
        if (testDist < 0f)
        {
            return (points, null);
        }
        if (testDist == 0f)
        {
            return (null, points);
        }

        float distance = 0f;
        for (int i = 0; i < points.Length - 1; i++)
        {

            if (distance + Vector3.Distance(points[i], points[i + 1]) > testDist)
            {
                float sectionDistance = (testDist - distance) / Vector3.Distance(points[i], points[i + 1]);
                Vector3 pointInTheMiddleOfTheSection = Vector3.Lerp(points[i], points[i + 1], sectionDistance);
                Vector3[] pointsAvailable = new Vector3[i + 2];
                System.Array.Copy(points, pointsAvailable, i + 1);
                pointsAvailable[^1] = pointInTheMiddleOfTheSection;
                Vector3[] pointsOutOfRange = new Vector3[points.Length - i];
                pointsOutOfRange[0] = pointInTheMiddleOfTheSection;
                System.Array.Copy(points, i + 1, pointsOutOfRange, 1, points.Length - i - 1);
                return (pointsAvailable, pointsOutOfRange);
            }
            distance += Vector3.Distance(points[i], points[i + 1]);
        }
        return (points, null);
    }
}
