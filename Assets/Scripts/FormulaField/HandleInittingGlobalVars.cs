
using UnityEngine;

public class HandleInittingGlobalVars : MonoBehaviour
{
    [SerializeField]
    private GlobalParameters globalParametersSettable;
    [SerializeField]
    private PlayerData playerMustHaveParamsSettable;
    [SerializeField]
    private ParameteredScriptableObject enemyMustHaveParamsSettable;
    public static ParameteredScriptableObject enemyMustHaveParams;
    public static PlayerData playerMustHaveParams;
    public static GlobalParameters globalParameters;
    void Awake()
    {
        if (globalParameters == null)
            globalParameters = Resources.Load<GlobalParameters>("GlobalParameters");
        GlobalParameters.InitWith(globalParameters);
        if (playerMustHaveParams == null)
            playerMustHaveParams = Resources.Load<PlayerData>("PlayerMustHaveParams");
        if (enemyMustHaveParams == null)
            enemyMustHaveParams = Resources.Load<ParameteredScriptableObject>("EnemyMustHaveParams");
    }

    void OnValidate()
    {
        if (playerMustHaveParamsSettable != null && playerMustHaveParams != playerMustHaveParamsSettable)
        {
            playerMustHaveParams = playerMustHaveParamsSettable;
        }
        if (enemyMustHaveParamsSettable != null && enemyMustHaveParams != enemyMustHaveParamsSettable)
        {
            enemyMustHaveParams = enemyMustHaveParamsSettable;
        }
        if (globalParametersSettable != null && globalParameters != globalParametersSettable)
        {
            globalParameters = globalParametersSettable;
            GlobalParameters.InitWith(globalParameters);
        }
    }
}
