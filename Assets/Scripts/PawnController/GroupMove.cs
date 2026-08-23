using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public static class GroupMove
{
    public static Vector3 LastTarget { get; private set; }
    public static Vector3 LastApproachDir { get; private set; } = Vector3.forward;
    public static bool HasLastCommand { get; private set; }

    public const float SoloBusySec = 15f;
    public const float FollowBackDist = 2f;
    public const float RallyRadius = 15f;

    static readonly HashSet<IControlableSelectable> rallying = new HashSet<IControlableSelectable>();
    static readonly HashSet<IControlableSelectable> pendingSoloBusy = new HashSet<IControlableSelectable>();
    static IControlableSelectable rallyAnchor;

    public static bool IsCtrlHeld()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
    }

    public static bool IsRallying(IControlableSelectable pawn)
    {
        return pawn != null && rallying.Contains(pawn);
    }

    public static bool IsPendingSolo(IControlableSelectable pawn)
    {
        if (pawn == null || !pendingSoloBusy.Contains(pawn)) return false;
        if (!pawn.IsMoving())
        {
            pendingSoloBusy.Remove(pawn);
            return false;
        }
        return true;
    }

    public static void Command(IControlableSelectable leader, Vector3 target)
    {
        if (leader == null || target == Vector3.zero) return;
        bool inCombat = PawnController.Instance != null && PawnController.Instance.IsInCombat();
        if (inCombat)
        {
            leader.OnMove(target);
            return;
        }

        if (IsCtrlHeld())
        {
            leader.SetOnTask(false);
            leader.ClearMoveHold();
            leader.OnMoveFree(target);
            if (leader.IsMoving())
                pendingSoloBusy.Add(leader);
            return;
        }

        Vector3 approach = ApproachDir(leader, target);
        LastTarget = SnapNav(target, 5f);
        LastApproachDir = approach;
        HasLastCommand = true;

        pendingSoloBusy.Remove(leader);
        leader.SetOnTask(false);
        leader.ClearMoveHold();
        leader.OnMoveFree(LastTarget);

        int follower = 0;
        foreach (IControlableSelectable p in PawnBrain.AlivePlayers)
        {
            if (p == null || !p.IsAlive || p == leader) continue;
            if (p.IsOnTask) continue;
            if (IsPendingSolo(p)) continue;
            pendingSoloBusy.Remove(p);
            p.ClearMoveHold();
            Vector3 dest = FollowerPoint(leader, LastTarget, approach, follower++);
            p.OnMoveFree(dest);
        }
    }

    public static void RallyForCombat(IControlableSelectable anchor, IList<Vector3> enemyPositions)
    {
        if (anchor == null) return;
        rallyAnchor = anchor;
        Vector3 ap = anchor.GetTransform().position;
        foreach (IControlableSelectable p in PawnBrain.AlivePlayers)
        {
            if (p == null || !p.IsAlive || p == anchor) continue;
            float dist = Vector3.Distance(p.GetTransform().position, ap);
            if (dist <= RallyRadius)
                continue;
            rallying.Add(p);
            p.OnMoveFree(ap);
            TickRally(p);
        }
    }

    public static void TickRally(IControlableSelectable pawn)
    {
        if (pawn == null || !rallying.Contains(pawn)) return;
        if (rallyAnchor == null || !rallyAnchor.IsAlive)
        {
            FinishRally(pawn);
            return;
        }
        float dist = Vector3.Distance(pawn.GetTransform().position, rallyAnchor.GetTransform().position);
        if (dist <= RallyRadius)
            FinishRally(pawn);
    }

    static void FinishRally(IControlableSelectable pawn)
    {
        if (pawn == null || !rallying.Remove(pawn)) return;
        pawn.StopMove();
        if (TurnManager.Instance != null)
            TurnManager.Instance.RegisterCombatantAtEnd(pawn);
    }

    public static void OnRallyArrived(IControlableSelectable pawn)
    {
        if (pawn == null || !rallying.Contains(pawn)) return;
        if (rallyAnchor != null && pawn.IsAlive)
        {
            float dist = Vector3.Distance(pawn.GetTransform().position, rallyAnchor.GetTransform().position);
            if (dist > RallyRadius + 0.5f) return;
        }
        FinishRally(pawn);
    }

    public static void OnPawnStopped(IControlableSelectable pawn)
    {
        if (pawn == null) return;
        OnRallyArrived(pawn);
        if (pendingSoloBusy.Remove(pawn))
        {
            pawn.MarkBusyFromNow();
            if (pawn is MonoBehaviour mb)
                mb.StartCoroutine(WatchBusyExpire(pawn));
        }
    }

    public static void OnBusyDropped(IControlableSelectable pawn)
    {
        if (pawn == null || !HasLastCommand) return;
        if (PawnController.Instance != null && PawnController.Instance.IsInCombat()) return;
        if (pawn.IsOnTask || pawn.IsAutoFollowHold || IsPendingSolo(pawn)) return;
        int slot = FollowerSlotOf(pawn);
        IControlableSelectable leader = PawnController.Instance != null ? PawnController.Instance.currentSelectedPawn : null;
        Vector3 dest = leader != null
            ? FollowerPoint(leader, LastTarget, LastApproachDir, slot)
            : SnapNav(LastTarget, 5f);
        pawn.OnMoveFree(dest);
    }

    public static void OnTaskFinished(IControlableSelectable pawn)
    {
        if (pawn == null) return;
        bool selected = PawnController.Instance != null && PawnController.Instance.currentSelectedPawn == pawn;
        if (selected)
        {
            pawn.MarkBusyFromNow();
            if (pawn is MonoBehaviour mb)
                mb.StartCoroutine(WatchBusyExpire(pawn));
            return;
        }
        OnBusyDropped(pawn);
    }

    static IEnumerator WatchBusyExpire(IControlableSelectable pawn)
    {
        yield return new WaitForSeconds(SoloBusySec + 0.05f);
        if (pawn == null || !pawn.IsAlive) yield break;
        if (pawn.IsOnTask) yield break;
        if (pawn.IsAutoFollowHold) yield break;
        OnBusyDropped(pawn);
    }

    static Vector3 ApproachDir(IControlableSelectable leader, Vector3 target)
    {
        var (available, _) = leader.GetPathPointsTo(target);
        if (available != null && available.Length >= 2)
        {
            Vector3 a = available[available.Length - 2];
            Vector3 b = available[available.Length - 1];
            Vector3 d = b - a;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f) return d.normalized;
        }
        Vector3 from = leader.GetTransform().position;
        Vector3 flat = target - from;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.01f) return flat.normalized;
        return Vector3.forward;
    }

    public static Vector3 FollowerPoint(IControlableSelectable leader, Vector3 leaderTarget, Vector3 approach, int followerIndex)
    {
        Vector3 back = -approach.normalized;
        float sign = followerIndex % 2 == 0 ? -1f : 1f;
        float deg = (15f + followerIndex * 8f) * sign;
        Vector3 dir = Quaternion.Euler(0f, deg, 0f) * back;
        Vector3 raw = leaderTarget + dir * FollowBackDist;
        Vector3 snapped = SnapNav(raw, 5f);
        if (!NavMesh.SamplePosition(snapped, out _, 0.2f, NavMesh.AllAreas) && leader != null)
            snapped = SnapNav(leader.GetTransform().position + dir * FollowBackDist, 8f);
        if (!NavMesh.SamplePosition(snapped, out _, 0.2f, NavMesh.AllAreas))
            snapped = SnapNav(leaderTarget, 8f);
        return snapped;
    }

    static Vector3 SnapNav(Vector3 pos, float maxDist)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, maxDist, NavMesh.AllAreas))
            return hit.position;
        return pos;
    }

    static int FollowerSlotOf(IControlableSelectable pawn)
    {
        int i = 0;
        foreach (IControlableSelectable p in PawnBrain.AlivePlayers)
        {
            if (p == null || !p.IsAlive) continue;
            if (PawnController.Instance != null && p == PawnController.Instance.currentSelectedPawn) continue;
            if (p == pawn) return i;
            i++;
        }
        return 0;
    }
}
