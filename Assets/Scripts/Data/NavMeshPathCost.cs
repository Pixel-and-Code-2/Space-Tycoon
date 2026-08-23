using UnityEngine;
using UnityEngine.AI;

public static class NavMeshPathCost
{
    public const float FinishEpsilon = 0.35f;
    public const float CloseClickMeters = 2f;

    public struct PathPlan
    {
        public bool valid;
        public Vector3[] corners;
        public Vector3 destination;
        public float pathMeters;
        public float polylineMeters;
        public float directMeters;
    }

    public static bool TrySample(Vector3 desired, float maxSampleDistance, out Vector3 sampled)
    {
        sampled = desired;
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, maxSampleDistance, NavMesh.AllAreas))
        {
            sampled = hit.position;
            return true;
        }
        return false;
    }

    public static PathPlan Plan(NavMeshAgent agent, Vector3 desiredFinish, float maxSampleDistance)
    {
        PathPlan plan = default;
        if (agent == null) return plan;
        if (!TrySample(desiredFinish, maxSampleDistance, out Vector3 sampled)) return plan;

        Vector3 agentPos = agent.transform.position;
        float directDist = HorizontalDistance(agentPos, desiredFinish);
        float directToSample = HorizontalDistance(agentPos, sampled);

        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(sampled, path) || path.corners == null || path.corners.Length < 2)
            return plan;

        Vector3[] corners = path.corners;
        float polylineMeters = PawnDataController.CalculateLineStringDistance(corners);

        plan.valid = true;
        plan.destination = sampled;
        plan.corners = corners;
        plan.pathMeters = polylineMeters;
        plan.polylineMeters = polylineMeters;
        plan.directMeters = directDist;

        if (directDist <= CloseClickMeters || directToSample <= CloseClickMeters)
        {
            plan.pathMeters = Mathf.Max(directToSample, directDist);
            plan.destination = sampled;
            plan.corners = new[] { agentPos, sampled };
        }
        else if (polylineMeters > directToSample * 2f && directToSample < 5f)
        {
            plan.pathMeters = directToSample + 0.1f;
            plan.destination = sampled;
            plan.corners = new[] { agentPos, sampled };
        }
        else
        {
            ClosestOnPolyline(corners, desiredFinish, out Vector3 closestOnPath, out float metersToClosest);
            float distClickToPath = Vector3.Distance(desiredFinish, closestOnPath);
            if (distClickToPath > FinishEpsilon)
            {
                plan.pathMeters = metersToClosest;
                plan.destination = PointAtDistance(corners, metersToClosest, out _);
                plan.corners = TrimPolyline(corners, metersToClosest);
            }
        }

        return plan;
    }

    public static PathPlan ClampMeters(PathPlan plan, float maxMeters)
    {
        if (!plan.valid) return plan;
        if (maxMeters <= 0.001f)
        {
            plan.valid = false;
            return plan;
        }
        if (plan.pathMeters <= maxMeters + 0.001f) return plan;

        plan.destination = PointAtDistance(plan.corners, maxMeters, out float actual);
        plan.pathMeters = actual;
        plan.corners = TrimPolyline(plan.corners, actual);
        return plan;
    }

    public static float AgentPathMeters(NavMeshAgent agent)
    {
        if (agent == null || !agent.hasPath || agent.path.corners == null || agent.path.corners.Length < 2)
            return 0f;
        return PawnDataController.CalculateLineStringDistance(agent.path.corners);
    }

    static void ClosestOnPolyline(Vector3[] corners, Vector3 point, out Vector3 closest, out float metersAlong)
    {
        closest = corners != null && corners.Length > 0 ? corners[0] : point;
        metersAlong = 0f;
        if (corners == null || corners.Length < 2) return;

        float bestDistSq = float.MaxValue;
        float bestAlong = 0f;
        Vector3 bestPoint = corners[0];
        float walked = 0f;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 a = corners[i];
            Vector3 b = corners[i + 1];
            Vector3 seg = b - a;
            float segLen = seg.magnitude;
            if (segLen < 0.0001f) continue;

            float t = Mathf.Clamp01(Vector3.Dot(point - a, seg) / (segLen * segLen));
            Vector3 onSeg = a + seg * t;
            float distSq = (point - onSeg).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestPoint = onSeg;
                bestAlong = walked + segLen * t;
            }
            walked += segLen;
        }

        closest = bestPoint;
        metersAlong = bestAlong;
    }

    public static Vector3 PointAtDistance(Vector3[] corners, float targetMeters, out float actualMeters)
    {
        actualMeters = 0f;
        if (corners == null || corners.Length == 0) return Vector3.zero;
        if (corners.Length == 1) return corners[0];

        float walked = 0f;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 a = corners[i];
            Vector3 b = corners[i + 1];
            float segLen = Vector3.Distance(a, b);
            if (walked + segLen >= targetMeters - 0.001f)
            {
                float ratio = segLen < 0.0001f ? 0f : (targetMeters - walked) / segLen;
                actualMeters = targetMeters;
                return Vector3.Lerp(a, b, ratio);
            }
            walked += segLen;
        }

        actualMeters = walked;
        return corners[corners.Length - 1];
    }

    public static Vector3[] TrimPolyline(Vector3[] corners, float targetMeters)
    {
        if (corners == null || corners.Length == 0) return null;
        if (corners.Length == 1) return new[] { corners[0] };

        float full = PawnDataController.CalculateLineStringDistance(corners);
        if (targetMeters >= full - 0.001f) return corners;

        Vector3 end = PointAtDistance(corners, targetMeters, out _);
        float walked = 0f;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            float segLen = Vector3.Distance(corners[i], corners[i + 1]);
            if (walked + segLen >= targetMeters - 0.001f)
            {
                Vector3[] trimmed = new Vector3[i + 2];
                System.Array.Copy(corners, trimmed, i + 1);
                trimmed[i + 1] = end;
                return trimmed;
            }
            walked += segLen;
        }
        return corners;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
