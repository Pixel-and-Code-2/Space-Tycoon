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
    public GameObject questionObj;
    public GameObject attentionObj;
    public string text
    {
        get
        {
            return questionObj.activeSelf ? "?" : "!";
        }
        set
        {
            if (value == "?")
            {
                questionObj.SetActive(true);
                attentionObj.SetActive(false);
            }
            else if (value == "!")
            {
                questionObj.SetActive(false);
                attentionObj.SetActive(true);
            }
        }
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
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
