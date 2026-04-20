using UnityEngine;
using TMPro;

public class SelectableToBoxConnector : MonoBehaviour
{
    public static string HelperTag = "[ЛКМ]";
    private string helperTagCached = string.Empty;
    [SerializeField]
    private TextMeshProUGUI helperText;
    public ISelectable selectable;
    public RectTransform rectTransform;
    private TextMeshProUGUI textObject;
    public string text
    {
        get
        {
            return textObject.text;
        }
        set
        {
            textObject.text = value;
        }
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textObject = GetComponentInChildren<TextMeshProUGUI>();
    }
    void Update()
    {
        if (helperText != null)
        {
            if (helperTagCached != HelperTag)
            {
                helperTagCached = HelperTag;
                helperText.text = HelperTag;
            }
        }
    }
}
