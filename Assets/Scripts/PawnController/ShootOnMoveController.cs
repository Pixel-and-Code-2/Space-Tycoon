using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShootOnMoveController : MonoBehaviour
{
    public static ShootOnMoveController Instance { get; private set; }

    public bool IsActive { get; private set; }

    readonly Dictionary<IControlableSelectable, int> plannedShots = new Dictionary<IControlableSelectable, int>();
    Coroutine execRoutine;
    float plannedMoveMeters;

    void Awake()
    {
        Instance = this;
    }

    public void Toggle()
    {
        if (!IsActive)
            Enter();
        else
            Exit();
    }

    public void Enter()
    {
        if (PawnController.Instance == null || !PawnController.Instance.IsInCombat()) return;
        IControlableSelectable pawn = PawnController.Instance.currentSelectedPawn;
        if (pawn == null || pawn.PawnData == null || !pawn.PawnData.HasRanged) return;
        IsActive = true;
        plannedShots.Clear();
        plannedMoveMeters = 0f;
        ForceWalkControl();
        RefreshStatusColors();
        RefreshHelperTag();
        PawnController.Instance.UpdateMoveOnShootButtonColor();
    }

    static void ForceWalkControl()
    {
        if (InputScreenMouseControlActions.Instance != null)
            InputScreenMouseControlActions.Instance.SetControlTypeTo(true);
        ControlsVariantEasy easy = Object.FindFirstObjectByType<ControlsVariantEasy>();
        if (easy != null)
            easy.SetControlTypeTo(true);
    }

    public void Exit()
    {
        IsActive = false;
        plannedShots.Clear();
        plannedMoveMeters = 0f;
        RefreshStatusColors();
        PlannedStaminaSpend = 0f;
        RefreshHelperTag();
        if (PawnController.Instance != null)
            PawnController.Instance.UpdateMoveOnShootButtonColor();
    }

    public static float PlannedStaminaSpend { get; private set; }

    public int GetShotCount(IControlableSelectable enemy)
    {
        if (enemy == null) return 0;
        return plannedShots.TryGetValue(enemy, out int n) ? n : 0;
    }

    public IEnumerable<KeyValuePair<IControlableSelectable, int>> Planned => plannedShots;

    public bool TryClickEnemy(IControlableSelectable enemy)
    {
        if (!IsActive || enemy == null || !enemy.IsAlive) return false;
        if (enemy.GetSelectableType() != SelectableType.Enemy) return false;
        IControlableSelectable self = PawnController.Instance.currentSelectedPawn;
        if (self == null || self.PawnData == null) return false;

        float shotCost = self.PawnData.GetAttackStaminaCost(false);
        int cur = GetShotCount(enemy);
        float staminaLeft = self.PawnData.Stamina - TotalShotCost(self);

        if (staminaLeft >= shotCost - 0.001f)
            plannedShots[enemy] = cur + 1;
        else
            plannedShots.Remove(enemy);

        RecomputePlannedShotsOnly(self);
        RefreshStatusColors();
        return true;
    }

    public float TotalShotCost(IControlableSelectable self)
    {
        if (self == null || self.PawnData == null) return 0f;
        float cost = self.PawnData.GetAttackStaminaCost(false);
        int n = 0;
        foreach (var kv in plannedShots) n += kv.Value;
        return n * cost;
    }

    public void RecomputePlannedShotsOnly(IControlableSelectable self)
    {
        plannedMoveMeters = 0f;
        if (self == null || self.PawnData == null)
        {
            PlannedStaminaSpend = 0f;
            return;
        }
        PlannedStaminaSpend = TotalShotCost(self);
    }

    public void RecomputePlannedEnemyHover(IControlableSelectable self)
    {
        if (self == null || self.PawnData == null)
        {
            PlannedStaminaSpend = 0f;
            return;
        }
        plannedMoveMeters = 0f;
        float nextShot = self.PawnData.GetAttackStaminaCost(false);
        float shots = TotalShotCost(self);
        float left = self.PawnData.Stamina - shots;
        PlannedStaminaSpend = left >= nextShot - 0.001f ? shots + nextShot : shots;
    }

    public void RecomputePlanned(IControlableSelectable self, float moveMeters)
    {
        plannedMoveMeters = Mathf.Max(0f, moveMeters);
        if (self == null || self.PawnData == null)
        {
            PlannedStaminaSpend = 0f;
            return;
        }
        PlannedStaminaSpend = TotalShotCost(self) + self.PawnData.MoveStaminaCost(plannedMoveMeters);
    }

    public bool TryConfirmWalk(Vector3 worldPoint)
    {
        if (!IsActive) return false;
        IControlableSelectable self = PawnController.Instance.currentSelectedPawn;
        if (self == null || self.PawnData == null) return false;

        float shotCost = TotalShotCost(self);
        if (self.PawnData.Stamina < shotCost - 0.001f)
        {
            if (UI3DManager.Instance != null)
                UI3DManager.Instance.ShowMessage("Нет стамины", worldPoint, Color.red);
            return true;
        }

        if (execRoutine != null) StopCoroutine(execRoutine);
        execRoutine = StartCoroutine(Execute(self, worldPoint));
        return true;
    }

    IEnumerator Execute(IControlableSelectable self, Vector3 worldPoint)
    {
        IsActive = false;
        RefreshHelperTag();
        PawnController.Instance.UpdateMoveOnShootButtonColor();
        List<KeyValuePair<IControlableSelectable, int>> shots = new List<KeyValuePair<IControlableSelectable, int>>(plannedShots);
        plannedShots.Clear();
        RefreshStatusColors();
        PlannedStaminaSpend = 0f;

        CombatAttackRunner runner = CombatAttackRunner.Ensure();

        foreach (var kv in shots)
        {
            for (int i = 0; i < kv.Value; i++)
            {
                IControlableSelectable target = kv.Key;
                if (target == null || !target.IsAlive) break;

                float wait = 0f;
                while (runner != null && runner.IsBusy)
                {
                    wait += Time.unscaledDeltaTime;
                    if (wait > 12f && PhysicalDiceRoller.Instance != null)
                    {
                        PhysicalDiceRoller.Instance.ForceUnlock("SoM wait busy timeout");
                        break;
                    }
                    yield return null;
                }

                bool finished = false;
                Vector3 aim = target.GetTransform().position;
                bool started = runner.ResolveAndApply(
                    self, target, aim, () => finished = true, forceDisadvantage: true);
                if (!started)
                {
                    Debug.LogWarning("[SoM] ResolveAndApply refused shot (busy/null)");
                    continue;
                }
                wait = 0f;
                while (!finished)
                {
                    wait += Time.unscaledDeltaTime;
                    if (wait > 20f)
                    {
                        if (PhysicalDiceRoller.Instance != null)
                            PhysicalDiceRoller.Instance.ForceUnlock("SoM shot finish timeout");
                        finished = true;
                    }
                    yield return null;
                }
            }
        }

        float clearWait = 0f;
        while (PhysicalDiceRoller.IsBusy)
        {
            clearWait += Time.unscaledDeltaTime;
            if (clearWait > 8f && PhysicalDiceRoller.Instance != null)
            {
                PhysicalDiceRoller.Instance.ForceUnlock("SoM pre-move clear");
                break;
            }
            yield return null;
        }

        self.OnMove(worldPoint);
        PawnNavMesh nav = self.GetComponent<PawnNavMesh>();
        if (nav != null && nav.navMeshAgent != null && nav.IsMoving())
        {
            nav.navMeshAgent.isStopped = false;
        }
        while (self.IsMoving())
            yield return null;

        execRoutine = null;
    }

    void RefreshStatusColors()
    {
        PawnStatusVisualizer.SetShootOnMoveCounts(plannedShots);
        foreach (var v in Object.FindObjectsByType<PawnStatusVisualizer>(FindObjectsSortMode.None))
            v.RefreshStatusColor();
    }

    public static void RefreshHelperTag()
    {
        if (Instance != null && Instance.IsActive)
            SliderToPawnConnector.HelperTag = "[2]->[ЛКМ]";
        else
            SliderToPawnConnector.HelperTag = "[ЛКМ]";
    }
}
