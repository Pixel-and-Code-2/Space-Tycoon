using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.EventSystems;


[RequireComponent(typeof(IPawnState))]
public class InputScreenMouseControlActions : ISelectorBrainWithUI
{
    public static InputScreenMouseControlActions Instance { get; private set; }
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
    private InputActionReference walkClick;
    [SerializeField]
    private InputActionReference attackClick;
    [SerializeField]
    private InputActionReference secondarySelectionClick;
    [SerializeField]
    private InputActionReference walkButtonClick;
    [SerializeField]
    private InputActionReference attackButtonClick;
    [SerializeField]
    private InputActionReference endTurnButtonClick;
    [SerializeField]
    private InputActionReference toggleShootMoveAction;
    [System.Serializable]
    struct PlayerActions
    {
        [SerializeField]
        public InputActionReference whenSelect;
        [SerializeField]
        public IControlableSelectable playerToSelect;
    }
    [SerializeField]
    private List<PlayerActions> playerActions = new List<PlayerActions>();

    [SerializeField]
    private float zeroPlaneHeight = 0f;
    [SerializeField]
    private bool showZeroPlane = false;
    [SerializeField]
    private IconButtonStyleFiller attackButton;
    [SerializeField]
    private IControlableSelectable forcedSelectedPlayer = null;
    private bool forceLockCameraOnSelect = true;
    private List<InputActionReference> actions = new List<InputActionReference>();
    private ControlType currentControlType = ControlType.walk;
    // here we tracking certain keys, to prevent multiple click handles on the same button press
    private Dictionary<InputControl, bool> handledControls = new Dictionary<InputControl, bool>();
    public const float RAYCAST_DISTANCE = 100.0f;

