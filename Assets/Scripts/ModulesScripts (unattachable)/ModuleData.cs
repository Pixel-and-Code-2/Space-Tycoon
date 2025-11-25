using UnityEngine;

[System.Serializable]
public class ModuleData
{
    private int gatewayAmount;
    public Vector3 position;
    public GameObject module;
    private int[] internalGateWayIds;
    [System.NonSerialized]
    private ModuleData[] connectedModules;

    public ModuleData(Vector3 position, GameObject module, int[] internalGateWayIds, int gatewayAmount)
    {

        this.gatewayAmount = gatewayAmount;
        this.position = position;
        this.module = module;
        this.internalGateWayIds = internalGateWayIds;
        connectedModules = new ModuleData[gatewayAmount];

    }

    public void updateGatewayArray(int[] newGateWayIds, int newGatewayAmount)
    {
        internalGateWayIds = newGateWayIds;
        gatewayAmount = newGatewayAmount;
        connectedModules = new ModuleData[gatewayAmount];
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
}