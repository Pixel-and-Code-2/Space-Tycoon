using System.Collections.Generic;
using UnityEngine;

public class PawnDataController : MonoBehaviour, IFormulaData
{
    // Static data storages
    [SerializeField]
    private ParameteredScriptableObject initialPawnData;
    private ParameteredScriptableObject pawnDataCached;
    // Additional developing params
    [Header("Additional Developing params")]
    [SerializeField, Tooltip("Max distance from mouse to walkable area, to show path")]
    public float maxSampleDistance = 5f;
    [SerializeField, Tooltip("Force of push when pawn collides with obstacle, 0 to disable")]
    public float obstaclePushForce = 10f;
    [SerializeField, Tooltip("Override of vertical push when pawn collides with obstacle, -1 to disable")]
    public float verticalPushOverride = 0.2f;

    // Dynamic parameters
    public Dictionary<string, float> dynamicParameters = new Dictionary<string, float>();

    public const string AVAILABLE_DISTANCE_KEY = "AvailableDistance";
    public const string INITIAL_HP_KEY = "HP";
    public const string INITIAL_AVAILABLE_DISTANCE_KEY = "SPD";
    public const string AVAILABLE_HEALTH_KEY = "AvailableHealth";
    public const string LAST_ROUND_WALKED_KEY = "LastRoundWalked";
    public const string WALKED_KEY = "WalkedDistance";
    public const string LAST_ROUND_SHOOTED_AMOUNT_KEY = "LastRoundShotAmount";
    public const string SHOOTED_AMOUNT_KEY = "ShotAmount";
    public const string LAST_ROUND_MELEE_AMOUNT_KEY = "LastRoundMeleeAmount";
    public const string MELEE_AMOUNT_KEY = "MeleeAmount";
    public const string MOVES_TO_SKIP_KEY = "MovesToSkip";
    public const string IS_SHOOT_ON_MOVE_KEY = "IsShootOnMove";
    public const string INITIAL_MAG_AMOUNT_KEY = "MAG";
    public const string INITIAL_TOTAL_AMMO_KEY = "TotalAmmo";
    public const string MAG_AMOUNT_KEY = "CurrentMag";
    public const string TOTAL_AMMO_KEY = "AvailableAmmo";
    public const string INITIAL_MOVES_TO_RELOAD_KEY = "MovesToReload";
    public const string AMOUNT_OF_DEFENDED_HITS_KEY = "Defenses";
    public const string AMOUNT_OF_HEALINGS_KEY = "HealingsAmount";

    [SerializeField]
    public SelectableType selectableType = SelectableType.Player;
    public string UNIQUE_ID => "PawnData_" + gameObject.name;

    void OnValidate()
    {
        if (pawnDataCached != initialPawnData)
        {
            pawnDataCached = initialPawnData;
            ResetKeys();
        }
    }

    void Start()
    {
        if (TurnManager.Instance != null)
        {
            if (selectableType == SelectableType.Player)
            {
                TurnManager.Instance.OnPlayerTurnStart += ResetActionPoints;
            }
            else if (selectableType == SelectableType.Enemy)
            {
                TurnManager.Instance.OnEnemyTurnStart += ResetActionPoints;
            }
        }
        initialPawnData.SetDirty();
        initialPawnData.RebuildParametersDict();
        ResetKeys();
        SaveHub.Instance.OnLoad += OnLoadData;
        SaveHub.Instance.OnSave += OnSaveData;
        TurnManager.Instance.OnTriggerZoneExit += OnTriggerZoneExit;
    }

    private void OnTriggerZoneExit()
    {
        ResetActionPoints();
        SetParameterValue(MOVES_TO_SKIP_KEY, 0);
    }
    void OnEnable()
    {
        if (TurnManager.Instance != null)
        {
            if (selectableType == SelectableType.Player)
            {
                TurnManager.Instance.OnPlayerTurnStart += ResetActionPoints;
            }
            else if (selectableType == SelectableType.Enemy)
            {
                TurnManager.Instance.OnEnemyTurnStart += ResetActionPoints;
            }
        }
    }

