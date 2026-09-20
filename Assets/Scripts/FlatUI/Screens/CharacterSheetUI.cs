using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class CharacterSheetUI : IUILayer
{
    [Serializable]
    public class StatRow
    {
        public string id;
        public TextMeshProUGUI label;
        public TextMeshProUGUI valueText;
        public Button minusButton;
        public Button plusButton;
        public bool editable = true;
        public bool rangedOnly;
    }

    [Serializable]
    public class CharacterPage
    {
        public IControlableSelectable pawn;
        public string displayName;
        public IconButtonStyleFiller pageIndicator;
    }

    [Header("Shared layout (one source)")]
    [SerializeField]
    GameObject leftRoot;
    [SerializeField]
    GameObject rightRoot;
    [SerializeField]
    TextMeshProUGUI levelText;
    [SerializeField]
    TextMeshProUGUI nameText;
    [SerializeField]
    Image portrait;
    [SerializeField]
    TextMeshProUGUI pointsText;
    [SerializeField]
    List<StatRow> rows = new List<StatRow>();

    [Header("Pages = pawn + indicator only")]
    [SerializeField]
    List<CharacterPage> pages = new List<CharacterPage>();
    [SerializeField]
    Button prevButton;
    [SerializeField]
    Button nextButton;
    [SerializeField]
    Button closeButton;

    int currentPage;
    int[] draftHp;
    int[] draftStr;
    int[] draftDex;
    int[] draftMelee;
    int[] draftRanged;
    int[] draftPoints;
    bool dirty;

    public override bool isStoppingGame => true;
    public override bool isBackgroundVisible => true;

    void OnEnable()
    {
        if (leftRoot != null) leftRoot.SetActive(true);
        if (rightRoot != null) rightRoot.SetActive(true);
        BeginSession();
        WirePageIndicators();
        ShowPage(currentPage);
    }

    void RebuildLayoutNow()
    {
        Canvas.ForceUpdateCanvases();
        if (rightRoot != null)
        {
            RectTransform rightRt = rightRoot.transform as RectTransform;
            if (rightRt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rightRt);
        }
        Transform body = transform.Find("Body");
        if (body != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(body as RectTransform);
        Transform buttonRow = transform.Find("ButtonRow");
        if (buttonRow != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRow as RectTransform);
        RectTransform root = transform as RectTransform;
        if (root != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        Canvas.ForceUpdateCanvases();
    }

    void WirePageIndicators()
    {
        for (int i = 0; i < pages.Count; i++)
        {
            CharacterPage p = pages[i];
            if (p.pageIndicator == null) continue;
            Button b = p.pageIndicator.GetComponent<Button>();
            if (b == null) continue;
            int idx = i;
            b.onClick = new Button.ButtonClickedEvent();
            b.onClick.AddListener(() => TryChangePage(idx));
        }
        if (prevButton != null)
        {
            prevButton.onClick = new Button.ButtonClickedEvent();
            prevButton.onClick.AddListener(OnPreviousPage);
        }
        if (nextButton != null)
        {
            nextButton.onClick = new Button.ButtonClickedEvent();
            nextButton.onClick.AddListener(OnNextPage);
        }
        if (closeButton != null)
        {
            closeButton.onClick = new Button.ButtonClickedEvent();
            closeButton.onClick.AddListener(OnClose);
        }
    }

    public void OpenAt(int pageIndex)
    {
        currentPage = Mathf.Clamp(pageIndex, 0, Mathf.Max(0, pages.Count - 1));
        if (UILayersController.Instance == null) return;
        // Always base on GameUI so we never open on top of MainMenu.
        UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.CharacterSheet);
    }

    void BeginSession()
    {
        int n = pages.Count;
        draftHp = new int[n];
        draftStr = new int[n];
        draftDex = new int[n];
        draftMelee = new int[n];
        draftRanged = new int[n];
        draftPoints = new int[n];
        for (int i = 0; i < n; i++)
        {
            PawnDataController data = GetData(i);
            if (data == null) continue;
            draftHp[i] = data.SkillHp;
            draftStr[i] = data.SkillStr;
            draftDex[i] = data.SkillDex;
            draftMelee[i] = data.SkillMeleeBonus;
            draftRanged[i] = data.SkillRangedBonus;
            draftPoints[i] = data.UnspentSkillPoints;
        }
        dirty = false;
    }

    void CommitAll()
    {
        for (int i = 0; i < pages.Count; i++)
        {
            PawnDataController data = GetData(i);
            if (data == null) continue;
            data.ApplySkillAllocations(draftHp[i], draftStr[i], draftDex[i], draftMelee[i], draftRanged[i]);
            data.SetUnspentSkillPoints(draftPoints[i]);
        }
        dirty = false;
        if (PartyProgress.Instance != null)
            PartyProgress.Instance.NotifyProgressChanged();
    }

    void Adjust(int page, string id, int delta)
    {
        PawnDataController data = GetData(page);
        if (data == null) return;
        int value;
        int committed;
        switch (id)
        {
            case "hp": value = draftHp[page]; committed = data.SkillHp; break;
            case "str": value = draftStr[page]; committed = data.SkillStr; break;
            case "dex": value = draftDex[page]; committed = data.SkillDex; break;
            case "melee": value = draftMelee[page]; committed = data.SkillMeleeBonus; break;
            case "ranged": value = draftRanged[page]; committed = data.SkillRangedBonus; break;
            default: return;
        }
        if (delta > 0)
        {
            if (draftPoints[page] <= 0) return;
            value++;
            draftPoints[page]--;
            dirty = true;
        }
        else if (delta < 0)
        {
            if (value <= committed) return;
            value--;
            draftPoints[page]++;
            dirty = true;
        }
        switch (id)
        {
            case "hp": draftHp[page] = value; break;
            case "str": draftStr[page] = value; break;
            case "dex": draftDex[page] = value; break;
            case "melee": draftMelee[page] = value; break;
            case "ranged": draftRanged[page] = value; break;
        }
        RefreshPage(page);
    }

    PawnDataController GetData(int page)
    {
        if (page < 0 || page >= pages.Count || pages[page].pawn == null) return null;
        return pages[page].pawn.GetComponent<PawnDataController>();
    }

    public void OnNextPage()
    {
        TryChangePage(currentPage + 1);
    }

    public void OnPreviousPage()
    {
        TryChangePage(currentPage - 1);
    }

    public void OnClose()
    {
        TryLeave(() =>
        {
            CommitAll();
            UILayersController.Instance.GoBack();
        });
    }

    public override void OnBackgroundClick()
    {
        OnClose();
    }

    void TryChangePage(int target)
    {
        if (pages.Count == 0) return;
        int next = (target % pages.Count + pages.Count) % pages.Count;
        if (next == currentPage) return;
        if (!dirty)
        {
            currentPage = next;
            ShowPage(currentPage);
            return;
        }
        ConfirmDialog.Show("Вы уверены?", () =>
        {
            CommitAll();
            BeginSession();
            currentPage = next;
            ShowPage(currentPage);
        }, () => { });
    }

    void TryLeave(Action leave)
    {
        if (!dirty)
        {
            leave?.Invoke();
            return;
        }
        ConfirmDialog.Show("Вы уверены?", () =>
        {
            CommitAll();
            leave?.Invoke();
        }, () => { });
    }

    void ShowPage(int index)
    {
        currentPage = index;
        if (leftRoot != null) leftRoot.SetActive(true);
        if (rightRoot != null) rightRoot.SetActive(true);
        for (int i = 0; i < pages.Count; i++)
        {
            CharacterPage p = pages[i];
            bool on = i == index;
            if (p.pageIndicator != null)
            {
                if (on) p.pageIndicator.TurnOnButton();
                else p.pageIndicator.TurnOffButton();
            }
        }
        RefreshPage(index);
        RebuildLayoutNow();
    }

    void RefreshPage(int index)
    {
        if (index < 0 || index >= pages.Count) return;
        CharacterPage page = pages[index];
        PawnDataController data = GetData(index);
        int level = PartyProgress.Instance != null ? PartyProgress.Instance.Level : 1;
        if (levelText != null)
            levelText.text = "Уровень " + level;
        if (nameText != null)
        {
            if (!string.IsNullOrEmpty(page.displayName))
                nameText.text = page.displayName;
            else if (page.pawn != null)
                nameText.text = page.pawn.name;
        }
        if (pointsText != null)
            pointsText.text = "Доступные очки: " + (draftPoints != null && index < draftPoints.Length ? draftPoints[index] : 0);

        bool hasRanged = data != null && data.HasRanged;
        for (int r = 0; r < rows.Count; r++)
        {
            StatRow row = rows[r];
            if (row.rangedOnly)
            {
                bool show = hasRanged;
                if (row.label != null) row.label.gameObject.SetActive(show);
                if (row.valueText != null) row.valueText.gameObject.SetActive(show);
                if (row.minusButton != null) row.minusButton.gameObject.SetActive(show && row.editable);
                if (row.plusButton != null) row.plusButton.gameObject.SetActive(show && row.editable);
                if (!show) continue;
            }
            if (row.valueText != null)
                row.valueText.text = FormatStat(index, data, row.id);
            WireRow(index, row);
        }
    }

    string FormatStat(int page, PawnDataController data, string id)
    {
        if (data == null) return "-";
        switch (id)
        {
            case "hp": return (data.BaseMaxHp + draftHp[page]).ToString("0");
            case "str": return (data.BaseStrength + draftStr[page]).ToString("0");
            case "dex": return (data.BaseDexterity + draftDex[page]).ToString("0");
            case "melee": return FormatDice(data.BaseMeleeDamageExpr, draftMelee[page]);
            case "ranged": return FormatDice(data.BaseRangedDamageExpr, draftRanged[page]);
            case "move": return data.MovePerTurn.ToString("0");
            case "range": return data.AttackRange.ToString("0");
            case "armor": return data.ArmorClass.ToString("0");
            default: return "-";
        }
    }

    static string FormatDice(string expr, int bonus)
    {
        if (string.IsNullOrWhiteSpace(expr)) expr = "1d6";
        string core = expr.Trim();
        int flat = 0;
        int plus = core.LastIndexOf('+');
        int minus = core.LastIndexOf('-');
        int cut = -1;
        if (plus > 0) cut = plus;
        else if (minus > 0) cut = minus;
        string dice = core;
        if (cut > 0)
        {
            dice = core.Substring(0, cut).Trim();
            int.TryParse(core.Substring(cut), out flat);
        }
        int total = flat + bonus;
        if (total == 0) return dice;
        if (total > 0) return dice + " + " + total;
        return dice + " " + total;
    }

    void WireRow(int page, StatRow row)
    {
        if (!row.editable) return;
        if (row.minusButton != null)
        {
            row.minusButton.onClick.RemoveAllListeners();
            string id = row.id;
            int p = page;
            row.minusButton.onClick.AddListener(() => Adjust(p, id, -1));
        }
        if (row.plusButton != null)
        {
            row.plusButton.onClick.RemoveAllListeners();
            string id = row.id;
            int p = page;
            row.plusButton.onClick.AddListener(() => Adjust(p, id, +1));
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            OnClose();
    }
}
