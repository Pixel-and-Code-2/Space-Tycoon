using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class NamedFloat
{
    public string name;
    public float value;
};


[CreateAssetMenu(fileName = "PlayerData", menuName = "PlayerData", order = 1)]
public class PawnData : ParameteredScriptableObject
{
    [Header("Additional Developing params")]
    [SerializeField, Tooltip("max distance from mouse to walkable area, to show path")]
    public float maxSampleDistance = 5f;
    public float obstaclePushForce = 10f;
    public float verticalPushOverride = 0.2f;



    public const string AVAILABLE_DISTANCE_KEY = "AvailableDistance";



    protected override void AddBuiltInParameters()
    {
        parametersDict[AVAILABLE_DISTANCE_KEY] = -1f;
        base.AddBuiltInParameters();
    }

    public Dictionary<string, float> GetCopyOfParameters()
    {
        if (parametersDict.Count == 0)
        {
            RebuildParametersDict();
        }
        return new Dictionary<string, float>(parametersDict);
    }
}