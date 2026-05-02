using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PathDrawer))]
[RequireComponent(typeof(PawnDataController))]
[RequireComponent(typeof(PawnNavMesh))]
[DefaultExecutionOrder(200)]
public class PawnBrain : IControlableSelectable
{
    private PathDrawer pathDrawer;
    private PawnDataController dataController;
    private PawnNavMesh pawnNavMesh;

    public override SelectableType GetSelectableType()
    {
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
    private static List<IControlableSelectable> playersAlive = new List<IControlableSelectable>();
    void Awake()
    {
        pathDrawer = GetComponent<PathDrawer>();
        dataController = GetComponent<PawnDataController>();
        pawnNavMesh = GetComponent<PawnNavMesh>();
        rb = GetComponent<Rigidbody>();
        animatorBrain = GetComponentInChildren<AnimatorBrainBase>();
        anim = GetComponentInChildren<Animator>();
        animatorBrain?.Initialize(1, (int)AnimatorBrainBase.Animations.IDLE, anim, (layer) => animatorBrain?.Play((int)AnimatorBrainBase.Animations.IDLE, layer, false, false));
        animatorBrain?.Play((int)AnimatorBrainBase.Animations.IDLE, 0, false, false);
        if (dataController.selectableType == SelectableType.Player)
        {
            playersAlive.Add(this);
        }
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
        HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict[PawnController.LAST_SHOT_ANGLE] = 0f;
        HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict[PawnController.CURRENT_TARGET_ANGLE] = 0f;
        SaveHub.Instance.OnLoad += OnLoadData;
    }

    private void ApplyDeadStateAtStart()
    {
        dataController.selectableType = SelectableType.Dead;
        gameObject.layer = LayerMask.NameToLayer("DeadPawn");
        pawnNavMesh.SetTypeOfModifierVolumes(-1, -1, 1);
        animatorBrain?.Play((int)AnimatorBrainBase.Animations.DEATH, 0, true, true);
        dataController.SetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY, 0f);
        if (playersAlive.Contains(this))
        {
            playersAlive.Remove(this);
        }
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
    }

