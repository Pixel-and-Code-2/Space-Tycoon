using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VectorPathFinder : ModulePathFinder
{
    static float EPS = 0.001f;
    public static List<PathNode> FindPath(ModuleMap map, Vector3 startPos, Vector3 targetPos)
    {
        moduleMapCache = map;
        ModuleData startModule = FindClosestModule(startPos);
        ModuleData targetModule = FindClosestModule(targetPos);

        if (startModule == targetModule)
        {
            return new List<PathNode> {
                new PathNode(startPos),
                new PathNode(targetPos),
            };
        }

        List<PathNode> path = ReconstructPath(AStarSearch(startModule, targetModule), targetModule, startPos, targetPos);

        return path;
    }

    public static ModuleData FindClosestModule(Vector3 position)
    {
        // ToDo: explain why this is needed
        // var moduleDataListField = typeof(ModuleMap).GetField("moduleList",
        //     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // if (moduleDataListField == null)
        // {
        //     return null;
        // }

        if (moduleMapCache == null)
        {
            return null;
        }

        ModuleData closest = null;
        float closestDistance = float.MaxValue;

        foreach (var moduleData in moduleMapCache.GetAllModules())
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

    /// <summary>
    /// Using dictionary of pairs of modules, reconstructs path from end node to start node and converts it to world coordinates
    /// </summary>
    /// <param name="cameFromPairs">Dictionary of pairs of modules</param>
    /// <param name="endNode">End node</param>
    /// <param name="startPos">Start position</param>
    /// <param name="targetPos">Target position</param>
    /// <returns>List of world coordinates</returns>
    private static List<PathNode> ReconstructPath(Dictionary<ModuleData, ModuleData> cameFromPairs, ModuleData endNode, Vector3 startPos, Vector3 targetPos)
    {
        if (cameFromPairs == null)
        {
            return null;
        }
        List<PathNode> path = new List<PathNode>();
        List<ModuleData> modulePath = ModulePathFinder.ReconstructPath(cameFromPairs, endNode);
        foreach (var module in modulePath)
        {
            path.Insert(path.Count, new PathNode(module.GetCenterPosition()));
        }
        path = AddGatewayPoints(path, modulePath);
        if ((path[0].position - startPos).magnitude > EPS)
        {
            path.Insert(0, new PathNode(startPos));
        }
        if ((path[path.Count - 1].position - targetPos).magnitude > EPS)
        {
            path.Insert(path.Count, new PathNode(targetPos));
        }
        return path;
    }

    /// <summary>
    /// Fullfills Vector3 array with positions of adjacent gateways
    /// </summary>
    /// <param name="path">Path with coordinates of modules</param>
    /// <param name="modulePath">Path with modules</param>
    /// <returns>Fullfilled path</returns>
    private static List<PathNode> AddGatewayPoints(List<PathNode> path, List<ModuleData> modulePath)
    {
        List<PathNode> detailedPath = new List<PathNode>();

        for (int i = 0; i < path.Count - 1; i++)
        {
            detailedPath.Add(new PathNode(path[i].position));

            if (i < modulePath.Count - 1)
            {
                ModuleData currentModule = modulePath[i];
                ModuleData nextModule = modulePath[i + 1];

                Vector3 gatewayPoint = FindConnectingGatewayPosition(currentModule, nextModule);
                if (gatewayPoint != Vector3.zero)
                {
                    detailedPath.Add(new PathNode(gatewayPoint));
                }
            }
        }

        detailedPath.Add(new PathNode(path[path.Count - 1].position));
        return detailedPath;
    }

    /// <summary>
    /// Finds position of gateway between two modules
    /// </summary>
    /// <param name="fromModule">First module</param>
    /// <param name="toModule">Second module</param>
    /// <returns>Position of gateway if it exists. Center of First module if module is connected yet gateway is null. Zero vector if modules aren't connected</returns>
    private static Vector3 FindConnectingGatewayPosition(ModuleData fromModule, ModuleData toModule)
    {
        for (int i = 0; i < fromModule.connectedModules.Count; i++)
        {
            if (fromModule.connectedModules[i] == toModule)
            {
                GateWay gateway = fromModule.GetGatewayByIndex(i);
                return gateway != null ? gateway.GetPosition() : fromModule.GetCenterPosition();
            }
        }
        return Vector3.zero;
    }
}