using UnityEngine;
using System.Collections.Generic;
using UnityEngine.U2D;

[CreateAssetMenu(fileName = "GlobalSettings", menuName = "GlobalSettingsAssets", order = 1)]
public class GlobalSettingsAssets : ScriptableObject
{
    [System.Serializable]
    public class ButtonStyle
    {
        public string name;
        public string highlightAddition;
        [Header("Backgrounds")]
        public string bgOn;
        public string bgOff;
        public string bgHighlight;
        public string bgPressed;
        [Header("Middlegrounds")]
        public string mgOn;
        public string mgOff;
        public string mgHighlight;
        public string mgPressed;
        [Header("Foregrounds")]
        public string fgOn;
        public string fgOff;
        public string fgHighlight;
        public string fgPressed;
        [Header("Colors")]
        public string colorOn;
        public string colorOff;
        public string colorHighlight;
        public string colorPressed;
        public bool isTextOnly = true;
    }
    [System.Serializable]
    public class SpriteLink { public string name; public Sprite sprite; }
    [System.Serializable]
    public class ColorLink { public string name; public Color color; }
    [System.Serializable]
    public struct SliderClassColors { public SelectableType selectableType; public string colorFront; public string colorBack; }
    [Header("Stamina (global action costs, scale 0–100)")]
    public StaminaCostSettings staminaCosts = new StaminaCostSettings();

    public enum BoostStat
    {
        Strength,
        Dexterity,
        ArmorClass,
        MaxHp
    }

    public enum BoostMode
    {
        Flat,
        Percent
    }

    [System.Serializable]
    public struct BoostEntry
    {
        public BoostStat stat;
        public BoostMode mode;
        public float value;
    }

    [System.Serializable]
    public class BoostPoolSettings
    {
        public List<BoostEntry> afterCombat = new List<BoostEntry>();
        public List<BoostEntry> afterKill = new List<BoostEntry>();
        public List<BoostEntry> afterTask = new List<BoostEntry>();
    }

    [Header("Stat boosts (empty pool = no grant / no UI)")]
    public BoostPoolSettings boostPools = new BoostPoolSettings();

    [System.Serializable]
    public class StaminaCostSettings
    {
        public float maxStamina = 100f;
        public float rangedAttackCost = 50f;
        public float meleeAttackCost = 60f;
        public float shooterMeleeAttackCost = 50f;
        public float reviveCost = 10f;
    }

    public static StaminaCostSettings GetStaminaCosts()
    {
        if (HandleInittingGlobalVars.globalSettingsAssets != null)
            return HandleInittingGlobalVars.globalSettingsAssets.staminaCosts;
        return new StaminaCostSettings();
    }

    public static BoostPoolSettings GetBoostPools()
    {
        if (HandleInittingGlobalVars.globalSettingsAssets != null
            && HandleInittingGlobalVars.globalSettingsAssets.boostPools != null)
            return HandleInittingGlobalVars.globalSettingsAssets.boostPools;
        return new BoostPoolSettings();
    }

    [Header("Slider class colors")]
    [SerializeField]
    private List<SliderClassColors> sliderClassColors;

    [Header("Pawn status colors")]
    public string selectedColorAlly;
    public string selectedColorEnemy;
    public string deadColor;
    public string allyColor;
    public string enemyColor;
    [SerializeField]
    private List<ColorLink> colorLinks;
    public SliderClassColors GetSliderClassColors(SelectableType selectableType)
    {
        return sliderClassColors.Find(x => x.selectableType == selectableType);
    }
    public ColorLink GetColorLink(string name)
    {
        var colorLink = colorLinks.Find(x => x.name == name);
        if (colorLink == null) return new ColorLink { name = "Default", color = Color.white };
        return colorLink;
    }
}