    void Update()
    {
        if (pawnNavMesh.IsMoving())
        {
            if (!pathDrawer.GetVisible())
            {
                pathDrawer.SetVisible(true);
                pathDrawer.SetPathPoints(pawnNavMesh.GetPathPointsTo(pawnNavMesh.targetPosition).pointsAvailable, null);
            }
            else
            {
                // we can update route runtime, but it's expensive
                // pathDrawer.SetPathPoints(GetPathPointsTo(targetPosition).pointsAvailable, null);
            }
        }
        else
        {
            if (animatorBrain?.GetCurrentAnimation(0) != (int)AnimatorBrainBase.Animations.IDLE)
            {
                animatorBrain?.Play((int)AnimatorBrainBase.Animations.IDLE, 0, false, false);
            }
            if (pathDrawer.GetVisible())
            {
                pathDrawer.SetVisible(false);
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
        pawnNavMesh.ResetMovement();
        if (dataController.selectableType == SelectableType.Player)
        {
            dataController.ResetActionPoints();
        }
    }
    private void OnTriggerZoneExit()
    {
        dataController.IsStepByStepOff();
        MakeReload();
    }

    public override void OnMove(Vector3 position)
    {
        pawnNavMesh.TravelToPosition(position);
        animatorBrain?.Play((int)AnimatorBrainBase.Animations.WALK, 0, false, false);
    }

    public override bool IsMoving()
    {
        return pawnNavMesh.IsMoving();
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
            TurnManager.Instance.EnterTrigger(other.gameObject);
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("WarFog"))
        {
            WarFog warFog = other.gameObject.GetComponent<WarFog>();
            warFog.ShowEverything();
        }
    }
    public override void OnCompleteTask()
    {
        string[] boosts = new string[] { "+ 1 к IQ", "+ 1 к ловкости", "+ 5% к ловкости", "+ 5% к IQ" };
        int boostIndex = UnityEngine.Random.Range(0, boosts.Length);
        UI3DManager.Instance.ShowMessage(boosts[boostIndex], transform.position, new Color(0f, 1f, 0f));
    }
    public override void OnShoot(Vector3 position, bool isAlive)
    {
        transform.LookAt(position);
        animatorBrain?.Play((int)AnimatorBrainBase.Animations.ATTACK, 0, true, false);
        dataController.SetParameterValue(
            PawnDataController.SHOOTED_AMOUNT_KEY,
            dataController.GetParameterValue(PawnDataController.SHOOTED_AMOUNT_KEY) + 1
        );
        dataController.SetParameterValue(
            PawnDataController.MAG_AMOUNT_KEY,
            dataController.GetParameterValue(PawnDataController.MAG_AMOUNT_KEY) - 1
        );
        PawnController.Instance.UpdateStartReloadButtonColor();
        PawnController.Instance.UpdateMoveOnShootButtonColor();
        if (!isAlive && dataController.selectableType == SelectableType.Player)
        {
            string[] boosts = new string[] { "+ 1 к защите", "+ 1 к силе", "", "+ 5% к силе", "+ 5% к защите" };
            int boostIndex = UnityEngine.Random.Range(0, boosts.Length);
            UI3DManager.Instance.ShowMessage(boosts[boostIndex], transform.position, new Color(0f, 1f, 0f));
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
        dataController.SetParameterValue(
            PawnDataController.AMOUNT_OF_DEFENDED_HITS_KEY,
            dataController.GetParameterValue(PawnDataController.AMOUNT_OF_DEFENDED_HITS_KEY) + 1
        );
        return isAlive;
    }
    public void OnHeal()
    {
        dataController.selectableType = SelectableType.Player;
        gameObject.layer = LayerMask.NameToLayer("Player");
        pawnNavMesh.SetTypeOfModifierVolumes(-1, -1, 0);
        playersAlive.Add(this);
        dataController.SetParameterValue(
            PawnDataController.AVAILABLE_HEALTH_KEY,
            dataController.GetParameterValue(PawnDataController.INITIAL_HP_KEY) / 2f
        );
        dataController.SetParameterValue(
            PawnDataController.AMOUNT_OF_HEALINGS_KEY,
            dataController.GetParameterValue(PawnDataController.AMOUNT_OF_HEALINGS_KEY) + 1
        );
    }

    public override void OnGetDefendedHit(Vector3 hitDirection, bool isMelee)
    {
        hitDirection.y = 0f;
        hitDirection.Normalize();
        hitDirection.y = 1f;
        rb.AddForce(hitDirection * hitForce, ForceMode.Impulse);
        dataController.SetParameterValue(
            PawnDataController.AMOUNT_OF_DEFENDED_HITS_KEY,
            dataController.GetParameterValue(PawnDataController.AMOUNT_OF_DEFENDED_HITS_KEY) + (isMelee ? 1 : 2)
        );
    }

    public override void MakeReload()
    {
        float currentAmmo = dataController.GetParameterValue(PawnDataController.TOTAL_AMMO_KEY);
        float currentMag = dataController.GetParameterValue(PawnDataController.MAG_AMOUNT_KEY);
        float initialMag = dataController.GetParameterValue(PawnDataController.INITIAL_MAG_AMOUNT_KEY);
        if (currentMag >= initialMag) return;
        float reloadMagWithAmount = Mathf.Min(initialMag - currentMag, currentAmmo);
        float reloadedAmmo = currentAmmo - reloadMagWithAmount;
        float reloadedMag = reloadMagWithAmount + currentMag;
        dataController.SetParameterValue(PawnDataController.MAG_AMOUNT_KEY, reloadedMag);
        dataController.SetParameterValue(PawnDataController.TOTAL_AMMO_KEY, reloadedAmmo);
        if (HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] > 0.5f)
        {
            float movesToSkipForFullMag = dataController.GetParameterValue(PawnDataController.INITIAL_MOVES_TO_RELOAD_KEY);
            float movesToSkip = Mathf.Ceil(movesToSkipForFullMag * (reloadMagWithAmount / initialMag));
            // Debug.Log("movesToSkip: " + movesToSkip + " reloadMagWithAmount: " + reloadMagWithAmount + " initialMag: " + initialMag + " movesToSkipForFullMag: " + movesToSkipForFullMag);
            dataController.SetParameterValue(PawnDataController.MOVES_TO_SKIP_KEY, movesToSkip);
        }
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
    public override IFormulaData GetFormulaData()
    {
        return dataController;
    }

    public override void FillFormulaData(FormulaDataMonoBase formulaData, string prefix)
    {
        dataController.FillFormulaData(formulaData, prefix);
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