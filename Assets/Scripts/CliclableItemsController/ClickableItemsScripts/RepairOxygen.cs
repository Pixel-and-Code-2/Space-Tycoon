using Unity.AI.Navigation;
using UnityEngine;

public class RepairOxygen : IScriptForClickable
{
    [SerializeField]
    private NavMeshLink navMeshLink;
    private string UNIQUE_ID => "RepairOxygen_" + gameObject.name;
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
    }
}