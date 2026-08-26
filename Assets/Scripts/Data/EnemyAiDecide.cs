using System.Collections.Generic;
using UnityEngine;

public static class EnemyAiDecide
{
    public enum AllyKind
    {
        Melee,
        Pistol,
        Rifle
    }

    public enum Intent
    {
        Wait,
        Move,
        Attack
    }

    public struct Decision
    {
        public Intent intent;
        public IControlableSelectable target;
        public Vector3 moveTo;
    }

    public static AllyKind GetAllyKind(PawnDataController data)
    {
        if (data == null || !data.HasRanged) return AllyKind.Melee;
        if (data.AttackRange >= 8.5f) return AllyKind.Rifle;
        return AllyKind.Pistol;
    }

    public static EnemyAiRole InferRole(PawnDataController data, EnemyAiProfile profile)
    {
        if (profile != null && profile.role == EnemyAiRole.Tank)
            return EnemyAiRole.Tank;
        if (data != null && data.HasRanged)
            return EnemyAiRole.Shooter;
        return EnemyAiRole.Melee;
    }

    public static Decision Decide(IControlableSelectable self, EnemyAiProfile profile)
    {
        Decision d = new Decision { intent = Intent.Wait, target = null, moveTo = Vector3.zero };
        if (self == null) return d;
        PawnDataController selfData = self.GetComponent<PawnDataController>();
        if (selfData == null) return d;
        EnemyAiRole role = InferRole(selfData, profile);
        if (profile == null) profile = CreateRuntimeDefault(role);

        List<IControlableSelectable> allies = CollectAliveAllies();
        if (allies.Count == 0) return d;

        switch (role)
        {
            case EnemyAiRole.Tank:
                return DecideTank(self, selfData, allies, profile);
            case EnemyAiRole.Shooter:
                return DecideShooter(self, selfData, allies, profile);
            default:
                return DecideMelee(self, selfData, allies, profile);
        }
    }

    static EnemyAiProfile CreateRuntimeDefault(EnemyAiRole role)
    {
        EnemyAiProfile p = ScriptableObject.CreateInstance<EnemyAiProfile>();
        p.role = role;
        return p;
    }

    static List<IControlableSelectable> CollectAliveAllies()
    {
        List<IControlableSelectable> list = new List<IControlableSelectable>();
        foreach (var pawn in PawnBrain.AlivePlayers)
        {
            if (pawn != null && pawn.GetSelectableType() == SelectableType.Player)
                list.Add(pawn);
        }
        return list;
    }

    static float PathDistance(IControlableSelectable from, Vector3 to)
    {
        (Vector3[] a, Vector3[] b) = from.GetPathPointsTo(to);
        float d = 0f;
        if (a != null) d += PawnDataController.CalculateLineStringDistance(a);
        if (b != null) d += PawnDataController.CalculateLineStringDistance(b);
        if (d <= 0.001f) d = Vector3.Distance(from.GetTransform().position, to);
        return d;
    }

    static bool IsFinisher(PawnDataController target, EnemyAiProfile profile)
    {
        if (!profile.useFinisher || target == null || target.MaxHp < 0.01f) return false;
        return target.CurrentHp / target.MaxHp <= profile.finisherHpFraction + 0.001f;
    }

    static CombatResolver.Preview Preview(PawnDataController atk, PawnDataController tgt, Vector3 a, Vector3 b)
    {
        return CombatResolver.GetPreview(atk, tgt, a, b);
    }

    static IControlableSelectable PickMeleeTarget(IControlableSelectable self, List<IControlableSelectable> allies, EnemyAiProfile profile)
    {
        IControlableSelectable best = null;
        int bestPri = int.MaxValue;
        float bestDist = float.MaxValue;
        foreach (var ally in allies)
        {
            PawnDataController data = ally.GetComponent<PawnDataController>();
            AllyKind kind = GetAllyKind(data);
            int pri = kind == AllyKind.Pistol ? profile.priorityPistol
                : kind == AllyKind.Melee ? profile.priorityMeleeAlly
                : profile.priorityRifle;
            float dist = PathDistance(self, ally.GetTransform().position);
            if (pri < bestPri || (pri == bestPri && dist < bestDist))
            {
                bestPri = pri;
                bestDist = dist;
                best = ally;
            }
        }
        return best;
    }

