using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class PhysicalDiceRoller : MonoBehaviour
{
    public static PhysicalDiceRoller Instance { get; private set; }
    public static bool IsBusy { get; private set; }

    const string DefaultShapeResource = "DiceShapeConfig_D10";

    [SerializeField]
    private DiceFaceMap diePrefab;
    [SerializeField]
    private DiceShapeConfig shapeConfig;
    [SerializeField]
    private List<DiceShapeConfig> diceCatalog = new List<DiceShapeConfig>();
    [SerializeField]
    private Material diceMaterial;
    [SerializeField]
    private float settleTimeout = 8f;
    [SerializeField]
    private float linearSleep = 0.15f;
    [SerializeField]
    private float angularSleep = 0.25f;
    [SerializeField]
    private float stableHold = 0.35f;
    [SerializeField]
    private float magnetVelocityKick = 40f;
    [SerializeField]
    private float magnetImpulseGap = 0.1f;
    [SerializeField]
    private float magnetDuration = 2.5f;
    [SerializeField]
    private float magnetGroundY = 0.5f;
    [SerializeField]
    private float spawnHeight = 6f;
    [SerializeField]
    private float spawnSide = 8f;
    [SerializeField]
    private float throwSpeed = 7f;
    [SerializeField]
    private float dieMass = 250f;
    [SerializeField]
    private float cameraThrowDistance = 2.2f;
    [SerializeField]
    private PhysicsMaterial softBounce;

    readonly List<DiceFaceMap> activeDice = new List<DiceFaceMap>();
    bool skipRequested;
    readonly List<NavMeshAgent> pausedAgents = new List<NavMeshAgent>();
    readonly List<bool> pausedWasStopped = new List<bool>();
    readonly Dictionary<NavMeshAgent, Vector3> pausedDestinations = new Dictionary<NavMeshAgent, Vector3>();

    void Awake()
    {
        Instance = this;
        if (shapeConfig == null)
            shapeConfig = Resources.Load<DiceShapeConfig>(DefaultShapeResource);
        if (diePrefab == null && shapeConfig != null)
            diePrefab = shapeConfig.diePrefab;
        if (softBounce == null)
        {
            softBounce = new PhysicsMaterial("DiceSoft");
            softBounce.bounciness = 0.01f;
            softBounce.dynamicFriction = 0.75f;
            softBounce.staticFriction = 0.75f;
            softBounce.bounceCombine = PhysicsMaterialCombine.Minimum;
            softBounce.frictionCombine = PhysicsMaterialCombine.Maximum;
        }
    }

    void Update()
    {
        if (!IsBusy) return;
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            skipRequested = true;
    }

    public void ForceUnlock(string reason)
    {
        Debug.LogWarning("[DiceGate] ForceUnlock: " + reason);
        skipRequested = true;
        ClearDice();
        EndLock();
    }

    public bool IsEnabled()
    {
        var s = HandleInittingGlobalVars.globalSettingsAssets;
        EnsurePrefab();
        bool hasAnyDie = diePrefab != null || FindShapeForSides(10) != null || FindShapeForSides(6) != null;
        if (!hasAnyDie && diceCatalog != null)
        {
            for (int i = 0; i < diceCatalog.Count; i++)
            {
                if (diceCatalog[i] != null && diceCatalog[i].diePrefab != null)
                {
                    hasAnyDie = true;
                    break;
                }
            }
        }
        bool ok = s != null && s.usePhysicalDice && hasAnyDie;
        if (!ok)
            Debug.Log(
                "[DiceGate] PhysicalDiceRoller.IsEnabled=false"
                + " settings=" + (s != null)
                + " usePhysicalDice=" + (s != null && s.usePhysicalDice)
                + " diePrefab=" + (diePrefab != null)
                + " catalog=" + (diceCatalog != null ? diceCatalog.Count : 0));
        return ok;
    }

    void EnsurePrefab()
    {
        if (diceCatalog == null)
            diceCatalog = new List<DiceShapeConfig>();
        if (diceCatalog.Count == 0)
        {
            DiceShapeConfig[] all = Resources.LoadAll<DiceShapeConfig>("");
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && !diceCatalog.Contains(all[i]))
                    diceCatalog.Add(all[i]);
            }
        }
        if (shapeConfig == null)
            shapeConfig = Resources.Load<DiceShapeConfig>(DefaultShapeResource);
        if (diePrefab == null && shapeConfig != null)
            diePrefab = shapeConfig.diePrefab;
    }

    public DiceShapeConfig FindShapeForSides(int sides)
    {
        EnsurePrefab();
        if (diceCatalog != null)
        {
            for (int i = 0; i < diceCatalog.Count; i++)
            {
                DiceShapeConfig c = diceCatalog[i];
                if (c == null || c.diePrefab == null) continue;
                if (c.sides == sides) return c;
            }
        }
        if (shapeConfig != null && shapeConfig.diePrefab != null && shapeConfig.sides == sides)
            return shapeConfig;
        return null;
    }

    public bool HasShapeForExpr(string damageExpr)
    {
        DiceExpr.ParsePhysical(damageExpr, out List<DiceExpr.DieTerm> terms, out _);
        if (terms == null || terms.Count == 0) return false;
        for (int i = 0; i < terms.Count; i++)
        {
            if (FindShapeForSides(terms[i].sides) == null)
                return false;
        }
        return true;
    }

    public void RollDamage(string damageExpr, Vector3 nearWorld, System.Action<float> done, bool throwFromCamera = false)
    {
        if (!IsEnabled() || !HasShapeForExpr(damageExpr))
        {
            done?.Invoke(DiceExpr.Roll(damageExpr));
            return;
        }
        StartCoroutine(RollDamageRoutine(damageExpr, nearWorld, done, throwFromCamera));
    }

    IEnumerator RollDamageRoutine(string damageExpr, Vector3 nearWorld, System.Action<float> done, bool throwFromCamera)
    {
        float waitBusy = 0f;
        while (IsBusy)
        {
            waitBusy += Time.unscaledDeltaTime;
            if (waitBusy > 12f)
            {
                ForceUnlock("RollDamage waited on IsBusy too long");
                break;
            }
            yield return null;
        }

        BeginLock();
        try
        {
            DiceExpr.ParsePhysical(damageExpr, out List<DiceExpr.DieTerm> terms, out float flat);
            List<DiceShapeConfig> perDie = new List<DiceShapeConfig>();
            for (int i = 0; i < terms.Count; i++)
            {
                DiceExpr.DieTerm term = terms[i];
                DiceShapeConfig cfg = FindShapeForSides(term.sides);
                if (cfg == null || cfg.diePrefab == null)
                {
                    done?.Invoke(DiceExpr.Roll(damageExpr));
                    yield break;
                }
                for (int c = 0; c < term.count; c++)
                    perDie.Add(cfg);
            }
            if (perDie.Count == 0)
            {
                done?.Invoke(flat);
                yield break;
            }

            List<DiceFaceMap.ReadResult> results = null;
            yield return RollMany(nearWorld, perDie, throwFromCamera, r => results = r);
            if (results == null || results.Count == 0)
            {
                done?.Invoke(DiceExpr.Roll(damageExpr));
                yield break;
            }

            float sum = flat;
            int ri = 0;
            for (int t = 0; t < terms.Count; t++)
            {
                DiceExpr.DieTerm term = terms[t];
                for (int c = 0; c < term.count; c++)
                {
                    int face = ri < results.Count ? results[ri].value : Random.Range(1, term.sides + 1);
                    ri++;
                    sum += term.sign * MapFaceToSides(face, term.sides);
                }
            }
            done?.Invoke(sum);
        }
        finally
        {
            EndLock();
        }
    }

    static int MapFaceToSides(int face, int sides)
    {
        int v = face == 0 ? 10 : face;
        if (sides <= 1) return 1;
        if (sides >= 10) return Mathf.Clamp(v, 1, sides);
        return ((v - 1) % sides) + 1;
    }

    IEnumerator RollMany(Vector3 nearWorld, List<DiceShapeConfig> perDie, bool throwFromCamera, System.Action<List<DiceFaceMap.ReadResult>> done)
    {
        skipRequested = false;

        if (DiceFloor.Instance != null)
            DiceFloor.Instance.SyncFromSettings();
        ClearDice();
        EnsurePrefab();

        int count = perDie != null ? perDie.Count : 0;
        Camera cam = Camera.main;
        Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;
        Vector3 camFwd = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
        if (camFwd.sqrMagnitude < 0.01f) camFwd = Vector3.forward;

        Vector3 target;
        Vector3 spawnBase;
        if (throwFromCamera && cam != null)
        {
            Vector3 lookFlat = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (lookFlat.sqrMagnitude < 0.01f) lookFlat = camFwd;
            lookFlat.Normalize();
            spawnBase = cam.transform.position + lookFlat * 1.4f + Vector3.up * 1.1f + cam.transform.up * 0.25f;
            target = nearWorld + Vector3.up * 0.35f;
            Vector3 ahead = cam.transform.position + lookFlat * 7f;
            if ((target - spawnBase).sqrMagnitude < 1f)
                target = ahead;
        }
        else
        {
            target = nearWorld + Vector3.up * 1.2f;
            spawnBase = target + Vector3.up * spawnHeight;
        }

        for (int i = 0; i < count; i++)
        {
            DiceShapeConfig cfg = perDie[i];
            DiceFaceMap prefab = cfg != null ? cfg.diePrefab : diePrefab;
            if (prefab == null) continue;

            float lane = count <= 1 ? 0f : (i - (count - 1) * 0.5f);
            Vector3 spawn;
            if (throwFromCamera && cam != null)
                spawn = spawnBase + camRight * (lane * 0.45f) + cam.transform.up * (lane * 0.1f);
            else
                spawn = spawnBase + camRight * (lane * spawnSide * 0.35f) - camFwd * 0.5f;

            DiceFaceMap die = Instantiate(prefab, spawn, Random.rotationUniform);
            if (cfg != null)
                cfg.ApplyTo(die);
            ApplySoftPhysics(die);
            Rigidbody rb = die.GetComponent<Rigidbody>();
            Vector3 aim = target + camRight * (lane * 0.25f);
            Vector3 toTarget = (aim - spawn).normalized;
            rb.linearVelocity = toTarget * throwSpeed + Vector3.down * 4.5f + Random.onUnitSphere * (throwSpeed * 0.45f);
            rb.angularVelocity = Random.insideUnitSphere * 14f;
            activeDice.Add(die);
        }

        float t = 0f;
        float stable = 0f;
        while (t < settleTimeout)
        {
            if (skipRequested)
            {
                float magnetT = 0f;
                float magnetStable = 0f;
                float nextImpulseAt = 0f;
                while (magnetT < magnetDuration)
                {
                    float dt = Time.unscaledDeltaTime;
                    if (dt < 0.0001f) dt = 0.02f;
                    magnetT += dt;
                    if (magnetT >= nextImpulseAt)
                    {
                        nextImpulseAt = magnetT + magnetImpulseGap;
                        foreach (var die in activeDice)
                        {
                            if (die == null) continue;
                            Rigidbody rb = die.GetComponent<Rigidbody>();
                            if (rb == null) continue;
                            rb.WakeUp();
                            rb.AddForce(Vector3.down * magnetVelocityKick, ForceMode.VelocityChange);
                            rb.angularVelocity *= 0.35f;
                            Vector3 v = rb.linearVelocity;
                            v.x *= 0.2f;
                            v.z *= 0.2f;
                            rb.linearVelocity = v;
                        }
                    }

                    if (AllDiceBelow(magnetGroundY))
                    {
                        if (AllDiceSleeping())
                        {
                            magnetStable += dt;
                            if (magnetStable >= stableHold)
                                break;
                        }
                        else
                            magnetStable = 0f;
                    }
                    else
                        magnetStable = 0f;
                    yield return null;
                }

                break;
            }

            SoftenUpwardBounce();
            float step = Time.unscaledDeltaTime;
            if (step < 0.0001f) step = 0.02f;
            if (AllDiceSleeping())
            {
                stable += step;
                if (stable >= stableHold)
                    break;
            }
            else
                stable = 0f;

            t += step;
            yield return null;
        }

        List<DiceFaceMap.ReadResult> results = new List<DiceFaceMap.ReadResult>();
        foreach (var die in activeDice)
        {
            if (die == null) continue;
            DiceFaceMap.ReadResult result = die.ReadUp(sumOnEdge: false);
            results.Add(result);
            Color c = Color.white;
            if (result.kind == DiceFaceMap.ReadKind.SuperCrit) c = Color.yellow;
            if (result.kind == DiceFaceMap.ReadKind.Corner) c = Color.red;
            if (UI3DManager.Instance != null)
                UI3DManager.Instance.ShowMessage(result.value.ToString(), die.transform.position, c, true);
        }

        done?.Invoke(results);
        yield return new WaitForSecondsRealtime(0.55f);
        ClearDice();
    }

    bool AllDiceSleeping()
    {
        if (activeDice.Count == 0) return false;
        foreach (var die in activeDice)
        {
            if (die == null) return false;
            Rigidbody rb = die.GetComponent<Rigidbody>();
            if (rb == null) return false;
            if (rb.linearVelocity.magnitude > linearSleep) return false;
            if (rb.angularVelocity.magnitude > angularSleep) return false;
        }
        return true;
    }

    bool AllDiceBelow(float yMax)
    {
        if (activeDice.Count == 0) return false;
        foreach (var die in activeDice)
        {
            if (die == null) return false;
            if (die.transform.position.y >= yMax) return false;
        }
        return true;
    }

    void SoftenUpwardBounce()
    {
        foreach (var die in activeDice)
        {
            if (die == null) continue;
            Rigidbody rb = die.GetComponent<Rigidbody>();
            if (rb == null) continue;
            Vector3 v = rb.linearVelocity;
            if (v.y > 1.2f)
            {
                v.y = 1.2f;
                rb.linearVelocity = v;
            }
        }
    }

    void ApplySoftPhysics(DiceFaceMap die)
    {
        Rigidbody rb = die.GetComponent<Rigidbody>();
        if (rb == null) rb = die.gameObject.AddComponent<Rigidbody>();
        rb.mass = dieMass;
        rb.linearDamping = 0.8f;
        rb.angularDamping = 1.1f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        foreach (var col in die.GetComponentsInChildren<Collider>(true))
            col.material = softBounce;
        if (diceMaterial != null)
        {
            foreach (var mr in die.GetComponentsInChildren<MeshRenderer>(true))
                mr.sharedMaterial = diceMaterial;
        }
    }

    void BeginLock()
    {
        IsBusy = true;
        pausedAgents.Clear();
        pausedWasStopped.Clear();
        pausedDestinations.Clear();
        foreach (var agent in Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None))
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) continue;
            pausedAgents.Add(agent);
            pausedWasStopped.Add(agent.isStopped);
            if (agent.hasPath)
                pausedDestinations[agent] = agent.destination;
            agent.isStopped = true;
        }
    }

    void EndLock()
    {

        for (int i = 0; i < pausedAgents.Count; i++)
        {
            NavMeshAgent agent = pausedAgents[i];
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) continue;
            bool wasStopped = pausedWasStopped[i];
            if (!wasStopped && pausedDestinations.TryGetValue(agent, out Vector3 dest))
            {
                agent.isStopped = false;
                agent.SetDestination(dest);
            }
            else
                agent.isStopped = wasStopped;
        }
        pausedAgents.Clear();
        pausedWasStopped.Clear();
        pausedDestinations.Clear();
        IsBusy = false;
    }

    void ClearDice()
    {

        foreach (var die in activeDice)
        {
            if (die != null)
                Destroy(die.gameObject);
        }
        activeDice.Clear();
    }
}
