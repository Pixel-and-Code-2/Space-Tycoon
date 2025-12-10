using UnityEngine;

public enum PathNodeType
{
    ModuleCenter,
    Gateway,
    Trigger,
    Task
}

public class PathNode
{
    public PathNodeType type;
    public Vector3 position;

    public PathNode(PathNodeType type, Vector3 position)
    {
        this.type = type;
        this.position = position;
    }

    public PathNode(Vector3 position)
    {
        this.type = PathNodeType.ModuleCenter;
        this.position = position;
    }
}