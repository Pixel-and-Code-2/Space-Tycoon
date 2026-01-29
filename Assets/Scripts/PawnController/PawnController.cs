using UnityEngine;

public enum ControlType
{
    walk,
    shoot
}

[RequireComponent(typeof(ISelectorBrain))]
public class PawnController : MonoBehaviour
{
    private ISelectorBrain selector;
    private ISelectable selectedPawn = null;
    private IWalkableSelectable selectedWalkablePawn = null;
    private Vector3 lastHitPoint = Vector3.zero;
    private bool hitPointValid = false;
    [SerializeField]
    private PathDrawerWithText pathDrawerWithText;
    private ControlType controlType = ControlType.shoot;

    private bool pathFrozen = false;

    void Awake()
    {
        selector = GetComponent<ISelectorBrain>();
        if (pathDrawerWithText == null)
        {
            Debug.LogError("PawnMoveUIController not found");
            return;
        }
        pathDrawerWithText.SetVisible(false);
        selector.OnControlTypeChange += ChangeControlType;
    }

    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnEnd += OnTurnEnd;
        }
    }

    private void OnTurnEnd()
    {
        if (selectedPawn != null)
        {
            selectedPawn.OnDeselect();
            selectedPawn = null;
            selectedWalkablePawn = null;
        }
        pathDrawerWithText.SetVisible(false);
        pathFrozen = false;
    }

    void Update()
    {

        if (TurnManager.Instance != null && !TurnManager.Instance.IsPlayerTurn)
        {
            return;
        }

        if (selectedPawn == null)
        {
            TryToTakeControlOfPawn();
        }
        else
        {
            ClickWithPawnHandle();
        }

        DeselectionCheck();

        if (pathFrozen && selectedWalkablePawn != null && !selectedWalkablePawn.IsMoving())
        {
            pathFrozen = false;
        }
    }


    private void TryToTakeControlOfPawn()
    {
        ISelectable selectionClick = selector.GetSelectionClickValue();
        if (selectionClick is IWalkableSelectable walkablePawn)
        {
            pathDrawerWithText.SetVisible(true);
            selectedWalkablePawn = walkablePawn;
            pathFrozen = walkablePawn.IsMoving();
        }

        if (selectionClick != null)
        {
            selectedPawn = selectionClick;
            selectedPawn.OnSelect();
        }
    }

    private void ClickWithPawnHandle()
    {
        bool clicked = selector.GetSelectionClickState();
        if (clicked && hitPointValid && selectedWalkablePawn != null && !pathFrozen)
        {
            selectedWalkablePawn.OnMove(lastHitPoint);
            pathFrozen = true;
            pathDrawerWithText.SetVisible(false);
        }
    }

    private void DeselectionCheck()
    {
        bool deselectionClick = selector.GetDeselectionClickState();
        if (deselectionClick && selectedPawn != null)
        {
            pathDrawerWithText.SetVisible(false);
            selectedPawn.OnDeselect();
            selectedPawn = null;
            selectedWalkablePawn = null;
        }
    }

    private void FixedUpdate()
    {
        if (TurnManager.Instance != null && !TurnManager.Instance.IsPlayerTurn)
        {
            return;
        }

        if (selectedWalkablePawn != null)
        {
            SetAimToPoint();
        }
        else
        {
            UnSetAim();
        }
    }

    private void SetAimToPoint()
    {

        if (pathFrozen)
        {

            return;
        }
        if (controlType == ControlType.walk)
        {
            (Vector3 worldPoint, Vector2 screenPoint, FloorHitResult hit) = selector.GetMoveSelectionValue();
            if (hit != FloorHitResult.NoHit)
            {
                (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) = selectedWalkablePawn.GetPathPointsTo(worldPoint);

                if (pointsAvailable != null || pointsOutOfRange != null)
                {
                    pathDrawerWithText.SetText((PawnNavMesh.CalculateDistance(pointsAvailable) + PawnNavMesh.CalculateDistance(pointsOutOfRange)).ToString("F1") + "m", screenPoint);
                    pathDrawerWithText.SetPathPoints(pointsAvailable, pointsOutOfRange);

                    if (!pathDrawerWithText.GetVisible())
                    {
                        pathDrawerWithText.SetVisible(true);
                    }
                }
                else
                {
                    pathDrawerWithText.SetVisible(false);
                }

                lastHitPoint = worldPoint;
                hitPointValid = true;
            }
        }
        else if (controlType == ControlType.shoot)
        {
            (Vector3 worldPoint, Vector2 screenPoint, PawnHitResult hit) = selector.GetShootSelectionValue();
            if (hit != PawnHitResult.NoHit)
            {
                Vector3 originPoint = selectedWalkablePawn.GetTransform().position;
                if (hit == PawnHitResult.PawnHit)
                {
                    pathDrawerWithText.SetText(
                        Vector3.Distance(originPoint, worldPoint).ToString("F1") + "m" + " 94%",
                        screenPoint
                    );
                }
                else if (hit == PawnHitResult.FloorHit)
                {
                    pathDrawerWithText.SetText(
                        Vector3.Distance(originPoint, worldPoint).ToString("F1") + "m" + " 0%",
                        screenPoint
                    );
                }
                pathDrawerWithText.SetPathPoints(new Vector3[] { selectedWalkablePawn.GetTransform().position, worldPoint }, null);
                pathDrawerWithText.SetVisible(true);
            }
        }
    }

    private void UnSetAim()
    {
        hitPointValid = false;
        lastHitPoint = Vector3.zero;
    }

    public void ChangeControlType(ControlType newControlType)
    {
        controlType = newControlType;
    }

}