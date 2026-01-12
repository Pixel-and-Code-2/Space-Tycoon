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

    void Update()
    {
        if (selectedPawn == null)
        {
            TryToTakeControlOfPawn();
        }
        else
        {
            ClickWithPawnHandle();
        }
        DeselectionCheck();
    }

    private void FixedUpdate()
    {
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
        (Vector3 point, FloorHitResult hit) = selector.GetMoveSelectionValue();
        if (hit != FloorHitResult.NoHit)
        {
            Vector3[] calculatedPath = selectedWalkablePawn.GetPathPointsTo(point);
            if (calculatedPath != null)
            {
                if (!pawnMoveUIController.GetVisible())
                {
                    pawnMoveUIController.SetVisible(true);
                }
                float availableDistance = selectedWalkablePawn.GetAvailableDistance();
                pawnMoveUIController.SetPathPoints(calculatedPath, availableDistance);
            }
            else
            {
                pawnMoveUIController.SetVisible(false);
            }
            lastHitPoint = point;
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
        if (clicked && hitPointValid && selectedWalkablePawn != null)
        {
            selectedWalkablePawn.OnMove(lastHitPoint);
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