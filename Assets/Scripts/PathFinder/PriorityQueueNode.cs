using UnityEngine;

public class PriorityQueueNode
{
    public ModuleData moduleData;
    public float gCost;
    public float hCost;
    public float fCost => gCost + hCost;
    public Vector3 position;

    public PriorityQueueNode(ModuleData module, float g, float h)
    {
        moduleData = module; 
        gCost = g;
        hCost = h;
        position = module.GetCenterPosition();
    }

    public override bool Equals(object obj)
    {
        return obj is PriorityQueueNode other && moduleData == other.moduleData;
    }

    public override int GetHashCode()
    {
        return moduleData.GetHashCode();
    }
}


