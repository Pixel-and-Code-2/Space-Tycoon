using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
        public int attackCount;
        public bool retreatAfterAttack;
        public Vector3 retreatTo;
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

    static int ResolveAttackCount(PawnDataController selfData, EnemyAiProfile profile, bool forceDouble)
    {
        if (selfData == null || profile == null || !profile.allowDoubleAttack) return 1;
        if (selfData.HasMovedThisTurn && !forceDouble) return 1;
        float cost = selfData.GetAttackStaminaCost(!selfData.HasRanged);
        if (selfData.Stamina < cost * 2f - 0.001f) return 1;
        return 2;
    }

    public static IControlableSelectable PickAlternateAttackTarget(
        IControlableSelectable self,
        IControlableSelectable exclude,
        EnemyAiProfile profile)
    {
        if (self == null) return null;
        PawnDataController selfData = self.GetComponent<PawnDataController>();
        List<IControlableSelectable> allies = CollectAliveAllies();
        if (exclude != null) allies.Remove(exclude);
        if (allies.Count == 0) return null;
        EnemyAiRole role = InferRole(selfData, profile);
        if (role == EnemyAiRole.Shooter)
            return PickShooterTarget(self, selfData, allies, profile, true);
        return PickMeleeTarget(self, allies, profile);
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
        var bag = new List<IControlableSelectable>();
        var weights = new List<float>();
        foreach (var ally in allies)
        {
            if (ally == null || !ally.IsAlive) continue;
            PawnDataController data = ally.GetComponent<PawnDataController>();
            AllyKind kind = GetAllyKind(data);
            float w = kind == AllyKind.Pistol ? profile.weightPistol
                : kind == AllyKind.Melee ? profile.weightMeleeAlly
                : profile.weightRifle;
            if (w <= 0.001f) continue;
            bag.Add(ally);
            weights.Add(w);
        }
        return PickWeighted(bag, weights);
    }

    static List<IControlableSelectable> CollectInMeleeReach(
        IControlableSelectable self,
        PawnDataController selfData,
        List<IControlableSelectable> allies)
    {
        var list = new List<IControlableSelectable>();
        if (self == null || selfData == null || allies == null) return list;
        Vector3 selfPos = self.GetTransform().position;
        float reach = Mathf.Max(0.5f, selfData.MeleeReach);
        for (int i = 0; i < allies.Count; i++)
        {
            IControlableSelectable ally = allies[i];
            if (ally == null || !ally.IsAlive) continue;
            float dist = Vector3.Distance(selfPos, ally.GetTransform().position);
            if (dist <= reach + 0.05f)
                list.Add(ally);
        }
        return list;
    }

    static IControlableSelectable PickShooterTarget(IControlableSelectable self, PawnDataController selfData, List<IControlableSelectable> allies, EnemyAiProfile profile, bool allowCloseZaya)
    {
        Vector3 selfPos = self.GetTransform().position;
        var bag = new List<IControlableSelectable>();
        var weights = new List<float>();
        foreach (var ally in allies)
        {
            PawnDataController data = ally.GetComponent<PawnDataController>();
            AllyKind kind = GetAllyKind(data);
            float dist = Vector3.Distance(selfPos, ally.GetTransform().position);
            if (kind == AllyKind.Melee && dist > profile.zayaThreatDistance + 0.05f && !allowCloseZaya)
                continue;
            if (kind == AllyKind.Rifle && CombatResolver.HasWallBetween(selfPos, ally.GetTransform().position))
                continue;
            float w = kind == AllyKind.Rifle ? profile.weightRifleForShooter
                : kind == AllyKind.Pistol ? profile.weightPistolForShooter
                : profile.weightMeleeAllyForShooter;
            if (w <= 0.001f) continue;
            bag.Add(ally);
            weights.Add(w);
        }
        IControlableSelectable picked = PickWeighted(bag, weights);
        if (picked != null) return picked;
        IControlableSelectable best = null;
        float bestDist = float.MaxValue;
        foreach (var ally in allies)
        {
            float dist = Vector3.Distance(selfPos, ally.GetTransform().position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = ally;
            }
        }
        return best;
    }

    static IControlableSelectable PickWeighted(List<IControlableSelectable> bag, List<float> weights)
    {
        if (bag == null || bag.Count == 0) return null;
        float sum = 0f;
        for (int i = 0; i < weights.Count; i++) sum += weights[i];
        if (sum <= 0.001f) return bag[0];
        float r = Random.value * sum;
        for (int i = 0; i < bag.Count; i++)
        {
            r -= weights[i];
            if (r <= 0f) return bag[i];
        }
        return bag[bag.Count - 1];
    }

    static Decision DecideMelee(IControlableSelectable self, PawnDataController selfData, List<IControlableSelectable> allies, EnemyAiProfile profile)
    {
        Decision d = new Decision { intent = Intent.Wait };
        List<IControlableSelectable> inReach = CollectInMeleeReach(self, selfData, allies);
        bool sticky = inReach.Count > 0;
        IControlableSelectable target = sticky
            ? PickMeleeTarget(self, inReach, profile)
            : PickMeleeTarget(self, allies, profile);
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
            d.attackCount = ResolveAttackCount(selfData, profile, true);
            return d;
        }

        bool lowStamina = profile.useStaminaGate && selfData.Stamina < profile.minStaminaToAttack - 0.001f;
        if (lowStamina && !finisher)
        {
            if (sticky)
            {
                if (preview.canAttack)
                {
                    d.intent = Intent.Attack;
                    d.attackCount = 1;
                }
                else d.intent = Intent.Wait;
                return d;
            }
            d.intent = Intent.Move;
            d.moveTo = ClosePoint(self, targetPos, 0.05f);
            return d;
        }

        if (profile.skipAttackAfterMove && selfData.HasMovedThisTurn && !finisher)
        {
            float chance = profile.attackAfterMoveChance;
            if (Random.value <= chance && preview.canAttack)
            {
                d.intent = Intent.Attack;
                d.attackCount = 1;
                return d;
            }
            if (sticky || dist <= reach + 0.05f)
            {
                d.intent = preview.canAttack ? Intent.Attack : Intent.Wait;
                if (d.intent == Intent.Attack) d.attackCount = 1;
                return d;
            }
            d.intent = Intent.Move;
            d.moveTo = ClosePoint(self, targetPos, 0.05f);
            return d;
        }

        if (preview.canAttack)
        {
            if (preview.disadvantage && !finisher && !sticky)
            {
                if (profile.skipAttackOnDisadvantage && Random.value > profile.disadvantageAttackChance)
                {
                    d.intent = Intent.Move;
                    d.moveTo = ClosePoint(self, targetPos, 0.05f);
                    return d;
                }
            }
            d.intent = Intent.Attack;
            d.attackCount = ResolveAttackCount(selfData, profile, false);
            return d;
        }

        if (sticky)
        {
            d.intent = Intent.Wait;
            return d;
        }

        d.intent = Intent.Move;
        d.moveTo = ClosePoint(self, targetPos, 0.05f);
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
            if (ally == null || !ally.IsAlive) continue;
            float dist = Vector3.Distance(selfPos, ally.GetTransform().position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestThreat = ally;
            }
        }

        // GDD: 1–4 м — не стрелять, только отход / смена цели / милиш в упор
        if (closestThreat != null && closestDist < profile.minShootDistance - 0.05f)
        {
            if (TryFindRetreat(self, allies, closestThreat.GetTransform().position, profile, out Vector3 retreatClose))
            {
                d.intent = Intent.Move;
                d.moveTo = retreatClose;
                d.target = closestThreat;
                return d;
            }

            if (TryForcedCloseAttack(self, selfData, allies, profile, out Decision forced))
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
        float targetDist = Vector3.Distance(selfPos, targetPos);

        // GDD: ~5 м — 1 выстрел и отойти
        if (closestThreat != null
            && closestDist <= profile.minShootDistance + 0.75f
            && preview.canAttack
            && !preview.isMelee
            && targetDist >= profile.minShootDistance - 0.05f
            && !selfData.HasMovedThisTurn)
        {
            d.intent = Intent.Attack;
            d.attackCount = 1;
            if (TryFindRetreat(self, allies, closestThreat.GetTransform().position, profile, out Vector3 retreatAfter))
            {
                d.retreatAfterAttack = true;
                d.retreatTo = retreatAfter;
            }
            return d;
        }

        if (profile.skipAttackAfterMove && selfData.HasMovedThisTurn && !finisher)
        {
            if (preview.canAttack && !preview.isMelee && targetDist >= profile.minShootDistance - 0.05f
                && Random.value <= profile.attackAfterMoveChance)
            {
                d.intent = Intent.Attack;
                d.attackCount = 1;
                return d;
            }
            d.intent = Intent.Wait;
            return d;
        }

        if (preview.canAttack)
        {
            if (preview.isMelee || targetDist < profile.minShootDistance - 0.05f)
            {
                if (preview.isMelee)
                {
                    d.intent = Intent.Attack;
                    d.attackCount = 1;
                    return d;
                }
                if (TryFindRetreat(self, allies, targetPos, profile, out Vector3 retreatNow))
                {
                    d.intent = Intent.Move;
                    d.moveTo = retreatNow;
                    return d;
                }
                d.intent = Intent.Wait;
                return d;
            }
            if (preview.disadvantage && !finisher)
            {
                if (profile.skipAttackOnDisadvantage && Random.value > profile.disadvantageAttackChance)
                {
                    if (TryFindFireSpot(self, selfData, allies, target, profile, out Vector3 prepSpot))
                    {
                        d.intent = Intent.Move;
                        d.moveTo = prepSpot;
                        return d;
                    }
                    d.intent = Intent.Wait;
                    return d;
                }
                d.intent = Intent.Attack;
                d.attackCount = selfData.HasMovedThisTurn ? 1 : ResolveAttackCount(selfData, profile, false);
                return d;
            }
            d.intent = Intent.Attack;
            d.attackCount = ResolveAttackCount(selfData, profile, finisher);
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
        Decision d = new Decision { intent = Intent.Wait, attackCount = 1 };
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
        float reach = Mathf.Max(0.75f, selfData.MeleeReach);
        float meleeDist = Vector3.Distance(selfPos, targetPos);
        CombatResolver.Preview preview = Preview(selfData, best.GetComponent<PawnDataController>(), selfPos, targetPos);
        if (preview.canAttack || meleeDist <= reach + 0.05f)
        {
            d.intent = Intent.Attack;
            d.attackCount = ResolveAttackCount(selfData, profile, true);
            return d;
        }
        d.intent = Intent.Move;
        d.moveTo = ClosePoint(self, targetPos, 0.05f);
        return d;
    }

    static Vector3 ClosePoint(IControlableSelectable self, Vector3 targetPos, float stopDistance)
    {
        Vector3 from = self != null ? self.GetTransform().position : targetPos;
        PawnNavMesh nav = self != null ? self.GetComponent<PawnNavMesh>() : null;
        NavMeshAgent agent = nav != null ? nav.navMeshAgent : null;
        float sample = 5f;
        if (self != null && self.PawnData != null)
            sample = Mathf.Max(1f, self.PawnData.maxSampleDistance);

        if (agent != null && agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, sample, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path)
                    && path.corners != null
                    && path.corners.Length >= 2)
                {
                    float full = PawnDataController.CalculateLineStringDistance(path.corners);
                    float go = Mathf.Max(0f, full - stopDistance);
                    return NavMeshPathCost.PointAtDistance(path.corners, go, out _);
                }
            }
        }

        Vector3 dir = targetPos - from;
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
            if (allies[i] == null || !allies[i].IsAlive) continue;
            float d = Vector3.Distance(pos, allies[i].GetTransform().position);
            if (d < min) min = d;
        }
        return min;
    }

    static bool TryForcedCloseAttack(
        IControlableSelectable self,
        PawnDataController selfData,
        List<IControlableSelectable> allies,
        EnemyAiProfile profile,
        out Decision d)
    {
        d = new Decision { intent = Intent.Wait };
        Vector3 selfPos = self.GetTransform().position;

        IControlableSelectable bestMelee = null;
        float bestMeleeDist = float.MaxValue;
        foreach (var ally in allies)
        {
            if (ally == null || !ally.IsAlive) continue;
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
            d.attackCount = 1;
            return true;
        }

        IControlableSelectable far = null;
        float farDist = -1f;
        foreach (var ally in allies)
        {
            if (ally == null || !ally.IsAlive) continue;
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
            if (farPreview.canAttack && !farPreview.isMelee
                && (!farPreview.disadvantage || !profile.skipAttackOnDisadvantage || finisher))
            {
                d.intent = Intent.Attack;
                d.target = far;
                d.attackCount = 1;
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

        float minAlly = profile.minShootDistance;
        float confident = Mathf.Max(profile.minShootDistance + 0.5f, selfData.AttackRange * 0.9f);
        float maxMove = selfData.MaxMoveMetersFromStamina;
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
        float minSafe = profile.minShootDistance;
        float ideal = profile.retreatDistance > 0.5f ? profile.retreatDistance : minSafe + 3f;
        if (ideal < minSafe) ideal = minSafe + 1f;
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
            if (distThreat < minSafe - 0.05f) continue;
            float minAll = MinDistToAllies(plan.destination, allies);
            if (minAll < minSafe - 0.05f) continue;
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
