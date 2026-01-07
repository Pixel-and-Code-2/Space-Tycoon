using UnityEngine;
using UnityEngine.InputSystem;


public class InputScreenMouseControlActions : MonoBehaviour, ISelectorBrain
{

    [SerializeField]
    private InputActionReference selectionClick;

    [SerializeField]
    private InputActionReference deselectionClick;
    // Those vars are used to prevent multiple click handles on the same button press
    private bool selectionClickHandled = false;
    private bool deselectionClickHandled = false;
    void OnValidate()
    {
        selectionClick.action.Enable();
        deselectionClick.action.Enable();
    }

    void Update()
    {
        if (selectionClickHandled && selectionClick.action.ReadValue<float>() != 1.0f)
        {
            selectionClickHandled = false;
        }
        if (deselectionClickHandled && deselectionClick.action.ReadValue<float>() != 1.0f)
        {
            deselectionClickHandled = false;
        }
    }

    public ISelectable GetSelectionClickValue()
    {
        if (GetSelectionClickState())
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Selectable", "Pawn")))
            {
                return hit.collider.GetComponent<ISelectable>();
            }
        }
        return null;
    }

    public bool GetDeselectionClickState()
    {
        if (deselectionClickHandled)
        {
            return false;
        }
        bool clicked = deselectionClick.action.ReadValue<float>() == 1.0f;
        if (clicked)
        {
            deselectionClickHandled = true;
        }
        return clicked;
    }

    public (Vector3 point, FloorHitResult hit) GetMoveSelectionValue()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Floor")))
        {
            return (hit.point, FloorHitResult.FloorHit);
        }
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // y=0 plane
        float distance;
        if (groundPlane.Raycast(ray, out distance))
        {
            Vector3 intersection = ray.GetPoint(distance);
            return (intersection, FloorHitResult.ZeroPlaneHit);
        }
        return (Vector3.zero, FloorHitResult.NoHit);
    }

    public bool GetSelectionClickState()
    {
        if (selectionClickHandled)
        {
            return false;
        }
        bool clicked = selectionClick.action.ReadValue<float>() == 1.0f;
        if (clicked)
        {
            selectionClickHandled = true;
        }
        return clicked;
    }
}