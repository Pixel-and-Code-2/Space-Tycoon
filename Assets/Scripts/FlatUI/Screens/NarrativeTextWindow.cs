using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class NarrativeTextWindow : IUILayer
{
    [System.Serializable]
    public class SelectionOffsets
    {
        public Vector2 noneSelected = new Vector2(320f, 120f);
        public Vector2 char0Selected = new Vector2(320f, 180f);
        public Vector2 char1Selected = new Vector2(320f, 220f);
        public Vector2 char2Selected = new Vector2(320f, 260f);
    }

    [System.Serializable]
    private class QueueItem
    {
        public string text;
        public int authorIndex;
    }

    [SerializeField]
    private List<QueueItem> queueItems = new List<QueueItem>();
    public override bool isQueueable => true;
    [SerializeField]
    private TextMeshProUGUI textMeshProUGUI;
    [SerializeField]
    private InputActionReference returnToGameButton;
    private RectTransform parentRect;
    public override bool isStoppingGame => true;
    [SerializeField]
    private float duration = 1f;
    [Header("Position vs background bottom-left (author x selection)")]
    [SerializeField]
    private RectTransform backgroundRect;
    [SerializeField]
    private SelectionOffsets[] positionsByAuthor = new SelectionOffsets[3]
    {
        new SelectionOffsets(),
        new SelectionOffsets(),
        new SelectionOffsets()
    };

#if UNITY_EDITOR
    SelectionOffsets[] previewCache;
#endif

    private void Awake()
    {
        parentRect = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        returnToGameButton.action.Enable();
        gameObject.SetActive(true);
        timeElapsed = 0f;
    }

    void OnDisable()
    {
        gameObject.SetActive(false);
        if (ClickableItemsController.Instance != null)
            ClickableItemsController.Instance.ClearNarrativeVoiceQueue();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureAuthorSlots();
        if (previewCache == null || previewCache.Length != 3)
        {
            previewCache = CloneOffsets(positionsByAuthor);
            return;
        }
        for (int a = 0; a < 3; a++)
        {
            SelectionOffsets cur = positionsByAuthor[a];
            SelectionOffsets prev = previewCache[a];
            if (cur == null || prev == null) continue;
            if (cur.noneSelected != prev.noneSelected) { ApplyOffset(GetOffset(a, -1)); break; }
            if (cur.char0Selected != prev.char0Selected) { ApplyOffset(GetOffset(a, 0)); break; }
            if (cur.char1Selected != prev.char1Selected) { ApplyOffset(GetOffset(a, 1)); break; }
            if (cur.char2Selected != prev.char2Selected) { ApplyOffset(GetOffset(a, 2)); break; }
        }
        previewCache = CloneOffsets(positionsByAuthor);
    }

    static SelectionOffsets[] CloneOffsets(SelectionOffsets[] src)
    {
        var dst = new SelectionOffsets[3];
        for (int i = 0; i < 3; i++)
        {
            SelectionOffsets s = src != null && i < src.Length ? src[i] : null;
            dst[i] = s == null ? new SelectionOffsets() : new SelectionOffsets
            {
                noneSelected = s.noneSelected,
                char0Selected = s.char0Selected,
                char1Selected = s.char1Selected,
                char2Selected = s.char2Selected
            };
        }
        return dst;
    }
#endif

    void EnsureAuthorSlots()
    {
        if (positionsByAuthor == null || positionsByAuthor.Length != 3)
        {
            var next = new SelectionOffsets[3];
            for (int i = 0; i < 3; i++)
                next[i] = (positionsByAuthor != null && i < positionsByAuthor.Length && positionsByAuthor[i] != null)
                    ? positionsByAuthor[i]
                    : new SelectionOffsets();
            positionsByAuthor = next;
        }
        for (int i = 0; i < 3; i++)
            if (positionsByAuthor[i] == null)
                positionsByAuthor[i] = new SelectionOffsets();
    }

    private float timeElapsed = 0f;
    void Update()
    {
        if (returnToGameButton.action.triggered)
        {
            UILayersController.Instance.GoBack();
        }
        timeElapsed += Time.unscaledDeltaTime;
        if (timeElapsed >= duration)
        {
            if (queueItems.Count > 0)
            {
                SetText(queueItems[0].text, queueItems[0].authorIndex);
                queueItems.RemoveAt(0);
                timeElapsed = 0f;
            }
            else
                UILayersController.Instance.GoBack();
        }
    }

    public override void OnBackgroundClick()
    {
        if (queueItems.Count > 0) {
            SetText(queueItems[0].text, queueItems[0].authorIndex);
            queueItems.RemoveAt(0);
            timeElapsed = 0f;
        } else {
            UILayersController.Instance.GoBack();
        }
    }

    public override void Initialize(string config)
    {
        (string text, int author) = ParseConfig(config);
        SetText(text, author);
        timeElapsed = 0f;
    }

    private void SetText(string text, int authorIndex)
    {
        if (textMeshProUGUI != null)
            textMeshProUGUI.text = text;
        int sel = -1;
        if (GameUI.Instance != null)
            sel = GameUI.Instance.GetSelectedPlayerIndex();
        ApplyOffset(GetOffset(authorIndex, sel));
        if (parentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        string who = authorIndex >= 0 ? ("author=" + authorIndex) : "author=?";
        Debug.Log("[NarrativeUI] show line near " + who + " text='" + (text != null && text.Length > 48 ? text.Substring(0, 48) + "..." : text) + "'");
        if (ClickableItemsController.Instance != null)
            ClickableItemsController.Instance.PlayNextNarrativeVoice();
    }

    Vector2 GetOffset(int authorIndex, int selectedIndex)
    {
        EnsureAuthorSlots();
        int a = Mathf.Clamp(authorIndex, 0, 2);
        SelectionOffsets set = positionsByAuthor[a];
        if (selectedIndex == 0) return set.char0Selected;
        if (selectedIndex == 1) return set.char1Selected;
        if (selectedIndex == 2) return set.char2Selected;
        return set.noneSelected;
    }

    void ApplyOffset(Vector2 offset)
    {
        var rt = (RectTransform)transform;
        if (backgroundRect == null)
        {
            rt.anchoredPosition = offset;
            return;
        }
        RectTransform parent = rt.parent as RectTransform;
        if (parent == null)
        {
            rt.anchoredPosition = offset;
            return;
        }
        Vector3 worldBl = backgroundRect.TransformPoint(new Vector3(backgroundRect.rect.xMin, backgroundRect.rect.yMin, 0f));
        Vector3 localBl = parent.InverseTransformPoint(worldBl);
        rt.anchoredPosition = (Vector2)localBl + offset;
    }

    public override void Queue(string config)
    {
        (string text, int author) = ParseConfig(config);
        queueItems.Add(new QueueItem { text = text, authorIndex = author });
    }

    private (string, int) ParseConfig(string config)
    {
        if (string.IsNullOrEmpty(config)) return ("", 0);
        int last = config.LastIndexOf('_');
        int parsedNumber = 0;
        string text = config;
        if (last >= 0
            && int.TryParse(config.Substring(last + 1), out parsedNumber)
            && parsedNumber >= -1 && parsedNumber <= 2)
        {
            text = config.Substring(0, last);
            if (parsedNumber < 0) parsedNumber = 0;
        }
        else parsedNumber = 0;
        return (text, parsedNumber);
    }
}
