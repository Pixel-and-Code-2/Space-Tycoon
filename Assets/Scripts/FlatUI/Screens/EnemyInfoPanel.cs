using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyInfoPanel : MonoBehaviour
{
    public static EnemyInfoPanel Instance { get; private set; }

    [Header("Slots (place Rects in GameUI, wire here)")]
    [SerializeField]
    private GameObject rootSlot;
    [SerializeField]
    private TextMeshProUGUI titleText;
    [SerializeField]
    private TextMeshProUGUI statsText;

    PawnDataController shown;

    void Awake()
    {
        Instance = this;
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void ShowEnemy(PawnDataController data)
    {
        if (Instance == null) return;
        Instance.Show(data);
    }

    public static void HideEnemy()
    {
        if (Instance == null) return;
        Instance.Hide();
    }

    public void Show(PawnDataController data)
    {
        if (rootSlot == null || data == null)
        {
            Hide();
            return;
        }
        if (data.selectableType != SelectableType.Enemy && data.selectableType != SelectableType.Neutral)
        {
            Hide();
            return;
        }
        shown = data;
        rootSlot.SetActive(true);

        string titlePattern = "${name}";
        string statsPattern =
            "HP ${hp}/${maxHp}\nКД ${ac}\nСИЛ ${str}  ЛОВ ${dex}\nДал. ${ranged}м  Бл. ${melee}м";
        if (HandleInittingGlobalVars.globalSettingsAssets != null)
        {
            GlobalSettingsAssets gs = HandleInittingGlobalVars.globalSettingsAssets;
            if (!string.IsNullOrEmpty(gs.enemyInfoTitlePattern))
                titlePattern = gs.enemyInfoTitlePattern;
            if (!string.IsNullOrEmpty(gs.enemyInfoStatsPattern))
                statsPattern = gs.enemyInfoStatsPattern;
        }

        if (titleText != null)
            titleText.text = FormatPattern(titlePattern, data);
        if (statsText != null)
            statsText.text = FormatPattern(statsPattern, data);
        RebuildRoot();
    }

    public void Hide()
    {
        shown = null;
        if (rootSlot != null)
            rootSlot.SetActive(false);
    }

    void RebuildRoot()
    {
        if (rootSlot == null) return;
        RectTransform rt = rootSlot.transform as RectTransform;
        if (rt == null) return;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    static string FormatPattern(string pattern, PawnDataController data)
    {
        if (string.IsNullOrEmpty(pattern) || data == null) return string.Empty;
        string s = StripCommentLines(pattern.Replace("\\n", "\n"));
        StringBuilder sb = new StringBuilder(s.Length + 32);
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '$' && i + 1 < s.Length && s[i + 1] == '{')
            {
                int end = s.IndexOf('}', i + 2);
                if (end > i + 2)
                {
                    string key = s.Substring(i + 2, end - (i + 2));
                    sb.Append(ResolveVar(key, data));
                    i = end;
                    continue;
                }
            }
            sb.Append(s[i]);
        }
        return sb.ToString();
    }

    static string StripCommentLines(string pattern)
    {
        string[] lines = pattern.Split('\n');
        StringBuilder sb = new StringBuilder(pattern.Length);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            int j = 0;
            while (j < line.Length && (line[j] == ' ' || line[j] == '\t')) j++;
            if (j + 1 < line.Length && line[j] == '/' && line[j + 1] == '/')
                continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(line);
        }
        return sb.ToString();
    }

    static string ResolveVar(string key, PawnDataController data)
    {
        switch (key)
        {
            case "name":
            case "role":
                return data.GetUiName();
            case "hp":
                return data.CurrentHp.ToString("0");
            case "maxHp":
                return data.MaxHp.ToString("0");
            case "ac":
                return data.ArmorClass.ToString("0");
            case "str":
                return data.Strength.ToString("0");
            case "dex":
                return data.Dexterity.ToString("0");
            case "ranged":
                return data.AttackRange.ToString("0.#");
            case "melee":
                return data.MeleeReach.ToString("0.#");
            default:
                return "${" + key + "}";
        }
    }
}
