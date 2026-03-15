using System;
using UnityEngine;

public class HandleInittingGlobalVars : MonoBehaviour
{
    [SerializeField]
    private ParameteredScriptableObject globalParametersSettable;
    [SerializeField]
    private ParameteredScriptableObject pawnMustHaveParamsSettable;
    [SerializeField]
    private ParameteredScriptableObject calculatableParametersSettable;
    [SerializeField]
    private GlobalSettingsAssets globalSettingsAssetsSettable;
    public static ParameteredScriptableObject pawnMustHaveParams;
    public static ParameteredScriptableObject globalParameters;
    public static GlobalSettingsAssets globalSettingsAssets;
    public static Action onParamsUpdated;
    public static FormulaDataMonoBase mainCalculatedFormulaData;
    public const string IS_STEP_BY_STEP_KEY = "IsStepByStep";
    public const string MELEE_ATTACK_DISTANCE_KEY = "MeleeDST";
    public const string RANDOM_KEY = "Random";

    void Awake()
    {
        if (globalParameters == null)
            globalParameters = GetDataAsset("GlobalParameters");
        globalParameters.AddParameter(IS_STEP_BY_STEP_KEY);
        globalParameters.AddParameter(MELEE_ATTACK_DISTANCE_KEY);
        globalParameters.AddParameter(RANDOM_KEY);
        if (pawnMustHaveParams == null)
            pawnMustHaveParams = GetDataAsset("PawnMustHaveParams");
        if (mainCalculatedFormulaData == null)
            mainCalculatedFormulaData = GetComponent<FormulaDataMonoBase>();
        if (globalSettingsAssets == null)
            globalSettingsAssets = Resources.Load<GlobalSettingsAssets>("GlobalSettings");
        onParamsUpdated?.Invoke();
    }


    private ParameteredScriptableObject GetDataAsset(string fileName)
    {
        return Resources.Load<ParameteredScriptableObject>(fileName);
    }

    void OnValidate()
    {
        if (mainCalculatedFormulaData == null)
        {
            mainCalculatedFormulaData = GetComponent<FormulaDataMonoBase>();
            foreach (var key in PawnController.ALL_KEYS)
            {
                mainCalculatedFormulaData.AddParameter(key);
            }
            PawnDataController.PreFillFormulaData(mainCalculatedFormulaData, PawnController.ATTACKER_PREFIX);
            PawnDataController.PreFillFormulaData(mainCalculatedFormulaData, PawnController.PREY_PREFIX);
        }
        bool doUpdate = false;
        if (pawnMustHaveParamsSettable != null && pawnMustHaveParams != pawnMustHaveParamsSettable)
        {
            pawnMustHaveParams = pawnMustHaveParamsSettable;
            doUpdate = true;
        }
        if (pawnMustHaveParams == null)
        {
            pawnMustHaveParams = GetDataAsset("PawnMustHaveParams");
            doUpdate = true;
        }
        if (pawnMustHaveParamsSettable == null)
        {
            pawnMustHaveParamsSettable = pawnMustHaveParams;
        }
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
}
