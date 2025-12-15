using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ModulePathFinder
{
    /// <summary>
    /// Stored module map for use in static methods
    /// </summary>
    protected static ModuleMap moduleMapCache;

    /// <summary>
    /// Finds path between two modules
    /// </summary>
    /// <param name="map">Module map</param>
    /// <param name="startModule">Start module</param>
    /// <param name="targetModule">Target module</param>
    /// <returns>List of modules</returns>
    public static List<ModuleData> FindPath(ModuleMap map, ModuleData startModule, ModuleData targetModule)
    {
        moduleMapCache = map;
        if (startModule == targetModule)
        {
            return new List<ModuleData> { startModule, targetModule };
        }

        return ReconstructPath(AStarSearch(startModule, targetModule), targetModule);
    }

    protected static Dictionary<ModuleData, ModuleData> AStarSearch(ModuleData start, ModuleData target)
    {
        var openSet = new PriorityQueue<PriorityQueueNode>();
        var closedSet = new HashSet<ModuleData>();
        var cameFromPairs = new Dictionary<ModuleData, ModuleData>();
        var gScore = new Dictionary<ModuleData, float>();

        float startH = Vector3.Distance(start.GetCenterPosition(), target.GetCenterPosition());
        var startNode = new PriorityQueueNode(start, 0, startH);
        gScore[start] = 0;
        openSet.Enqueue(startNode, startNode.fCost);

        while (openSet.Count > 0)
        {
            PriorityQueueNode current = openSet.Dequeue();

            if (current.moduleData == target)
            {
                return cameFromPairs;
            }

            closedSet.Add(current.moduleData);

            foreach (var neighbor in moduleMapCache.GetConnectedModulesTo(current.moduleData))
            {
                if (neighbor == null || closedSet.Contains(neighbor))
                    continue;

                float tentativeGScore = gScore[current.moduleData] +
                    Vector3.Distance(current.moduleData.GetCenterPosition(), neighbor.GetCenterPosition());

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFromPairs[neighbor] = current.moduleData;
                    gScore[neighbor] = tentativeGScore;

                    float hScore = Vector3.Distance(neighbor.GetCenterPosition(), target.GetCenterPosition());
                    var neighborNode = new PriorityQueueNode(neighbor, tentativeGScore, hScore);

                    if (!openSet.Contains(neighborNode))
                    {
                        openSet.Enqueue(neighborNode, neighborNode.fCost);
                    }
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Using dictionary of pairs of modules, reconstructs path from end node to start node
    /// </summary>
    /// <param name="cameFromPairs">Dictionary of pairs of modules</param>
    /// <param name="endNode">End node</param>
    /// <returns>List of modules</returns>
    protected static List<ModuleData> ReconstructPath(Dictionary<ModuleData, ModuleData> cameFromPairs, ModuleData endNode)
    {
        if (cameFromPairs == null)
        {
            return null;
        }
        List<ModuleData> modulePath = new List<ModuleData> { endNode };
        ModuleData current = endNode;
        while (cameFromPairs.ContainsKey(current))
        {
            current = cameFromPairs[current];
            modulePath.Insert(0, current);
        }
        return modulePath;
    }
}