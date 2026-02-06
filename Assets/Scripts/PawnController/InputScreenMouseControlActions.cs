using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;


public class InputScreenMouseControlActions : MonoBehaviour, ISelectorBrain
{

    [SerializeField]
    private InputActionReference selectionClick;

    [SerializeField]
    private InputActionReference deselectionClick;
    [SerializeField]
    private float zeroPlaneHeight = 0f;
    [SerializeField]
    private bool showZeroPlane = false;
    // Change control type
    public event System.Action<ControlType> OnControlTypeChange;
    private ControlType currentControlType = ControlType.shoot;
    [SerializeField]
    private TextMeshProUGUI controlTypeText;
    // These vars are used to prevent multiple click handles on the same button press
    private bool selectionClickHandled = false;
    private bool deselectionClickHandled = false;
    public const float RAYCAST_DISTANCE = 100.0f;
    void OnEnable()
    {
        selectionClick.action.Enable();
        deselectionClick.action.Enable();
    }
    void OnDisable()
    {
        selectionClick.action.Disable();
        deselectionClick.action.Disable();
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

    // public ISelectable GetSelectionClickValue()
    // {
    //     if (GetSelectionClickState())
    //     {
    //         GetSelectionValue();
    //     }
    //     return null;
    // }

    public ISelectable GetSelectionValue()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Selectable", "Pawn")))
        {
            return hit.collider.GetComponent<ISelectable>();
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

    public (Vector3 worldPoint, Vector2 screenPoint, FloorHitResult hit) GetMoveSelectionValue()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, RAYCAST_DISTANCE, LayerMask.GetMask("Floor")))
        {
            return (hit.point, mousePosition, FloorHitResult.FloorHit);
        }
        Plane groundPlane = new Plane(Vector3.up, Vector3.up * zeroPlaneHeight); // y={zeroPlaneHeight} plane
        float distance;
        if (groundPlane.Raycast(ray, out distance) && distance <= RAYCAST_DISTANCE)
        {
            Vector3 intersection = ray.GetPoint(distance);
            return (intersection, mousePosition, FloorHitResult.ZeroPlaneHit);
        }
        return (Vector3.zero, mousePosition, FloorHitResult.NoHit);
    }

    public (Vector3 worldPoint, Vector2 screenPoint, PawnHitResult hit) GetShootSelectionValue()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, RAYCAST_DISTANCE, LayerMask.GetMask("Pawn")))
        {
            return (hit.point, mousePosition, PawnHitResult.PawnHit);
        }
        (Vector3 worldPoint2, Vector2 _, FloorHitResult hit2) = GetMoveSelectionValue();
        if (hit2 != FloorHitResult.NoHit)
        {
            return (worldPoint2, mousePosition, PawnHitResult.FloorHit);
        }
        return (Vector3.zero, mousePosition, PawnHitResult.NoHit);
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

    void OnDrawGizmos()
    {
        if (showZeroPlane)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.up * zeroPlaneHeight, new Vector3(10f, 0.01f, 10f));
        }
    }

    public void ChangeControlType()
    {
        currentControlType =
            currentControlType == ControlType.walk ? ControlType.shoot :
            currentControlType == ControlType.shoot ? ControlType.melee :
            ControlType.walk;
        if (controlTypeText != null)
        {
            controlTypeText.text =
                currentControlType == ControlType.walk ? "Control: Walk" :
                currentControlType == ControlType.shoot ? "Control: Shoot" :
                "Control: Melee";
        }
        OnControlTypeChange?.Invoke(currentControlType);
    }
}