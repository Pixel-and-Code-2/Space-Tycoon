
using System;
using UnityEngine;

public class HandleInittingGlobalVars : MonoBehaviour
{
    [SerializeField]
    private GlobalParameters globalParametersSettable;
    [SerializeField]
    private PawnData pawnMustHaveParamsSettable;
    public static PawnData pawnMustHaveParams;
    public static GlobalParameters globalParameters;
    public static Action onParamsUpdated;
    void Awake()
    {
        if (globalParameters == null)
            globalParameters = Resources.Load<GlobalParameters>("GlobalParameters");
        GlobalParameters.InitWith(globalParameters);
        if (pawnMustHaveParams == null)
            pawnMustHaveParams = Resources.Load<PawnData>("PawnMustHaveParams");
        onParamsUpdated?.Invoke();
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
            pawnMustHaveParams = Resources.Load<PawnData>("PawnMustHaveParams");
            doUpdate = true;
        }
        if (pawnMustHaveParamsSettable == null)
        {
            pawnMustHaveParamsSettable = pawnMustHaveParams;
        }
        if (globalParametersSettable != null && globalParameters != globalParametersSettable)
        {
            globalParameters = globalParametersSettable;
            GlobalParameters.InitWith(globalParameters);
            doUpdate = true;
        }
        if (globalParameters == null)
        {
            globalParameters = Resources.Load<GlobalParameters>("GlobalParameters");
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
