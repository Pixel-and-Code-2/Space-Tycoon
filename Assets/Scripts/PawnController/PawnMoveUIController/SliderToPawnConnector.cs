using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class SliderToPawnConnector : MonoBehaviour
{
    public static string HelperTag = "[ЛКМ]";
    private string helperTagCached = string.Empty;
    [SerializeField]
    private TextMeshProUGUI helperText;
    private RectTransform _rectTransform;
    public RectTransform rectTransform
    {
        get
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            return _rectTransform;
        }
    }

    public PawnDataController pawn;
    private PawnDataController pawnCached;
    private ClickableItem otherSelectable;

    [Header("Sliders")]
    [SerializeField] private SliderController allyHpSlider;
    [SerializeField] private SliderController enemyHpSlider;
    [SerializeField] private SliderController allyStaminaSlider;

    [Header("Action Icons")]
    [SerializeField] private GameObject walkIcon;
    [SerializeField] private GameObject attackIcon;
    [SerializeField] private GameObject reloadIcon;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (pawn == null) Debug.LogWarning("SliderToPawnConnector: pawn not found");
        else otherSelectable = pawn.gameObject.GetComponent<ClickableItem>();
        ColorTheSlider();
        PawnDataController.OnStaminaChanged += OnStaminaChanged;
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
            TurnManager.Instance.OnEnemyTurnStart += OnEnemyTurnStart;
        }

    }

    void OnDestroy()
    {
        PawnDataController.OnStaminaChanged -= OnStaminaChanged;
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnEnemyTurnStart -= OnEnemyTurnStart;
        }
    }

    private void OnStaminaChanged(PawnDataController changed)
    {
        if (changed != pawn) return;
        RefreshStaminaBar();
    }

    private void RefreshStaminaBar()
    {
        if (pawn == null || allyStaminaSlider == null) return;
        if (pawn.selectableType != SelectableType.Player) return;
        pawnStamina = pawn.Stamina;
        bool staminaChanged = Mathf.Abs(pawnStamina - pawnStaminaCached) >= 0.001f;
        if (staminaChanged)
        {
            pawnStaminaCached = pawnStamina;
            allyStaminaSlider.SetValue(pawnStamina);
        }
        bool alive = pawnSelectableType != SelectableType.Dead && pawnHealth > 0.01f;
        UpdateActionIcons(alive);
    }

    private float GetStamina()
    {
        bool isStepByStep = HandleInittingGlobalVars.globalParameters != null
            && HandleInittingGlobalVars.globalParameters.parametersDict.ContainsKey(HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY)
            && HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] > 0.5f;
        if (!isStepByStep) return 9999f;
        return pawn != null ? pawn.Stamina : 0f;
    }

    private bool CanAttack()
    {
        if (pawn == null) return false;
        if (pawn.MovesToSkip > 0.1f) return false;
        float stamina = GetStamina();
        float rangedCost = GlobalSettingsAssets.GetStaminaCosts().rangedAttackCost;
        float meleeCost = pawn.HasRanged
            ? GlobalSettingsAssets.GetStaminaCosts().shooterMeleeAttackCost
            : GlobalSettingsAssets.GetStaminaCosts().meleeAttackCost;
        float minCost = Mathf.Min(rangedCost, meleeCost);
        return stamina >= minCost - 0.01f;
    }

    private void UpdateActionIcons(bool isAlive)
    {
        if (pawn == null || pawn.selectableType != SelectableType.Player)
        {
            if (walkIcon != null) walkIcon.SetActive(false);
            if (attackIcon != null) attackIcon.SetActive(false);
            if (reloadIcon != null) reloadIcon.SetActive(false);
            return;
        }
        if (!isAlive)
        {
            if (walkIcon != null) walkIcon.SetActive(false);
            if (attackIcon != null) attackIcon.SetActive(false);
            if (reloadIcon != null) reloadIcon.SetActive(false);
            return;
        }

        bool isStepByStep = HandleInittingGlobalVars.globalParameters != null
            && HandleInittingGlobalVars.globalParameters.parametersDict.ContainsKey(HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY)
            && HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] > 0.5f;

        if (walkIcon != null)
        {
            bool canWalk = !isStepByStep || pawn.HasUsefulMoveBudget;
            walkIcon.SetActive(canWalk);
        }

        if (reloadIcon != null)
            reloadIcon.SetActive(pawn.MovesToSkip > 0.1f);

        if (attackIcon != null)
            attackIcon.SetActive(CanAttack());
    }

    private void SetHelperTextEnabled(bool enabled)
    {
        if (helperText != null)
            helperText.enabled = enabled;
    }

    private void OnPlayerTurnStart()
    {
        SetHelperTextEnabled(true);
    }
    private void OnEnemyTurnStart()
    {
        SetHelperTextEnabled(false);
        if (helperText != null) helperText.text = "";
        helperTagCached = string.Empty;
    }

    private bool ShouldHideDeadHelperHint()
    {
        if (otherSelectable != null && otherSelectable.OccupiedBy != null) return true;
        if (ClickableItemsController.Instance != null
            && otherSelectable != null
            && ClickableItemsController.Instance.currentSelectedItem == otherSelectable)
            return true;
        if (TurnManager.Instance == null) return false;
        if (HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] < 0.5f)
            return false;
        return !TurnManager.Instance.IsPlayerTurn;
    }

    private float pawnHealthCached;
    private float pawnHealth;
    private float pawnStaminaCached;
    private float pawnStamina;
    private SelectableType pawnSelectableTypeCached;
    private SelectableType pawnSelectableType;
    private bool isAliveCached = true;

    void Update()
    {
        if (pawn == null) return;

        pawnSelectableType = pawn.selectableType;
        if (pawnSelectableType != pawnSelectableTypeCached)
        {
            pawnSelectableTypeCached = pawnSelectableType;
            ColorTheSlider(); // Re-initialize if type changes
        }

        if (pawn.selectableType == SelectableType.Dead)
        {
            bool hideHint = ShouldHideDeadHelperHint();
            if (hideHint)
            {
                if (helperText != null) helperText.text = "";
                SetHelperTextEnabled(false);
            }
            else
            {
                float maxHealings = 0f;
                if (HandleInittingGlobalVars.globalParameters != null
                    && HandleInittingGlobalVars.globalParameters.parametersDict.ContainsKey(HandleInittingGlobalVars.AMOUNT_OF_HEALINGS_KEY))
                    maxHealings = HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.AMOUNT_OF_HEALINGS_KEY];
                var usedHealings = pawn.GetParameterValue(PawnDataController.AMOUNT_OF_HEALINGS_KEY);
                var revivesLeft = maxHealings - usedHealings;
                SetHelperTextEnabled(revivesLeft > 0.5f);
                if (helperText != null && helperText.enabled)
                {
                    const string deadHint = "[ЛКМ]";
                    if (helperTagCached != deadHint)
                    {
                        helperTagCached = deadHint;
                        helperText.text = deadHint;
                    }
                }
            }
            return;
        }
        pawnHealth = TryGetParam(PawnDataController.AVAILABLE_HEALTH_KEY);
        bool isAlive = pawnSelectableType != SelectableType.Dead && pawnHealth > 0.01f;
        if (isAlive != isAliveCached)
        {
            isAliveCached = isAlive;
            ForceLayoutRebuild();
        }

        if (pawnHealth != pawnHealthCached)
        {
            pawnHealthCached = pawnHealth;
            if (pawnSelectableType == SelectableType.Player && allyHpSlider != null)
            {
                allyHpSlider.SetValue(pawnHealth);
                allyHpSlider.gameObject.SetActive(isAlive);
            }
            else if (pawnSelectableType == SelectableType.Enemy && enemyHpSlider != null)
            {
                enemyHpSlider.SetValue(pawnHealth);
                enemyHpSlider.gameObject.SetActive(isAlive);
                if (!isAlive)
                {
                    SetHelperTextEnabled(false);
                }
            }
            else if (pawnSelectableType == SelectableType.Dead)
            {
                if (allyHpSlider != null) allyHpSlider.gameObject.SetActive(false);
                if (enemyHpSlider != null)
                {
                    enemyHpSlider.gameObject.SetActive(false);
                    helperText.enabled = false;
                }
            }
        }

        if (pawnSelectableType == SelectableType.Player)
        {
            RefreshStaminaBar();
            if (allyStaminaSlider != null)
            {
                allyStaminaSlider.gameObject.SetActive(isAlive);
            }

            UpdateActionIcons(isAlive);
        }
        else
        {
            UpdateActionIcons(isAlive);
        }

        if (helperText != null)
        {
            if (pawn.selectableType == SelectableType.Player)
            {

                if (helperTagCached != "[ЛКМ]")
                {
                    helperTagCached = "[ЛКМ]";
                    helperText.text = "[ЛКМ]";
                }
            }
            else if (helperTagCached != HelperTag)
            {
                helperTagCached = HelperTag;
                helperText.text = HelperTag;
            }
        }
    }
    void OnValidate()
    {
        if (pawn != null && pawn != pawnCached)
        {
            ColorTheSlider();
        }
    }

    void ColorTheSlider()
    {
        if (pawn == null) return;
        pawnCached = pawn;
        pawnSelectableTypeCached = pawn.selectableType;

        pawnHealth = TryGetParam(PawnDataController.AVAILABLE_HEALTH_KEY);
        pawnHealthCached = pawnHealth;
        bool isAlive = pawn.selectableType != SelectableType.Dead && pawnHealth > 0.01f;

        float maxHp = TryGetParam(PawnDataController.INITIAL_HP_KEY);

        if (pawn.selectableType == SelectableType.Player)
        {
            if (allyHpSlider != null)
            {
                allyHpSlider.gameObject.SetActive(isAlive);
                allyHpSlider.SetBounds(0f, maxHp);
                allyHpSlider.SetValue(pawnHealth);
            }
            if (enemyHpSlider != null)
            {
                enemyHpSlider.gameObject.SetActive(false);
            }
            if (allyStaminaSlider != null)
            {
                allyStaminaSlider.gameObject.SetActive(isAlive);
                float maxStamina = TryGetParam(PawnDataController.MAX_STAMINA_KEY);
                allyStaminaSlider.SetBounds(0f, maxStamina);
                pawnStamina = TryGetParam(PawnDataController.STAMINA_KEY);
                pawnStaminaCached = pawnStamina;
                allyStaminaSlider.SetValue(pawnStamina);
            }
            UpdateActionIcons(isAlive);
        }
        else if (pawn.selectableType == SelectableType.Enemy)
        {
            if (enemyHpSlider != null)
            {
                enemyHpSlider.gameObject.SetActive(isAlive);
                enemyHpSlider.SetBounds(0f, maxHp);
                enemyHpSlider.SetValue(pawnHealth);
            }
            if (allyHpSlider != null)
            {
                allyHpSlider.gameObject.SetActive(false);
            }
            if (allyStaminaSlider != null)
            {
                allyStaminaSlider.gameObject.SetActive(false);
            }
            UpdateActionIcons(false);
        }
        else
        {
            if (enemyHpSlider != null)
            {
                enemyHpSlider.gameObject.SetActive(false);
                SetHelperTextEnabled(false);
            }
            if (allyHpSlider != null) allyHpSlider.gameObject.SetActive(false);
            if (allyStaminaSlider != null) allyStaminaSlider.gameObject.SetActive(false);
            UpdateActionIcons(false);
        }
        ForceLayoutRebuild();
    }

    private float TryGetParam(string parameterName)
    {
        try
        {
            return pawn.GetParameterValue(parameterName);
        }
        catch (System.Exception)
        {
            return 1f;
        }
    }

    private void ForceLayoutRebuild()
    {
        // If this object participates in a layout group, force it to recalc after active toggles.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        if (rectTransform.parent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
    }
}
