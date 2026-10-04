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
        PreviewActor = pawn;
        PlannedStaminaSpend = 0f;
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
        PreviewActor = null;
        RefreshStatusColors();
        PlannedStaminaSpend = 0f;
        RefreshHelperTag();
        if (PawnController.Instance != null)
            PawnController.Instance.UpdateMoveOnShootButtonColor();
    }

    public static float PlannedStaminaSpend { get; private set; }
    public static IControlableSelectable PreviewActor { get; private set; }

    public bool HasPlannedShots => plannedShots.Count > 0;

    public static bool IsStaminaPreviewActiveFor(IControlableSelectable pawn)
    {
        if (Instance == null || !Instance.IsActive) return false;
        if (pawn == null || PreviewActor == null || pawn != PreviewActor) return false;
        return true;
    }

    public static float GetStaminaPreviewSpendFor(IControlableSelectable pawn)
    {
        if (!IsStaminaPreviewActiveFor(pawn)) return 0f;
        return PlannedStaminaSpend;
    }

    public static float GetReservedShotStaminaFor(IControlableSelectable pawn)
    {
        if (!IsStaminaPreviewActiveFor(pawn)) return 0f;
        return Instance.TotalShotCost(pawn);
    }

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
        PreviewActor = self;
        if (self == null || self.PawnData == null || !HasPlannedShots)
        {
            PlannedStaminaSpend = 0f;
            return;
        }
        PlannedStaminaSpend = TotalShotCost(self);
    }

    public void RecomputePlannedEnemyHover(IControlableSelectable self)
    {
        PreviewActor = self;
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
        PreviewActor = self;
        if (self == null || self.PawnData == null || !HasPlannedShots)
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

        if (!HasPlannedShots)
        {
            if (UI3DManager.Instance != null)
                UI3DManager.Instance.ShowMessage("Сначала выбери цель", worldPoint, Color.red);
            return true;
        }

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
        PreviewActor = null;
        RefreshStatusColors();
        PlannedStaminaSpend = 0f;

        List<KeyValuePair<IControlableSelectable, int>> queue =
            new List<KeyValuePair<IControlableSelectable, int>>();
        for (int i = 0; i < shots.Count; i++)
        {
            if (shots[i].Key == null || shots[i].Value <= 0) continue;
            queue.Add(shots[i]);
        }
        float pathMeters = EstimatePathMeters(self, worldPoint);
        int targetCount = Mathf.Max(1, queue.Count);

        self.OnMove(worldPoint);
        PawnNavMesh nav = self.GetComponent<PawnNavMesh>();
        if (nav != null && nav.navMeshAgent != null && nav.IsMoving())
            nav.navMeshAgent.isStopped = false;

        CombatAttackRunner runner = CombatAttackRunner.Ensure();
        float moveStartTime = Time.time;
        float gap = GlobalSettingsAssets.GetMultiAttackGapSeconds();
        float pathDelay = GlobalSettingsAssets.GetSomShotPathDelaySeconds();
        int nextTarget = 0;

        while (nextTarget < queue.Count)
        {
            IControlableSelectable target = queue[nextTarget].Key;
            int shotN = queue[nextTarget].Value;
            if (target == null || !target.IsAlive || shotN <= 0)
            {
                nextTarget++;
                continue;
            }

            float fracNeed = (nextTarget + 1f) / targetCount;
            float distNeed = pathMeters * fracNeed;
            float timeNeed = pathDelay * (nextTarget + 1f);
            float walked = GetWalkedMeters(nav, pathMeters);
            bool arrived = nav == null || !nav.IsMoving();
            bool ready = arrived
                || walked >= distNeed - 0.05f
                || (Time.time - moveStartTime) >= timeNeed;
            if (!ready)
            {
                yield return null;
                continue;
            }

            for (int i = 0; i < shotN; i++)
            {
                if (target == null || !target.IsAlive) break;
                yield return WaitRunnerIdle(runner, 12f, "SoM wait busy timeout");

                bool finished = false;
                Vector3 aim = target.GetTransform().position;
                bool started = runner.ResolveAndApply(
                    self, target, aim, () => finished = true, forceDisadvantage: true);
                if (!started)
                {
                    Debug.LogWarning("[SoM] ResolveAndApply refused shot (busy/null)");
                    continue;
                }
                float wait = 0f;
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
                bool moreShots = i < shotN - 1 || nextTarget < queue.Count - 1;
                if (moreShots && gap > 0.001f)
                {
                    float gapLeft = gap;
                    while (gapLeft > 0f)
                    {
                        gapLeft -= Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
            }
            nextTarget++;
        }

        yield return WaitRunnerIdle(runner, 8f, "SoM post-shot clear");
        while (self.IsMoving())
            yield return null;

        execRoutine = null;
    }

    static float EstimatePathMeters(IControlableSelectable self, Vector3 worldPoint)
    {
        if (self == null) return 0f;
        (Vector3[] a, Vector3[] b) = self.GetPathPointsTo(worldPoint);
        float d = PawnDataController.CalculateLineStringDistance(a)
            + PawnDataController.CalculateLineStringDistance(b);
        if (d > 0.01f) return d;
        return Vector3.Distance(self.GetTransform().position, worldPoint);
    }

    static float GetWalkedMeters(PawnNavMesh nav, float pathMeters)
    {
        if (nav == null || nav.navMeshAgent == null) return pathMeters;
        if (!nav.IsMoving()) return pathMeters;
        float rem = nav.navMeshAgent.remainingDistance;
        if (float.IsInfinity(rem) || float.IsNaN(rem)) return 0f;
        return Mathf.Clamp(pathMeters - rem, 0f, pathMeters);
    }

    static IEnumerator WaitRunnerIdle(CombatAttackRunner runner, float timeout, string unlockReason)
    {
        float wait = 0f;
        while ((runner != null && runner.IsBusy) || PhysicalDiceRoller.IsBusy)
        {
            wait += Time.unscaledDeltaTime;
            if (wait > timeout && PhysicalDiceRoller.Instance != null)
            {
                PhysicalDiceRoller.Instance.ForceUnlock(unlockReason);
                break;
            }
            yield return null;
        }
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
