using UnityEngine;

public class PathNode
{
    public ModuleData moduleData;
    public float gCost;
    public float hCost;
    public float fCost => gCost + hCost;
    public Vector3 position;

    public PathNode(ModuleData module, float g, float h)
    {
        moduleData = module; 
        gCost = g;
        hCost = h;
        position = module.GetCenterPosition();
    }

    public override bool Equals(object obj)
    {
        return obj is PathNode other && moduleData == other.moduleData;
    }

    public override int GetHashCode()
    {
        return moduleData.GetHashCode();
    }
}


