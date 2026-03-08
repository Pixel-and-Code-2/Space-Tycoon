using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class navmeshtest : MonoBehaviour
{
    [SerializeField]
    private NavMeshAgent navMeshAgent;
    [SerializeField]
    private Transform target;
    [SerializeField]
    private NavMeshSurface navMeshSurface;
    float timeSpent = 7f;
    void Update()
    {
        timeSpent += Time.deltaTime;
        if (timeSpent > 10f)
        {
            timeSpent = 0f;
            navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);

            //     Collider zoneCollider = GetComponent<Collider>();
            //     if (zoneCollider != null)
            //     {
            //         Bounds updateBounds = zoneCollider.bounds;

            //         // Использование перегрузки с Bounds. 
            //         // Передача null в первый аргумент заставляет Unity использовать только 
            //         // динамические источники (Modifier Volumes) в этих границах.
            //         navMeshSurface.UpdateNavMesh(null, updateBounds);

            //     }

            navMeshAgent.SetDestination(target.position);
            //     navMeshAgent.SetDestination(target.position);
        }
    }
}