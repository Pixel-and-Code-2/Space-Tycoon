using UnityEngine;

[RequireComponent(typeof(PawnDataController))]
public class ShootState : IPawnState
{
    private PathDrawerWithText pathDrawer => PawnController.Instance.pathDrawer;
    private IControlableSelectable controlableSelectable => PawnController.Instance.currentSelectedPawn;

    void OnDisable()
    {
        IControlableSelectable pawn = controlableSelectable;
        if (pawn == null || !pawn.IsMoving())
            pathDrawer.SetVisible(false);
    }

    PawnDataController AttackerData =>
        controlableSelectable != null ? controlableSelectable.GetComponent<PawnDataController>() : null;

    public override void HandleDoingSth(Vector3 worldPoint, ISelectable selectable)
    {
        if (!(selectable is IAttackableSelectable attackable)) return;
        if (worldPoint == Vector3.zero || selectable == null) return;
        PawnDataController attacker = AttackerData;
        PawnDataController target = attackable.GetComponent<PawnDataController>();
        if (attacker == null || target == null) return;

        CombatResolver.Result r = CombatResolver.Resolve(
            attacker, target,
            controlableSelectable.GetTransform().position,
            attackable.GetTransform().position);

        if (!r.canAttack)
        {
            if (!string.IsNullOrEmpty(r.blockMessage))
                UI3DManager.Instance.ShowMessage(r.blockMessage, worldPoint, Color.red);
            return;
        }

        PawnNavMesh nav = controlableSelectable.GetComponent<PawnNavMesh>();
        nav?.StopIfNoMoveBudget();

        if (!r.hit)
        {
            UI3DManager.Instance.ShowMessage("Промах", worldPoint, Color.yellow);
            if (r.isMelee) controlableSelectable.OnMelee(worldPoint);
            else controlableSelectable.OnShoot(worldPoint, true);
            return;
        }

        if (r.crit)
            UI3DManager.Instance.ShowMessage("Крит!", worldPoint, Color.magenta);
        bool isAlive = attackable.OnGetHit(r.damage);
        if (r.isMelee)
        {
            controlableSelectable.OnMelee(worldPoint);
            if (!isAlive && attacker.selectableType == SelectableType.Player)
                StatBoostService.TryGrantAfterKill(controlableSelectable);
        }
        else controlableSelectable.OnShoot(worldPoint, isAlive);
    }

    public override void HandleUIDrawing(ISelectable selectable, Vector3 worldPoint, Vector2 screenPoint, ScreenCastHitResult hit)
    {
        if (hit == ScreenCastHitResult.NoHit) return;
        Vector3 originPoint = controlableSelectable.GetTransform().position;

        if (hit == ScreenCastHitResult.SelectableHit && (
            selectable == controlableSelectable ||
            selectable.GetSelectableType() == controlableSelectable.GetSelectableType() ||
            selectable.GetSelectableType() == SelectableType.Dead
        )) hit = ScreenCastHitResult.FloorHit;

        float dist = Vector3.Distance(originPoint, worldPoint);
        if (hit == ScreenCastHitResult.SelectableHit && selectable is IAttackableSelectable attackable)
        {
            PawnDataController attacker = AttackerData;
            PawnDataController target = attackable.GetComponent<PawnDataController>();
            CombatResolver.Preview p = CombatResolver.GetPreview(attacker, target, originPoint, attackable.GetTransform().position);
            Vector3[] line = new Vector3[] { originPoint, attackable.GetTransform().position };
            if (!p.canAttack)
            {
                pathDrawer.SetTextColor(Color.red);
                pathDrawer.SetText(dist.ToString("F1") + "m, " + p.blockMessage, screenPoint);
                pathDrawer.SetPathPoints(null, line);
            }
            else
            {
                float accuracy = p.hitChance;
                if (accuracy < 0.1f) pathDrawer.SetTextColor(Color.red);
                else if (accuracy > 0.9f) pathDrawer.SetTextColor(Color.green);
                else pathDrawer.SetTextColor(Color.HSVToRGB((accuracy - 0.1f) / 0.8f * 0.33f, 1f, 1f));
                string tag = p.disadvantage ? " помеха" : "";
                string kind = p.isMelee ? " melee" : "";
                pathDrawer.SetText(dist.ToString("F1") + "m, " + (accuracy * 100f).ToString("F0") + "%" + tag + kind, screenPoint);
                pathDrawer.SetPathPoints(line, null);
            }
        }
        else if (hit == ScreenCastHitResult.FloorHit)
        {
            pathDrawer.SetTextColor(Color.red);
            pathDrawer.SetText(dist.ToString("F1") + "m", screenPoint);
            pathDrawer.SetPathPoints(null, new Vector3[] { originPoint, worldPoint });
        }
        else
        {
            pathDrawer.SetVisible(false);
            return;
        }
        pathDrawer.SetVisible(true);
    }

    public override bool IsErrorChance(IAttackableSelectable attackableSelectable)
    {
        PawnDataController attacker = AttackerData;
        PawnDataController target = attackableSelectable != null ? attackableSelectable.GetComponent<PawnDataController>() : null;
        if (attacker == null || target == null) return true;
        CombatResolver.Preview p = CombatResolver.GetPreview(
            attacker, target,
            controlableSelectable.GetTransform().position,
            attackableSelectable.GetTransform().position);
        return !p.canAttack || !p.isMelee;
    }
}
