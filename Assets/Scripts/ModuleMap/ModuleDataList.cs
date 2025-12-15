using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ModuleDataList
{
    [SerializeField, HideInInspector]
    public GameObject spawnContext;
    [SerializeField]
    private List<GateWay> gateWayList = new List<GateWay>();
    [SerializeField]
    private List<ModuleData> moduleDataList = new List<ModuleData>();


    [SerializeField]
    // test prefabs in inspector
    private List<GameObject> modulePrefabs = new List<GameObject>();

    public GateWay GetGatewayById(int gatewayId)
    {
        if (gatewayId >= 0 && gatewayId < gateWayList.Count)
            return gateWayList[gatewayId];
        return null;
    }
    public List<ModuleData> GetAllModules()
    {
        return new List<ModuleData>(moduleDataList);
    }

    public ModuleData GetModuleByGatewayId(int gatewayId)
    {
        foreach (var moduleData in moduleDataList)
        {
            int amount = moduleData.GetConnectedModules().Count;
            for (int i = 0; i < amount; i++)
            {
                GateWay gateway = moduleData.GetGatewayByIndex(i);
                if (gateway != null && gateway.id == gatewayId)
                {
                    return moduleData;
                }
            }
        }
        return null;
    }

    public void Add(ModuleData moduleData, Vector3 pos, int externalGatewayId = GateWay.NO_GATEWAY_ID, int gatewayId = GateWay.NO_GATEWAY_ID)
    {
        if (!moduleData.module.scene.IsValid())
        {
            moduleData.module = GameObject.Instantiate(moduleData.module, pos, Quaternion.identity, spawnContext != null ? spawnContext.transform : null);
        }
        GateWay[] gateways = moduleData.module.GetComponents<GateWay>();
        int[] internalGateWayIds = new int[gateways.Length];
        for (int i = 0; i < gateways.Length; i++)
        {
            gateways[i].id = gateWayList.Count;
            gateWayList.Add(gateways[i]);
            internalGateWayIds[i] = gateways[i].id;

            // test prefabs in inspector
            modulePrefabs.Add(null);
        }
        moduleData.updateGatewayArray(internalGateWayIds, gateways.Length);
        moduleDataList.Add(moduleData);
        if (externalGatewayId != GateWay.NO_GATEWAY_ID)
        {
            ConnectModules(moduleData, gatewayId, moduleDataList[externalGatewayId], externalGatewayId);
        }
    }

    public void Add(GameObject module, int externalGatewayId = GateWay.NO_GATEWAY_ID, int gatewayId = GateWay.NO_GATEWAY_ID)
    {
        GateWay[] internalGateWays = module.GetComponents<GateWay>();
        int[] internalGateWayIds = new int[internalGateWays.Length];
        if (!module.scene.IsValid())
        {
            module = GameObject.Instantiate(module, spawnContext != null ? spawnContext.transform : null);
            gatewayId = GateWay.NO_GATEWAY_ID;
        }

        if (gatewayId == GateWay.NO_GATEWAY_ID)
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
        moduleDataList.Add(new ModuleData(module, internalGateWayIds, internalGateWays.Length, moduleDataList.Count));
        if (externalGatewayId != GateWay.NO_GATEWAY_ID)
        {
            ModuleData module1 = moduleDataList[moduleDataList.Count - 1];
            ModuleData module2 = moduleDataList.Find(moduleData => moduleData.GetLocalGatewayId(externalGatewayId) != -1);
            ConnectModules(module1, gatewayId, module2, externalGatewayId);
        }
    }

    public void ConnectModules(ModuleData module1, int gatewayId1, ModuleData module2, int gatewayId2)
    {
        int localGatewayId1 = module1.GetLocalGatewayId(gatewayId1);
        int localGatewayId2 = module2.GetLocalGatewayId(gatewayId2);
        module1.ConnectModules(module2, localGatewayId1, localGatewayId2);

        // test prefabs in inspector
        modulePrefabs[gatewayId1] = gateWayList[gatewayId2].gameObject;

        // if no gateways are there in prefab, then getting gateway by id here will fail
        gateWayList[gatewayId1].LinkToAnotherGateWay(gateWayList[gatewayId2]);
    }

    public List<GateWay> GetFreeGateWays()
    {
        return gateWayList.FindAll(gateway => gateway.connectedGatewayId == GateWay.NO_GATEWAY_ID);
    }

    // test prefabs in inspector
    public void OnValidate()
    {
        for (int i = 0; i < modulePrefabs.Count; i++)
        {
            if (modulePrefabs[i] != null && !modulePrefabs[i].scene.IsValid())
            {
                modulePrefabs[i] = GameObject.Instantiate(modulePrefabs[i], spawnContext != null ? spawnContext.transform : null);
                this.Add(modulePrefabs[i], i);
            }
        }
    }

    public ModuleData GetModuleDataByObject(GameObject gameObject)
    {
        return moduleDataList.Find(moduleData => moduleData.module == gameObject);
    }

    public void Clear()
    {
        moduleDataList.Clear();
        gateWayList.Clear();
        modulePrefabs.Clear();
    }

    public List<ModuleData> GetConnectedModulesTo(ModuleData moduleDataInstance)
    {
        List<int> ids = moduleDataInstance.GetConnectedModules();
        List<ModuleData> res = new List<ModuleData> { };
        for (int i = 0; i < ids.Count; i++)
        {
            res.Add(moduleDataList[ids[i]]);
        }
        return res;
    }
    public List<ModuleData> GetConnectedModulesTo(int id)
    {
        return GetConnectedModulesTo(moduleDataList[id]);
    }

}