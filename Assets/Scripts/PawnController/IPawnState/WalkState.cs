using UnityEngine;

public class WalkState : IPawnState
{
    private IControlableSelectable controlableSelectable => PawnController.Instance.currentSelectedPawn;
    private PathDrawerWithText pathDrawer => PawnController.Instance.pathDrawer;

    void OnEnable()
    {
        SliderToPawnConnector.HelperTag = "[Персонаж]->[ЛКМ]";
    }
    void OnDisable()
    {
        SliderToPawnConnector.HelperTag = "[ЛКМ]";
        pathDrawer.SetVisible(false);
    }

    PawnDataController Data =>
        controlableSelectable != null ? controlableSelectable.GetComponent<PawnDataController>() : null;

    float GetWalkDistance(Vector3 target)
    {
        PawnDataController data = Data;
        if (data == null) return 0f;
        if (!PawnController.Instance.IsInCombat())
            return 9999f;
        if (data.MovesToSkip > 0f)
            return -1f;
        if ((data.ShotAmount > 0f || data.MeleeAmount > 0f) && !data.HasMovedThisTurn)
            return data.ShotAmount > 0f ? -2f : -3f;
        return Mathf.Max(0f, data.AvailableDistance);
    }

    public override void HandleDoingSth(Vector3 worldPoint, ISelectable selectable)
    {
        if (worldPoint == Vector3.zero) return;
        if (controlableSelectable.IsMoving() && GroupMove.IsCtrlHeld()) return;
        float availableDistance = GetWalkDistance(worldPoint);
        if (availableDistance <= 0.0001f)
        {
            controlableSelectable.SetDynamicParameterValue(PawnDataController.AVAILABLE_DISTANCE_KEY, 0f);
            return;
        }
        controlableSelectable.SetDynamicParameterValue(PawnDataController.AVAILABLE_DISTANCE_KEY, availableDistance);
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
            controlableSelectable.SetDynamicParameterValue(PawnDataController.AVAILABLE_DISTANCE_KEY, GetWalkDistance(worldPoint));
            (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) = controlableSelectable.GetPathPointsTo(worldPoint);
            if (pointsAvailable != null || pointsOutOfRange != null)
            {
                pathDrawer.SetText((PawnDataController.CalculateLineStringDistance(pointsAvailable) + PawnDataController.CalculateLineStringDistance(pointsOutOfRange)).ToString("F1") + "m", screenPoint);
                pathDrawer.SetPathPoints(pointsAvailable, pointsOutOfRange);
                pathDrawer.SetTextColor(pointsOutOfRange != null ? Color.red : Color.green);
                if (!pathDrawer.GetVisible()) pathDrawer.SetVisible(true);
            }
            else pathDrawer.SetVisible(false);
        }
        else pathDrawer.SetVisible(false);
    }
}
