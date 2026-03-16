using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(FormulaDataMonoBase))]
public class MeleeState : IPawnState
{
    private PathDrawerWithText pathDrawer => PawnController.Instance.pathDrawer;
    private IControlableSelectable controlableSelectable => PawnController.Instance.currentSelectedPawn;

    private IAttackableSelectable lastAttackableSelectableCached = null;
    [SerializeField]
    private FormulaFieldWithMemo calculateMeleeDamage;
    [SerializeField]
    private FormulaFieldWithMemo calculateMeleeAccuracy;
    [SerializeField]
    private int tooFarCode = -1;
    public List<ExitCode> exitCodes;
    [SerializeField]
    private FormulaFieldWithMemo calculateMeleeDefense1;
    [SerializeField]
    private string defenseMessage1;
    [SerializeField]
    private FormulaFieldWithMemo calculateMeleeDefense2;
    [SerializeField]
    private string defenseMessage2;
    public (IFormulaData, string) GetMeleeFormulaData() => (HandleInittingGlobalVars.mainCalculatedFormulaData, "Calculated");
    private IFormulaData initiatorFormulaData => controlableSelectable == null ? HandleInittingGlobalVars.pawnMustHaveParams : controlableSelectable.GetFormulaData();
    public (IFormulaData, string) GetInitiatorFormulaData() => (initiatorFormulaData, "Initiator");
    private IFormulaData lastAttackableSelectableFormulaData => lastAttackableSelectableCached == null ? HandleInittingGlobalVars.pawnMustHaveParams : lastAttackableSelectableCached.GetFormulaData();
    public (IFormulaData, string) GetTargetFormulaData() => (lastAttackableSelectableFormulaData, "Target");

    void Awake()
    {
        RefillFormulas();
    }

    void OnValidate()
    {
        if (calculateMeleeAccuracy == null)
        {
            Debug.LogWarning("calculateMeleeAccuracy is null" + gameObject.name);

        }
        RefillFormulas();
        calculateMeleeAccuracy.OnParamsUpdated();
        calculateMeleeDamage.OnParamsUpdated();
    }

    private void RefillFormulas()
    {
        if (calculateMeleeDamage == null)
        {
            calculateMeleeDamage = new FormulaFieldWithMemo();
        }
        if (calculateMeleeDamage.memorySize != 3)
        {
            calculateMeleeDamage.ClearMemorizedDatasets();
            calculateMeleeDamage.AddMemorizedDataset(GetMeleeFormulaData);
            calculateMeleeDamage.AddMemorizedDataset(GetInitiatorFormulaData);
            calculateMeleeDamage.AddMemorizedDataset(GetTargetFormulaData);
        }
        if (calculateMeleeAccuracy == null)
        {
            calculateMeleeAccuracy = new FormulaFieldWithMemo();
        }
        if (calculateMeleeAccuracy.memorySize != 3)
        {
            calculateMeleeAccuracy.ClearMemorizedDatasets();
            calculateMeleeAccuracy.AddMemorizedDataset(GetMeleeFormulaData);
            calculateMeleeAccuracy.AddMemorizedDataset(GetInitiatorFormulaData);
            calculateMeleeAccuracy.AddMemorizedDataset(GetTargetFormulaData);
        }
        if (calculateMeleeDefense1 == null)
        {
            calculateMeleeDefense1 = new FormulaFieldWithMemo();
        }
        if (calculateMeleeDefense1.memorySize != 3)
        {
            calculateMeleeDefense1.ClearMemorizedDatasets();
            calculateMeleeDefense1.AddMemorizedDataset(GetMeleeFormulaData);
            calculateMeleeDefense1.AddMemorizedDataset(GetInitiatorFormulaData);
            calculateMeleeDefense1.AddMemorizedDataset(GetTargetFormulaData);
        }
        if (calculateMeleeDefense2 == null)
        {
            calculateMeleeDefense2 = new FormulaFieldWithMemo();
        }
        if (calculateMeleeDefense2.memorySize != 3)
        {
            calculateMeleeDefense2.ClearMemorizedDatasets();
            calculateMeleeDefense2.AddMemorizedDataset(GetMeleeFormulaData);
            calculateMeleeDefense2.AddMemorizedDataset(GetInitiatorFormulaData);
            calculateMeleeDefense2.AddMemorizedDataset(GetTargetFormulaData);
        }
    }

    void OnDisable()
    {
        pathDrawer.SetVisible(false);
    }

    public override void HandleDoingSth(Vector3 worldPoint, ISelectable selectable)
    {
        if (selectable is IAttackableSelectable attackableSelectable)
        {

            if (worldPoint != Vector3.zero && selectable != null)
            {
                float randomValue = Random.value;
                controlableSelectable.OnMelee(worldPoint);
                float chance = GetMeleeAccuracy(attackableSelectable);
                float defenseChance1 = GetMeleeDefense1(attackableSelectable);
                float defenseChance2 = GetMeleeDefense2(attackableSelectable);
                (string message, Color color) = GetMessage(chance);
                if (message != null)
                {
                    UI3DManager.Instance.ShowMessage(message, worldPoint, color);
                }
                if (randomValue < chance)
                {
                    bool isDefended = false;
                    randomValue = Random.value;
                    if (randomValue < defenseChance1)
                    {
                        UI3DManager.Instance.ShowMessage(defenseMessage1, worldPoint, Color.red);
                        isDefended = true;
                    }
                    randomValue = Random.value;
                    if (randomValue < defenseChance2)
                    {
                        UI3DManager.Instance.ShowMessage(defenseMessage2, worldPoint, Color.red);
                        isDefended = true;
                    }
                    if (isDefended)
                    {
                        attackableSelectable.OnGetDefendedHit(worldPoint - controlableSelectable.GetTransform().position, true);
                    }
                    else
                    {
                        attackableSelectable.OnGetHit(GetMeleeDamage(attackableSelectable));
                    }
                }
                else
                {
                    if (message == null)
                    {
                        UI3DManager.Instance.ShowMessage("Miss", worldPoint, Color.yellow);
                    }
                }
            }
        }
    }

