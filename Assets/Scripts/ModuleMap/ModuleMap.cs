using System.Collections.Generic;
// using System.Numerics;
using UnityEngine;

public class ModuleMap : MonoBehaviour
{
    [SerializeField]
    private GameObject initialModulePrefab = null;

    [SerializeField]
    private ModuleDataList moduleList = new ModuleDataList();

    [SerializeField]
    private GameObject defaultPawnPrefab = null;

    [SerializeField]
    private List<GameObject> pawns = new List<GameObject>();

    [SerializeField]
    private GameObject getPawnsTo = null;

    private GameObject getPawnsToCached = null;

    void Start()
    {
        Restart();
    }

    void Restart()
    {
        moduleList.Clear();
        for (int i = transform.childCount - 1; i >= 1; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        moduleList.spawnContext = this.gameObject;
        if (!initialModulePrefab.activeInHierarchy)
            initialModulePrefab = Instantiate(initialModulePrefab, transform);
        moduleList.Add(new ModuleData(initialModulePrefab, new int[0], 0), Vector3.zero);
    }

    void OnValidate()
    {
        if (initialModulePrefab != null && this.transform.childCount == 0) Restart();
        moduleList.OnValidate();

        for (int i = 0; i < pawns.Count; i++) if (pawns[i] == null)
            {
                GameObject newPawn = Instantiate(defaultPawnPrefab, transform);
                pawns[i] = newPawn;
            }

        if (getPawnsTo != getPawnsToCached)
        {
            foreach (var pawn in pawns)
            {
                // PawnBrain brain = pawn.GetComponent<PawnBrain>();
                // ModuleData module = moduleList.GetModuleDataByObject(getPawnsTo);
                // brain.TravelToModule(this, module);
                PawnBrainNavMesh brain = pawn.GetComponent<PawnBrainNavMesh>();
                brain.TravelToPosition(getPawnsTo.transform.position);
            }
        }

    }

    public List<ModuleData> GetAllModules()
    {
        return moduleList.GetAllModules();
    }

    public List<ModuleData> GetConnectedModulesTo(ModuleData md)
    {
        return moduleList.GetConnectedModulesTo(md);
    }

}
