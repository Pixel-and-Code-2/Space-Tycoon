using UnityEngine;
using System.Collections.Generic;

public class ClickableItemsController : MonoBehaviour
{
    public static ClickableItemsController Instance { get; private set; }
    public ISelectable currentSelectedItem { get; private set; }
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Debug.LogError("ClickableItemsController instance already exists");
    }
    public void OnSelect(ISelectable selectable)
    {
        if (currentSelectedItem == null)
        {
            currentSelectedItem = selectable;
            currentSelectedItem.OnSelect();
        }
        else
        {
            if (currentSelectedItem != selectable)
            {
                OnDeselect();
                currentSelectedItem = selectable;
                currentSelectedItem.OnSelect();
            }
        }
    }
    public void OnDeselect()
    {
        if (currentSelectedItem == null) return;
        currentSelectedItem.OnDeselect();
        currentSelectedItem = null;
    }
    public void OnContextMenu()
    {
        if (currentSelectedItem == null) return;
        List<ContextMenuItem> items = currentSelectedItem.OnContextMenu();
        if (items != null)
        {
            UI3DManager.Instance.ShowContextMenu(currentSelectedItem.GetTransform().position, items);
        }
    }
}