    void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= ResetActionPoints;
            TurnManager.Instance.OnEnemyTurnStart -= ResetActionPoints;
        }
    }
    private void OnLoadData(LoadedData data)
    {
        dynamicParameters = data.GetData("DynamicParameters", UNIQUE_ID, dynamicParameters);
        selectableType = (SelectableType)data.GetData("SelectableType", UNIQUE_ID, (int)selectableType);
        PawnController.Instance.UpdateStartReloadButtonColor();
    }
    private void OnSaveData(System.Action<SaveRecord[], string> addSaveData)
    {
        addSaveData(new SaveRecord[] {
            new SaveRecord() {
                recordName = "DynamicParameters",
                recordType = SaveRecordType.dictionary,
                dictValue = dynamicParameters
            },
            new SaveRecord() {
                recordName = "SelectableType",
                recordType = SaveRecordType.integerNumber,
                intValue = (int)selectableType
            }
        }, UNIQUE_ID);
    }
    private void ResetKeys()
    {
        var dict = initialPawnData.GetParametersDict();
        if (dict.ContainsKey(INITIAL_HP_KEY))
        {
            dynamicParameters[AVAILABLE_HEALTH_KEY] = dict[INITIAL_HP_KEY];
        }
        if (dict.ContainsKey(INITIAL_AVAILABLE_DISTANCE_KEY))
        {
            dynamicParameters[AVAILABLE_DISTANCE_KEY] = dict[INITIAL_AVAILABLE_DISTANCE_KEY];
        }
        if (!dynamicParameters.ContainsKey(MAG_AMOUNT_KEY))
        {
            dynamicParameters[MAG_AMOUNT_KEY] = dict[INITIAL_MAG_AMOUNT_KEY];
        }
        if (!dynamicParameters.ContainsKey(TOTAL_AMMO_KEY))
        {
            dynamicParameters[TOTAL_AMMO_KEY] = dict[INITIAL_TOTAL_AMMO_KEY];
        }
        dynamicParameters[LAST_ROUND_WALKED_KEY] = 0f;
        dynamicParameters[WALKED_KEY] = 0f;

        dynamicParameters[LAST_ROUND_SHOOTED_AMOUNT_KEY] = 0f;
        dynamicParameters[SHOOTED_AMOUNT_KEY] = 0f;

        dynamicParameters[LAST_ROUND_MELEE_AMOUNT_KEY] = 0f;
        dynamicParameters[MELEE_AMOUNT_KEY] = 0f;
        dynamicParameters[MOVES_TO_SKIP_KEY] = 0f;
        dynamicParameters[IS_SHOOT_ON_MOVE_KEY] = 0f;
        dynamicParameters[AMOUNT_OF_DEFENDED_HITS_KEY] = 0f;
        dynamicParameters[AMOUNT_OF_HEALINGS_KEY] = 0f;
    }

    public void FillFormulaData(FormulaDataMonoBase formulaData, string prefix)
    {
        bool exportCombatTurnShotMelee =
            HandleInittingGlobalVars.globalParameters != null
            && HandleInittingGlobalVars.globalParameters.parametersDict.TryGetValue(
                HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY, out float stepByStepFlag)
            && stepByStepFlag > 0.5f;

        formulaData.parametersDict[prefix + LAST_ROUND_WALKED_KEY] = dynamicParameters[LAST_ROUND_WALKED_KEY];
        formulaData.parametersDict[prefix + WALKED_KEY] = dynamicParameters[WALKED_KEY];
        formulaData.parametersDict[prefix + LAST_ROUND_SHOOTED_AMOUNT_KEY] =
            exportCombatTurnShotMelee ? dynamicParameters[LAST_ROUND_SHOOTED_AMOUNT_KEY] : 0f;
        formulaData.parametersDict[prefix + SHOOTED_AMOUNT_KEY] =
            exportCombatTurnShotMelee ? dynamicParameters[SHOOTED_AMOUNT_KEY] : 0f;
        formulaData.parametersDict[prefix + LAST_ROUND_MELEE_AMOUNT_KEY] =
            exportCombatTurnShotMelee ? dynamicParameters[LAST_ROUND_MELEE_AMOUNT_KEY] : 0f;
        formulaData.parametersDict[prefix + MELEE_AMOUNT_KEY] =
            exportCombatTurnShotMelee ? dynamicParameters[MELEE_AMOUNT_KEY] : 0f;
        formulaData.parametersDict[prefix + MOVES_TO_SKIP_KEY] = dynamicParameters[MOVES_TO_SKIP_KEY];
        formulaData.parametersDict[prefix + IS_SHOOT_ON_MOVE_KEY] = dynamicParameters[IS_SHOOT_ON_MOVE_KEY];
        formulaData.parametersDict[prefix + MAG_AMOUNT_KEY] = dynamicParameters[MAG_AMOUNT_KEY];
        formulaData.parametersDict[prefix + TOTAL_AMMO_KEY] = dynamicParameters[TOTAL_AMMO_KEY];
        formulaData.parametersDict[prefix + AMOUNT_OF_DEFENDED_HITS_KEY] = dynamicParameters[AMOUNT_OF_DEFENDED_HITS_KEY];
        formulaData.parametersDict[prefix + AMOUNT_OF_HEALINGS_KEY] = dynamicParameters[AMOUNT_OF_HEALINGS_KEY];
    }

    public static void PreFillFormulaData(FormulaDataMonoBase formulaData, string prefix)
    {
        formulaData.parametersDict[prefix + LAST_ROUND_WALKED_KEY] = 0f;
        formulaData.parametersDict[prefix + WALKED_KEY] = 0f;
        formulaData.parametersDict[prefix + LAST_ROUND_SHOOTED_AMOUNT_KEY] = 0f;
        formulaData.parametersDict[prefix + SHOOTED_AMOUNT_KEY] = 0f;
        formulaData.parametersDict[prefix + LAST_ROUND_MELEE_AMOUNT_KEY] = 0f;
        formulaData.parametersDict[prefix + MELEE_AMOUNT_KEY] = 0f;
        formulaData.parametersDict[prefix + MOVES_TO_SKIP_KEY] = 0f;
        formulaData.parametersDict[prefix + IS_SHOOT_ON_MOVE_KEY] = 0f;
        formulaData.parametersDict[prefix + MAG_AMOUNT_KEY] = 0f;
        formulaData.parametersDict[prefix + TOTAL_AMMO_KEY] = 0f;
        formulaData.parametersDict[prefix + AMOUNT_OF_DEFENDED_HITS_KEY] = 0f;
        formulaData.parametersDict[prefix + AMOUNT_OF_HEALINGS_KEY] = 0f;
    }

    public float GetParameterValue(string parameterName)
    {
        if (dynamicParameters.ContainsKey(parameterName))
        {
            return dynamicParameters[parameterName];
        }
        if (initialPawnData.GetParametersDict().ContainsKey(parameterName))
        {
            return initialPawnData.GetParametersDict()[parameterName];
        }
        // Debug.LogError($"Parameter {parameterName} not found in initialPlayerData");
        // return 12f;
        throw new System.Exception($"Parameter {parameterName} not found in initialPlayerData");
    }

    public void SetParameterValue(string parameterName, float value)
    {
        if (!dynamicParameters.ContainsKey(parameterName))
        {
            Debug.LogWarning($"Parameter {parameterName} not found in dynamicParameters of pawn data controller, creating new one");
        }
        dynamicParameters[parameterName] = value;
        if (GameUI.Instance != null) GameUI.Instance.OnChangeStats();
        if (parameterName == AVAILABLE_DISTANCE_KEY || parameterName == AVAILABLE_HEALTH_KEY || parameterName == AMOUNT_OF_HEALINGS_KEY)
        {
            if (GameUI.Instance != null) GameUI.Instance.UpdatePlayerData();
        }
    }

    public void ResetActionPoints()
    {
        SetParameterValue(AVAILABLE_DISTANCE_KEY, GetParameterValue(INITIAL_AVAILABLE_DISTANCE_KEY));

        SetParameterValue(LAST_ROUND_WALKED_KEY, GetParameterValue(WALKED_KEY));
        SetParameterValue(WALKED_KEY, 0f);

        SetParameterValue(LAST_ROUND_SHOOTED_AMOUNT_KEY, GetParameterValue(SHOOTED_AMOUNT_KEY));
        SetParameterValue(SHOOTED_AMOUNT_KEY, 0f);

        SetParameterValue(LAST_ROUND_MELEE_AMOUNT_KEY, GetParameterValue(MELEE_AMOUNT_KEY));
        SetParameterValue(MELEE_AMOUNT_KEY, 0f);

        float movesToSkip = GetParameterValue(MOVES_TO_SKIP_KEY);
        SetParameterValue(MOVES_TO_SKIP_KEY, movesToSkip > 0 ? movesToSkip - 1 : 0);

        SetParameterValue(IS_SHOOT_ON_MOVE_KEY, 0f);

        // SetParameterValue(AMOUNT_OF_DEFENDED_HITS_KEY, 0f); // do we need to reset this?
    }

    public static float CalculateLineStringDistance(Vector3[] points)
    {
        if (points == null || points.Length == 0)
        {
            return 0f;
        }
        float distance = 0f;
        for (int i = 0; i < points.Length - 1; i++)
        {
            distance += Vector3.Distance(points[i], points[i + 1]);
        }
        return distance;
    }

    public List<string> GetParameterNames()
    {
        return initialPawnData.GetParameterNames();
    }

    public Dictionary<string, float> parametersDict
    {
        get
        {
            return initialPawnData.GetParametersDict();
        }
    }

    public void IsStepByStepOff()
    {
        SetParameterValue(MOVES_TO_SKIP_KEY, 0f);
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