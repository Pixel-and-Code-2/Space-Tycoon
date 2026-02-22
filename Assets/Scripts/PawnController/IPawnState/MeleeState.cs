using UnityEngine;

[RequireComponent(typeof(FormulaDataMonoBase))]
public class MeleeState : IPawnState
{
    private PathDrawerWithText pathDrawer => PawnController.Instance?.pathDrawer ?? throw new System.Exception("PathDrawer not found");
    private IControlableSelectable controlableSelectable
    {
        get
        {
            if (PawnController.Instance == null)
            {
                Debug.LogError("PawnController.Instance is null " + gameObject.name);
                return null;
            }
            return PawnController.Instance.currentSelectedPawn;
        }
    }

    private IAttackableSelectable lastAttackableSelectableCached = null;
    private FormulaDataMonoBase meleeFormulaData;
    [SerializeField]
    private FormulaFieldWithMemo calculateMeleeDamage;
    [SerializeField]
    private FormulaFieldWithMemo calculateMeleeAccuracy;
    public static readonly string MELEE_DISTANCE_LABEL = "meleeDistance";


    public (IFormulaData, string) GetMeleeFormulaData()
    {
        if (this == null)
        {
            Debug.LogError("MeleeState is null " + gameObject.name);
            return (HandleInittingGlobalVars.pawnMustHaveParams, "Calculated");
        }
        if (meleeFormulaData == null)
        {
            meleeFormulaData = FormulaDataMonoBase.GetValidFormulaData(this);
        }
        return (meleeFormulaData, "Calculated");
    }
    public (IFormulaData, string) GetInitiatorFormulaData()
    {
        if (this == null)
        {
            Debug.LogError("MeleeState is null " + gameObject.name);
            return (HandleInittingGlobalVars.pawnMustHaveParams, "Initiator");
        }
        if (controlableSelectable == null)
        {
            return (HandleInittingGlobalVars.pawnMustHaveParams, "Initiator");
        }
        IFormulaData formulaData = controlableSelectable.GetFormulaData();
        if (formulaData == null)
        {
            return (HandleInittingGlobalVars.pawnMustHaveParams, "Initiator");
        }
        return (formulaData, "Initiator");
    }
    public (IFormulaData, string) GetTargetFormulaData()
    {
        if (this == null)
        {
            Debug.LogError("MeleeState is null " + gameObject.name);
            return (HandleInittingGlobalVars.pawnMustHaveParams, "Target");
        }
        if (lastAttackableSelectableCached == null)
        {
            return (HandleInittingGlobalVars.pawnMustHaveParams, "Target");
        }
        IFormulaData formulaData = lastAttackableSelectableCached.GetFormulaData();
        if (formulaData == null)
        {
            return (HandleInittingGlobalVars.pawnMustHaveParams, "Target");
        }
        return (formulaData, "Target");
    }

    void Awake()
    {
        if (meleeFormulaData == null || meleeFormulaData.owner != this)
        {
            meleeFormulaData = FormulaDataMonoBase.GetValidFormulaData(this);
        }
        RefillFormulas();
    }

    void OnValidate()
    {
        if (meleeFormulaData == null || meleeFormulaData.owner != this)
        {
            meleeFormulaData = FormulaDataMonoBase.GetValidFormulaData(this);
        }
        if (meleeFormulaData.parametersDict.Count < 1)
        {
            // Debug.LogWarning("formulaData is empty" + gameObject.name);
            meleeFormulaData.FullFillWithParameters(new string[] { MELEE_DISTANCE_LABEL });
        }
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
        meleeFormulaData.parametersDict[MELEE_DISTANCE_LABEL] = Vector3.Distance(controlableSelectable.GetTransform().position, attackableSelectable.GetTransform().position);
        return calculateMeleeDamage.EvaluateFormula(
            new System.Collections.Generic.Dictionary<string, float>[] {
                meleeFormulaData.parametersDict,
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
        meleeFormulaData.parametersDict[MELEE_DISTANCE_LABEL] = distance;

        int wallLayer = LayerMask.NameToLayer("Wall");
        int wallMask = 1 << wallLayer;
        if (Physics.Raycast(origin, direction, out hitInfo, distance, wallMask))
        {
            return 0f;
        }
        float res = calculateMeleeAccuracy.EvaluateFormula(
            new System.Collections.Generic.Dictionary<string, float>[] {
                meleeFormulaData.parametersDict,
                controlableSelectable.GetFormulaData().parametersDict, attackableSelectable.GetFormulaData().parametersDict
            }
        );
        return res;
    }
}