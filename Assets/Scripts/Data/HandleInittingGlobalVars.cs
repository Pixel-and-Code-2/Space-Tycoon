
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
    private const string IS_SHOOT_ON_MOVE_KEY = "IsShootOnMove";
    [SerializeField]
    private Button toggleShootOnMoveButton;
    [SerializeField]
    private Color buttonColorOn;
    [SerializeField]
    private Color buttonColorOff;
    [SerializeField]
    private InputActionReference toggleShootOnMoveAction;
    void Awake()
    {
        if (globalParameters == null)
            globalParameters = GetDataAsset("GlobalParameters");
        globalParameters.AddParameter(IS_SHOOT_ON_MOVE_KEY);
        if (pawnMustHaveParams == null)
            pawnMustHaveParams = GetDataAsset("PawnMustHaveParams");
        if (mainCalculatedFormulaData == null)
            mainCalculatedFormulaData = GetComponent<FormulaDataMonoBase>();
        if (globalSettingsAssets == null)
            globalSettingsAssets = Resources.Load<GlobalSettingsAssets>("GlobalSettings");
        onParamsUpdated?.Invoke();
    }
    void Start()
    {
        UpdateButtonColor();
    }
    void OnEnable()
    {
        if (toggleShootOnMoveAction != null) toggleShootOnMoveAction.action.Enable();
    }
    void OnDisable()
    {
        if (toggleShootOnMoveAction != null) toggleShootOnMoveAction.action.Disable();
    }
    void Update()
    {
        if (toggleShootOnMoveAction != null && toggleShootOnMoveAction.action.triggered)
        {
            ToggleShootOnMove();
        }
    }
    public void ToggleShootOnMove()
    {
        globalParameters.parametersDict[IS_SHOOT_ON_MOVE_KEY] = globalParameters.parametersDict[IS_SHOOT_ON_MOVE_KEY] == 0f ? 1f : 0f;
        UpdateButtonColor();
    }
    private void UpdateButtonColor()
    {
        toggleShootOnMoveButton.image.color = Mathf.Abs(globalParameters.parametersDict[IS_SHOOT_ON_MOVE_KEY] - 0f) < 0.1f ? buttonColorOff : buttonColorOn;
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
