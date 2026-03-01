using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

class ButtonCache
{
    public GameObject button;
    public Button buttonComponent;
    public TextMeshProUGUI textComponent;
}

public struct ContextMenuItem
{
    public string text;
    public System.Action action;
}

public class ContextMenuController : MonoBehaviour
{
    [SerializeField]
    private GameObject buttonPrefab;
    public Vector3 attachToPosition;
    private List<ButtonCache> buttons = new List<ButtonCache>();
    private int lastUnused = 0;

    public void UpdateAttach(Canvas canvas)
    {
        RectTransform uiElementRect = GetComponent<RectTransform>();
        Vector3 screenPosition = canvas.worldCamera.WorldToScreenPoint(attachToPosition);
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            new Vector2(screenPosition.x, screenPosition.y),
            canvas.worldCamera,
            out localPoint))
        {
            // uiElementRect.localPosition = localPoint;
            uiElementRect.localPosition = new Vector3(localPoint.x, localPoint.y, 0.1f);
        }
    }
    public void AddButton(string text, System.Action action)
    {
        if (lastUnused < buttons.Count)
        {
            buttons[lastUnused].button.SetActive(true);
            buttons[lastUnused].textComponent.text = text;
            buttons[lastUnused].buttonComponent.onClick.AddListener(() => action());
            lastUnused++;
            return;
        }
        lastUnused = buttons.Count;
        GameObject button = Instantiate(buttonPrefab, transform);
        ButtonCache newButton = new ButtonCache();
        newButton.button = button;
        newButton.buttonComponent = button.GetComponent<Button>();
        newButton.textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
        newButton.textComponent.text = text;
        newButton.buttonComponent.onClick.AddListener(() => action());
        buttons.Add(newButton);
    }

    public void AddButtons(List<ContextMenuItem> items)
    {
        foreach (ContextMenuItem item in items)
        {
            AddButton(item.text, item.action);
        }
    }

    public void ClearButtons()
    {
        lastUnused = 0;
        foreach (ButtonCache button in buttons)
        {
            button.button.SetActive(false);
            button.buttonComponent.onClick.RemoveAllListeners();
        }
    }

    void OnDestroy()
    {
        foreach (ButtonCache button in buttons)
        {
            button.buttonComponent.onClick.RemoveAllListeners();
        }
    }
}