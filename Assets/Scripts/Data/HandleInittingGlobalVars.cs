using System;
using Random = UnityEngine.Random;
using UnityEngine;

public class HandleInittingGlobalVars : MonoBehaviour
{
    [SerializeField]
    private ParameteredScriptableObject globalParametersSettable;
    [SerializeField]
    private GlobalSettingsAssets globalSettingsAssetsSettable;
    public static ParameteredScriptableObject globalParameters;
    public static GlobalSettingsAssets globalSettingsAssets;
    public static Action onParamsUpdated;
    public const string IS_STEP_BY_STEP_KEY = "IsStepByStep";
    public const string MELEE_ATTACK_DISTANCE_KEY = "MeleeDST";
    public const string RANDOM_KEY = "Random";
    public const string UNIQUE_ID = "HandleInittingGlobalVars";
    public const string AMOUNT_OF_HEALINGS_KEY = "Healings";

    void Awake()
    {
        if (globalParameters == null)
            globalParameters = GetDataAsset("GlobalParameters");
        globalParameters.AddParameter(IS_STEP_BY_STEP_KEY);
        globalParameters.AddParameter(MELEE_ATTACK_DISTANCE_KEY);
        globalParameters.AddParameter(RANDOM_KEY);
        globalParameters.AddParameter(AMOUNT_OF_HEALINGS_KEY);
        globalParameters.SetDirty();
        globalParameters.RebuildParametersDict();
        if (globalSettingsAssets == null)
            globalSettingsAssets = Resources.Load<GlobalSettingsAssets>("GlobalSettings");
        onParamsUpdated?.Invoke();
        ParameteredScriptableObject.OnUpdateParams += (parametersObj) =>
        {
            globalParameters.parametersDict[RANDOM_KEY] = Random.value;
        };
    }

    void Start()
    {
        SaveHub.Instance.OnLoad += OnLoadData;
        SaveHub.Instance.OnSave += OnSaveData;
    }
    private void OnLoadData(LoadedData data)
    {
        globalParameters.parametersDict[IS_STEP_BY_STEP_KEY] =
            data.GetData(
                "IsStepByStep",
                UNIQUE_ID,
                globalParameters.parametersDict[IS_STEP_BY_STEP_KEY] > 0.5f
            ) ? 1f : 0f;
    }
    private void OnSaveData(Action<SaveRecord[], string> addSaveData)
    {
        SaveRecord records = new()
        {
            recordName = "IsStepByStep",
            recordType = SaveRecordType.boolean,
            boolValue = globalParameters.parametersDict[IS_STEP_BY_STEP_KEY] > 0.5f
        };
        addSaveData(new SaveRecord[] { records }, UNIQUE_ID);
    }
    private ParameteredScriptableObject GetDataAsset(string fileName)
    {
        return Resources.Load<ParameteredScriptableObject>(fileName);
    }

    void OnValidate()
    {
        bool doUpdate = false;
        if (globalParametersSettable != null && globalParameters != globalParametersSettable)
        {
            globalParameters = globalParametersSettable;
            doUpdate = true;
        }
        if (globalParameters == null)
        {
            globalParameters = GetDataAsset("GlobalParameters");
            doUpdate = true;
        }
        if (globalParametersSettable == null)
        {
            globalParametersSettable = globalParameters;
        }
        if (globalSettingsAssetsSettable != null && globalSettingsAssets != globalSettingsAssetsSettable)
        {
            globalSettingsAssets = globalSettingsAssetsSettable;
            doUpdate = true;
        }
        if (globalSettingsAssets == null)
        {
            globalSettingsAssets = Resources.Load<GlobalSettingsAssets>("GlobalSettings");
        }
        if (globalSettingsAssetsSettable == null)
        {
            globalSettingsAssetsSettable = globalSettingsAssets;
        }
        if (doUpdate)
        {
            onParamsUpdated?.Invoke();
        }
    }

    void OnDestroy()
    {
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnLoad -= OnLoadData;
            SaveHub.Instance.OnSave -= OnSaveData;
        }
    }
}
