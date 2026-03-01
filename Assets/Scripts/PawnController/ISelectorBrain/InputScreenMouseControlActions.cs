using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public enum ControlType
{
    walk,
    shoot,
    melee
}

[RequireComponent(typeof(IPawnState))]
public class InputScreenMouseControlActions : ISelectorBrainWithUI
{
    [SerializeField]
    private IPawnState meleeState;
    [SerializeField]
    private IPawnState shootState;
    [SerializeField]
    private IPawnState walkState;

    [SerializeField]
    private InputActionReference selectionClick;

    [SerializeField]
    private InputActionReference deselectionClick;
    [SerializeField]
    private float zeroPlaneHeight = 0f;
    [SerializeField]
    private bool showZeroPlane = false;

    [SerializeField]
    private TextMeshProUGUI controlTypeText;
    // These vars are used to prevent multiple click handles on the same button press
    private bool selectionClickHandled = false;
    private bool deselectionClickHandled = false;
    private bool controlTypeChangeEventSent = false;
    public const float RAYCAST_DISTANCE = 100.0f;
    private ControlType currentControlType = ControlType.walk;

    // MonoBehaviour methods
    void Awake()
    {
        meleeState = GetComponent<MeleeState>();
        walkState = GetComponent<WalkState>();
        shootState = GetComponent<ShootState>();
        OnValidate();
    }

    void OnValidate()
    {
        if (meleeState == null)
        {
            meleeState = GetComponent<IPawnState>();
        }
        if (shootState == null)
        {
            shootState = GetComponent<IPawnState>();
        }
        if (walkState == null)
        {
            walkState = GetComponent<IPawnState>();
        }
    }
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

    void Start()
    {
        ButtonStopPropagation.OnUIClickHandled += SetClickAsHandled;
    }
    void OnDestroy()
    {
        ButtonStopPropagation.OnUIClickHandled -= SetClickAsHandled;
    }

    // ISelectorBrain methods
    public override IControlableSelectable PollSelectPawn(IControlableSelectable defaultPawn)
    {
        if (IsPawnSelected())
        {
            if (GetDeselectionClickState())
            {
                return null;
            }
            return defaultPawn;
        }
        if (GetSelectionClickState())
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Player")))
            {
                IControlableSelectable controlableSelectable = hit.collider.GetComponent<IControlableSelectable>();
                if (
                    controlableSelectable != null &&
                    controlableSelectable.GetSelectableType() == SelectableType.Player
                )
                {
                    return controlableSelectable;
                }
            }
        }
        return null;
    }

    public override ISelectable PollSelectClickableItem(ISelectable defaultSelectable)
    {
        if (!IsPawnSelected())
        {
            return null;
        }
        if (GetSelectionClickState())
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("ClickableItem")))
            {
                ISelectable selectable = hit.collider.GetComponent<ISelectable>();
                if (
                    selectable != null &&
                    selectable.GetSelectableType() == SelectableType.Neutral
                )
                {
                    return selectable;
                }
            }
            // kostil'
            selectionClickHandled = false;
            return null;
        }
        return defaultSelectable;
    }

    public override IPawnState PollChangeState()
    {
        if (!controlTypeChangeEventSent)
        {
            controlTypeChangeEventSent = true;
            IPawnState newState =
                currentControlType == ControlType.walk ? walkState :
                currentControlType == ControlType.shoot ? shootState :
                meleeState;
            return newState;
        }
        return null;
    }
    public override (ISelectable selectable, Vector3 worldPoint) PollSelectPosForState()
    {
        if (!IsPawnSelected())
        {
            return (null, Vector3.zero);
        }
        if (GetSelectionClickState())
        {
            (ISelectable selectable, Vector3 worldPoint, Vector2 screenPoint, ScreenCastHitResult hit) = PollForIntermidiateAiming();
            if (hit != ScreenCastHitResult.SelectableHit)
            {
                return (null, worldPoint);
            }
            return (selectable, worldPoint);
        }
        return (null, Vector3.zero);
    }

    private Vector2 mousePositionCached = Vector2.zero; // caching variables to prevent useless raycasts
    private Vector3 worldPointCached = Vector3.zero;
    private ISelectable selectableCached = null;
    private ScreenCastHitResult hitCached = ScreenCastHitResult.NoHit;
    private RaycastHit raycastHitCached;
    public override (ISelectable selectable, Vector3 worldPoint, Vector2 screenPoint, ScreenCastHitResult hit) PollForIntermidiateAiming()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (!IsPawnSelected())
        {
            return (null, Vector3.zero, mousePosition, ScreenCastHitResult.NoHit);
        }
        if (mousePosition == mousePositionCached && mousePositionCached != Vector2.zero)
        {
            return (selectableCached, worldPointCached, mousePositionCached, hitCached);
        }
        mousePositionCached = mousePosition;

        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        if (
            (currentControlType == ControlType.shoot || currentControlType == ControlType.melee) &&
            Physics.Raycast(ray, out raycastHitCached, RAYCAST_DISTANCE, LayerMask.GetMask("Hitable")))
        {
            selectableCached = raycastHitCached.collider.GetComponent<ISelectable>();
            worldPointCached = raycastHitCached.point;
            hitCached = ScreenCastHitResult.SelectableHit;
        }
        else if (Physics.Raycast(ray, out raycastHitCached, RAYCAST_DISTANCE, LayerMask.GetMask("Floor")))
        {
            worldPointCached = raycastHitCached.point;
            hitCached = ScreenCastHitResult.FloorHit;
            selectableCached = null;
        }
        else
        {
            Plane groundPlane = new Plane(Vector3.up, Vector3.up * zeroPlaneHeight); // y={zeroPlaneHeight} plane
            float distance;
            if (groundPlane.Raycast(ray, out distance) && distance <= RAYCAST_DISTANCE)
            {
                worldPointCached = ray.GetPoint(distance);
                hitCached = ScreenCastHitResult.ZeroPlaneHit;
                selectableCached = null;
            }
            else
            {
                hitCached = ScreenCastHitResult.NoHit;
                selectableCached = null;
                worldPointCached = Vector3.zero;
            }
        }
        return (selectableCached, worldPointCached, mousePosition, hitCached);
    }

    // Helper methods
    private bool GetSelectionClickState()
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

    public void SetClickAsHandled()
    {
        selectionClickHandled = true;
    }

    private bool GetDeselectionClickState()
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

    void OnDrawGizmos()
    {
        if (showZeroPlane)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.up * zeroPlaneHeight, new Vector3(10f, 0.01f, 10f));
        }
    }

    bool IsPawnSelected()
    {
        return PawnController.Instance.currentSelectedPawn != null;
    }

    public void ChangeControlType()
    {
        currentControlType =
            currentControlType == ControlType.walk ? ControlType.shoot :
            currentControlType == ControlType.shoot ? ControlType.melee :
            ControlType.walk;
        controlTypeChangeEventSent = false;
        if (controlTypeText != null)
        {
            controlTypeText.text =
                currentControlType == ControlType.walk ? "Control: Walk" :
                currentControlType == ControlType.shoot ? "Control: Shoot" :
                "Control: Melee";
        }
    }
}