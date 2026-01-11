using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PawnBrainNavMesh : MonoBehaviour, ISelectable
{
    private NavMeshAgent navMeshAgent;

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
}
