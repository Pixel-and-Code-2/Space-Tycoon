using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PawnBrainNavMesh : MonoBehaviour
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
}
