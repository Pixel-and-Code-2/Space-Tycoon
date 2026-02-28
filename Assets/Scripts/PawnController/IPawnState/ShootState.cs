using UnityEngine;

[RequireComponent(typeof(FormulaDataMonoBase))]
public class ShootState : IPawnState
{
    private PathDrawerWithText pathDrawer => PawnController.Instance.pathDrawer;
    private IControlableSelectable controlableSelectable => PawnController.Instance.currentSelectedPawn;
    private IAttackableSelectable lastAttackableSelectableCached = null;
    // private FormulaDataMonoBase shootingFormulaData;
    [SerializeField]
    private FormulaFieldWithMemo calculateShootDamage;
    [SerializeField]
    private FormulaFieldWithMemo calculateShootAccuracy;

    public (IFormulaData, string) GetShootFormulaData() => (HandleInittingGlobalVars.mainCalculatedFormulaData, "Calculated");
    private IFormulaData initiatorFormulaData => controlableSelectable == null ? HandleInittingGlobalVars.pawnMustHaveParams : controlableSelectable.GetFormulaData();
    public (IFormulaData, string) GetInitiatorFormulaData() => (initiatorFormulaData, "Initiator");
    private IFormulaData lastAttackableSelectableFormulaData => lastAttackableSelectableCached == null ? HandleInittingGlobalVars.pawnMustHaveParams : lastAttackableSelectableCached.GetFormulaData();
    public (IFormulaData, string) GetTargetFormulaData() => (lastAttackableSelectableFormulaData, "Target");

    void Awake()
    {
        // if (shootingFormulaData == null || shootingFormulaData.owner != this)
        // {
        //     shootingFormulaData = FormulaDataMonoBase.GetValidFormulaData(this);
        // }
        RefillFormulas();
    }

    void OnValidate()
    {
        // if (shootingFormulaData == null || shootingFormulaData.owner != this)
        // {
        //     shootingFormulaData = FormulaDataMonoBase.GetValidFormulaData(this);
        // }
        // if (shootingFormulaData.parametersDict.Count < 1)
        // {
        //     // Debug.LogWarning("formulaData is empty");
        //     shootingFormulaData.FullFillWithParameters(new string[] { SHOOTING_DISTANCE_LABEL });
        // }
        RefillFormulas();
        calculateShootAccuracy.OnParamsUpdated();
        calculateShootDamage.OnParamsUpdated();
    }

    private void RefillFormulas()
    {
        if (calculateShootDamage == null)
        {
            calculateShootDamage = new FormulaFieldWithMemo();
        }
        if (calculateShootDamage.memorySize != 3)
        {
            calculateShootDamage.ClearMemorizedDatasets();
            calculateShootDamage.AddMemorizedDataset(GetShootFormulaData);
            calculateShootDamage.AddMemorizedDataset(GetInitiatorFormulaData);
            calculateShootDamage.AddMemorizedDataset(GetTargetFormulaData);
        }
        if (calculateShootAccuracy == null)
        {
            calculateShootAccuracy = new FormulaFieldWithMemo();
        }
        if (calculateShootAccuracy.memorySize != 3)
        {
            calculateShootAccuracy.ClearMemorizedDatasets();
            calculateShootAccuracy.AddMemorizedDataset(GetShootFormulaData);
            calculateShootAccuracy.AddMemorizedDataset(GetInitiatorFormulaData);
            calculateShootAccuracy.AddMemorizedDataset(GetTargetFormulaData);
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
                controlableSelectable.OnShoot(worldPoint);
                // Debug.Log("targetPawn: " + attackableSelectable + " " + randomValue + " < " + GetShootAccuracy(lastHitPoint));
                if (randomValue < GetShootAccuracy(attackableSelectable) && attackableSelectable != null)
                {
                    attackableSelectable.OnGetHit(GetShootDamage(attackableSelectable));
                }
                else
                {
                    UI3DManager.Instance.ShowMessage("Miss", worldPoint, Color.yellow);
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
            float accuracy = GetShootAccuracy(selectable as IAttackableSelectable);
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

    private float GetShootDamage(IAttackableSelectable attackableSelectable)
    {
        // shootingFormulaData.parametersDict[SHOOTING_DISTANCE_LABEL] = Vector3.Distance(controlableSelectable.GetTransform().position, attackableSelectable.GetTransform().position);
        HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict[HandleInittingGlobalVars.PAWN_DISTANCE_LABEL] = Vector3.Distance(controlableSelectable.GetTransform().position, attackableSelectable.GetTransform().position);
        return calculateShootDamage.EvaluateFormula(
            new System.Collections.Generic.Dictionary<string, float>[] {
                // shootingFormulaData.parametersDict,
                HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict,
                controlableSelectable.GetFormulaData().parametersDict, attackableSelectable.GetFormulaData().parametersDict,
            }
        );
    }

    private float GetShootAccuracy(IAttackableSelectable attackableSelectable)
    {

        RaycastHit hitInfo;
        Vector3 origin = controlableSelectable.GetTransform().position;
        Vector3 targetPoint = attackableSelectable.GetTransform().position;
        Vector3 direction = (targetPoint - origin).normalized;
        float distance = Vector3.Distance(origin, targetPoint);
        // shootingFormulaData.parametersDict[SHOOTING_DISTANCE_LABEL] = distance;
        HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict[HandleInittingGlobalVars.PAWN_DISTANCE_LABEL] = distance;

        int wallLayer = LayerMask.NameToLayer("Wall");
        int wallMask = 1 << wallLayer;
        if (Physics.Raycast(origin, direction, out hitInfo, distance, wallMask))
        {
            return 0f;
        }
        float res = calculateShootAccuracy.EvaluateFormula(
            new System.Collections.Generic.Dictionary<string, float>[] {
                // shootingFormulaData.parametersDict,
                HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict,
                controlableSelectable.GetFormulaData().parametersDict, attackableSelectable.GetFormulaData().parametersDict
            }
        );
        return res;
    }
}