    static IControlableSelectable PickShooterTarget(IControlableSelectable self, PawnDataController selfData, List<IControlableSelectable> allies, EnemyAiProfile profile, bool allowCloseZaya)
    {
        IControlableSelectable best = null;
        int bestPri = int.MaxValue;
        float bestDist = float.MaxValue;
        Vector3 selfPos = self.GetTransform().position;
        foreach (var ally in allies)
        {
            PawnDataController data = ally.GetComponent<PawnDataController>();
            AllyKind kind = GetAllyKind(data);
            float dist = Vector3.Distance(selfPos, ally.GetTransform().position);
            if (kind == AllyKind.Melee && dist > profile.zayaThreatDistance + 0.05f && !allowCloseZaya)
                continue;
            if (kind == AllyKind.Rifle && CombatResolver.HasWallBetween(selfPos, ally.GetTransform().position))
                continue;
            int pri = kind == AllyKind.Rifle ? profile.priorityRifleForShooter
                : kind == AllyKind.Pistol ? profile.priorityPistolForShooter
                : profile.priorityMeleeAllyForShooter;
            if (pri < bestPri || (pri == bestPri && dist < bestDist))
            {
                bestPri = pri;
                bestDist = dist;
                best = ally;
            }
        }
        if (best == null)
        {
            foreach (var ally in allies)
            {
                float dist = Vector3.Distance(selfPos, ally.GetTransform().position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = ally;
                }
            }
        }
        return best;
    }

    static Decision DecideMelee(IControlableSelectable self, PawnDataController selfData, List<IControlableSelectable> allies, EnemyAiProfile profile)
    {
        Decision d = new Decision { intent = Intent.Wait };
        IControlableSelectable target = PickMeleeTarget(self, allies, profile);
        if (target == null) return d;
        d.target = target;
        PawnDataController targetData = target.GetComponent<PawnDataController>();
        Vector3 selfPos = self.GetTransform().position;
        Vector3 targetPos = target.GetTransform().position;
        float dist = Vector3.Distance(selfPos, targetPos);
        CombatResolver.Preview preview = Preview(selfData, targetData, selfPos, targetPos);
        bool finisher = IsFinisher(targetData, profile);
        float reach = selfData.MeleeReach;

        if (preview.canAttack && finisher)
        {
            d.intent = Intent.Attack;
            return d;
        }

        bool lowStamina = profile.useStaminaGate && selfData.Stamina < profile.minStaminaToAttack - 0.001f;
        if (lowStamina && !finisher)
        {
            d.intent = Intent.Move;
            d.moveTo = ClosePoint(selfPos, targetPos, Mathf.Max(0.5f, reach * 0.5f));
            return d;
        }

        if (profile.skipAttackAfterMove && selfData.HasMovedThisTurn && !finisher)
        {
            if (dist > reach + 0.05f)
            {
                d.intent = Intent.Move;
                d.moveTo = ClosePoint(selfPos, targetPos, 0.75f);
            }
            else d.intent = Intent.Wait;
            return d;
        }

        if (preview.canAttack && (!preview.disadvantage || !profile.skipAttackOnDisadvantage || finisher))
        {
            d.intent = Intent.Attack;
            return d;
        }

        d.intent = Intent.Move;
        d.moveTo = ClosePoint(selfPos, targetPos, Mathf.Max(0.5f, reach * 0.5f));
        return d;
    }

    static Decision DecideShooter(IControlableSelectable self, PawnDataController selfData, List<IControlableSelectable> allies, EnemyAiProfile profile)
    {
        Decision d = new Decision { intent = Intent.Wait };
        Vector3 selfPos = self.GetTransform().position;

        IControlableSelectable closestThreat = null;
        float closestDist = float.MaxValue;
        foreach (var ally in allies)
        {
            float dist = Vector3.Distance(selfPos, ally.GetTransform().position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestThreat = ally;
            }
        }

        if (closestThreat != null && closestDist < profile.minShootDistance - 0.05f)
        {
            if (TryFindRetreat(self, allies, closestThreat.GetTransform().position, profile, out Vector3 retreat))
            {
                d.intent = Intent.Move;
                d.moveTo = retreat;
                d.target = closestThreat;
                return d;
            }

            if (TryForcedCloseAttack(self, selfData, allies, closestThreat, profile, out Decision forced))
                return forced;

            d.intent = Intent.Wait;
            d.target = closestThreat;
            return d;
        }

        IControlableSelectable target = PickShooterTarget(self, selfData, allies, profile, false);
        if (target == null) return d;
        d.target = target;
        PawnDataController targetData = target.GetComponent<PawnDataController>();
        Vector3 targetPos = target.GetTransform().position;
        CombatResolver.Preview preview = Preview(selfData, targetData, selfPos, targetPos);
        bool finisher = IsFinisher(targetData, profile);

        if (profile.skipAttackAfterMove && selfData.HasMovedThisTurn && !finisher)
        {
            d.intent = Intent.Wait;
            return d;
        }

        if (preview.canAttack && (!preview.disadvantage || !profile.skipAttackOnDisadvantage || finisher))
        {
            d.intent = Intent.Attack;
            return d;
        }

        if (TryFindFireSpot(self, selfData, allies, target, profile, out Vector3 fireSpot))
        {
            d.intent = Intent.Move;
            d.moveTo = fireSpot;
            return d;
        }

        d.intent = Intent.Wait;
        return d;
    }

