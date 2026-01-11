using System.Collections.Generic;
using UnityEngine;

public class ModuleMap : MonoBehaviour, IBoundsGetter
{
    [SerializeField]
    private GameObject initialModulePrefab = null;
    [SerializeField, HideInInspector] // if we start the game, serialization saves state of this variable, without you'll get null every restart
    private GameObject initialModulePrefabCache = null;

    [SerializeField]
    private ModuleDataList moduleList = new ModuleDataList();

    [SerializeField]
    private GameObject defaultPawnPrefab = null;

    [SerializeField]
    private List<GameObject> pawns = new List<GameObject>();

    [SerializeField]
    private GameObject getPawnsTo = null;

    public Bounds bounds { get; private set; } = new Bounds();

    void Start()
    {
        Restart();
        RecalculateBounds();
    }

    void Restart()
    {
        moduleList.DeinstantiateAllPrefabs();
        moduleList.spawnContext = this.gameObject;
        if (moduleList.GetAllModules().Count == 0)
        {
            moduleList.Add(new ModuleData(initialModulePrefab, new int[0], 0), Vector3.zero);
        }
        moduleList.InstantiateAllPrefabs();
        CheckPawns();
    }

    void FullReset()
    {
        moduleList.Clear();
        for (int i = transform.childCount - 1; i >= 1; i--)
        {
            GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
        }
        moduleList.spawnContext = this.gameObject;
        if (moduleList.GetAllModules().Count == 0)
        {
            moduleList.Add(new ModuleData(initialModulePrefab, new int[0], 0), Vector3.zero);
        }
        CheckPawns();

    }

    void OnValidate()
    {
        if (initialModulePrefab != initialModulePrefabCache)
        {
            if (initialModulePrefab != null)
            {
                // on validate Unity engine refuses to use DestroyImmediate, so we use async call, to do it outside of the validate call
                UnityEditor.EditorApplication.delayCall += FullReset;
            }
            initialModulePrefabCache = initialModulePrefab;
        }
        moduleList.OnValidate();
        CheckPawns();
        RecalculateBounds();
    }

    private void CheckPawns()
    {
        for (int i = 0; i < pawns.Count; i++) if (pawns[i] == null)
            {
                GameObject newPawn = Instantiate(defaultPawnPrefab, transform);
                pawns[i] = newPawn;
            }
    }

    public void SendPawnsToTarget()
    {
        foreach (var pawn in pawns)
        {
            PawnBrainNavMesh brain = pawn.GetComponent<PawnBrainNavMesh>();
            brain.TravelToPosition(getPawnsTo.transform.position);
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

    private void RecalculateBounds()
    {
        Bounds newBounds = new Bounds();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            newBounds.Encapsulate(renderer.bounds);
        }
        this.bounds = newBounds;
    }

    public Bounds GetBounds()
    {
        return bounds;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
