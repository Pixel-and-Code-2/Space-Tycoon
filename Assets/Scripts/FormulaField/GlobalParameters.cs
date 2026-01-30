using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GlobalParameters", menuName = "GlobalParameters", order = 1)]
public class GlobalParameters : ParameteredScriptableObject
{
    public static GlobalParameters Instance { get; private set; }

    public static void InitWith(GlobalParameters globalParameters)
    {
        if (Instance != null)
            return;
        if (globalParameters == null)
            return;
        Instance = globalParameters;
        Instance.RebuildParametersDict();
    }
}
