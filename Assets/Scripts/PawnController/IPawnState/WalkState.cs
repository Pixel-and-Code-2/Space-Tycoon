using UnityEngine;

public class WalkState : IPawnState
{
    private IControlableSelectable controlableSelectable => PawnController.Instance.currentSelectedPawn;
    private PathDrawerWithText pathDrawer => PawnController.Instance.pathDrawer;

    void OnEnable()
    {
        ShootOnMoveController.RefreshHelperTag();
    }
    void OnDisable()
    {
        if (ShootOnMoveController.Instance == null || !ShootOnMoveController.Instance.IsActive)
            SliderToPawnConnector.HelperTag = "[ЛКМ]";
        IControlableSelectable pawn = controlableSelectable;
        if (pawn == null || !pawn.IsMoving())
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
        if (!data.HasUsefulMoveBudget)
            return 0f;
        return data.MaxMoveMetersFromStamina;
    }

    public override void HandleDoingSth(Vector3 worldPoint, ISelectable selectable)
    {
        if (worldPoint == Vector3.zero) return;
        if (ShootOnMoveController.Instance != null && ShootOnMoveController.Instance.IsActive)
        {
            IControlableSelectable enemy = selectable as IControlableSelectable;
            if (enemy != null && enemy.GetSelectableType() == SelectableType.Enemy)
            {
                ShootOnMoveController.Instance.TryClickEnemy(enemy);
                return;
            }
            if (ShootOnMoveController.Instance.TryConfirmWalk(worldPoint))
            {
                pathDrawer.SetVisible(false);
                return;
            }
        }
        if (controlableSelectable.IsMoving() && GroupMove.IsCtrlHeld()) return;
        float budgetMeters = GetWalkBudgetMeters(worldPoint);
        if (budgetMeters < PawnDataController.MinUsefulMoveMeters - 0.001f) return;
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
        if (ShootOnMoveController.Instance != null && ShootOnMoveController.Instance.IsActive
            && hit == ScreenCastHitResult.SelectableHit
            && selectable is IControlableSelectable enemy
            && enemy.GetSelectableType() == SelectableType.Enemy)
        {
            ShootOnMoveController.Instance.RecomputePlannedEnemyHover(controlableSelectable);
            int n = ShootOnMoveController.Instance.GetShotCount(enemy);
            float shotCost = controlableSelectable.PawnData != null
                ? controlableSelectable.PawnData.GetAttackStaminaCost(false)
                : 50f;
            float left = controlableSelectable.PawnData != null
                ? controlableSelectable.PawnData.Stamina - ShootOnMoveController.Instance.TotalShotCost(controlableSelectable)
                : 0f;
            bool canAdd = left >= shotCost - 0.001f;
            if (canAdd)
            {
                pathDrawer.SetTextColor(n > 0 ? new Color(1f, 0.55f, 0f) : Color.yellow);
                pathDrawer.SetText("Планировать выстрел ×" + (n + 1), screenPoint);
            }
            else
            {
                pathDrawer.SetTextColor(new Color(1f, 0.45f, 0.35f));
                pathDrawer.SetText(n > 0 ? "Снять выстрелы" : "Не хватает стамины", screenPoint);
            }
            Vector3[] line = new Vector3[] { controlableSelectable.GetTransform().position, enemy.GetTransform().position };
            pathDrawer.SetPathPoints(line, null);
            pathDrawer.SetVisible(true);
            return;
        }
        if (hit != ScreenCastHitResult.NoHit)
        {
            (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) = controlableSelectable.GetPathPointsTo(worldPoint);
            if (pointsAvailable != null || pointsOutOfRange != null)
            {
                float totalMeters = PawnDataController.CalculateLineStringDistance(pointsAvailable)
                    + PawnDataController.CalculateLineStringDistance(pointsOutOfRange);
                if (ShootOnMoveController.Instance != null && ShootOnMoveController.Instance.IsActive)
                    ShootOnMoveController.Instance.RecomputePlanned(controlableSelectable, totalMeters);
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
