using UnityEngine;

[RequireComponent(typeof(PawnBrain))]
public class PawnHealing : IScriptForClickable
{
    [SerializeField]
    private PawnBrain pawnBrain;
    public override void OnComplete()
    {
        base.OnComplete();
        if (!IsTask || IsSide) return;
        pawnBrain.OnHeal();
    }
}