using UnityEngine;
using Unity.AI.Navigation;

public class SecondDoorOpen : IScriptForClickable
{
    [SerializeField]
    private NavMeshLink navMeshLink;
    private string UNIQUE_ID => "SecondDoorOpen_" + gameObject.name;
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
    public override void OnComplete()
    {
        base.OnComplete();
        navMeshLink.enabled = true;
    }

    void OnDestroy()
    {
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnLoad -= OnLoad;
            SaveHub.Instance.OnSave -= OnSave;
        }
    }
}