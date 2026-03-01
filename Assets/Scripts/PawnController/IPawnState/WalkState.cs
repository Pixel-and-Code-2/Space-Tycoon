using UnityEngine;

public class WalkState : IPawnState
{
    private IControlableSelectable controlableSelectable => PawnController.Instance.currentSelectedPawn;
    private PathDrawerWithText pathDrawer => PawnController.Instance.pathDrawer;

    void OnDisable()
    {
        pathDrawer.SetVisible(false);
    }

    public override void HandleDoingSth(Vector3 worldPoint, ISelectable selectable)
    {
        if (worldPoint != Vector3.zero && !controlableSelectable.IsMoving())
        {
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