    // MonoBehaviour methods
    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Debug.LogError("Constructor met second InputScreenMouseControlActions instance");
        }
        meleeState = GetComponent<MeleeState>();
        walkState = GetComponent<WalkState>();
        shootState = GetComponent<ShootState>();
        OnValidate();
        if (actions.Count != 8 + playerActions.Count)
        {
            actions.Clear();
            actions.Add(selectionClick);
            actions.Add(deselectionClick);
            actions.Add(walkButtonClick);
            actions.Add(attackClick);
            actions.Add(secondarySelectionClick);
            actions.Add(endTurnButtonClick);
            actions.Add(walkClick);
            actions.Add(attackButtonClick);
            foreach (var playerAction in playerActions)
            {
                actions.Add(playerAction.whenSelect);
            }
        }
        foreach (var action in actions)
        {
            if (action != null)
            {
                foreach (var control in action.action.controls)
                {
                    handledControls[control] = false;
                }
            }
        }
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
        foreach (var action in actions)
        {
            if (action != null)
                action.action.Enable();
        }
    }
    void OnDisable()
    {
        foreach (var action in actions)
        {
            if (action != null)
                action.action.Disable();
        }
    }

    void Update()
    {
        foreach (var action in actions)
        {
            if (action != null)
                if (action.action.ReadValue<float>() != 1.0f)
                {
                    SetHandleClick(action, false);
                }
        }
        CheckAdditionalButtonClicks();
    }

    void Start()
    {
        ButtonStopPropagation.OnUIClickHandled += SetClickAsHandled;
        TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
    }
    void OnDestroy()
    {
        ButtonStopPropagation.OnUIClickHandled -= SetClickAsHandled;
        TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
    }

    // ISelectorBrain methods
    public override IControlableSelectable PollSelectPawn(IControlableSelectable defaultPawn)
    {
        IControlableSelectable locked = null;
        if (PawnController.Instance != null && PawnController.Instance.IsSelectionLockedToCurrentActor())
            locked = PawnController.Instance.GetLockedActor();

        if (forcedSelectedPlayer != null)
        {
            IControlableSelectable pl = forcedSelectedPlayer;
            forcedSelectedPlayer = null;
            if (locked != null && pl != locked) pl = locked;
            if (forceLockCameraOnSelect && CameraTargetController.Instance != null)
                CameraTargetController.Instance.ForceLockTarget();
            forceLockCameraOnSelect = true;
            return pl;
        }
        if (locked != null)
        {
            if (GetClickState(selectionClick))
                SetHandleClick(selectionClick, false);
            if (GetClickState(deselectionClick))
                SetHandleClick(deselectionClick, false);
            return locked;
        }
        if (GetClickState(selectionClick))
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
            SetHandleClick(selectionClick, false);
        }
        if (IsPawnSelected())
        {
            if (GetClickState(deselectionClick))
            {
                return null;
            }
        }
        return defaultPawn;
    }

    public override ISelectable PollSelectClickableItem(ISelectable defaultSelectable)
    {
        if (!IsPawnSelected())
        {
            return null;
        }
        if (GetClickState(secondarySelectionClick))
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("ClickableItem", "DeadPawn")))
            {
                ISelectable[] selectables = hit.collider.GetComponents<ISelectable>();
                ISelectable clickableSelectable = null;
                foreach (var item in selectables)
                {
                    if (item is IControlableSelectable) continue;
                    clickableSelectable = item;
                    break;
                }
                if (
                    clickableSelectable != null &&
                    (clickableSelectable.GetSelectableType() == SelectableType.Neutral || clickableSelectable.GetSelectableType() == SelectableType.Dead)
                )
                {
                    return clickableSelectable;
                }
            }
            SetHandleClick(secondarySelectionClick, false);
            return null;
        }
        return defaultSelectable;
    }

    public override IPawnState PollChangeState()
    {
        if (currentControlType == ControlType.attack
            && (PawnController.Instance == null || !PawnController.Instance.CanEnterShootMode()))
        {
            currentControlType = ControlType.walk;
        }

        UpdateControlButtons();

        IPawnState newState = null;
        if (currentControlType == ControlType.attack)
            newState = shootState;
        else newState = walkState;

        if (newState != PawnController.Instance.currentState)
            return newState;
        return null;
    }
    public override (ISelectable selectable, Vector3 worldPoint) PollSelectPosForState()
    {
        if (!IsPawnSelected())
        {
            return (null, Vector3.zero);
        }
        if (currentControlType == ControlType.walk && GetClickState(walkClick) || currentControlType == ControlType.attack && GetClickState(attackClick))
        {
            if (currentControlType == ControlType.walk)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Ray ray = Camera.main.ScreenPointToRay(mousePosition);
                if (Physics.Raycast(ray, out RaycastHit playerHit, RAYCAST_DISTANCE, LayerMask.GetMask("Player")))
                {
                    SetHandleClick(walkClick, false);
                    return (null, Vector3.zero);
                }
            }
            (ISelectable selectable, Vector3 worldPoint, Vector2 screenPoint, ScreenCastHitResult hit) = PollForIntermidiateAiming();
            if (hit != ScreenCastHitResult.SelectableHit)
            {
                SetHandleClick(currentControlType == ControlType.walk ? walkClick : attackClick, false);
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
        if (mousePosition == mousePositionCached && mousePositionCached != Vector2.zero) // cache is turned off when it's commented
        {
            return (selectableCached, worldPointCached, mousePositionCached, hitCached);
        }
        mousePositionCached = mousePosition;

        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        if (
            (currentControlType == ControlType.attack) &&
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
    private void CheckAdditionalButtonClicks()
    {
        if (UILayersController.Instance.overlayStack.Peek() != UILayersController.UILayer.GameUI) return;
        foreach (var playerAction in playerActions)
        {
            if (GetClickState(playerAction.whenSelect))
            {
                if (PawnController.Instance != null && PawnController.Instance.IsSelectionLockedToCurrentActor())
                {
                    IControlableSelectable locked = PawnController.Instance.GetLockedActor();
                    if (locked != null) SelectPlayer(locked);
                }
                else
                {
                    SelectPlayer(playerAction.playerToSelect);
                }
            }
        }
        if (GetClickState(endTurnButtonClick))
        {
            TurnManager.Instance.EndPlayerTurn();
        }
        if (GetClickState(walkButtonClick))
        {
            SetControlTypeTo(true);
        }
        if (GetClickState(attackButtonClick))
        {
            ToggleControlType();
        }
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            CycleAllySelection();
    }
    private void SetHandleClick(InputActionReference action, bool value)
    {
        if (action != null && action.action.controls.Count > 0)
        {
            if (value)
            {
                if (action.action.activeControl != null)
                    handledControls[action.action.activeControl] = true;
                else
                {
                    foreach (var control in action.action.controls)
                    {
                        handledControls[control] = true;
                    }
                }
            }
            else
            {
                foreach (var control in action.action.controls)
                {
                    handledControls[control] = false;
                }
            }
        }
    }
    private InputActionReference lastHitAction = null;
    private bool GetClickState(InputActionReference action)
    {
        if (
            action.action.activeControl != null && handledControls[action.action.activeControl] ||
            action.action.controls.Count > 0 && handledControls[action.action.controls[0]]
        )
        {
            return false;
        }
        bool clicked = action.action.ReadValue<float>() == 1.0f;
        if (clicked)
        {
            SetHandleClick(action, true);
            if (action == selectionClick && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return false;
            }
            lastHitAction = action;
        }
        return clicked;
    }

    public void SetClickAsHandled()
    {
        if (lastHitAction != null && lastHitAction.action.activeControl != null)
        {
            SetHandleClick(lastHitAction, true);
        }
        else
        {
            SetHandleClick(selectionClick, true);
        }
    }
    public override void SetClickAsUnhandled()
    {
        if (lastHitAction != null)
        {
            SetHandleClick(lastHitAction, false);
        }
    }
    public override void SetUICacheAsDirty()
    {
        mousePositionCached = Vector2.zero;
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

    private void OnPlayerTurnStart()
    {
        UpdateControlButtons();
    }

    private void UpdateControlButtons()
    {
        bool canShoot = PawnController.Instance != null && PawnController.Instance.CanEnterShootMode();
        if (!canShoot && currentControlType == ControlType.attack)
            currentControlType = ControlType.walk;

        if (attackButton != null)
        {
            if (currentControlType == ControlType.walk)
                attackButton.TurnOffButton();
            else
                attackButton.TurnOnButton();
            attackButton.SetInteractable(canShoot);
        }
    }

    public void ToggleControlType()
    {
        SetControlTypeTo(currentControlType == ControlType.walk ? false : true);
    }

    public void SetControlTypeTo(bool isWalk)
    {
        if (!isWalk && (PawnController.Instance == null || !PawnController.Instance.CanEnterShootMode()))
            isWalk = true;
        currentControlType = isWalk ? ControlType.walk : ControlType.attack;
        UpdateControlButtons();
    }

    public void SelectPlayer(IControlableSelectable pl, bool magnetizeCamera = true)
    {
        if (PawnController.Instance != null && PawnController.Instance.IsSelectionLockedToCurrentActor())
        {
            IControlableSelectable locked = PawnController.Instance.GetLockedActor();
            if (locked != null && pl != locked) return;
        }
        forceLockCameraOnSelect = magnetizeCamera;
        forcedSelectedPlayer = pl;
    }

    void CycleAllySelection()
    {
        if (PawnController.Instance != null && PawnController.Instance.IsSelectionLockedToCurrentActor())
            return;
        List<IControlableSelectable> allies = new List<IControlableSelectable>();
        foreach (var pawn in PawnBrain.AlivePlayers)
        {
            if (pawn != null && pawn.GetSelectableType() == SelectableType.Player)
                allies.Add(pawn);
        }
        if (allies.Count == 0) return;
        allies.Sort((a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));
        IControlableSelectable current = PawnController.Instance != null
            ? PawnController.Instance.currentSelectedPawn
            : null;
        int idx = allies.FindIndex(p => p == current);
        int next = idx < 0 ? 0 : (idx + 1) % allies.Count;
        SelectPlayer(allies[next], true);
    }

}