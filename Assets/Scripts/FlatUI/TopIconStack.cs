using UnityEngine;
using UnityEngine.UI;

public class TopIconStack : MonoBehaviour
{
    const float IconAspect = 111f / 195f;
    const float IconScale = 1.6f;
    const float SmallBoost = 1.4f;
    const float SmallWidth = 111f / 3f / IconScale * SmallBoost;
    const float BigWidth = 111f * 2f / 3f / IconScale;

    [SerializeField]
    private float width = SmallWidth;
    [SerializeField]
    private SpriteProvider bgProvider;
    [SerializeField]
    private SpriteProvider portraitProvider;
    [SerializeField]
    private SpriteProvider frameProvider;

    public float Width => width;
    public float Height => width / IconAspect;

    void Awake()
    {
        if (gameObject.name.Contains("Big"))
            width = BigWidth;
        ApplyRectSize();
        EnsureLayers();
    }

    void ApplyRectSize()
    {
        var rt = GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(width, width / IconAspect);
    }

    void EnsureLayers()
    {
        bgProvider = EnsureLayer(ref bgProvider, "bg", false);
        portraitProvider = EnsureLayer(ref portraitProvider, "portrait", true);
        frameProvider = EnsureLayer(ref frameProvider, "frame", false);
        RemoveRootImage();
    }

    SpriteProvider EnsureLayer(ref SpriteProvider provider, string layerName, bool preserveAspect)
    {
        if (provider != null) return provider;
        var existing = transform.Find(layerName);
        if (existing != null)
        {
            provider = existing.GetComponent<SpriteProvider>();
            if (provider != null) return provider;
        }

        var go = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SpriteProvider));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = preserveAspect;
        provider = go.GetComponent<SpriteProvider>();
        if (layerName == "bg") go.transform.SetSiblingIndex(0);
        if (layerName == "portrait") go.transform.SetSiblingIndex(1);
        if (layerName == "frame") go.transform.SetSiblingIndex(2);
        return provider;
    }

    void RemoveRootImage()
    {
        var rootImage = GetComponent<Image>();
        if (rootImage != null)
            rootImage.enabled = false;
        var rootProvider = GetComponent<SpriteProvider>();
        if (rootProvider != null)
            rootProvider.enabled = false;
    }

    public void Apply(TurnOrderPortrait portrait)
    {
        ApplyRectSize();
        EnsureLayers();
        if (portrait == null) return;
        bgProvider.SetLinks(portrait.BgSpriteName, "");
        portraitProvider.SetLinks(portrait.PortraitSpriteName, "");
        frameProvider.SetLinks(portrait.FrameSpriteName, "UIMainColor");
    }
}
