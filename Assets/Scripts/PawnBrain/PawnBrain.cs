using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PathDrawer))]
[RequireComponent(typeof(PawnDataController))]
[RequireComponent(typeof(PawnNavMesh))]
[RequireComponent(typeof(AudioSource))]
[DefaultExecutionOrder(200)]
public class PawnBrain : IControlableSelectable
{
    private PathDrawer pathDrawer;
    private PawnDataController dataController;
    private PawnNavMesh pawnNavMesh;
    private AudioSource audioSource;
    public override SelectableType GetSelectableType()
    {
        if (dataController == null) return SelectableType.Neutral;
        return dataController.selectableType;
    }

    [SerializeField]
    private AnimatorBrainBase animatorBrain;
    private Animator anim;
    private Rigidbody rb;
    [SerializeField]
    // ToDo: move this variable to global data or sth
    private float hitForce = 2f;
    [SerializeField]
    Material defaultMaterial;
    [SerializeField]
    Material selectedMaterial;
    [SerializeField]
    SkinnedMeshRenderer skinnedMeshRenderer;
    private bool warFogEventsSubscribed;
    private static HashSet<IControlableSelectable> playersAlive = new HashSet<IControlableSelectable>();
    public static IReadOnlyCollection<IControlableSelectable> AlivePlayers => playersAlive;
    private bool onTask;
    private float busyUntilTime = -9999f;
    private Vector3 lastDrawnPathTarget = new Vector3(99999f, 99999f, 99999f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RebuildAlivePlayers()
    {
        playersAlive.Clear();
        PawnBrain[] brains = Object.FindObjectsByType<PawnBrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < brains.Length; i++)
        {
            PawnBrain brain = brains[i];
            if (brain == null) continue;
            PawnDataController data = brain.GetComponent<PawnDataController>();
            if (data != null && data.selectableType == SelectableType.Player)
                playersAlive.Add(brain);
        }
    }
    [Header("Sounds")]
    [SerializeField]
    private AudioClip hitSound;
    [SerializeField]
    private AudioClip deathSound;
    [SerializeField]
    private AudioClip shootSound;
    [SerializeField]
    private AudioClip meleeSound;
    [SerializeField]
    private AudioClip walkSound;
    [SerializeField]
    private AudioClip noAmmoSound;
    [SerializeField]
    private AudioClip reloadSound;
    void Awake()
    {
        pathDrawer = GetComponent<PathDrawer>();
        dataController = GetComponent<PawnDataController>();
        pawnNavMesh = GetComponent<PawnNavMesh>();
        rb = GetComponent<Rigidbody>();
        animatorBrain = GetComponentInChildren<AnimatorBrainBase>();
        anim = GetComponentInChildren<Animator>();
        animatorBrain?.Initialize(2, (int)AnimatorBrainBase.Animations.IDLE, anim, (layer) => animatorBrain?.Play((int)AnimatorBrainBase.Animations.IDLE, layer, false, false));
        animatorBrain?.Play((int)AnimatorBrainBase.Animations.IDLE, 0, false, false);
        if (dataController.selectableType == SelectableType.Player)
        {
            playersAlive.Add(this);
        }
        if (pawnNavMesh != null)
            pawnNavMesh.OnMoveStopped += HandleMoveStopped;
        audioSource = GetComponent<AudioSource>();
    }

    void HandleMoveStopped()
    {
        GroupMove.OnPawnStopped(this);
    }

    void Start()
    {
        if (dataController != null && dataController.StartDead)
        {
            ApplyDeadStateAtStart();
        }
        if (gameObject.layer != LayerMask.NameToLayer("WarFog"))
        {
            UI3DManager.Instance.RegisterPawn(gameObject);
        }
        else
        {
            WarFog.OnWarFogEnd += OnWarFogEnd;
            WarFog.OnWarFogStart += OnWarFogStart;
            warFogEventsSubscribed = true;
        }
        TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
        TurnManager.Instance.OnEnemyTurnStart += OnEnemyTurnStart;
        TurnManager.Instance.OnTriggerZoneEnter += OnTriggerZoneEnter;
        TurnManager.Instance.OnTriggerZoneExit += OnTriggerZoneExit;
        pawnNavMesh.SetTypeOfModifierVolumes(dataController.selectableType == SelectableType.Player ? 1 : 0, 0, 0);
        SaveHub.Instance.OnLoad += OnLoadData;
    }

