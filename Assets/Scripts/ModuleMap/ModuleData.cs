using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ModuleData
{
    [SerializeField]
    public int gatewayAmount;
    public GameObject module;
    public int moduleId;

    [SerializeField, HideInInspector]
    private int[] internalGateWayIds;

    [SerializeField, HideInInspector]
    public int[] connectedModules;

    public ModuleData(GameObject module, int[] internalGateWayIds, int gatewayAmount, int moduleId = -1)
    {
        this.gatewayAmount = gatewayAmount;
        this.module = module;
        this.moduleId = moduleId;
        this.internalGateWayIds = internalGateWayIds;
        connectedModules = new int[gatewayAmount];
        for (int i = 0; i < gatewayAmount; i++)
        {
            connectedModules[i] = -1;
        }
    }

    public void updateGatewayArray(int[] newGateWayIds, int newGatewayAmount)
    {
        internalGateWayIds = newGateWayIds;
        gatewayAmount = newGatewayAmount;
        connectedModules = new int[gatewayAmount];
        for (int i = 0; i < gatewayAmount; i++)
        {
            connectedModules[i] = -1;
        }
    }

    public void ConnectModules(ModuleData otherModule, int myGateWayId, int otherGateWayId)
    {
        connectedModules[myGateWayId] = otherModule.moduleId;
        otherModule.connectedModules[otherGateWayId] = moduleId;
    }

    public int GetLocalGatewayId(int gatewayId)
    {
        for (int i = 0; i < gatewayAmount; i++)
        {
            if (internalGateWayIds[i] == gatewayId)
            {
                return i;
            }
        }
        return -1;
    }

    // Add new methods for finding path
    public Vector3 GetCenterPosition()
    {
        return module.transform.position;
    }

    /// <returns>All modules ids connected with this*</returns>
    public List<int> GetConnectedModules()
    {
        List<int> connected = new List<int>();
        for (int i = 0; i < gatewayAmount; i++)
        {
            if (connectedModules[i] != -1)
            {
                connected.Add(connectedModules[i]);
            }
        }
        return connected;
    }

    public GateWay GetGatewayByIndex(int index)
    {
        if (module != null && index >= 0 && index < gatewayAmount)
        {
            GateWay[] gateways = module.GetComponents<GateWay>();
            for (int i = 0; i < gateways.Length; i++)
            {
                if (gateways[i].id == internalGateWayIds[index])
                {
                    return gateways[i];
                }
            }
        }
        return null;
    }

    public Vector3 GetGatewayPosition(int gatewayIndex)
    {
        GateWay gateway = GetGatewayByIndex(gatewayIndex);
        return gateway != null ? gateway.GetPosition() : GetCenterPosition();
    }

    public override int GetHashCode()
    {
        return module.transform.position.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is ModuleData other && this.GetHashCode() == other.GetHashCode();
    }
}