    static Decision DecideTank(IControlableSelectable self, PawnDataController selfData, List<IControlableSelectable> allies, EnemyAiProfile profile)
    {
        Decision d = new Decision { intent = Intent.Wait };
        IControlableSelectable best = null;
        float bestDist = float.MaxValue;
        foreach (var ally in allies)
        {
            float dist = PathDistance(self, ally.GetTransform().position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = ally;
            }
        }
        if (best == null) return d;
        d.target = best;
        Vector3 selfPos = self.GetTransform().position;
        Vector3 targetPos = best.GetTransform().position;
        CombatResolver.Preview preview = Preview(selfData, best.GetComponent<PawnDataController>(), selfPos, targetPos);
        if (preview.canAttack)
        {
            d.intent = Intent.Attack;
            return d;
        }
        d.intent = Intent.Move;
        d.moveTo = ClosePoint(selfPos, targetPos, 0.75f);
        return d;
    }

    static Vector3 ClosePoint(Vector3 from, Vector3 to, float stopDistance)
    {
        Vector3 dir = to - from;
        dir.y = 0f;
        float mag = dir.magnitude;
        if (mag < 0.05f) return from;
        float t = Mathf.Max(0f, mag - stopDistance);
        return from + dir.normalized * t;
    }

    static float MinDistToAllies(Vector3 pos, List<IControlableSelectable> allies)
    {
        float min = float.MaxValue;
        for (int i = 0; i < allies.Count; i++)
        {
            float d = Vector3.Distance(pos, allies[i].GetTransform().position);
            if (d < min) min = d;
        }
        return min;
    }

    static bool TryForcedCloseAttack(
        IControlableSelectable self,
        PawnDataController selfData,
        List<IControlableSelectable> allies,
        IControlableSelectable closestThreat,
        EnemyAiProfile profile,
        out Decision d)
    {
        d = new Decision { intent = Intent.Wait };
        Vector3 selfPos = self.GetTransform().position;

        IControlableSelectable bestMelee = null;
        float bestMeleeDist = float.MaxValue;
        foreach (var ally in allies)
        {
            PawnDataController data = ally.GetComponent<PawnDataController>();
            CombatResolver.Preview p = Preview(selfData, data, selfPos, ally.GetTransform().position);
            if (!p.canAttack || !p.isMelee) continue;
            if (p.distance < bestMeleeDist)
            {
                bestMeleeDist = p.distance;
                bestMelee = ally;
            }
        }
        if (bestMelee != null)
        {
            d.intent = Intent.Attack;
            d.target = bestMelee;
            return true;
        }

        IControlableSelectable far = null;
        float farDist = -1f;
        foreach (var ally in allies)
        {
            float dist = Vector3.Distance(selfPos, ally.GetTransform().position);
            if (dist >= profile.minShootDistance - 0.05f && dist > farDist)
            {
                farDist = dist;
                far = ally;
            }
        }
        if (far != null)
        {
            PawnDataController farData = far.GetComponent<PawnDataController>();
            CombatResolver.Preview farPreview = Preview(selfData, farData, selfPos, far.GetTransform().position);
            bool finisher = IsFinisher(farData, profile);
            if (farPreview.canAttack && (!farPreview.disadvantage || !profile.skipAttackOnDisadvantage || finisher))
            {
                d.intent = Intent.Attack;
                d.target = far;
                return true;
            }
        }

        IControlableSelectable trappedTarget = PickShooterTarget(self, selfData, allies, profile, true);
        if (trappedTarget == null) trappedTarget = closestThreat;
        if (trappedTarget != null)
        {
            PawnDataController trappedData = trappedTarget.GetComponent<PawnDataController>();
            CombatResolver.Preview trappedPreview = Preview(selfData, trappedData, selfPos, trappedTarget.GetTransform().position);
            if (trappedPreview.canAttack)
            {
                d.intent = Intent.Attack;
                d.target = trappedTarget;
                return true;
            }
        }

        if (closestThreat != null)
        {
            CombatResolver.Preview closestPreview = Preview(
                selfData,
                closestThreat.GetComponent<PawnDataController>(),
                selfPos,
                closestThreat.GetTransform().position);
            if (closestPreview.canAttack)
            {
                d.intent = Intent.Attack;
                d.target = closestThreat;
                return true;
            }
        }

        return false;
    }