    public override void HandleUIDrawing(ISelectable selectable, Vector3 worldPoint, Vector2 screenPoint, ScreenCastHitResult hit)
    {
        if (hit == ScreenCastHitResult.NoHit) return;
        Vector3 originPoint = controlableSelectable.GetTransform().position;

        // we don't need to shoot at same pawn, pawn from same team or dead pawn
        if (hit == ScreenCastHitResult.SelectableHit && (
            selectable == controlableSelectable ||
                selectable.GetSelectableType() == controlableSelectable.GetSelectableType() ||
                selectable.GetSelectableType() == SelectableType.Dead
            )
        ) hit = ScreenCastHitResult.FloorHit;

        if (hit == ScreenCastHitResult.SelectableHit)
        {
            float accuracy = GetMeleeAccuracy(selectable as IAttackableSelectable);
            (string message, Color color) = GetMessage(accuracy);
            if (message != null)
            {
                pathDrawer.SetTextColor(color);
                pathDrawer.SetText(
                    Vector3.Distance(originPoint, worldPoint).ToString("F1") + "m, " + message,
                    screenPoint
                );
            }
            else
            {
                if (accuracy < 0.1f)
                {
                    pathDrawer.SetTextColor(Color.red);
                }
                else if (accuracy > 0.9f)
                {
                    pathDrawer.SetTextColor(Color.green);
                }
                else
                {
                    float h = (accuracy - 0.1f) / 0.8f * 0.33f;
                    pathDrawer.SetTextColor(Color.HSVToRGB(h, 1f, 1f));
                }
                pathDrawer.SetText(
                    Vector3.Distance(originPoint, worldPoint).ToString("F1") + "m, " + (accuracy * 100f).ToString("F0") + "%",
                    screenPoint
                );
            }
        }
        else if (hit == ScreenCastHitResult.FloorHit)
        {
            pathDrawer.SetTextColor(Color.red);
            pathDrawer.SetText(
                Vector3.Distance(originPoint, worldPoint).ToString("F1") + "m",
                screenPoint
            );
        }
        pathDrawer.SetPathPoints(new Vector3[] { controlableSelectable.GetTransform().position, worldPoint }, null);
        pathDrawer.SetVisible(true);
    }

    private float GetMeleeDamage(IAttackableSelectable attackableSelectable)
    {
        PawnController.SetCalculatableParamsForTwoPawns(controlableSelectable, attackableSelectable);

        return calculateMeleeDamage.EvaluateFormula(
            new System.Collections.Generic.Dictionary<string, float>[] {
                // meleeFormulaData.parametersDict,
                HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict,
                controlableSelectable.GetFormulaData().parametersDict,
                attackableSelectable.GetFormulaData().parametersDict,
            }
        );
    }

    private float GetMeleeAccuracy(IAttackableSelectable attackableSelectable)
    {
        PawnController.SetCalculatableParamsForTwoPawns(controlableSelectable, attackableSelectable);

        float res = calculateMeleeAccuracy.EvaluateFormula(
            new System.Collections.Generic.Dictionary<string, float>[] {
                // meleeFormulaData.parametersDict,
                HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict,
                controlableSelectable.GetFormulaData().parametersDict, attackableSelectable.GetFormulaData().parametersDict
            }
        );
        return res;
    }

    private float GetMeleeDefense1(IAttackableSelectable attackableSelectable)
    {
        PawnController.SetCalculatableParamsForTwoPawns(controlableSelectable, attackableSelectable);
        return calculateMeleeDefense1.EvaluateFormula(
            new System.Collections.Generic.Dictionary<string, float>[] {
                HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict,
                controlableSelectable.GetFormulaData().parametersDict,
                attackableSelectable.GetFormulaData().parametersDict
            }
        );
    }
    private float GetMeleeDefense2(IAttackableSelectable attackableSelectable)
    {
        PawnController.SetCalculatableParamsForTwoPawns(controlableSelectable, attackableSelectable);
        return calculateMeleeDefense2.EvaluateFormula(
            new System.Collections.Generic.Dictionary<string, float>[] {
                HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict,
                controlableSelectable.GetFormulaData().parametersDict,
                attackableSelectable.GetFormulaData().parametersDict
            }
        );
    }

    public override bool IsErrorChance(IAttackableSelectable attackableSelectable)
    {
        float accuracy = GetMeleeAccuracy(attackableSelectable);
        if (Mathf.Abs(accuracy - tooFarCode) < 0.01f)
        {
            return true;
        }
        return false;
    }

    private (string, Color) GetMessage(float chance)
    {
        foreach (ExitCode exitCode in exitCodes)
        {
            if (exitCode.IsEqual(chance))
            {
                return (exitCode.message, exitCode.color);
            }
        }
        return (null, Color.white);
    }
}