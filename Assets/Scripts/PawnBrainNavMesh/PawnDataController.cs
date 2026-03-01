using System.Collections.Generic;
using UnityEngine;

public class PawnDataController : MonoBehaviour, IFormulaData
{
    // ToDo (deprecated?): action must vary based on the type of the pawn's team. It can be either player or enemy, but now it is ONLY player.
    // Static data storages
    [SerializeField]
    private ParameteredScriptableObject initialPawnData;
    private ParameteredScriptableObject pawnDataCached;
    // Additional developing params
    [Header("Additional Developing params")]
    [SerializeField, Tooltip("Max distance from mouse to walkable area, to show path")]
    public float maxSampleDistance { get; private set; } = 5f;
    [SerializeField, Tooltip("Force of push when pawn collides with obstacle, 0 to disable")]
    public float obstaclePushForce { get; private set; } = 10f;
    [SerializeField, Tooltip("Override of vertical push when pawn collides with obstacle, -1 to disable")]
    public float verticalPushOverride { get; private set; } = 0.2f;

    // Dynamic parameters
    public Dictionary<string, float> dynamicParameters = new Dictionary<string, float>();

    public const string AVAILABLE_DISTANCE_KEY = "AvailableDistance"; // working
    public const string INITIAL_HP_KEY = "HP";
    public const string INITIAL_AVAILABLE_DISTANCE_KEY = "SPD";
    public const string AVAILABLE_HEALTH_KEY = "AvailableHealth";  // working
    public const string LAST_ROUND_WALKED_KEY = "LastRoundWalked";
    public const string WALKED_KEY = "WalkedDistance";
    public const string LAST_ROUND_SHOOTED_AMOUNT_KEY = "LastRoundShotAmount";
    public const string SHOOTED_AMOUNT_KEY = "ShotAmount";
    public const string LAST_ROUND_MELEE_AMOUNT_KEY = "LastRoundMeleeAmount";
    public const string MELEE_AMOUNT_KEY = "MeleeAmount";
    [SerializeField]
    public SelectableType selectableType = SelectableType.Player;

    private void ResetKeys()
    {
        var dict = initialPawnData.GetParametersDict();
        // dynamicParameters[AVAILABLE_DISTANCE_KEY] = 12.5f;
        // dynamicParameters[AVAILABLE_HEALTH_KEY] = 12.5f;
        if (dict.ContainsKey(INITIAL_HP_KEY))
        {
            dynamicParameters[AVAILABLE_HEALTH_KEY] = dict[INITIAL_HP_KEY];
        }
        if (dict.ContainsKey(INITIAL_AVAILABLE_DISTANCE_KEY))
        {
            dynamicParameters[AVAILABLE_DISTANCE_KEY] = dict[INITIAL_AVAILABLE_DISTANCE_KEY];
        }
        dynamicParameters[LAST_ROUND_WALKED_KEY] = 0f;
        dynamicParameters[WALKED_KEY] = 0f;

        dynamicParameters[LAST_ROUND_SHOOTED_AMOUNT_KEY] = 0f;
        dynamicParameters[SHOOTED_AMOUNT_KEY] = 0f;

        dynamicParameters[LAST_ROUND_MELEE_AMOUNT_KEY] = 0f;
        dynamicParameters[MELEE_AMOUNT_KEY] = 0f;
    }

    public void FillFormulaData(FormulaDataMonoBase formulaData, string prefix)
    {
        formulaData.parametersDict[prefix + LAST_ROUND_WALKED_KEY] = dynamicParameters[LAST_ROUND_WALKED_KEY];
        formulaData.parametersDict[prefix + WALKED_KEY] = dynamicParameters[WALKED_KEY];
        formulaData.parametersDict[prefix + LAST_ROUND_SHOOTED_AMOUNT_KEY] = dynamicParameters[LAST_ROUND_SHOOTED_AMOUNT_KEY];
        formulaData.parametersDict[prefix + SHOOTED_AMOUNT_KEY] = dynamicParameters[SHOOTED_AMOUNT_KEY];
        formulaData.parametersDict[prefix + LAST_ROUND_MELEE_AMOUNT_KEY] = dynamicParameters[LAST_ROUND_MELEE_AMOUNT_KEY];
        formulaData.parametersDict[prefix + MELEE_AMOUNT_KEY] = dynamicParameters[MELEE_AMOUNT_KEY];
    }

    public static void PreFillFormulaData(FormulaDataMonoBase formulaData, string prefix)
    {
        formulaData.parametersDict[prefix + LAST_ROUND_WALKED_KEY] = 0f;
        formulaData.parametersDict[prefix + WALKED_KEY] = 0f;
        formulaData.parametersDict[prefix + LAST_ROUND_SHOOTED_AMOUNT_KEY] = 0f;
        formulaData.parametersDict[prefix + SHOOTED_AMOUNT_KEY] = 0f;
        formulaData.parametersDict[prefix + LAST_ROUND_MELEE_AMOUNT_KEY] = 0f;
        formulaData.parametersDict[prefix + MELEE_AMOUNT_KEY] = 0f;
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
    }

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
        ResetKeys();
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

    private void ResetActionPoints()
    {
        SetParameterValue(AVAILABLE_DISTANCE_KEY, GetParameterValue(INITIAL_AVAILABLE_DISTANCE_KEY));

        SetParameterValue(LAST_ROUND_WALKED_KEY, GetParameterValue(WALKED_KEY));
        SetParameterValue(WALKED_KEY, 0f);

        SetParameterValue(LAST_ROUND_SHOOTED_AMOUNT_KEY, GetParameterValue(SHOOTED_AMOUNT_KEY));
        SetParameterValue(SHOOTED_AMOUNT_KEY, 0f);

        SetParameterValue(LAST_ROUND_MELEE_AMOUNT_KEY, GetParameterValue(MELEE_AMOUNT_KEY));
        SetParameterValue(MELEE_AMOUNT_KEY, 0f);
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

}