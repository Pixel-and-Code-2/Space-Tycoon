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

    [System.Serializable]
    public struct SliderClassColors { public SelectableType selectableType; public Color colorFront; public Color colorBack; }
    [Header("Slider colors")]
    [SerializeField]
    private List<SliderClassColors> sliderClassColors = new List<SliderClassColors>();
    private static Dictionary<SelectableType, SliderClassColors> sliderClassColorsDict = new Dictionary<SelectableType, SliderClassColors>();

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
    }

    void OnValidate()
    {
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
        foreach (SliderClassColors sliderClassColor in sliderClassColors)
        {
            sliderClassColorsDict[sliderClassColor.selectableType] = sliderClassColor;
        }
        foreach (var kv in sliderClassColorsDict)
        {
            if (!sliderClassColors.Exists(x => x.selectableType == kv.Key))
            {
                sliderClassColors.Add(kv.Value);
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

    public void SetClass(SelectableType selectableType)
    {
        if (sliderClassColorsDict.TryGetValue(selectableType, out SliderClassColors sliderClassColor))
        {
            fillImage.color = sliderClassColor.colorFront;
            backgroundImage.color = sliderClassColor.colorBack;
            return;
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
        slider.value = maxValue;
    }
}
