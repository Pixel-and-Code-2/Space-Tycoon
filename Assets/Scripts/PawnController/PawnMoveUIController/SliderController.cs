using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Slider))]

public class SliderController : MonoBehaviour
{
    [Header("Slider links")]
    private Slider slider;
    [SerializeField]
    private Image fillImage;
    [SerializeField]
    private Image backgroundImage;
    [Header("Text value")]
    [SerializeField]
    private bool showText = true;
    [SerializeField]
    private TextMeshProUGUI textValue = null;
    [SerializeField]
    private Color textValueColorRaised = Color.green;
    [SerializeField]
    private Color textValueColorLowered = Color.red;
    [SerializeField]
    private float textShowTime = 1f;
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
        if (textValue != null) textValue.enabled = false;
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

    private float timeTextShown = 0f;
    void Update()
    {
        if (textValue != null && textValue.enabled)
        {
            timeTextShown += Time.deltaTime;
            if (timeTextShown >= textShowTime)
            {
                textValue.enabled = false;
                timeTextShown = 0f;
            }
        }
    }

    private float cachedValue = 0f;
    public void SetValue(float value)
    {
        if (slider == null) return;
        SetText(value, cachedValue < value);
        if (value == cachedValue) return;
        cachedValue = value;
        slider.value = value;
    }

    public void SetText(float value, bool raised = false)
    {
        if (textValue == null || !showText) return;
        textValue.enabled = true;
        if (raised)
        {
            textValue.color = textValueColorRaised;
            textValue.text = "+" + value.ToString("F1");
        }
        else
        {
            textValue.color = textValueColorLowered;
            textValue.text = "-" + value.ToString("F1");
        }
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
