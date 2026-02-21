using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PathDrawer))]
[RequireComponent(typeof(PawnDataController))]
[RequireComponent(typeof(PawnNavMesh))]
public class PawnBrain : MonoBehaviour, IControlableSelectable
{
    private PathDrawer pathDrawer;
    private PawnDataController dataController;
    private PawnNavMesh pawnNavMesh;

    public bool IsShootable => true;
    [SerializeField]
    private SelectableType selectableType = SelectableType.Player;
    public SelectableType GetSelectableType() => selectableType;

    private AnimatorBrainPlayer animatorBrainPlayer;
    private Animator anim;

    void Awake()
    {
        pathDrawer = GetComponent<PathDrawer>();
        dataController = GetComponent<PawnDataController>();
        pawnNavMesh = GetComponent<PawnNavMesh>();

        animatorBrainPlayer = GetComponentInChildren<AnimatorBrainPlayer>();
        anim = GetComponentInChildren<Animator>();
        animatorBrainPlayer.Initialize(1, PlayerAnimations.IDLE, anim, (layer) => animatorBrainPlayer.Play(PlayerAnimations.IDLE, layer, false, false));
        animatorBrainPlayer.Play(PlayerAnimations.IDLE, 0, false, false);
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
            if (animatorBrainPlayer.GetCurrentAnimation(0) != PlayerAnimations.IDLE)
            {
                animatorBrainPlayer.Play(PlayerAnimations.IDLE, 0, false, false);
            }
            if (pathDrawer.GetVisible())
            {
                pathDrawer.SetVisible(false);
            }
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void OnMove(Vector3 position)
    {
        pawnNavMesh.TravelToPosition(position);
        animatorBrainPlayer.Play(PlayerAnimations.WALK, 0, false, false);
    }

    public bool IsMoving()
    {
        return pawnNavMesh.IsMoving();
    }

    public (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) GetPathPointsTo(Vector3 position)
    {
        return pawnNavMesh.GetPathPointsTo(position);
    }

    public void OnShoot(Vector3 position)
    {
        Debug.Log("OnShoot: " + position);
        transform.LookAt(position);
        animatorBrainPlayer.Play(PlayerAnimations.ATTACK, 0, true, false);
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

    public void OnGetHit(float damage)
    {
        float newHealth = dataController.GetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY) - damage;
        if (newHealth <= 0f)
        {
            //  Debug.Log("Dying" + name + " " + damage);
            selectableType = SelectableType.Dead;
            transform.position -= transform.up * 0.5f;
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            animatorBrainPlayer.Play(PlayerAnimations.DEATH, 0, true, true);
            newHealth = 0f;
        }
        dataController.SetParameterValue(
            PawnDataController.AVAILABLE_HEALTH_KEY,
            newHealth
        );
    }

    public string GetHPText()
    {
        return $"{dataController.GetParameterValue(PawnDataController.AVAILABLE_HEALTH_KEY)} / {dataController.GetParameterValue(PawnDataController.INITIAL_HP_KEY)}";
    }

    public IFormulaData GetFormulaData()
    {
        return dataController;
    }

}