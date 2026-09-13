using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class HelpPage
{
    public List<GameObject> objects;
    public IconButtonStyleFiller currentPage;
}

public class HelpUI : IUILayer
{
    private int currentPage = 0;
    [SerializeField]
    private List<HelpPage> helpPages;
    [SerializeField]
    private GameObject onlyEducationObj;
    [SerializeField]
    private Image fullscreenSlideImage;
    [SerializeField]
    private GameObject textHelpRoot;
    [SerializeField]
    private GameObject slideHelpRoot;
    [SerializeField]
    private GameObject revealingObj;
    [SerializeField, Range(0f, 20f)]
    private float timeBeforeRevealingObj = 5f;

    private List<Sprite> slideSprites = new List<Sprite>();
    private bool slideMode;
    private bool forcedPending;
    private float timeOnSlide;
    private bool revealReady;
    private Button revealButton;
    private IconButtonStyleFiller revealStyle;

    void OnEnable()
    {
        if (onlyEducationObj != null)
            onlyEducationObj.SetActive(PlayerPrefs.GetInt("EducationCompleted", 0) == 0);
        if (!forcedPending)
            BeginSession();
        forcedPending = false;
    }

    void BeginSession()
    {
        timeBeforeRevealingObj = 5f;
        EnsureRevealButton();
        slideSprites = HelpSlideService.GetUnlockedUnion();
        if (slideSprites.Count == 0)
        {
            List<Sprite> start = HelpSlideService.GetSprites(HelpSlideService.SlideSet.Start);
            start.RemoveAll(s => s == null);
            if (start.Count > 0)
            {
                HelpSlideService.Unlock(HelpSlideService.SlideSet.Start);
                slideSprites = start;
            }
        }
        ApplyMode();
    }

    public void PlayForcedSlides(List<Sprite> sprites)
    {
        timeBeforeRevealingObj = 5f;
        EnsureRevealButton();
        forcedPending = true;
        slideSprites = sprites != null ? new List<Sprite>(sprites) : new List<Sprite>();
        slideSprites.RemoveAll(s => s == null);
        ApplyMode();
    }

    void EnsureRevealButton()
    {
        CleanupWrongHelpText();
        if (revealingObj == null)
        {
            Transform existing = transform.Find("ButtonIcon");
            if (existing != null)
                Destroy(existing.gameObject);

            CutScene cut = Object.FindFirstObjectByType<CutScene>(FindObjectsInactive.Include);
            GameObject src = cut != null ? cut.RevealingObj : null;
            if (src == null && cut != null)
            {
                Transform t = cut.transform.Find("ButtonIcon");
                if (t != null) src = t.gameObject;
            }
            if (src != null)
            {
                bool wasActive = src.activeSelf;
                src.SetActive(true);
                revealingObj = Instantiate(src, transform, false);
                src.SetActive(wasActive);
                revealingObj.name = "ButtonIcon";
            }
        }

        if (revealingObj == null) return;

        LayoutElement le = revealingObj.GetComponent<LayoutElement>();
        if (le == null) le = revealingObj.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        RectTransform rt = revealingObj.transform as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(30f, 30f);
            rt.localScale = Vector3.one;
        }

        SpriteProvider[] providers = revealingObj.GetComponentsInChildren<SpriteProvider>(true);
        for (int i = 0; i < providers.Length; i++)
            providers[i].Refresh();

        SyncActiveLayerVisuals(revealingObj.transform, "bg", "bgActive");
        SyncActiveLayerVisuals(revealingObj.transform, "mg", "mgActive");
        SyncActiveLayerVisuals(revealingObj.transform, "fg", "fgActive");
        EnsureLayerSprite(revealingObj.transform, "bgActive", "ControlButtonBorder", "UIMainColor");
        EnsureLayerSprite(revealingObj.transform, "mgActive", "SkipButtonFG", "");
        EnsureLayerSprite(revealingObj.transform, "fgActive", "SkipButtonHighlight", "");

