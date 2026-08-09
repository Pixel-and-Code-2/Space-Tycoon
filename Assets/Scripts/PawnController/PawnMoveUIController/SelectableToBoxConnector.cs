using UnityEngine;
using TMPro;

public class SelectableToBoxConnector : MonoBehaviour
{
    public static string HelperTag = "[ЛКМ]";
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
            if (helperText.text != HelperTag)
            {
                helperText.text = HelperTag;
            }
        }
    }
}
