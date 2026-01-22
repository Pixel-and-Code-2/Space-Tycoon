using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PawnBrainNavMesh : MonoBehaviour, IWalkableSelectable
{
    [SerializeField]
    private PlayerData playerData;
    private NavMeshAgent navMeshAgent;
    private float initialDistance = 0f;

    private bool isMoving = false;

    public bool IsMoving()
    {
        return isMoving;
    }

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
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
                    initialDistance += sectionDistance * dist;
                    isMoving = true;
                    // Debug.Log("Setting Moving to True: " + isMoving);
                    return;
                }
                initialDistance += dist;
            }
            navMeshAgent.SetDestination(samplePosition);
            isMoving = true;
            // Debug.Log("Setting Moving to True: " + isMoving);
        }
    }

    void Update()
    {
        // Debug.Log("Update: " + isMoving);
        // ToDo: isMoving here somehow turns again true, even without calling TravelToPosition, which is the only place to have isMoving = true;
        if (isMoving)
        {
            if (
                !navMeshAgent.pathPending &&
                navMeshAgent.remainingDistance < 0.1f &&
                navMeshAgent.velocity.magnitude < 0.01f
                )
            {
                isMoving = false;
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
