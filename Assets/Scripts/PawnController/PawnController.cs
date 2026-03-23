using UnityEngine;
using UnityEngine.UI;
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
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurn;
            TurnManager.Instance.OnEnemyTurnStart += OnEnemyTurn;
            TurnManager.Instance.OnTriggerZoneExit += OnTriggerZoneExit;
        }
    }
    void Update()
    {
        if (UILayersController.Instance.currentLayer != UILayersController.UILayer.GameUI) return;
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
            UpdateStartReloadButtonColor();
            if (currentSelectedPawn != null) currentSelectedPawn.OnDeselect();
            currentSelectedPawn = newSelection;
            if (newSelection != null)
            {
                UpdateMoveOnShootButtonColor();
                newSelection.OnSelect();
            }
        }

        ISelectable selectable = currentSelector.PollSelectClickableItem(clickableItemsController.currentSelectedItem);
        if (selectable != null)
        {
            HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict[PAWN_DISTANCE_LABEL] =
                Vector3.Distance(
                    currentSelectedPawn.GetTransform().position,
                    selectable.GetTransform().position
                );
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
    }

    public void ToggleShootOnMove()
    {
        if (currentSelectedPawn == null) return;
        float res = currentSelectedPawn.GetDynamicParameterValue(PawnDataController.IS_SHOOT_ON_MOVE_KEY) < 0.5f ? 1f : 0f;
        if (currentSelectedPawn.GetDynamicParameterValue(PawnDataController.WALKED_KEY) > 0.5f) res = 1f;
        currentSelectedPawn.SetDynamicParameterValue(PawnDataController.IS_SHOOT_ON_MOVE_KEY, res);
        UpdateMoveOnShootButtonColor();
    }
    public void UpdateMoveOnShootButtonColor()
    {
        if (!currentSelector.SyncUI) return;
        if (Mathf.Abs(currentSelectedPawn.GetDynamicParameterValue(PawnDataController.IS_SHOOT_ON_MOVE_KEY) - 0f) < 0.1f)
        {
            shootOnMoveButton.TurnOffButton();
        }
        else
        {
            shootOnMoveButton.TurnOnButton();
        }
    }
    public void StartReload()
    {
        if (currentSelectedPawn == null)
        {
            UpdateStartReloadButtonColor();
            return;
        }
        if (currentSelectedPawn.GetDynamicParameterValue(PawnDataController.MOVES_TO_SKIP_KEY) > 0.001f)
        {
            UpdateStartReloadButtonColor();
            return;
        }

        currentSelectedPawn.MakeReload();
        UpdateStartReloadButtonColor();
    }

    public void OnTriggerZoneExit()
    {
        StartReload();
    }
    public void UpdateStartReloadButtonColor()
    {
        if (!currentSelector.SyncUI) return;
        if (currentSelectedPawn == null)
        {
            startReloadButton.TurnOffButton();
            return;
        }
        if (currentSelectedPawn.GetDynamicParameterValue(PawnDataController.MOVES_TO_SKIP_KEY) < 0.1f)
        {
            startReloadButton.TurnOnButton();
        }
        else
        {
            startReloadButton.TurnOffButton();
        }
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
    }

    void OnEnemyTurn()
    {
        ChangeSelectorBrain(enemySelectorBrain);
    }

    public static bool isValidStage1 = false;
    public static void SetCalculatableParamsForTwoPawns(IControlableSelectable attacker, Vector3 target)
    {
        if (isValidStage1) return;
        Vector3 origin = attacker.GetTransform().position;
        Vector3 direction = (target - origin).normalized;
        float distance = Vector3.Distance(origin, target);
        HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict[PAWN_DISTANCE_LABEL] = distance;
        // float randomValue = Random.value;
        HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.RANDOM_KEY] = Random.value;

        Vector3 dir2D = new Vector3(direction.x, 0f, direction.z).normalized;
        float angle = Mathf.Atan2(dir2D.z, dir2D.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict[CURRENT_TARGET_ANGLE] = angle;

        RaycastHit hitInfo;
        HandleInittingGlobalVars.mainCalculatedFormulaData.parametersDict[IS_WALLS_BETWEEN_KEY] =
            Physics.Raycast(origin, direction, out hitInfo, distance, LayerMask.GetMask("Wall")) ? 1f : 0f;

        attacker.FillFormulaData(HandleInittingGlobalVars.mainCalculatedFormulaData, PawnController.ATTACKER_PREFIX);

        isValidStage1 = true;
    }

    public static bool isValidStage2 = false;
    public static void SetCalculatableParamsForTwoPawns(IControlableSelectable attacker, IAttackableSelectable prey)
    {
        if (isValidStage2) return;

        SetCalculatableParamsForTwoPawns(attacker, prey.GetTransform().position);
        prey.FillFormulaData(HandleInittingGlobalVars.mainCalculatedFormulaData, PawnController.PREY_PREFIX);

        isValidStage2 = true;
    }
}