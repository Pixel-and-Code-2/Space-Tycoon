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

    [Header("Slider values")]
    [SerializeField]
    private float minValue = 0f;
    [SerializeField]
    private float maxValue = 100f;
    public RectTransform rectTransform;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
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
            }
            else
            {
                Debug.LogError("SliderController: fillImage or backgroundImage not found");
                return;
            }
        }

    }

    private float cachedValue = 0f;
    public void SetValue(float value)
    {
        if (slider == null) return;
        if (value == cachedValue) return;
        cachedValue = value;
        slider.value = value;
    }

    public float GetValue()
    {
        return cachedValue;
    }

    public void SetClass(SelectableType selectableType)
    {
        SliderSettingsAssets.SliderClassColors sliderClassColor = HandleInittingGlobalVars.sliderSettingsAssets.GetSliderClassColors(selectableType);
        if (sliderClassColor.selectableType == selectableType)
        {
            fillImage.color = sliderClassColor.colorFront;
            backgroundImage.color = sliderClassColor.colorBack;
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
}
