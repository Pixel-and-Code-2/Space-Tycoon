
using System;
using UnityEngine;

public class HandleInittingGlobalVars : MonoBehaviour
{
    [SerializeField]
    private ParameteredScriptableObject globalParametersSettable;
    [SerializeField]
    private ParameteredScriptableObject pawnMustHaveParamsSettable;
    public static ParameteredScriptableObject pawnMustHaveParams;
    public static ParameteredScriptableObject globalParameters;
    public static Action onParamsUpdated;
    void Awake()
    {
        if (globalParameters == null)
            globalParameters = GetDataAsset("GlobalParameters");
        if (pawnMustHaveParams == null)
            pawnMustHaveParams = GetDataAsset("PawnMustHaveParams");
        onParamsUpdated?.Invoke();
    }

    private ParameteredScriptableObject GetDataAsset(string fileName)
    {
        return Resources.Load<ParameteredScriptableObject>(fileName);
    }

    void OnValidate()
    {
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
        if (doUpdate)
        {
            onParamsUpdated?.Invoke();
        }
    }
}
