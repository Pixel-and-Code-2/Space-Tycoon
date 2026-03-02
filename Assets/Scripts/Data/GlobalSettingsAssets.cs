using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GlobalSettings", menuName = "GlobalSettingsAssets", order = 1)]
public class GlobalSettingsAssets : ScriptableObject
{
    [System.Serializable]
    public struct SliderClassColors { public SelectableType selectableType; public Color colorFront; public Color colorBack; }
    [Header("Slider class colors")]
    [SerializeField]
    private List<SliderClassColors> sliderClassColors;

    [Header("Pawn status colors")]
    public Color selectedColorAlly = Color.yellow;
    public Color selectedColorEnemy = Color.orange;
    public Color deadColor = Color.gray;
    public Color allyColor = Color.green;
    public Color enemyColor = Color.red;

    public SliderClassColors GetSliderClassColors(SelectableType selectableType)
    {
        return sliderClassColors.Find(x => x.selectableType == selectableType);
    }
}