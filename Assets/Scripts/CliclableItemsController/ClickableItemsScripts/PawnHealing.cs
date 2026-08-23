using UnityEngine;

[RequireComponent(typeof(PawnBrain))]
public class PawnHealing : IScriptForClickable
{
    [SerializeField]
    private PawnBrain pawnBrain;

    void OnValidate()
    {
        if (pawnBrain == null) pawnBrain = GetComponent<PawnBrain>();
    }

    public static bool CanRevive(PawnBrain brain)
    {
        if (brain == null || brain.PawnData == null) return false;
        if (brain.GetSelectableType() != SelectableType.Dead) return false;
        float max = 0f;
        if (HandleInittingGlobalVars.globalParameters != null
            && HandleInittingGlobalVars.globalParameters.parametersDict.ContainsKey(HandleInittingGlobalVars.AMOUNT_OF_HEALINGS_KEY))
            max = HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.AMOUNT_OF_HEALINGS_KEY];
        float used = brain.PawnData.HealingsAmount;
        return max - used > 0.5f;
    }

    public override void OnComplete()
    {
        base.OnComplete();
        if (IsTask && IsSide) return;
        if (pawnBrain == null) pawnBrain = GetComponent<PawnBrain>();
        if (!CanRevive(pawnBrain)) return;
        pawnBrain.OnHeal();
    }
}
