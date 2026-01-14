using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PawnBrainNavMesh : MonoBehaviour, IWalkableSelectable
{
    private NavMeshAgent navMeshAgent;
    // less than 0 - unlimited
    [SerializeField, Range(-1f, float.MaxValue), Tooltip("less than 0 - unlimited")]
    private float availableDistance = -1f;
    private float initialDistance = 0f;

    [SerializeField, Tooltip("max distance from mouse to walkable area, to show path")]
    private float maxSampleDistance = 5f;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public void TravelToPosition(Vector3 position)
    {
        NavMeshHit navHit;
        if (!NavMesh.SamplePosition(position, out navHit, maxSampleDistance, NavMesh.AllAreas))
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
                if (initialDistance + dist > availableDistance)
                {
                    float sectionDistance = (availableDistance - initialDistance) / dist;
                    Vector3 pointInTheMiddleOfTheSection = Vector3.Lerp(pointPrev, pointNext, sectionDistance);
                    navMeshAgent.SetDestination(pointInTheMiddleOfTheSection);
                    initialDistance += sectionDistance * dist;
                    return;
                }
                initialDistance += dist;
            }
            navMeshAgent.SetDestination(samplePosition);
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
        if (!NavMesh.SamplePosition(position, out navHit, maxSampleDistance, NavMesh.AllAreas))
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
        if (availableDistance < 0f)
        {
            return (points, null);
        }
        if (availableDistance == 0f)
        {
            return (null, points);
        }

        float distance = 0f;
        for (int i = 0; i < points.Length - 1; i++)
        {

            if (distance + Vector3.Distance(points[i], points[i + 1]) > availableDistance)
            {
                float sectionDistance = (availableDistance - distance) / Vector3.Distance(points[i], points[i + 1]);
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
