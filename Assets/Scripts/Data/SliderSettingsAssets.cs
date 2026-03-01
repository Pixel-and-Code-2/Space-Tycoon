using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SliderSettingsAssets", menuName = "SliderSettingsAssets", order = 1)]
public class SliderSettingsAssets : ScriptableObject
{
    [System.Serializable]
    public struct SliderClassColors { public SelectableType selectableType; public Color colorFront; public Color colorBack; }
    [Header("Slider colors")]
    [SerializeField]
    private List<SliderClassColors> sliderClassColors;

    public SliderClassColors GetSliderClassColors(SelectableType selectableType)
    {
        return sliderClassColors.Find(x => x.selectableType == selectableType);
    }
}