using UnityEngine;

public class WalkState : IPawnState
{
    private IControlableSelectable controlableSelectable => PawnController.Instance.currentSelectedPawn;
    private PathDrawerWithText pathDrawer => PawnController.Instance.pathDrawer;

    void OnEnable()
    {
        SliderToPawnConnector.HelperTag = "[1]->[ЛКМ]";
    }
    void OnDisable()
    {
        SliderToPawnConnector.HelperTag = "[ЛКМ]";
        pathDrawer.SetVisible(false);
    }

    PawnDataController Data =>
        controlableSelectable != null ? controlableSelectable.GetComponent<PawnDataController>() : null;

    float GetWalkBudgetMeters(Vector3 target)
    {
        PawnDataController data = Data;
        if (data == null) return 0f;
        if (!PawnController.Instance.IsInCombat())
            return 9999f;
        if (data.MovesToSkip > 0f)
            return -1f;
        if (data.Stamina <= 0.001f)
            return 0f;
        return data.MaxMoveMetersFromStamina;
    }

    public override void HandleDoingSth(Vector3 worldPoint, ISelectable selectable)
    {
        if (worldPoint == Vector3.zero) return;
        if (controlableSelectable.IsMoving() && GroupMove.IsCtrlHeld()) return;
        float budgetMeters = GetWalkBudgetMeters(worldPoint);
        if (budgetMeters <= 0.0001f) return;
        if (Data != null) Data.SetHasMovedThisTurn(true);
        PawnController.Instance.UpdateMoveOnShootButtonColor();
        GroupMove.Command(controlableSelectable, worldPoint);
        pathDrawer.SetVisible(false);
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
            (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) = controlableSelectable.GetPathPointsTo(worldPoint);
            if (pointsAvailable != null || pointsOutOfRange != null)
            {
                float totalMeters = PawnDataController.CalculateLineStringDistance(pointsAvailable)
                    + PawnDataController.CalculateLineStringDistance(pointsOutOfRange);
                pathDrawer.SetText(totalMeters.ToString("F1") + "m", screenPoint);
                pathDrawer.SetPathPoints(pointsAvailable, pointsOutOfRange);
                pathDrawer.SetTextColor(pointsOutOfRange != null ? Color.red : Color.green);
                if (!pathDrawer.GetVisible()) pathDrawer.SetVisible(true);
            }
            else pathDrawer.SetVisible(false);
        }
        else pathDrawer.SetVisible(false);
    }
}
