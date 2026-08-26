using UnityEngine;

[RequireComponent(typeof(PawnBrain))]
[RequireComponent(typeof(ClickableItem))]
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

    public static bool TryPayRevive(PawnDataController healerData)
    {
        if (healerData == null) return false;
        float reviveCost = GlobalSettingsAssets.GetStaminaCosts().reviveCost;
        return healerData.SpendStamina(reviveCost);
    }

    public override void OnComplete()
    {
        base.OnComplete();
        if (SaveHub.Instance != null && SaveHub.Instance.IsLoading) return;
        if (IsTask && IsSide) return;
        if (pawnBrain == null) pawnBrain = GetComponent<PawnBrain>();
        if (!CanRevive(pawnBrain))
        {
            UI3DManager.Instance.ShowMessage("Нет подъёмов", transform.position, Color.red);
            return;
        }
        pawnBrain.OnHeal();
    }
}
