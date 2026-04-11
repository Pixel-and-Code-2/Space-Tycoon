using UnityEngine;

public class FirstComputer : IScriptForClickable
{
    [SerializeField]
    private Material[] computerOnMaterials;
    [SerializeField]
    private Material[] systemOnMaterials;
    [SerializeField]
    private Material[] computerOffMaterials;
    [SerializeField]
    private MeshRenderer computerRenderer;
    [SerializeField]
    private MeshRenderer systemRenderer;

    public override void OnComplete()
    {
        base.OnComplete();
        systemRenderer.materials = systemOnMaterials;
    }
    public override void OnStart()
    {
        base.OnStart();
        computerRenderer.materials = computerOnMaterials;
    }
    public override void OnCancel()
    {
        base.OnCancel();
        computerRenderer.materials = computerOffMaterials;
    }

}