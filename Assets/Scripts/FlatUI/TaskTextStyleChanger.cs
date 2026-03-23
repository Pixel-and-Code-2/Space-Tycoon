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
        if (isInProgress != -1)
        {
            textMeshProUGUI.color = isInProgress == 0 ? Color.black : Color.yellow;
        }
        if (isMainTask) return;
        if (isAvailable != -1)
        {
            textMeshProUGUI.color = isAvailable == 0 ? Color.gray : Color.black;
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
