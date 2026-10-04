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

    [Header("Help slides (fullscreen; assign in inspector)")]
    public List<Sprite> helpSlidesStart = new List<Sprite>();
    public List<Sprite> helpSlidesFirstCombat = new List<Sprite>();

    [Header("Dice")]
    public bool usePhysicalDice = false;
    public bool useD10Times2InsteadOfD20 = true;
    public float diceFloorY = 0f;

    [Header("Enemy info panel")]
    [Tooltip("Title pattern. Lines starting with // are comments. Vars: ${name} ${role}.")]
    [TextArea(2, 6)]
    public string enemyInfoTitlePattern =
        "// ${name} ${role} — Shooter|Tank|Melee\n${name}";
    [TextArea(6, 16)]
    [Tooltip("Stats pattern. // comments stripped. Vars: ${name} ${role} ${hp} ${maxHp} ${ac} ${str} ${dex} ${ranged} ${melee}")]
    public string enemyInfoStatsPattern =
        "// vars: ${name} ${role} ${hp} ${maxHp} ${ac} ${str} ${dex} ${ranged} ${melee}\n"
        + "HP ${hp}/${maxHp}\nКД ${ac}\nСИЛ ${str}  ЛОВ ${dex}\nДал. ${ranged}м  Бл. ${melee}м";

    [Header("Combat pacing")]
    [Tooltip("Gap between multi-attacks (enemy AI double shot / SoM burst).")]
    public float multiAttackGapSeconds = 0.85f;
    [Tooltip("SoM: min seconds before firing target index t is (t+1) * this value.")]
    public float somShotPathDelaySeconds = 1f;

    [System.Serializable]
    public class SkillAllocationCaps
    {
        [Tooltip("-1 = no cap")]
        public int maxHp = -1;
        [Tooltip("-1 = no cap")]
        public int maxStr = -1;
        [Tooltip("-1 = no cap")]
        public int maxDex = -1;
        [Tooltip("-1 = no cap")]
        public int maxMelee = -1;
        [Tooltip("-1 = no cap")]
        public int maxRanged = -1;
    }

    [Header("Default skill caps (fallback when CombatantStats uses -1)")]
    public SkillAllocationCaps skillAllocationCaps = new SkillAllocationCaps
    {
        maxHp = 10,
        maxStr = 8,
        maxDex = 8,
        maxMelee = 8,
        maxRanged = 8
    };

    public static float GetMultiAttackGapSeconds()
    {
        if (HandleInittingGlobalVars.globalSettingsAssets != null)
            return Mathf.Max(0f, HandleInittingGlobalVars.globalSettingsAssets.multiAttackGapSeconds);
        return 0.85f;
    }

    public static float GetSomShotPathDelaySeconds()
    {
        if (HandleInittingGlobalVars.globalSettingsAssets != null)
            return Mathf.Max(0f, HandleInittingGlobalVars.globalSettingsAssets.somShotPathDelaySeconds);
        return 1f;
    }

    public static SkillAllocationCaps GetSkillAllocationCaps()
    {
        if (HandleInittingGlobalVars.globalSettingsAssets != null
            && HandleInittingGlobalVars.globalSettingsAssets.skillAllocationCaps != null)
            return HandleInittingGlobalVars.globalSettingsAssets.skillAllocationCaps;
        return new SkillAllocationCaps
        {
            maxHp = 10,
            maxStr = 8,
            maxDex = 8,
            maxMelee = 8,
            maxRanged = 8
        };
    }

    public static int GetSkillCap(string statId, CombatantStats stats = null)
    {
        int fromStats = ReadCap(stats != null ? stats.skillAllocationCaps : null, statId);
        if (fromStats >= 0) return fromStats;
        return ReadCap(GetSkillAllocationCaps(), statId);
    }

    static int ReadCap(SkillAllocationCaps caps, string statId)
    {
        if (caps == null) return -1;
        switch (statId)
        {
            case "hp": return caps.maxHp;
            case "str": return caps.maxStr;
            case "dex": return caps.maxDex;
            case "melee": return caps.maxMelee;
            case "ranged": return caps.maxRanged;
            default: return -1;
        }
    }

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