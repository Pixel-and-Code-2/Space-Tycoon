using UnityEngine;
using Unity.AI.Navigation;

public class SecondDoorOpen : IScriptForClickable
{
    [SerializeField]
    private NavMeshLink navMeshLink;
    void Start()
    {
        navMeshLink.enabled = false;
    }
    public override void OnComplete()
    {
        navMeshLink.enabled = true;
    }
}