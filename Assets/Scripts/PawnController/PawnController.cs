using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(ISelectorBrain))]
[RequireComponent(typeof(ClickableItemsController))]
public class PawnController : MonoBehaviour
{
    public static PawnController Instance { get; private set; }
    public ClickableItemsController clickableItemsController { get; private set; }
    PawnController()
    {
        if (Instance == null) Instance = this;
        else
        {
            Debug.LogError("Constructor met second PawnController instance");
        }
    }

    [SerializeField]
    public ISelectorBrain playerSelectorBrain;
    [SerializeField]
    public ISelectorBrain enemySelectorBrain;
    [SerializeField]
    public PathDrawerWithText pathDrawer;
    [SerializeField]
    private IconButtonStyleFiller shootOnMoveButton;
    [SerializeField]
    public InputActionReference toggleShootOnMoveAction;
    [SerializeField]
    private IconButtonStyleFiller startReloadButton;
    [SerializeField]
    private InputActionReference startReloadAction;


    public ISelectorBrain currentSelector
    { get; private set; }
    public ISelectorBrainWithUI currentSelectorWithUICached { get; private set; }
    public IPawnState currentState { get; private set; }
    public IControlableSelectable _currentSelectedPawn;
    public IControlableSelectable currentSelectedPawn
    {
        get => _currentSelectedPawn;
        private set
        {
            _currentSelectedPawn = value;
            if (currentState == null) return;
            if (_currentSelectedPawn == null)
            {
                currentState.enabled = false;
            }
            else
            {
                currentState.enabled = true;
            }
        }
    }
    public const string ATTACKER_PREFIX = "Attacker";
    public const string PREY_PREFIX = "Prey";
    public const string IS_WALLS_BETWEEN_KEY = "isWallsBetween";
    public const string PAWN_DISTANCE_LABEL = "pawnDistance";
    public const string CURRENT_TARGET_ANGLE = "targetAngle";
    public const string LAST_SHOT_ANGLE = "lastShotAngle";
    public static string[] ALL_KEYS = new string[] { IS_WALLS_BETWEEN_KEY, PAWN_DISTANCE_LABEL, CURRENT_TARGET_ANGLE, LAST_SHOT_ANGLE };

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Awake met second PawnController instance");
        }
        SetSelectorBrain(GetComponent<ISelectorBrain>());
        clickableItemsController = GetComponent<ClickableItemsController>();
    }

    void Start()
    {
        if (toggleShootOnMoveAction != null)
            toggleShootOnMoveAction.action.Enable();
        if (startReloadAction != null)
            startReloadAction.action.Enable();
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurn;
            TurnManager.Instance.OnEnemyTurnStart += OnEnemyTurn;
            TurnManager.Instance.OnTriggerZoneExit += OnTriggerZoneExit;
        }
        SelectableToBoxConnector.HelperTag = currentSelectedPawn == null ? "[персонаж]->[ЛКМ]" : "[ЛКМ]";
        SliderToPawnConnector.HelperTag = currentSelectedPawn == null ? "[персонаж]->[ЛКМ]" : "[ЛКМ]";
    }
    void Update()
    {
        if (UILayersController.Instance.overlayStack.Peek() != UILayersController.UILayer.GameUI) return;
        // Polling selector brain and addressing logic to the current state
        if (currentSelector == null)
        {
            SetSelectorBrain(GetComponent<ISelectorBrain>());
        }
        IPawnState newState = currentSelector.PollChangeState();
        if (newState != null)
        {
            if (currentState != null) currentState.enabled = false;
            currentState = newState;
            currentState.enabled = true;
        }

        IControlableSelectable newSelection = currentSelector.PollSelectPawn(currentSelectedPawn);
        if (newSelection != currentSelectedPawn)
        {
            if (currentSelectedPawn != null) currentSelectedPawn.OnDeselect();
            currentSelectedPawn = newSelection;
            if (newSelection != null)
            {
                newSelection.OnSelect();
            }
            UpdateMoveOnShootButtonColor();
            UpdateStartReloadButtonColor();
            SelectableToBoxConnector.HelperTag = currentSelectedPawn == null ? "[персонаж]->[ЛКМ]" : "[ЛКМ]";
            SliderToPawnConnector.HelperTag = currentSelectedPawn == null ? "[персонаж]->[ЛКМ]" : "[ЛКМ]";
        }

        ISelectable selectable = currentSelector.PollSelectClickableItem(clickableItemsController.currentSelectedItem);
        if (selectable != null)
        {
            if (selectable != clickableItemsController.currentSelectedItem)
            {
                bool selecting = clickableItemsController.OnSelect(selectable);
                if (!selecting)
                {

                    currentSelector.SetClickAsUnhandled();
                }
            }
        }
        else
        {
            clickableItemsController.OnDeselect();
            UI3DManager.Instance.HideContextMenu();
        }

        if (currentState != null)
        {

            (ISelectable selectable2, Vector3 worldPoint) = currentSelector.PollSelectPosForState();
            if (selectable2 != null || worldPoint != Vector3.zero)
            {
                SetCalculatableParamsForTwoPawns(currentSelectedPawn, selectable2 == null ? worldPoint : selectable2.GetTransform().position);
                currentState.HandleDoingSth(worldPoint, selectable2);
            }

            if (currentSelectorWithUICached != null)
            {
                (ISelectable selectable3, Vector3 worldPoint2, Vector2 screenPoint, ScreenCastHitResult hit) = currentSelectorWithUICached.PollForIntermidiateAiming();
                currentState.HandleUIDrawing(selectable3, worldPoint2, screenPoint, hit);
            }
        }
        isValidStage1 = false;
        isValidStage2 = false;

        if (toggleShootOnMoveAction != null && toggleShootOnMoveAction.action.triggered && currentSelectedPawn != null)
        {
            ToggleShootOnMove();
        }
        if (startReloadAction != null && startReloadAction.action.triggered && currentSelectedPawn != null)
        {
            StartReload();
        }
    }

    public bool IsInCombat()
    {
        return HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] > 0.5f;
    }
    public void ToggleShootOnMove()
    {
        if (currentSelectedPawn == null) return;
        if (!IsInCombat()) return;
        PawnDataController data = currentSelectedPawn.PawnData;
        if (data == null) return;
        if (data.WalkedDistance > 0.5f || data.ShotAmount > 0.5f) return;
        data.SetHasMovedThisTurn(!data.HasMovedThisTurn);
        UpdateMoveOnShootButtonColor();
    }
    public void UpdateMoveOnShootButtonColor()
    {
        if (!currentSelector.SyncUI) return;
        if (currentSelectedPawn == null || !IsInCombat())
        {
            shootOnMoveButton.TurnOffButton();
            return;
        }
        PawnDataController data = currentSelectedPawn.PawnData;
        bool isOn = data != null && data.HasMovedThisTurn;
        bool hasWalked = data != null && data.WalkedDistance > 0.5f;
        bool hasShot = data != null && data.ShotAmount > 0.5f;
        bool locked = hasWalked || hasShot;
        if (isOn)
        {
            shootOnMoveButton.TurnOnButton();
            if (locked)
                shootOnMoveButton.SetInteractable(false);
        }
        else
        {
            shootOnMoveButton.TurnOffButton();
            if (!locked)
                shootOnMoveButton.SetInteractable(true);
        }
    }
    public void StartReload()
    {
        UpdateStartReloadButtonColor();
    }

    public void OnTriggerZoneExit()
    {
    }
    public void UpdateStartReloadButtonColor()
    {
        if (!currentSelector.SyncUI) return;
        if (startReloadButton != null)
            startReloadButton.TurnOffButton();
    }

    void OnValidate()
    {
        if (Instance == null) Instance = this;
        if (Instance != this) Debug.LogWarning("Two instances of PawnController found " + gameObject.name);
        if (playerSelectorBrain == null)
        {
            ISelectorBrain[] selectorBrains = GetComponents<ISelectorBrain>();
            if (selectorBrains.Length > 0)
            {
                playerSelectorBrain = selectorBrains[0];
            }
        }
        if (enemySelectorBrain == null)
        {
            ISelectorBrain[] selectorBrains = GetComponents<ISelectorBrain>();
            if (selectorBrains.Length > 1)
            {
                enemySelectorBrain = selectorBrains[1];
            }
            else
            {
                Debug.LogError("No second selector brain found, using first one");
                enemySelectorBrain = playerSelectorBrain;
            }
        }
    }

    public void ChangeSelectorBrain(ISelectorBrain newSelectorBrain)
    {
        SetSelectorBrain(newSelectorBrain);
    }

    private void SetSelectorBrain(ISelectorBrain newSelectorBrain)
    {
        currentSelectorWithUICached = null;
        currentSelector = newSelectorBrain;
        if (newSelectorBrain is ISelectorBrainWithUI selectorWithUI)
        {
            currentSelectorWithUICached = selectorWithUI;
        }
    }

    void OnPlayerTurn()
    {
        ChangeSelectorBrain(playerSelectorBrain);
        IControlableSelectable actor = TurnManager.Instance != null ? TurnManager.Instance.CurrentActor : null;
        if (actor != null && actor.GetSelectableType() == SelectableType.Player)
        {
            if (InputScreenMouseControlActions.Instance != null)
                InputScreenMouseControlActions.Instance.SelectPlayer(actor);
        }
    }

    void OnEnemyTurn()
    {
        ChangeSelectorBrain(enemySelectorBrain);
    }

    public bool IsSelectionLockedToCurrentActor()
    {
        if (!IsInCombat()) return false;
        if (TurnManager.Instance == null || TurnManager.Instance.CurrentActor == null) return false;
        return TurnManager.Instance.CurrentActor.GetSelectableType() == SelectableType.Player;
    }

    public IControlableSelectable GetLockedActor()
    {
        return TurnManager.Instance != null ? TurnManager.Instance.CurrentActor : null;
    }

    public static bool isValidStage1 = false;
    public static void SetCalculatableParamsForTwoPawns(IControlableSelectable attacker, Vector3 target)
    {
        isValidStage1 = true;
    }

    public static bool isValidStage2 = false;
    public static void SetCalculatableParamsForTwoPawns(IControlableSelectable attacker, IAttackableSelectable prey)
    {
        SetCalculatableParamsForTwoPawns(attacker, prey.GetTransform().position);
        isValidStage2 = true;
    }
}