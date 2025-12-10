using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ModuleData
{
    [SerializeField]
    private int gatewayAmount;
    public Vector3 position;
    public GameObject module;

    [SerializeField, HideInInspector]
    private int[] internalGateWayIds;
    // ToDo: make this an array of indeces and add getter method to extract objects
    [HideInInspector]
    public List<ModuleData> connectedModules;

    public ModuleData(Vector3 position, GameObject module, int[] internalGateWayIds, int gatewayAmount)
    {

        this.gatewayAmount = gatewayAmount;
        this.position = position;
        this.module = module;
        this.internalGateWayIds = internalGateWayIds;
        connectedModules = new List<ModuleData>();
        for (int i = 0; i < gatewayAmount; i++)
        {
            connectedModules.Add(null);
        }

    }

    public void updateGatewayArray(int[] newGateWayIds, int newGatewayAmount)
    {
        internalGateWayIds = newGateWayIds;
        gatewayAmount = newGatewayAmount;
        connectedModules = new List<ModuleData>();
        for (int i = 0; i < gatewayAmount; i++)
        {
            connectedModules.Add(null);
        }
    }

    public void ConnectModules(ModuleData otherModule, int myGateWayId, int otherGateWayId)
    {
        connectedModules[myGateWayId] = otherModule;
        otherModule.connectedModules[otherGateWayId] = this;
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
        return module != null ? module.transform.position : position;
    }

    public List<ModuleData> GetConnectedModules()
    {
        List<ModuleData> connected = new List<ModuleData>();
        for (int i = 0; i < gatewayAmount; i++)
        {
            if (connectedModules[i] != null)
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
        return module != null ? module.GetInstanceID() : position.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is ModuleData other && this.GetHashCode() == other.GetHashCode();
    }
}