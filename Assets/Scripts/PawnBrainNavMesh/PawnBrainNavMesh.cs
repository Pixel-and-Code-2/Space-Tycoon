using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class PawnBrainNavMesh : MonoBehaviour, IWalkableSelectable
{
    [SerializeField]
    private PlayerData playerData;
    private NavMeshAgent navMeshAgent;
    private float initialDistance = 0f;
    private Vector3 targetPosition = Vector3.zero;

    private bool isMoving = false;
    private Rigidbody rb = null;

    public bool IsMoving()
    {
        return isMoving;
    }

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
<<<<<<< Updated upstream

        // Add some margin to stopping distance to avoid getting stuck just before reaching the target
        navMeshAgent.stoppingDistance = 1.05f;

=======
>>>>>>> Stashed changes
    }

    public void TravelToPosition(Vector3 position)
    {
        NavMeshHit navHit;
        if (!NavMesh.SamplePosition(position, out navHit, playerData.maxSampleDistance, NavMesh.AllAreas))
        {
            return;
        }
        Vector3 samplePosition = navHit.position;

        initialDistance = 0f;
        NavMeshPath path = new NavMeshPath();
        if (navMeshAgent.CalculatePath(samplePosition, path))
        {
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Vector3 pointPrev = path.corners[i];
                Vector3 pointNext = path.corners[i + 1];
                float dist = Vector3.Distance(pointPrev, pointNext);
                if (initialDistance + dist > playerData.availableDistance)
                {
                    float sectionDistance = (playerData.availableDistance - initialDistance) / dist;
                    Vector3 pointInTheMiddleOfTheSection = Vector3.Lerp(pointPrev, pointNext, sectionDistance);
                    navMeshAgent.SetDestination(pointInTheMiddleOfTheSection);
                    targetPosition = pointInTheMiddleOfTheSection;
                    initialDistance += sectionDistance * dist;
                    isMoving = true;
                    // Debug.Log("Setting Moving to True: " + isMoving);
                    return;
                }
                initialDistance += dist;
            }
            navMeshAgent.SetDestination(samplePosition);
            targetPosition = samplePosition;
            isMoving = true;
            // Debug.Log("Setting Moving to True: " + isMoving);
        }
    }

    void Update()
    {
        if (navMeshAgent.hasPath && navMeshAgent.remainingDistance < 0.3f)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        // Debug.Log("Update: " + isMoving);
        // ToDo: isMoving here somehow turns again true, even without calling TravelToPosition, which is the only place to have isMoving = true;
        if (isMoving)
        {
            // More reliable way to check if the agent has reached the destination
            if (!navMeshAgent.pathPending)
            {
                // If the remaining distance is less than or equal to the stopping distance
                if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                {
                    // If the agent has no path or is not moving
                    if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                    {
                        isMoving = false;
                        navMeshAgent.ResetPath(); // Reset the path to avoid any residual movement
                    }
                }
            }
        }
    }

    public void OnSelect()
    {
        // Debug.Log("OnSelect: " + name);
    }

    public void OnDeselect()
    {
        // Debug.Log("OnDeselect: " + name);
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void OnMove(Vector3 position)
    {
        TravelToPosition(position);
    }

    public (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) GetPathPointsTo(Vector3 position)
    {
        NavMeshHit navHit;
        if (!NavMesh.SamplePosition(position, out navHit, playerData.maxSampleDistance, NavMesh.AllAreas))
        {
            return (null, null);
        }
        Vector3 samplePosition = navHit.position;

        NavMeshPath path = new NavMeshPath();
        if (navMeshAgent.CalculatePath(samplePosition, path))
        {
            return DividePath(path.corners);
        }
        return (null, null);
    }

    (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) DividePath(Vector3[] points)
    {
        if (playerData.availableDistance < 0f)
        {
            return (points, null);
        }
        if (playerData.availableDistance == 0f)
        {
            return (null, points);
        }

        float distance = 0f;
        for (int i = 0; i < points.Length - 1; i++)
        {

            if (distance + Vector3.Distance(points[i], points[i + 1]) > playerData.availableDistance)
            {
                float sectionDistance = (playerData.availableDistance - distance) / Vector3.Distance(points[i], points[i + 1]);
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
