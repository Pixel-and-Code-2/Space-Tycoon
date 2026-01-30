using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class NamedFloat
{
    public string name;
    public float value;
};


[CreateAssetMenu(fileName = "PlayerData", menuName = "PlayerData", order = 1)]
public class PlayerData : ParameteredScriptableObject
{
    [Header("Additional Developing params")]
    [SerializeField, Tooltip("max distance from mouse to walkable area, to show path")]
    public float maxSampleDistance = 5f;
    public float obstaclePushForce = 10f;
    public float verticalPushOverride = 0.2f;


    public const string AVAILABLE_DISTANCE_KEY = "AvailableDistance";



    public new void RebuildParametersDict()
    {
        parametersDict.Clear();
        parametersDict[AVAILABLE_DISTANCE_KEY] = -1f;
        foreach (var parameter in GetParameters())
        {
            parameter.name = ParameteredScriptableObject.processParameterName(parameter.name);
            if (parameter.name != null && parameter.name != "")
                parametersDict[parameter.name] = parameter.value;
        }
    }

    public Dictionary<string, float> GetCopyOfParameters()
    {
        return new Dictionary<string, float>(parametersDict);
    }
}