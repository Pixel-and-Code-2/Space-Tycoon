using UnityEngine;


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
    private ISelectorBrain playerSelectorBrain;
    [SerializeField]
    private ISelectorBrain enemySelectorBrain;
    [SerializeField]
    public PathDrawerWithText pathDrawer; // { get; private set; }


    public ISelectorBrain currentSelector { get; private set; }
    private ISelectorBrainWithUI currentSelectorWithUICached;
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
    public static string[] ALL_KEYS = new string[] { IS_WALLS_BETWEEN_KEY, PAWN_DISTANCE_LABEL };

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
        }
    }
    void Update()
    {
        // Polling selector brain and addressing logic to the current state
        if (currentSelector == null)
        {
            SetSelectorBrain(GetComponent<ISelectorBrain>());
            if (currentSelector == null)
            {
                Debug.LogError("Current selector is null and somehow doesn't exist");
                return;
            }
        }

        currentSelectedPawn = currentSelector.PollSelectPawn(currentSelectedPawn);
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

        IPawnState newState = currentSelector.PollChangeState();
        if (newState != null)
        {
            if (currentState != null) currentState.enabled = false;
            currentState = newState;
            currentState.enabled = true;
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