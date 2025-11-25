using UnityEngine;

public class PawnBrain : MonoBehaviour
{
    [SerializeField] private ModuleData mainModule;
    private ModuleData mainModuleCache;

    void OnValidate()
    {
        if (mainModule != mainModuleCache)
        {
            mainModuleCache = mainModule;

        }
    }

    void GoThroughGateway(GateWay gateway)
    {

    }
}
