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
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
            TurnManager.Instance.OnEnemyTurnStart += OnEnemyTurnStart;
        }

    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnEnemyTurnStart -= OnEnemyTurnStart;
        }
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
            if (otherSelectable != null && otherSelectable.OccupiedBy != null) {
                helperText.text = "";
            }
            else {
                var maxHealings = HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.AMOUNT_OF_HEALINGS_KEY];
                var usedHealings = pawn.GetParameterValue(PawnDataController.AMOUNT_OF_HEALINGS_KEY);
                var revivesLeft = maxHealings - usedHealings;
                SetHelperTextEnabled(revivesLeft > 0.5f);
                if (helperText != null && helperText.enabled && helperTagCached != HelperTag)
                {
                    helperTagCached = HelperTag;
                    helperText.text = HelperTag;
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
            pawnStamina = calcWalk();
            if (pawnStamina != pawnStaminaCached)
            {
                pawnStaminaCached = pawnStamina;
                if (allyStaminaSlider != null)
                {
                    allyStaminaSlider.SetValue(pawnStamina);
                }
            }
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
    private float calcWalk()
    {
        bool isStepByStep = TryGetParam(HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY) > 0.5f;
        if (!isStepByStep) return 9999f;
        float avDist = TryGetParam(PawnDataController.AVAILABLE_DISTANCE_KEY);
        bool shooted = TryGetParam(PawnDataController.SHOOTED_AMOUNT_KEY) > 0.5f;
        bool melee = TryGetParam(PawnDataController.MELEE_AMOUNT_KEY) > 0.5f;
        bool movesToSkip = TryGetParam(PawnDataController.MOVES_TO_SKIP_KEY) > 0.5f;
        bool isShootWalk = TryGetParam(PawnDataController.IS_SHOOT_ON_MOVE_KEY) > 0.5f;
        float prediction = (shooted && !isShootWalk) || (melee && isShootWalk) || movesToSkip ? 0f : avDist;
        // Debug.Log("Prediction: " + pawnStamina + " AvDist: " + avDist + " shooted this round: " + shooted + " melee this round: " + melee + " moves to skip: " + movesToSkip + " isShootWalk: " + isShootWalk);
        return prediction;
    }
    private bool CanAttack()
    {
        bool skips = TryGetParam(PawnDataController.MOVES_TO_SKIP_KEY) > 0.1f;
        float shooted = TryGetParam(PawnDataController.SHOOTED_AMOUNT_KEY);
        bool melee = TryGetParam(PawnDataController.MELEE_AMOUNT_KEY) > 0.5f;
        float mag = TryGetParam(PawnDataController.MAG_AMOUNT_KEY);
        bool canAttack = !skips && ((mag > 0.5f && !melee) || (shooted < 0.5f && !melee));
        // Debug.Log("CanAttack: " + canAttack + " skips: " + skips + " shooted: " + shooted + " melee: " + melee + " mag: " + mag);
        return canAttack;
    }

    private void UpdateActionIcons(bool isAlive)
    {
        if (pawn.selectableType != SelectableType.Player)
        {
            walkIcon.SetActive(false);
            attackIcon.SetActive(false);
            reloadIcon.SetActive(false);
            return;
        }
        if (pawn == null || !isAlive)
        {
            if (walkIcon != null) walkIcon.SetActive(false);
            if (attackIcon != null) attackIcon.SetActive(false);
            if (reloadIcon != null) reloadIcon.SetActive(false);
            return;
        }

        if (walkIcon != null)
        {
            float walkAvailable = calcWalk();
            bool canWalk = walkAvailable > 0.1f;
            walkIcon.SetActive(canWalk);
        }

        if (reloadIcon != null)
        {
            bool isReloading = TryGetParam(PawnDataController.MOVES_TO_SKIP_KEY) > 0.1f;
            reloadIcon.SetActive(isReloading);
        }

        if (attackIcon != null)
        {
            bool canAttack = CanAttack();
            attackIcon.SetActive(canAttack);
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
                float maxStamina = TryGetParam(PawnDataController.INITIAL_AVAILABLE_DISTANCE_KEY);
                allyStaminaSlider.SetBounds(0f, maxStamina);
                pawnStamina = TryGetParam(PawnDataController.AVAILABLE_DISTANCE_KEY);
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