    static bool TryFindFireSpot(
        IControlableSelectable self,
        PawnDataController selfData,
        List<IControlableSelectable> allies,
        IControlableSelectable focus,
        EnemyAiProfile profile,
        out Vector3 destination)
    {
        destination = Vector3.zero;
        if (self == null || focus == null) return false;
        PawnNavMesh nav = self.GetComponent<PawnNavMesh>();
        if (nav == null || nav.navMeshAgent == null) return false;

        float minAlly = Mathf.Max(profile.minShootDistance, selfData.AttackRange * 0.55f);
        float confident = Mathf.Max(profile.minShootDistance + 0.5f, selfData.AttackRange * 0.9f);
        float maxMove = selfData.MaxMoveMetersFromStamina;
        Vector3 selfPos = self.GetTransform().position;
        Vector3 focusPos = focus.GetTransform().position;
        float sample = selfData.maxSampleDistance;

        float[] radii = { confident * 0.75f, confident, Mathf.Min(selfData.AttackRange - 0.2f, confident + 1f) };
        float[] angles = new float[12];
        float baseAngle = Random.Range(0f, 360f);
        for (int i = 0; i < angles.Length; i++)
            angles[i] = baseAngle + i * (360f / angles.Length);

        float bestScore = float.MinValue;
        Vector3 best = Vector3.zero;
        bool found = false;
        for (int r = 0; r < radii.Length; r++)
        {
            for (int a = 0; a < angles.Length; a++)
            {
                Vector3 dir = Quaternion.Euler(0f, angles[a], 0f) * Vector3.forward;
                Vector3 candidate = focusPos + dir * radii[r];
                NavMeshPathCost.PathPlan plan = NavMeshPathCost.Plan(nav.navMeshAgent, candidate, sample);
                if (!plan.valid) continue;
                if (plan.pathMeters > maxMove + 0.05f)
                {
                    plan = NavMeshPathCost.ClampMeters(plan, maxMove);
                    if (!plan.valid) continue;
                }
                float minD = MinDistToAllies(plan.destination, allies);
                if (minD < minAlly - 0.05f) continue;
                float toFocus = Vector3.Distance(plan.destination, focusPos);
                if (toFocus > selfData.AttackRange - 0.05f) continue;
                bool wall = CombatResolver.HasWallBetween(plan.destination, focusPos);
                float score = -Mathf.Abs(toFocus - confident) * 3f + minD * 0.5f + (wall ? -20f : 10f) + Random.Range(0f, 1.5f);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = plan.destination;
                    found = true;
                }
            }
        }
        destination = best;
        return found;
    }

    public static bool TryFindRetreat(
        IControlableSelectable self,
        List<IControlableSelectable> allies,
        Vector3 threatPos,
        EnemyAiProfile profile,
        out Vector3 destination)
    {
        destination = Vector3.zero;
        PawnNavMesh nav = self.GetComponent<PawnNavMesh>();
        if (nav == null || nav.navMeshAgent == null) return false;
        PawnDataController data = self.GetComponent<PawnDataController>();
        float maxMove = data != null ? data.MaxMoveMetersFromStamina : profile.retreatMaxPath;
        float attackRange = data != null ? data.AttackRange : profile.retreatDistance;
        float ideal = attackRange * 0.9f;
        if (ideal < 0.5f) return false;
        Vector3 selfPos = self.GetTransform().position;
        Vector3 away = selfPos - threatPos;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
        away.Normalize();

        float[] angles = { 0f, 25f, -25f, 50f, -50f, 80f, -80f, 120f, -120f, 150f, -150f, 180f };
        float bestScore = float.MinValue;
        Vector3 best = Vector3.zero;
        bool found = false;
        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, angles[i], 0f) * away;
            Vector3 candidate = threatPos + dir * ideal;
            NavMeshPathCost.PathPlan plan = NavMeshPathCost.Plan(nav.navMeshAgent, candidate, data != null ? data.maxSampleDistance : 5f);
            if (!plan.valid) continue;
            if (plan.pathMeters > maxMove + 0.05f)
            {
                plan = NavMeshPathCost.ClampMeters(plan, maxMove);
                if (!plan.valid) continue;
            }
            float distThreat = Vector3.Distance(plan.destination, threatPos);
            if (distThreat < ideal - 0.05f) continue;
            float minAll = MinDistToAllies(plan.destination, allies);
            if (minAll < ideal - 0.05f) continue;
            float score = minAll * 2f - Mathf.Abs(distThreat - ideal) - plan.pathMeters * 0.05f + Random.Range(0f, 0.5f);
            if (score > bestScore)
            {
                bestScore = score;
                best = plan.destination;
                found = true;
            }
        }
        destination = best;
        return found;
    }
}
