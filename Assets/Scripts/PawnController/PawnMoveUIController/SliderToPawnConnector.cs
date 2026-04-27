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
        ColorTheSlider();
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
            var maxHealings = HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.AMOUNT_OF_HEALINGS_KEY];
            var usedHealings = pawn.GetParameterValue(PawnDataController.AMOUNT_OF_HEALINGS_KEY);
            var revivesLeft = maxHealings - usedHealings;
            helperText.enabled = revivesLeft > 0.5f;
            if (helperText.enabled && helperTagCached != "[ЛКМ]")
            {
                helperTagCached = "[ЛКМ]";
                helperText.text = "[ЛКМ]";
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
                    helperText.enabled = false;
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
            pawnStamina = TryGetParam(PawnDataController.AVAILABLE_DISTANCE_KEY);
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
            bool canWalk = TryGetParam(PawnDataController.AVAILABLE_DISTANCE_KEY) > 0.1f;
            walkIcon.SetActive(canWalk);
        }

        if (reloadIcon != null)
        {
            bool isReloading = TryGetParam(PawnDataController.MOVES_TO_SKIP_KEY) > 0.1f;
            reloadIcon.SetActive(isReloading);
        }

        if (attackIcon != null)
        {
            bool canAttack =
                TryGetParam(PawnDataController.MOVES_TO_SKIP_KEY) < 0.1f &&
                TryGetParam(PawnDataController.SHOOTED_AMOUNT_KEY) < 0.5f &&
                TryGetParam(PawnDataController.MELEE_AMOUNT_KEY) <= 1.0f &&
                TryGetParam(PawnDataController.MAG_AMOUNT_KEY) > 0.0f;
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
                helperText.enabled = false;
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