    private void ApplyDeadStateAtStart()
    {
        dataController.selectableType = SelectableType.Dead;
        gameObject.layer = LayerMask.NameToLayer("DeadPawn");
        pawnNavMesh.SetTypeOfModifierVolumes(-1, -1, 1);
        animatorBrain?.InstaPlay((int)AnimatorBrainBase.Animations.DEATH, 0);
        dataController.SetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY, 0f);
        if (playersAlive.Contains(this))
        {
            playersAlive.Remove(this);
        }
        RefreshStatusVisualizers();
    }

    private void RefreshStatusVisualizers()
    {
        PawnStatusVisualizer[] visualizers = GetComponentsInChildren<PawnStatusVisualizer>(true);
        for (int i = 0; i < visualizers.Length; i++)
            visualizers[i].RefreshStatusColor();
    }
    private void OnLoadData(LoadedData data)
    {
        SelectableType selectableType = (SelectableType)data.GetData("SelectableType", dataController.UNIQUE_ID, (int)dataController.selectableType);
        if (selectableType != SelectableType.Dead)
        {
            if (!playersAlive.Contains(this) && selectableType == SelectableType.Player)
            {
                playersAlive.Add(this);
            }
            animatorBrain?.SetLocked(false, 0);
        }
        else
        {
            if (playersAlive.Contains(this))
            {
                playersAlive.Remove(this);
            }
        }
    }
    void OnDestroy()
    {
        playersAlive.Remove(this);
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnEnemyTurnStart -= OnEnemyTurnStart;
            TurnManager.Instance.OnTriggerZoneEnter -= OnTriggerZoneEnter;
            TurnManager.Instance.OnTriggerZoneExit -= OnTriggerZoneExit;
        }
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnLoad -= OnLoadData;
        }
        if (warFogEventsSubscribed)
        {
            WarFog.OnWarFogEnd -= OnWarFogEnd;
            WarFog.OnWarFogStart -= OnWarFogStart;
        }
        if (pawnNavMesh != null)
            pawnNavMesh.OnMoveStopped -= HandleMoveStopped;
    }

    void Update()
    {
        if (pawnNavMesh.IsMoving())
        {
            GroupMove.TickRally(this);
            Vector3 dest = pawnNavMesh.targetPosition;
            bool needRedraw = !pathDrawer.GetVisible() || (dest - lastDrawnPathTarget).sqrMagnitude > 0.01f;
            if (needRedraw)
            {
                lastDrawnPathTarget = dest;
                pathDrawer.SetVisible(true);
                var points = pawnNavMesh.GetPathPointsTo(dest);
                pathDrawer.SetPathPoints(points.pointsAvailable, null);
            }
        }
        else
        {
            lastDrawnPathTarget = new Vector3(99999f, 99999f, 99999f);
            if (animatorBrain?.GetCurrentAnimation(0) != (int)AnimatorBrainBase.Animations.IDLE)
            {
                animatorBrain?.Play((int)AnimatorBrainBase.Animations.IDLE, 0, false, false);
            }
            if (pathDrawer.GetVisible())
            {
                pathDrawer.SetVisible(false);
            }
            if (audioSource.clip == walkSound && audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
        }
        if (Time.timeScale - 0f < 0.01f)
        {
            audioSource.Stop();
        }
        else
        {
            if (!audioSource.isPlaying && audioSource.clip == walkSound && pawnNavMesh.IsMoving())
            {
                audioSource.Play();
            }
        }
    }

    public override bool IsInActiveTriggerZone()
    {
        return TurnManager.Instance.IsInActiveTriggerZone(this);
    }
    public override Transform GetTransform()
    {
        return transform;
    }

    public override void OnSelect()
    {
        pawnNavMesh.SetTypeOfModifierVolumes(-1, 1);
        TurnManager.Instance.UpdateNavMesh();
        if (skinnedMeshRenderer != null)
            skinnedMeshRenderer.material = selectedMaterial;
    }

    public override void OnDeselect()
    {
        pawnNavMesh.SetTypeOfModifierVolumes(-1, 0);
        TurnManager.Instance.UpdateNavMesh();
        if (skinnedMeshRenderer != null)
            skinnedMeshRenderer.material = defaultMaterial;
    }
    private void OnPlayerTurnStart()
    {
        pawnNavMesh.SetTypeOfModifierVolumes(dataController.selectableType == SelectableType.Player ? 1 : 0, -1);
    }
    private void OnEnemyTurnStart()
    {
        pawnNavMesh.SetTypeOfModifierVolumes(dataController.selectableType == SelectableType.Enemy ? 1 : 0, -1);
    }
    private void OnTriggerZoneEnter()
    {
        if (dataController.selectableType == SelectableType.Player)
            dataController.ResetActionPoints();
        if (!GroupMove.IsRallying(this))
            pawnNavMesh.ResetMovement();
    }
    private void OnTriggerZoneExit()
    {
        dataController.IsStepByStepOff();
        MakeReload();
        if (dataController.selectableType == SelectableType.Player)
        {
            float hpBefore = dataController.GetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY);
            dataController.SetParameterValue(
                PawnDataController.AVAILABLE_HEALTH_KEY,
                dataController.GetParameterValue(PawnDataController.INITIAL_HP_KEY)
            );
            float healed = dataController.GetParameterValue(PawnDataController.INITIAL_HP_KEY) - hpBefore;
            if (healed > 0.001f)
            {
                UI3DManager.Instance.ShowMessage("+" + healed.ToString("F1") + " hp", transform.position, new Color(0f, 1f, 0f));
            }
        }
    }

    public override void OnMove(Vector3 position)
    {
        OnMoveInternal(position, false);
    }

    public override void OnMoveFree(Vector3 position)
    {
        OnMoveInternal(position, true);
    }

    void OnMoveInternal(Vector3 position, bool ignoreStamina)
    {
        pawnNavMesh.TravelToPosition(position, ignoreStamina);
        animatorBrain?.Play((int)AnimatorBrainBase.Animations.WALK, 0, false, false);
        audioSource.loop = true;
        audioSource.clip = walkSound;
        audioSource.Play();
    }

    public override bool IsMoving()
    {
        return pawnNavMesh.IsMoving();
    }

    public override PawnDataController PawnData => dataController;
    public override bool IsOnTask => onTask;
    public override bool IsBusy => onTask || GroupMove.IsPendingSolo(this);
    public override bool IsAutoFollowHold => Time.time < busyUntilTime;
    public override void SetOnTask(bool value) => onTask = value;
    public override void MarkCtrlSoloMove() { }
    public override void MarkBusyFromNow() => busyUntilTime = Time.time + GroupMove.SoloBusySec;
    public override void ClearMoveHold() => busyUntilTime = -9999f;
    public override void StopMove()
    {
        if (pawnNavMesh != null)
            pawnNavMesh.ResetMovement();
    }

    public override (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) GetPathPointsTo(Vector3 position)
    {
        return pawnNavMesh.GetPathPointsTo(position);
    }


    void OnCollisionEnter(Collision other)
    {
        if (!other.rigidbody) return;
        Vector3 dir = -transform.position + other.transform.position;
        if (dataController.verticalPushOverride != -1f) dir.y = dataController.verticalPushOverride;
        float pushForce = dataController.obstaclePushForce;
        if (pawnNavMesh.navMeshAgent != null)
        {
            pushForce *= pawnNavMesh.navMeshAgent.speed;
        }
        other.rigidbody.AddForce(dir * pushForce, ForceMode.Impulse);
    }

    void OnTriggerEnter(Collider other)
    {
        if (GetSelectableType() != SelectableType.Player) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("Trigger"))
        {
            TurnManager.Instance.EnterTrigger(other.gameObject, this);
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("WarFog"))
        {
            WarFog warFog = other.gameObject.GetComponent<WarFog>();
            warFog.ShowEverything();
        }
    }
    public override void OnCompleteTask()
    {
        onTask = false;
        string[] boosts = new string[] { "+ 1 к IQ", "+ 1 к ловкости", "+ 5% к ловкости", "+ 5% к IQ" };
        int boostIndex = UnityEngine.Random.Range(0, boosts.Length);
        UI3DManager.Instance.ShowMessage(boosts[boostIndex], transform.position, new Color(0f, 1f, 0f));
        GroupMove.OnTaskFinished(this);
    }
    public override void OnShoot(Vector3 position, bool isAlive)
    {
        if (audioSource != null && shootSound != null)
        {
            audioSource.clip = shootSound;
            audioSource.PlayOneShot(shootSound);
        }
        transform.LookAt(position);
        animatorBrain?.Play((int)AnimatorBrainBase.Animations.ATTACK, 0, true, false);
        dataController.SetParameterValue(
            PawnDataController.SHOOTED_AMOUNT_KEY,
            dataController.GetParameterValue(PawnDataController.SHOOTED_AMOUNT_KEY) + 1
        );
        PawnController.Instance.UpdateMoveOnShootButtonColor();
        if (!isAlive && dataController.selectableType == SelectableType.Player)
        {
            string[] boosts = new string[] { "+ 1 к защите", "+ 1 к силе", "", "+ 5% к силе", "+ 5% к защите" };
            int boostIndex = UnityEngine.Random.Range(0, boosts.Length);
            UI3DManager.Instance.ShowMessage(boosts[boostIndex], transform.position, new Color(0f, 1f, 0f));
        }
    }
    public override void OnNoAmmoShoot()
    {
        if (audioSource != null && noAmmoSound != null)
        {
            audioSource.clip = noAmmoSound;
            audioSource.PlayOneShot(noAmmoSound);
        }
    }
    public override void OnMelee(Vector3 position)
    {
        transform.LookAt(position);
        animatorBrain?.Play((int)AnimatorBrainBase.Animations.ATTACK, 0, true, false);
        dataController.SetParameterValue(
            PawnDataController.MELEE_AMOUNT_KEY,
            dataController.GetParameterValue(PawnDataController.MELEE_AMOUNT_KEY) + 1
        );
        if (audioSource != null && meleeSound != null)
        {
            audioSource.clip = meleeSound;
            audioSource.PlayOneShot(meleeSound);
        }
    }
    public override bool OnGetHit(float damage)
    {
        bool isAlive = true;
        float newHealth = dataController.GetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY) - damage;
        UI3DManager.Instance.ShowMessage("-" + damage.ToString("F1"), transform.position, Color.red);
        if (newHealth <= 0f)
        {
            UI3DManager.Instance.ShowMessage("Kill", transform.position + transform.up * 0.5f, Color.red);
            dataController.selectableType = SelectableType.Dead;
            TurnManager.Instance.CheckTriggers();
            gameObject.layer = LayerMask.NameToLayer("DeadPawn");
            pawnNavMesh.SetTypeOfModifierVolumes(-1, -1, 1);
            RefreshStatusVisualizers();
            animatorBrain?.Play((int)AnimatorBrainBase.Animations.DEATH, 0, true, true);
            newHealth = 0f;
            isAlive = false;
            playersAlive.Remove(this);
            if (playersAlive.Count == 0)
            {
                UILayersController.Instance.SetLayer(UILayersController.UILayer.CutScene, "lose");
            }
        }
        dataController.SetParameterValue(
            PawnDataController.AVAILABLE_HEALTH_KEY,
            newHealth
        );
        if (audioSource != null)
        {
            if (!isAlive && deathSound != null)
            {
                audioSource.clip = deathSound;
                audioSource.PlayOneShot(deathSound);
            }
            if (isAlive && hitSound != null)
            {
                audioSource.clip = hitSound;
                audioSource.PlayOneShot(hitSound);
            }
        }
        return isAlive;
    }
    public void OnHeal()
    {
        dataController.selectableType = SelectableType.Player;
        gameObject.layer = LayerMask.NameToLayer("Player");
        pawnNavMesh.SetTypeOfModifierVolumes(-1, -1, 0);
        if (!playersAlive.Contains(this))
            playersAlive.Add(this);
        animatorBrain?.SetLocked(false, 0);
        animatorBrain?.InstaPlay((int)AnimatorBrainBase.Animations.IDLE, 0, false, true);
        dataController.SetParameterValue(
            PawnDataController.AVAILABLE_HEALTH_KEY,
            dataController.GetParameterValue(PawnDataController.INITIAL_HP_KEY)
        );
        dataController.SetParameterValue(
            PawnDataController.AMOUNT_OF_HEALINGS_KEY,
            dataController.GetParameterValue(PawnDataController.AMOUNT_OF_HEALINGS_KEY) + 1
        );
        // ClickableItem clickable = GetComponent<ClickableItem>();
        // if (clickable != null)
        // {
        //     Collider c = clickable.GetComponent<Collider>();
        //     if (c != null) c.enabled = true;
        // }
        RefreshStatusVisualizers();
        float healed = dataController.GetParameterValue(PawnDataController.INITIAL_HP_KEY);
        UI3DManager.Instance.ShowMessage("+" + healed.ToString("F0") + " hp", transform.position, new Color(0f, 1f, 0f));
        // if (ClickableItemsController.Instance != null)
        //     ClickableItemsController.Instance.RefreshTasksAfterHeal();
        if (HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] > 0.5f
            && TurnManager.Instance != null)
        {
            TurnManager.Instance.RegisterCombatant(this);
        }
        GroupMove.OnRevived(this);
    }

    public override void OnGetDefendedHit(Vector3 hitDirection, bool isMelee)
    {
        hitDirection.y = 0f;
        hitDirection.Normalize();
        hitDirection.y = 1f;
        rb.AddForce(hitDirection * hitForce, ForceMode.Impulse);
    }

    public override void MakeReload()
    {
    }
    private void OnWarFogEnd()
    {
        if (gameObject.layer != LayerMask.NameToLayer("WarFog"))
        {
            UI3DManager.Instance.RegisterPawn(gameObject);
        }
    }
    private void OnWarFogStart()
    {
        if (gameObject.layer == LayerMask.NameToLayer("WarFog"))
        {
            UI3DManager.Instance.UnregisterPawn(gameObject);
        }
    }
    public override void SetDynamicParameterValue(string parameterName, float value)
    {
        dataController.SetParameterValue(parameterName, value);
    }
    public override float GetDynamicParameterValue(string parameterName)
    {
        return dataController.GetParameterValue(parameterName);
    }
}