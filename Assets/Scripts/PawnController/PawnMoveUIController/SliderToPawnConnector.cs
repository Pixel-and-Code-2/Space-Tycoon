using UnityEngine;

[RequireComponent(typeof(SliderController))]
[RequireComponent(typeof(RectTransform))]
public class SliderToPawnConnector : MonoBehaviour
{
    [SerializeField]
    private SliderController sliderController;
    public RectTransform rectTransform;

    public PawnDataController pawn;
    private PawnDataController pawnCached;
    void Awake()
    {
        if (sliderController == null) sliderController = GetComponent<SliderController>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

    }

    void Start()
    {
        if (pawn == null) Debug.LogWarning("SliderToPawnConnector: pawn not found");
        ColorTheSlider();
    }

    private float pawnHealthCached;
    private float pawnHealth;
    private SelectableType pawnSelectableTypeCached;
    private SelectableType pawnSelectableType;
    void Update()
    {
        pawnSelectableType = pawn.selectableType;
        if (pawnSelectableType != pawnSelectableTypeCached)
        {
            pawnSelectableTypeCached = pawnSelectableType;
            sliderController.SetClass(pawnSelectableType);
        }

        pawnHealth = TryGetParam(PawnDataController.AVAILABLE_HEALTH_KEY);
        if (pawnHealth != pawnHealthCached)
        {
            pawnHealthCached = pawnHealth;
            sliderController.SetValue(pawnHealth);
        }

    }
    void OnValidate()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (sliderController == null) sliderController = GetComponent<SliderController>();
        if (pawn != null && pawn != pawnCached)
        {
            ColorTheSlider();
        }
    }

    void ColorTheSlider()
    {
        pawnCached = pawn;
        pawnHealth = TryGetParam(PawnDataController.AVAILABLE_HEALTH_KEY);
        sliderController.SetBounds(0f, TryGetParam(PawnDataController.INITIAL_HP_KEY));
        sliderController.SetValue(pawnHealth);
        pawnHealthCached = pawnHealth;
        sliderController.SetClass(pawn.selectableType);
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
}
