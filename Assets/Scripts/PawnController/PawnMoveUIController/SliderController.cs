using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderController : MonoBehaviour
{
    [Header("Slider links")]
    private Slider slider;
    [SerializeField]
    private Image fillImage;
    [SerializeField]
    private Image backgroundImage;
    [Tooltip("Optional red preview fill (right→left). Assign Rect/Image slot in prefab.")]
    [SerializeField]
    private Image previewFillImage;

    [Header("Slider values")]
    [SerializeField]
    private float minValue = 0f;
    [SerializeField]
    private float maxValue = 100f;
    public RectTransform rectTransform;
    [Header("Slider style")]
    [SerializeField]
    private Color fillColor = Color.white;
    [SerializeField]
    private Color backgroundColor = Color.white;
    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (fillImage == null) fillImage = GetComponentInChildren<Image>();
        if (backgroundImage == null) backgroundImage = GetComponentInChildren<Image>();
        fillImage.color = fillColor;
        backgroundImage.color = backgroundColor;
    }

    void OnValidate()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (slider == null) slider = GetComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = maxValue;
        if (fillImage == null || backgroundImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>();
            if (images.Length > 1)
            {
                fillImage = images[0];
                backgroundImage = images[1];
                fillImage.color = fillColor;
                backgroundImage.color = backgroundColor;
            }
            else
            {
                Debug.LogError("SliderController: fillImage or backgroundImage not found");
                return;
            }
        }
        else
        {
            fillImage.color = fillColor;
            backgroundImage.color = backgroundColor;
        }
    }

    private float cachedValue = 0f;
    public void SetValue(float value)
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (slider == null) return;
        if (value == cachedValue) return;
        cachedValue = value;
        slider.SetValueWithoutNotify(value);
    }

    public void ForceSetValue(float value)
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (slider == null) return;
        cachedValue = value;
        slider.SetValueWithoutNotify(value);
    }

    public float GetValue()
    {
        return cachedValue;
    }

    public void SetRange(float min, float max)
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (slider == null) return;
        minValue = min;
        maxValue = max;
        slider.minValue = min;
        slider.maxValue = max;
    }

    public void SetClass(SelectableType selectableType)
    {
        GlobalSettingsAssets.SliderClassColors sliderClassColor = HandleInittingGlobalVars.globalSettingsAssets.GetSliderClassColors(selectableType);
        if (sliderClassColor.selectableType == selectableType)
        {
            fillImage.color = HandleInittingGlobalVars.globalSettingsAssets.GetColorLink(sliderClassColor.colorFront).color;
            backgroundImage.color = HandleInittingGlobalVars.globalSettingsAssets.GetColorLink(sliderClassColor.colorBack).color;
            // Debug.Log("Setting slider class to " + fillImage.color + " and " + backgroundImage.color);
        }
        else
        {
            Debug.LogError("SliderController: selectableType " + selectableType + " not found");
        }
    }

    public void SetBounds(float minValue, float maxValue)
    {
        if (slider == null) return;
        this.minValue = minValue;
        this.maxValue = maxValue;
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        SetValue(maxValue);
    }

    public void SetPreviewSpend(float spend, float max, bool forceShow = false)
    {
        if (previewFillImage == null) return;
        if (max <= 0.001f || (!forceShow && spend <= 0.001f))
        {
            previewFillImage.gameObject.SetActive(false);
            return;
        }
        previewFillImage.gameObject.SetActive(true);
        previewFillImage.type = Image.Type.Filled;
        previewFillImage.fillMethod = Image.FillMethod.Horizontal;
        previewFillImage.fillOrigin = (int)Image.OriginHorizontal.Right;
        previewFillImage.fillAmount = Mathf.Clamp01(spend / max);
    }
}
