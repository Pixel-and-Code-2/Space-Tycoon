using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GlobalSettings", menuName = "GlobalSettingsAssets", order = 1)]
public class GlobalSettingsAssets : ScriptableObject
{
    [System.Serializable]
    public class IconButtonStyle { public string name; public Sprite spriteOff; public Sprite spriteOn; public Color colorOff; public Color colorOn; }
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
    [SerializeField]
    private List<IconButtonStyle> iconButtonStyles;

    public SliderClassColors GetSliderClassColors(SelectableType selectableType)
    {
        return sliderClassColors.Find(x => x.selectableType == selectableType);
    }
    public IconButtonStyle GetIconButtonStyle(string name)
    {
        return iconButtonStyles.Find(x => x.name == name);
    }
}