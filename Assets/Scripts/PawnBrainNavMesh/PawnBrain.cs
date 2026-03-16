using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PathDrawer))]
[RequireComponent(typeof(PawnDataController))]
[RequireComponent(typeof(PawnNavMesh))]
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
    void Awake()
    {
        pathDrawer = GetComponent<PathDrawer>();
        dataController = GetComponent<PawnDataController>();
        pawnNavMesh = GetComponent<PawnNavMesh>();
        rb = GetComponent<Rigidbody>();
        animatorBrain = GetComponentInChildren<AnimatorBrainBase>();
        anim = GetComponentInChildren<Animator>();
        animatorBrain.Initialize(1, (int)AnimatorBrainBase.Animations.IDLE, anim, (layer) => animatorBrain.Play((int)AnimatorBrainBase.Animations.IDLE, layer, false, false));
        animatorBrain.Play((int)AnimatorBrainBase.Animations.IDLE, 0, false, false);

    }

    void Start()
    {
        UI3DManager.Instance.RegisterPawn(gameObject);
        TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
        TurnManager.Instance.OnEnemyTurnStart += OnEnemyTurnStart;
        pawnNavMesh.SetTypeOfModifierVolumes(dataController.selectableType == SelectableType.Player ? 1 : 0, 0, 0);

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
            if (animatorBrain.GetCurrentAnimation(0) != (int)AnimatorBrainBase.Animations.IDLE)
            {
                animatorBrain.Play((int)AnimatorBrainBase.Animations.IDLE, 0, false, false);
            }
            if (pathDrawer.GetVisible())
            {
                pathDrawer.SetVisible(false);
            }
        }
    }

    public override Transform GetTransform()
    {
        return transform;
    }

    public override void OnSelect()
    {
        pawnNavMesh.SetTypeOfModifierVolumes(-1, 1);
        TurnManager.Instance.UpdateNavMesh();
    }

    public override void OnDeselect()
    {
        pawnNavMesh.SetTypeOfModifierVolumes(-1, 0);
        TurnManager.Instance.UpdateNavMesh();
    }
    private void OnPlayerTurnStart()
    {
        pawnNavMesh.SetTypeOfModifierVolumes(dataController.selectableType == SelectableType.Player ? 1 : 0, -1);
    }
    private void OnEnemyTurnStart()
    {
        pawnNavMesh.SetTypeOfModifierVolumes(dataController.selectableType == SelectableType.Enemy ? 1 : 0, -1);
    }

    public override void OnMove(Vector3 position)
    {
        pawnNavMesh.TravelToPosition(position);
        animatorBrain.Play((int)AnimatorBrainBase.Animations.WALK, 0, false, false);
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
        other.rigidbody.AddForce(dir * dataController.obstaclePushForce, ForceMode.Impulse);
    }

    public override void OnShoot(Vector3 position)
    {
        transform.LookAt(position);
        animatorBrain.Play((int)AnimatorBrainBase.Animations.ATTACK, 0, true, false);
        dataController.SetParameterValue(
            PawnDataController.SHOOTED_AMOUNT_KEY,
            dataController.GetParameterValue(PawnDataController.SHOOTED_AMOUNT_KEY) + 1
        );
        dataController.SetParameterValue(
            PawnDataController.MAG_AMOUNT_KEY,
            dataController.GetParameterValue(PawnDataController.MAG_AMOUNT_KEY) - 1
        );
    }
    public override void OnMelee(Vector3 position)
    {
        transform.LookAt(position);
        animatorBrain.Play((int)AnimatorBrainBase.Animations.ATTACK, 0, true, false);
        dataController.SetParameterValue(
            PawnDataController.MELEE_AMOUNT_KEY,
            dataController.GetParameterValue(PawnDataController.MELEE_AMOUNT_KEY) + 1
        );
    }
    public override void OnGetHit(float damage)
    {
        float newHealth = dataController.GetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY) - damage;
        UI3DManager.Instance.ShowMessage("-" + damage.ToString("F1"), transform.position, Color.red);
        if (newHealth <= 0f)
        {
            UI3DManager.Instance.ShowMessage("Kill", transform.position, Color.red);
            dataController.selectableType = SelectableType.Dead;
            gameObject.layer = LayerMask.NameToLayer("DeadPawn");
            pawnNavMesh.SetTypeOfModifierVolumes(-1, -1, 1);
            transform.position -= transform.up * 0.5f;
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            animatorBrain.Play((int)AnimatorBrainBase.Animations.DEATH, 0, true, true);
            newHealth = 0f;
        }
        dataController.SetParameterValue(
            PawnDataController.AVAILABLE_HEALTH_KEY,
            newHealth
        );
        dataController.SetParameterValue(
            PawnDataController.AMOUNT_OF_DEFENDED_HITS_KEY,
            dataController.GetParameterValue(PawnDataController.AMOUNT_OF_DEFENDED_HITS_KEY) + 1
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