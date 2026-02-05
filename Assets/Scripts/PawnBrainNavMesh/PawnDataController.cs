using System.Collections.Generic;
using UnityEngine;

public class PawnDataController : MonoBehaviour
{
    // ToDo: action must vary based on the type of the pawn's team. It can be either player or enemy, but now it is ONLY player.
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
    private Dictionary<string, float> dynamicParameters = new Dictionary<string, float>();
    public const string AVAILABLE_DISTANCE_KEY = "AvailableDistance";
    public const string INITIAL_HP_KEY = "HP";
    public const string INITIAL_AVAILABLE_DISTANCE_KEY = "SPD";
    public const string AVAILABLE_HEALTH_KEY = "AvailableHealth";

    private void ResetKeys()
    {
        var dict = initialPawnData.GetParametersDict();
        dynamicParameters[AVAILABLE_DISTANCE_KEY] = 0f;
        dynamicParameters[AVAILABLE_HEALTH_KEY] = 0f;
        if (dict.ContainsKey(INITIAL_HP_KEY))
        {
            dynamicParameters[AVAILABLE_HEALTH_KEY] = dict[INITIAL_HP_KEY];
        }
        if (dict.ContainsKey(INITIAL_AVAILABLE_DISTANCE_KEY))
        {
            dynamicParameters[AVAILABLE_DISTANCE_KEY] = dict[INITIAL_AVAILABLE_DISTANCE_KEY];
        }
    }

    void Awake()
    {
        ResetKeys();
    }

    public float GetParameterValue(string parameterName)
    {
        if (dynamicParameters.ContainsKey(parameterName))
        {
            return dynamicParameters[parameterName];
        }
        if (initialPawnData.parametersDict.ContainsKey(parameterName))
        {
            return initialPawnData.parametersDict[parameterName];
        }
        Debug.LogError($"Parameter {parameterName} not found in initialPlayerData");
        return 0f;
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
            TurnManager.Instance.OnPlayerTurnStart += ResetActionPoints;
        }
    }

    void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= ResetActionPoints;
        }
    }

    private void ResetActionPoints()
    {
        SetParameterValue(AVAILABLE_DISTANCE_KEY, 12f);
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

}