        revealButton = revealingObj.GetComponent<Button>();
        if (revealButton == null)
            revealButton = revealingObj.GetComponentInChildren<Button>(true);
        if (revealButton == null)
            revealButton = revealingObj.AddComponent<Button>();
        revealButton.onClick.RemoveAllListeners();
        revealButton.onClick.AddListener(OnRevealClicked);
        revealButton.transition = Selectable.Transition.None;
        if (revealButton.targetGraphic == null)
        {
            Image any = revealingObj.GetComponentInChildren<Image>(true);
            if (any != null) revealButton.targetGraphic = any;
        }

        revealStyle = revealingObj.GetComponent<IconButtonStyleFiller>();
        if (revealStyle == null)
            revealStyle = revealingObj.GetComponentInChildren<IconButtonStyleFiller>(true);

        HideRevealButton();
    }

    static void SyncActiveLayerVisuals(Transform root, string fromName, string toName)
    {
        Transform from = root.Find(fromName);
        Transform to = root.Find(toName);
        if (from == null || to == null) return;

        Image fromImg = from.GetComponent<Image>();
        Image toImg = to.GetComponent<Image>();
        if (fromImg != null && toImg != null)
        {
            toImg.enabled = true;
            if (fromImg.sprite != null)
                toImg.sprite = fromImg.sprite;
            toImg.color = fromImg.color;
            toImg.type = fromImg.type;
            toImg.preserveAspect = fromImg.preserveAspect;
        }

        SpriteProvider fromSp = from.GetComponent<SpriteProvider>();
        SpriteProvider toSp = to.GetComponent<SpriteProvider>();
        if (fromSp != null && toSp != null)
        {
            string sprite = fromSp.GetSpriteLinkName();
            string color = fromSp.GetColorLinkName();
            if (!string.IsNullOrEmpty(sprite))
                toSp.SetLinks(sprite, color ?? "");
            else
                toSp.Refresh();
        }
    }

    static void EnsureLayerSprite(Transform root, string layerName, string spriteLink, string colorLink)
    {
        Transform t = root.Find(layerName);
        if (t == null) return;
        Image img = t.GetComponent<Image>();
        if (img != null) img.enabled = true;
        if (img != null && img.sprite != null) return;
        SpriteProvider sp = t.GetComponent<SpriteProvider>();
        if (sp == null) return;
        if (string.IsNullOrEmpty(sp.GetSpriteLinkName()))
            sp.SetLinks(spriteLink, colorLink ?? "");
        else
            sp.Refresh();
    }

    void HideRevealButton()
    {
        revealReady = false;
        timeOnSlide = 0f;
        if (revealingObj != null)
            revealingObj.SetActive(false);
        if (revealButton != null)
            revealButton.interactable = false;
    }

    void ShowRevealButton()
    {
        if (revealingObj == null) return;
        revealReady = true;
        revealingObj.SetActive(true);
        revealingObj.transform.SetAsLastSibling();
        if (revealStyle != null)
        {
            revealStyle.SetInteractable(true);
            revealStyle.TurnOnButton();
        }
        if (revealButton != null)
            revealButton.interactable = true;
    }

    void OnRevealClicked()
    {
        OnNextPage();
    }

    void CleanupWrongHelpText()
    {
        Transform wrong = transform.Find("HelpText");
        if (wrong != null)
            Destroy(wrong.gameObject);
    }

    void ApplyMode()
    {
        if (fullscreenSlideImage == null && slideHelpRoot != null)
            fullscreenSlideImage = slideHelpRoot.GetComponentInChildren<Image>(true);

        slideMode = slideSprites.Count > 0 && fullscreenSlideImage != null;

        if (slideHelpRoot != null) slideHelpRoot.SetActive(slideMode);
        if (textHelpRoot != null) textHelpRoot.SetActive(!slideMode);

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (slideHelpRoot != null && child == slideHelpRoot.transform)
            {
                child.gameObject.SetActive(slideMode);
                continue;
            }
            if (IsRevealButton(child))
            {
                child.gameObject.SetActive(revealReady);
                continue;
            }
            if (slideMode)
                child.gameObject.SetActive(false);
        }

        if (!slideMode)
        {
            if (textHelpRoot != null) textHelpRoot.SetActive(true);
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (slideHelpRoot != null && child == slideHelpRoot.transform) continue;
                if (IsRevealButton(child))
                {
                    child.gameObject.SetActive(false);
                    continue;
                }
                child.gameObject.SetActive(true);
            }
        }

        currentPage = 0;
        if (slideMode) ShowSlide();
        else if (helpPages != null && helpPages.Count > 0) UpdatePages();

        transform.SetAsLastSibling();
        if (revealingObj != null && revealReady)
            revealingObj.transform.SetAsLastSibling();
    }

    void ShowSlide()
    {
        if (fullscreenSlideImage == null || currentPage < 0 || currentPage >= slideSprites.Count) return;
        Sprite sp = slideSprites[currentPage];
        if (sp == null) return;

        if (slideHelpRoot != null)
        {
            slideHelpRoot.SetActive(true);
            slideHelpRoot.transform.SetAsLastSibling();
            var nested = slideHelpRoot.GetComponent<Canvas>();
            if (nested != null)
                Destroy(nested);
            var ray = slideHelpRoot.GetComponent<GraphicRaycaster>();
            if (ray != null)
                Destroy(ray);
        }

        fullscreenSlideImage.gameObject.SetActive(true);
        fullscreenSlideImage.enabled = true;
        fullscreenSlideImage.sprite = sp;
        fullscreenSlideImage.overrideSprite = sp;
        fullscreenSlideImage.material = null;
        fullscreenSlideImage.color = Color.white;
        fullscreenSlideImage.preserveAspect = true;
        fullscreenSlideImage.type = Image.Type.Simple;
        fullscreenSlideImage.raycastTarget = false;

        RectTransform rt = fullscreenSlideImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        if (slideHelpRoot != null)
        {
            RectTransform rootRt = slideHelpRoot.transform as RectTransform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
        }

        RectTransform helpRt = transform as RectTransform;
        helpRt.anchorMin = Vector2.zero;
        helpRt.anchorMax = Vector2.one;
        helpRt.offsetMin = Vector2.zero;
        helpRt.offsetMax = Vector2.zero;

        transform.SetAsLastSibling();
        HideRevealButton();
        Canvas.ForceUpdateCanvases();
    }

    bool IsRevealButton(Transform child)
    {
        return revealingObj != null && child == revealingObj.transform;
    }

    void Update()
    {
        if (!slideMode || revealingObj == null || revealReady) return;
        timeOnSlide += Time.unscaledDeltaTime;
        if (timeOnSlide >= timeBeforeRevealingObj)
            ShowRevealButton();
    }

    public void OnClose()
    {
        HelpSlideService.ConsumePendingUnlock();
        var stack = UILayersController.Instance.overlayStack;
        if (stack.Count > 0 && stack.Peek() == UILayersController.UILayer.Help)
            UILayersController.Instance.GoBack();

        if (stack.Count == 0
            || stack.Peek() == UILayersController.UILayer.CutScene
            || stack.Peek() == UILayersController.UILayer.Background)
        {
            UILayersController.Instance.SetLayerKeepingGameUI(UILayersController.UILayer.GameUI);
        }
    }

    public override void OnBackgroundClick()
    {
        if (slideMode) OnNextPage();
        else OnClose();
    }

    public void OnNextPage()
    {
        if (slideMode)
        {
            currentPage++;
            if (currentPage >= slideSprites.Count)
            {
                OnClose();
                return;
            }
            ShowSlide();
            return;
        }
        if (helpPages == null || helpPages.Count == 0) return;
        currentPage++;
        if (currentPage >= helpPages.Count)
            currentPage = 0;
        UpdatePages();
    }

    public void OnPreviousPage()
    {
        if (slideMode)
        {
            currentPage--;
            if (currentPage < 0) currentPage = 0;
            ShowSlide();
            return;
        }
        if (helpPages == null || helpPages.Count == 0) return;
        currentPage--;
        if (currentPage < 0)
            currentPage = helpPages.Count - 1;
        UpdatePages();
    }

    private void UpdatePages()
    {
        for (int i = 0; i < helpPages.Count; i++)
        {
            helpPages[i].objects.ForEach(obj => obj.SetActive(i == currentPage));
            if (i == currentPage)
                helpPages[i].currentPage.TurnOnButton();
            else
                helpPages[i].currentPage.TurnOffButton();
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }
}
