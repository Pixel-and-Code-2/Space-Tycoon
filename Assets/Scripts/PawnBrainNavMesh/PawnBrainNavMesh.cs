using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PawnBrainNavMesh : MonoBehaviour, IWalkableSelectable
{
    private NavMeshAgent navMeshAgent;
    [SerializeField]
    private float availableDistance = 10f;

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public void TravelToPosition(Vector3 position)
    {
        navMeshAgent.SetDestination(position);
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

    public Vector3[] GetPathPointsTo(Vector3 position)
    {
        NavMeshPath path = new NavMeshPath();
        if (navMeshAgent.CalculatePath(position, path))
        {
            return path.corners;
        }
        return null;
    }

    public float GetAvailableDistance()
    {
        return availableDistance;
    }
}
