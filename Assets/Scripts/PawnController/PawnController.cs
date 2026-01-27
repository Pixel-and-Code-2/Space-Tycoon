using UnityEngine;

[RequireComponent(typeof(ISelectorBrain))]
public class PawnController : MonoBehaviour
{
    private ISelectorBrain selector;
    private ISelectable selectedPawn = null;
    private IWalkableSelectable selectedWalkablePawn = null;
    private Vector3 lastHitPoint = Vector3.zero;
    private bool hitPointValid = false;
    [SerializeField]
    private PawnMoveUIController pawnMoveUIController;

    private bool pathFrozen = false;
    private Vector3 worldPointFrozen = Vector3.zero;

    void Awake()
    {
        selector = GetComponent<ISelectorBrain>();
        if (pawnMoveUIController == null)
        {
            Debug.LogError("PawnMoveUIController not found");
            return;
        }
        pawnMoveUIController.SetVisible(false);
    }

    private void Start()
    {
        if(TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnEnd += OnTurnEnd;  
        }
    }

    private void OnTurnEnd()
    {
        if(selectedPawn != null)
        {
            selectedPawn.OnDeselect();
            selectedPawn = null;
            selectedWalkablePawn = null;
        }
        pawnMoveUIController.SetVisible(false);
        pathFrozen = false;
    }

    void Update()
    {

        if( TurnManager.Instance != null && !TurnManager.Instance.IsPlayerTurn)
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
            worldPointFrozen = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        if(TurnManager.Instance != null && !TurnManager.Instance.IsPlayerTurn)
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

        (Vector3 worldPoint, Vector2 screenPoint, FloorHitResult hit) = selector.GetMoveSelectionValue();
        if (hit != FloorHitResult.NoHit)
        {
            Vector3 targetPoint = pathFrozen ? worldPointFrozen : worldPoint;
            (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) = selectedWalkablePawn.GetPathPointsTo(targetPoint);
            if (pathFrozen)
            {
                if (pointsAvailable != null)
                {
                    pawnMoveUIController.SetPathPoints(pointsAvailable, null, screenPoint);
                }
                else
                {
                    pawnMoveUIController.SetVisible(false);
                }
            }
            else
            {
                if (pointsAvailable != null || pointsOutOfRange != null)
                {
                    pawnMoveUIController.SetPathPoints(pointsAvailable, pointsOutOfRange, screenPoint);
                    if (pointsAvailable != null && pointsAvailable.Length > 0) 
                    {
                        worldPointFrozen = pointsAvailable[^1]; 
                    }

                    if (!pawnMoveUIController.GetVisible())
                    {
                        pawnMoveUIController.SetVisible(true);
                    }
                }
                else
                {
                    pawnMoveUIController.SetVisible(false);
                }
            }
            lastHitPoint = targetPoint;
            hitPointValid = true;
        }
    }

    private void UnSetAim()
    {
        hitPointValid = false;
        lastHitPoint = Vector3.zero;
    }

    private void TryToTakeControlOfPawn()
    {
        ISelectable selectionClick = selector.GetSelectionClickValue();
        if (selectionClick is IWalkableSelectable walkablePawn)
        {
            pawnMoveUIController.SetVisible(true);
            selectedWalkablePawn = walkablePawn;
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
        }
    }

    private void DeselectionCheck()
    {
        bool deselectionClick = selector.GetDeselectionClickState();
        if (deselectionClick && selectedPawn != null)
        {
            pawnMoveUIController.SetVisible(false);
            selectedPawn.OnDeselect();
            selectedPawn = null;
            selectedWalkablePawn = null;
        }
    }

}