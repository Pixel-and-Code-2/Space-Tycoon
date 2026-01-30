
using UnityEngine;

public class HandleInittingGlobalVars : MonoBehaviour
{
    [SerializeField]
    private GlobalParameters globalParameters;
    void Awake()
    {
        if (globalParameters == null)
            globalParameters = Resources.Load<GlobalParameters>("GlobalParameters");
        GlobalParameters.InitWith(globalParameters);
    }

    void OnValidate()
    {
        GlobalParameters.InitWith(globalParameters);
    }
}
