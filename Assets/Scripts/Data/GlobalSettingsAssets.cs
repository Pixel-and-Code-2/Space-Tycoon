using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GlobalSettings", menuName = "GlobalSettingsAssets", order = 1)]
public class GlobalSettingsAssets : ScriptableObject
{
    [System.Serializable]
    public class IconButtonStyle
    {
        public string name;
        [Header("Backgrounds")]
        public string bgOn;
        public string bgOff;
        public string bgHighlightAddition;
        public string bgPressedAddition;
        [Header("Middlegrounds")]
        public string mgOn;
        public string mgOff;
        public string mgHighlightAddition;
        public string mgPressedAddition;
        [Header("Foregrounds")]
        public string fgOn;
        public string fgOff;
        public string fgHighlightAddition;
        public string fgPressedAddition;
    }
    [System.Serializable]
    public class SpriteLink { public string name; public Sprite sprite; }
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
    [SerializeField]
    private List<SpriteLink> spriteLinks;

    public SliderClassColors GetSliderClassColors(SelectableType selectableType)
    {
        return sliderClassColors.Find(x => x.selectableType == selectableType);
    }
    public IconButtonStyle GetIconButtonStyle(string name)
    {
        return iconButtonStyles.Find(x => x.name == name);
    }
    public SpriteLink GetSpriteLink(string name)
    {
        return spriteLinks.Find(x => x.name == name);
    }
}