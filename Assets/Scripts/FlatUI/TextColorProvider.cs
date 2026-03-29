using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextColorProvider : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI text;
    [SerializeField]
    private string colorName;
    void Awake()
    {
        OnValidate();
    }
    void OnValidate()
    {
        if (text == null) text = GetComponent<TextMeshProUGUI>();
        if (text != null && HandleInittingGlobalVars.globalSettingsAssets != null)
            text.color = HandleInittingGlobalVars.globalSettingsAssets.GetColorLink(colorName).color;
    }
}