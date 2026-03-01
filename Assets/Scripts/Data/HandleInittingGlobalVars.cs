
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
    private SliderSettingsAssets sliderSettingsAssetsSettable;
    public static ParameteredScriptableObject pawnMustHaveParams;
    public static ParameteredScriptableObject globalParameters;
    public static SliderSettingsAssets sliderSettingsAssets;
    public static Action onParamsUpdated;
    public const string PAWN_DISTANCE_LABEL = "pawnDistance";
    public static FormulaDataMonoBase mainCalculatedFormulaData;
    void Awake()
    {
        if (globalParameters == null)
            globalParameters = GetDataAsset("GlobalParameters");
        globalParameters.SetDirty();
        if (pawnMustHaveParams == null)
            pawnMustHaveParams = GetDataAsset("PawnMustHaveParams");
        if (mainCalculatedFormulaData == null)
            mainCalculatedFormulaData = GetComponent<FormulaDataMonoBase>();
        if (sliderSettingsAssets == null)
            sliderSettingsAssets = Resources.Load<SliderSettingsAssets>("SliderSettingsAssets");
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
            mainCalculatedFormulaData.AddParameter(PAWN_DISTANCE_LABEL);
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
        if (sliderSettingsAssetsSettable != null && sliderSettingsAssets != sliderSettingsAssetsSettable)
        {
            sliderSettingsAssets = sliderSettingsAssetsSettable;
            doUpdate = true;
        }
        if (sliderSettingsAssets == null)
        {
            sliderSettingsAssets = Resources.Load<SliderSettingsAssets>("SliderSettingsAssets");
        }
        if (sliderSettingsAssetsSettable == null)
        {
            sliderSettingsAssetsSettable = sliderSettingsAssets;
        }
        if (doUpdate)
        {
            onParamsUpdated?.Invoke();
        }
    }
}
