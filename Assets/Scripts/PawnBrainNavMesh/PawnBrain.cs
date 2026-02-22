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

    [SerializeField]
    private SelectableType selectableType = SelectableType.Player;
    public override SelectableType GetSelectableType() => selectableType;

    [SerializeField]
    private AnimatorBrainBase animatorBrain;
    private Animator anim;

    void Awake()
    {
        pathDrawer = GetComponent<PathDrawer>();
        dataController = GetComponent<PawnDataController>();
        pawnNavMesh = GetComponent<PawnNavMesh>();

        animatorBrain = GetComponentInChildren<AnimatorBrainBase>();
        anim = GetComponentInChildren<Animator>();
        animatorBrain.Initialize(1, (int)AnimatorBrainBase.Animations.IDLE, anim, (layer) => animatorBrain.Play((int)AnimatorBrainBase.Animations.IDLE, layer, false, false));
        animatorBrain.Play((int)AnimatorBrainBase.Animations.IDLE, 0, false, false);
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
                // we can update route runtime, but it's epensive
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

    public override void OnShoot(Vector3 position)
    {
        Debug.Log("OnShoot: " + position);
        transform.LookAt(position);
        animatorBrain.Play((int)AnimatorBrainBase.Animations.ATTACK, 0, true, false);
        // transform.position += Vector3.up * 10f;
        // transform.position += transform.forward * 0.2f;
    }

    void OnCollisionEnter(Collision other)
    {
        if (!other.rigidbody) return;
        Vector3 dir = -transform.position + other.transform.position;
        if (dataController.verticalPushOverride != -1f) dir.y = dataController.verticalPushOverride;
        other.rigidbody.AddForce(dir * dataController.obstaclePushForce, ForceMode.Impulse);
    }

    public override void OnGetHit(float damage)
    {
        float newHealth = dataController.GetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY) - damage;
        if (newHealth <= 0f)
        {
            //  Debug.Log("Dying" + name + " " + damage);
            selectableType = SelectableType.Dead;
            transform.position -= transform.up * 0.5f;
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            animatorBrain.Play((int)AnimatorBrainBase.Animations.DEATH, 0, true, true);
            newHealth = 0f;
        }
        dataController.SetParameterValue(
            PawnDataController.AVAILABLE_HEALTH_KEY,
            newHealth
        );
    }

    public override IFormulaData GetFormulaData()
    {
        return dataController;
    }

}