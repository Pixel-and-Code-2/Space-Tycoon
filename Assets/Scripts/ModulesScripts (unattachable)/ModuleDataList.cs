using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public class ModuleDataList
{
    [HideInInspector]
    // we better move this one to gateway class as public one
    private const int NO_GATEWAY_ID = -1;
    [HideInInspector]
    public GameObject spawnContext;
    [HideInInspector]
    private List<GateWay> gateWayList = new List<GateWay>();
    [SerializeField]
    private List<ModuleData> moduleDataList = new List<ModuleData>();


    [SerializeField]
    // test prefabs in inspector
    private List<GameObject> modulePrefabs = new List<GameObject>();



    public void Add(ModuleData moduleData)
    {
        if (!moduleData.module.scene.IsValid())
        {
            moduleData.module = GameObject.Instantiate(moduleData.module, moduleData.position, Quaternion.identity);
        }
        GateWay[] gateways = moduleData.module.GetComponents<GateWay>();
        moduleData.internalGateWayIds = new int[gateways.Length];
        for (int i = 0; i < gateways.Length; i++)
        {
            gateways[i].id = gateWayList.Count;
            gateWayList.Add(gateways[i]);
            moduleData.internalGateWayIds[i] = gateways[i].id;

            // test prefabs in inspector
            modulePrefabs.Add(null);
        }
        moduleDataList.Add(moduleData);
    }

    public void Add(GameObject module, int externalGatewayId = NO_GATEWAY_ID, int gatewayId = NO_GATEWAY_ID)
    {
        GateWay[] internalGateWays = module.GetComponents<GateWay>();
        int[] internalGateWayIds = new int[internalGateWays.Length];
        if (!module.scene.IsValid())
        {
            module = GameObject.Instantiate(module);
            gatewayId = NO_GATEWAY_ID;
        }

        if (gatewayId == NO_GATEWAY_ID)
        {
            gatewayId = gateWayList.Count;
        }
        for (int i = 0; i < internalGateWays.Length; i++)
        {
            internalGateWays[i].id = gateWayList.Count;
            gateWayList.Add(internalGateWays[i]);

            // test prefabs in inspector
            modulePrefabs.Add(null);

            internalGateWayIds[i] = internalGateWays[i].id;
        }
        moduleDataList.Add(new ModuleData
        {
            position = module.transform.position,
            module = module,
            internalGateWayIds = internalGateWayIds
        });
        if (externalGatewayId != NO_GATEWAY_ID)
        {
            // test prefabs in inspector
            modulePrefabs[gatewayId] = gateWayList[externalGatewayId].gameObject;

            // if no gateways are there in prefab, then getting gateway by id here will fail
            gateWayList[gatewayId].LinkToAnotherGateWay(gateWayList[externalGatewayId]);
        }
    }

    public List<GateWay> GetFreeGateWays()
    {
        return gateWayList.FindAll(gateway => gateway.connectedGatewayId == NO_GATEWAY_ID);
    }

    public void OnValidate()
    {
        for (int i = 0; i < modulePrefabs.Count; i++)
        {
            if (modulePrefabs[i] != null && !modulePrefabs[i].scene.IsValid())
            {
                modulePrefabs[i] = GameObject.Instantiate(modulePrefabs[i]);
                this.Add(modulePrefabs[i], i);
            }
        }
    }
}