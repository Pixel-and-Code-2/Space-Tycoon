using UnityEngine;
using System.Collections.Generic;
using System;

public class WalkState : IPawnState
{
    private IControlableSelectable controlableSelectable => PawnController.Instance.currentSelectedPawn;
    private PathDrawerWithText pathDrawer => PawnController.Instance.pathDrawer;
    [SerializeField]
    private FormulaFieldWithMemo calculateWalkDistance;
    public List<ExitCode> exitCodes;
    public (IFormulaData, string) GetShootFormulaData() => (HandleInittingGlobalVars.mainCalculatedFormulaData, "Calculated");
    private IFormulaData initiatorFormulaData => controlableSelectable == null ? HandleInittingGlobalVars.pawnMustHaveParams : controlableSelectable.GetFormulaData();
    public (IFormulaData, string) GetInitiatorFormulaData() => (initiatorFormulaData, "Initiator");
    void Awake()
    {
        RefillFormulas();
    }

    void OnValidate()
    {
        RefillFormulas();
        calculateWalkDistance.OnParamsUpdated();
    }

    private void RefillFormulas()
    {
        if (calculateWalkDistance == null)
        {
            calculateWalkDistance = new FormulaFieldWithMemo();
        }
        if (calculateWalkDistance.memorySize != 3)
        {
            calculateWalkDistance.ClearMemorizedDatasets();
            calculateWalkDistance.AddMemorizedDataset(GetShootFormulaData);
            calculateWalkDistance.AddMemorizedDataset(GetInitiatorFormulaData);
        }
    }
    private float GetWalkDistance(Vector3 target)
    {
        PawnController.SetCalculatableParamsForTwoPawns(controlableSelectable, target);
        float res = calculateWalkDistance.EvaluateFormula(
            new Dictionary<string, float>[] {
                HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict,
                controlableSelectable.GetFormulaData().parametersDict,
            }
        );
        return Mathf.Max(0f, res);
    }


    void OnEnable()
    {
        SliderToPawnConnector.HelperTag = "[1]->[ЛКМ]";
    }
    void OnDisable()
    {
        SliderToPawnConnector.HelperTag = "[ЛКМ]";
        pathDrawer.SetVisible(false);
    }

    public override void HandleDoingSth(Vector3 worldPoint, ISelectable selectable)
    {
        if (worldPoint != Vector3.zero && !controlableSelectable.IsMoving())
        {
            // if (Mathf.Abs(controlableSelectable.GetDynamicParameterValue(PawnDataController.IS_SHOOT_ON_MOVE_KEY) - 1f) > 0.01f && Mathf.Abs(controlableSelectable.GetDynamicParameterValue(PawnDataController.SHOOTED_AMOUNT_KEY) - 0f) > 0.01f ||
            // Mathf.Abs(controlableSelectable.GetDynamicParameterValue(PawnDataController.IS_SHOOT_ON_MOVE_KEY) - 1f) > 0.01f && Mathf.Abs(controlableSelectable.GetDynamicParameterValue(PawnDataController.MELEE_AMOUNT_KEY) - 0f) > 0.01f)
            // {
            //     return;
            // }
            float availableDistance = GetWalkDistance(worldPoint);
            if (availableDistance <= 0.0001f)
            {
                controlableSelectable.SetDynamicParameterValue(PawnDataController.AVAILABLE_DISTANCE_KEY, 0f);
                return;
            }
            controlableSelectable.SetDynamicParameterValue(PawnDataController.AVAILABLE_DISTANCE_KEY, availableDistance);
            controlableSelectable.SetDynamicParameterValue(PawnDataController.IS_SHOOT_ON_MOVE_KEY, 1f);
            PawnController.Instance.UpdateMoveOnShootButtonColor();
            controlableSelectable.OnMove(worldPoint);
            pathDrawer.SetVisible(false);
        }
    }

    public override void HandleUIDrawing(ISelectable selectable, Vector3 worldPoint, Vector2 screenPoint, ScreenCastHitResult hit)
    {
        if (controlableSelectable == null) return;
        if (controlableSelectable.IsMoving())
        {
            pathDrawer.SetVisible(false);
            return;
        }
        if (hit != ScreenCastHitResult.NoHit)
        {
            controlableSelectable.SetDynamicParameterValue(PawnDataController.AVAILABLE_DISTANCE_KEY, GetWalkDistance(worldPoint));
            (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) = controlableSelectable.GetPathPointsTo(worldPoint);

            if (pointsAvailable != null || pointsOutOfRange != null)
            {
                pathDrawer.SetText((PawnDataController.CalculateLineStringDistance(pointsAvailable) + PawnDataController.CalculateLineStringDistance(pointsOutOfRange)).ToString("F1") + "m", screenPoint);
                pathDrawer.SetPathPoints(pointsAvailable, pointsOutOfRange);
                if (pointsOutOfRange != null)
                {
                    pathDrawer.SetTextColor(Color.red);
                }
                else
                {
                    pathDrawer.SetTextColor(Color.green);
                }
                if (!pathDrawer.GetVisible())
                {
                    pathDrawer.SetVisible(true);
                }
            }
            else
            {
                pathDrawer.SetVisible(false);
            }
        }
        else
        {
            pathDrawer.SetVisible(false);
        }
    }
}
