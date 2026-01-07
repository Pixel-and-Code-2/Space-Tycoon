using UnityEngine;

[RequireComponent(typeof(ISelectorBrain))]
public class PawnController : MonoBehaviour
{
    private ISelectorBrain selector;
    private ISelectable selectedPawn = null;
    private Vector3 lastHitPoint = Vector3.zero;
    private bool hitPointValid = false;
    [SerializeField]
    private Transform showSelectionSphere;

    void Awake()
    {
        selector = GetComponent<ISelectorBrain>();
    }

    void Update()
    {
        HandleInputCheck();
    }

    void HandleInputCheck()
    {
        if (selectedPawn == null)
        {
            ISelectable selectionClick = selector.GetSelectionClickValue();
            if (selectionClick != null)
            {
                selectedPawn = selectionClick;
                selectedPawn.OnSelect();
            }
        }
        else
        {
            bool clicked = selector.GetSelectionClickState();
            if (clicked && hitPointValid)
            {
                selectedPawn.OnMove(lastHitPoint);
            }
        }
        bool deselectionClick = selector.GetDeselectionClickState();
        if (deselectionClick && selectedPawn != null)
        {
            selectedPawn.OnDeselect();
            selectedPawn = null;
        }

        if (selectedPawn != null)
        {
            (Vector3 point, FloorHitResult hit) = selector.GetMoveSelectionValue();
            if (hit != FloorHitResult.NoHit)
            {
                showSelectionSphere.position = point;
                lastHitPoint = point;
                hitPointValid = true;
            }
        }
        else
        {
            hitPointValid = false;
            lastHitPoint = Vector3.zero;
            showSelectionSphere.position = Vector3.zero;
        }
    }
}