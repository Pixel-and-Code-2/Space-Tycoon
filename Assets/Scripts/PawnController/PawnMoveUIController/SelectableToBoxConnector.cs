using UnityEngine;
using TMPro;

public class SelectableToBoxConnector : MonoBehaviour
{
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
}
