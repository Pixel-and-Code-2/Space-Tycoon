using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TaskTextStyleChanger : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI textMeshProUGUI;
    [SerializeField]
    private bool isMainTask = false;
    public string text { get { return textMeshProUGUI.text; } }
    [Header("Colors")]
    [SerializeField]
    private string defaultColor = "";
    [SerializeField]
    private string notAvailableColor = "";
    [SerializeField]
    private string inProgressColor = "";
    private void Awake()
    {
        textMeshProUGUI = GetComponent<TextMeshProUGUI>();
    }
    public void ChangeText(string newText = "", int isAvailable = -1, int isCompleted = -1, int isInProgress = -1)
    {
        if (newText != "")
        {
            textMeshProUGUI.text = newText;
        }
        if (isCompleted != -1)
        {
            if (isMainTask)
            {
                textMeshProUGUI.fontStyle = isCompleted == 1 ? FontStyles.Strikethrough : FontStyles.Bold;
            }
            else
            {
                textMeshProUGUI.fontStyle = isCompleted == 1 ? FontStyles.Strikethrough : FontStyles.Normal;
            }
        }
        if (isInProgress == 0)
        {
            textMeshProUGUI.color = HandleInittingGlobalVars.globalSettingsAssets.GetColorLink(defaultColor).color;
        }
        else if (isInProgress == 1)
        {
            textMeshProUGUI.color = HandleInittingGlobalVars.globalSettingsAssets.GetColorLink(inProgressColor).color;
        }

        if (isMainTask) return;
        if (isAvailable == 0)
        {
            textMeshProUGUI.color = HandleInittingGlobalVars.globalSettingsAssets.GetColorLink(notAvailableColor).color;
        }
        else if (isAvailable == 1)
        {
            textMeshProUGUI.color = HandleInittingGlobalVars.globalSettingsAssets.GetColorLink(defaultColor).color;
        }

    }
    void OnValidate()
    {
        textMeshProUGUI.fontStyle = isMainTask ? FontStyles.Bold : FontStyles.Normal;
    }
    public void ClearText()
    {
        textMeshProUGUI.text = "";
    }
}
