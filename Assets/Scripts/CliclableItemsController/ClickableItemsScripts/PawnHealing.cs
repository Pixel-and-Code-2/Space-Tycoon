using UnityEngine;

[RequireComponent(typeof(PawnBrain))]
public class PawnHealing : IScriptForClickable
{
    [SerializeField]
    private PawnBrain pawnBrain;
    public override void OnComplete()
    {
        pawnBrain.OnHeal();
    }
}