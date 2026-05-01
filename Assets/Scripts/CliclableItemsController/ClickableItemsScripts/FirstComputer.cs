using Unity.AI.Navigation;
using UnityEngine;

public class FirstComputer : IScriptForClickable
{
    [SerializeField]
    private Material[] computerOnMaterials;
    [SerializeField]
    private Material[] systemOnMaterials;
    [SerializeField]
    private Material[] computerOffMaterials;
    [SerializeField]
    private MeshRenderer computerRenderer;
    [SerializeField]
    private MeshRenderer systemRenderer;
    [SerializeField]
    private NavMeshLink navMeshLink;
    private string UNIQUE_ID => "FirstDoorOpen_" + gameObject.name;
    void Start()
    {
        navMeshLink.enabled = false;
        SaveHub.Instance.OnLoad += OnLoad;
        SaveHub.Instance.OnSave += OnSave;
    }
    private void OnLoad(LoadedData data)
    {
        navMeshLink.enabled = data.GetData("enabled", UNIQUE_ID, false);
    }
    private void OnSave(System.Action<SaveRecord[], string> addSaveData)
    {
        addSaveData(new SaveRecord[] {
            new SaveRecord() {
                recordName = "enabled",
                recordType = SaveRecordType.boolean,
                boolValue = navMeshLink.enabled
            }
        }, UNIQUE_ID);
    }
    void OnDestroy()
    {
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnLoad -= OnLoad;
            SaveHub.Instance.OnSave -= OnSave;
        }
    }
    public override void OnComplete()
    {
        base.OnComplete();
        navMeshLink.enabled = true;
        systemRenderer.materials = systemOnMaterials;
    }

    public override void OnStart()
    {
        base.OnStart();
        computerRenderer.materials = computerOnMaterials;
    }
    public override void OnCancel()
    {
        base.OnCancel();
        computerRenderer.materials = computerOffMaterials;
    }

}