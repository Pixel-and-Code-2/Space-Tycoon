using System.Collections.Generic;
using UnityEngine;
public class ModulePathfinder : MonoBehaviour
{
    [SerializeField]
    private bool drawGizmos = true;
    [SerializeField]
    private Color pathColor = Color.cyan;

    private ModuleMap moduleMap;
    private List<Vector3> lastFoundPath = new List<Vector3>();

    private void Start() // как мы получаем отсюда компонент?
    {
        moduleMap = GetComponent<ModuleMap>();
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        ModuleData startModule = FindClosestModule(startPos);
        ModuleData targetModule = FindClosestModule(targetPos);

        if (startModule == targetModule)
        {
            return new List<Vector3> { startPos, targetPos };
        }

        List<Vector3> path = AStarSearch(startModule, targetModule, startPos, targetPos);

        if (path != null)
        {
            lastFoundPath = path;
        }

        return path;
    }

    private ModuleData FindClosestModule(Vector3 position)
    {
        var moduleDataListField = typeof(ModuleMap).GetField("moduleList",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (moduleDataListField == null)
        {
            return null;
        }

        var moduleDataList = moduleDataListField.GetValue(moduleMap) as ModuleDataList;
        if (moduleDataList == null)
        {
            return null;
        }

        ModuleData closest = null;
        float closestDistance = float.MaxValue;

        foreach (var moduleData in moduleDataList.GetAllModules())
        {
            if (moduleData == null || moduleData.module == null)
            {
                continue;
            }

            float distance = Vector3.Distance(position, moduleData.GetCenterPosition());
            if (distance < closestDistance)
            {
                closest = moduleData;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private List<Vector3> AStarSearch(ModuleData start, ModuleData target, Vector3 startPos, Vector3 targetPos)
    {
        var openSet = new PriorityQueue<PathNode>();
        var closedSet = new HashSet<ModuleData>();
        var cameFrom = new Dictionary<ModuleData, ModuleData>();
        var gScore = new Dictionary<ModuleData, float>();

        float startH = Heuristic(start.GetCenterPosition(), target.GetCenterPosition());
        var startNode = new PathNode(start, 0, startH);
        gScore[start] = 0;
        openSet.Enqueue(startNode, startNode.fCost);

        while (openSet.Count > 0)
        {
            PathNode current = openSet.Dequeue();

            if (current.moduleData == target)
            {
                Debug.Log("Path found! Reconstructing...");
                return ReconstructPath(cameFrom, current.moduleData, startPos, targetPos);
            }

            closedSet.Add(current.moduleData);

            foreach (var neighbor in current.moduleData.GetConnectedModules())
            {
                if (neighbor == null || closedSet.Contains(neighbor))
                    continue;

                float tentativeGScore = gScore[current.moduleData] +
                    Vector3.Distance(current.moduleData.GetCenterPosition(), neighbor.GetCenterPosition());

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current.moduleData;
                    gScore[neighbor] = tentativeGScore;

                    float hScore = Heuristic(neighbor.GetCenterPosition(), target.GetCenterPosition());
                    var neighborNode = new PathNode(neighbor, tentativeGScore, hScore);

                    if (!openSet.Contains(neighborNode))
                    {
                        openSet.Enqueue(neighborNode, neighborNode.fCost);
                    }
                }
            }
        }

        return null;
    }

    private float Heuristic(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a, b);
    }

    private List<Vector3> ReconstructPath(Dictionary<ModuleData, ModuleData> cameFrom, ModuleData endNode, Vector3 startPos, Vector3 targetPos)
    {
        List<Vector3> path = new List<Vector3> { targetPos };
        List<ModuleData> modulePath = new List<ModuleData> { endNode };

        ModuleData current = endNode;

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            modulePath.Insert(0, current);
            path.Insert(0, current.GetCenterPosition());
        }

        path.Insert(0, startPos);

        path = AddGatewayPoints(path, modulePath);

        return path;
    }

    private List<Vector3> AddGatewayPoints(List<Vector3> path, List<ModuleData> modulePath)
    {
        List<Vector3> detailedPath = new List<Vector3>();

        for (int i = 0; i < path.Count - 1; i++)
        {
            detailedPath.Add(path[i]);

            if (i < modulePath.Count - 1)
            {
                ModuleData currentModule = modulePath[i];
                ModuleData nextModule = modulePath[i + 1];

                Vector3 gatewayPoint = FindConnectingGatewayPosition(currentModule, nextModule);
                if (gatewayPoint != Vector3.zero)
                {
                    detailedPath.Add(gatewayPoint);
                }
            }
        }

        detailedPath.Add(path[path.Count - 1]);
        return detailedPath;
    }

    private Vector3 FindConnectingGatewayPosition(ModuleData fromModule, ModuleData toModule)
    {
        var connectedModules = fromModule.GetConnectedModules();
        for (int i = 0; i < connectedModules.Count; i++)
        {
            if (connectedModules[i] == toModule)
            {
                GateWay gateway = fromModule.GetGatewayByIndex(i);
                return gateway != null ? gateway.GetPosition() : fromModule.GetCenterPosition();
            }
        }
        return Vector3.zero;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || lastFoundPath == null || lastFoundPath.Count < 2) return;

        Gizmos.color = pathColor;
        for (int i = 0; i < lastFoundPath.Count - 1; i++)
        {
            Gizmos.DrawLine(lastFoundPath[i], lastFoundPath[i + 1]);
            Gizmos.DrawSphere(lastFoundPath[i], 0.1f);
        }
        Gizmos.DrawSphere(lastFoundPath[lastFoundPath.Count - 1], 0.1f);
    }

    public void ClearLastPath()
    {
        lastFoundPath.Clear();
    }
}
