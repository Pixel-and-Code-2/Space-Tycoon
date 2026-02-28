using System;
using System.Collections.Generic;
using UnityEngine;

public class ClickableItem : ISelectable
{
    private enum AvailableActions
    {
        StartWork,
        MoveHere
    }
    [System.Serializable]
    private class InspectorContextMenuItem
    {
        public string text;
        public AvailableActions action;
    }

    [SerializeField]
    private bool requirePawn = true;
    [SerializeField]
    private List<InspectorContextMenuItem> availableActions = new List<InspectorContextMenuItem>();

    public override void OnSelect()
    {
        if (requirePawn && PawnController.Instance.currentSelectedPawn == null) return;
        ClickableItemsController.Instance.OnContextMenu();
    }

    public override List<ContextMenuItem> OnContextMenu()
    {
        List<ContextMenuItem> items = new List<ContextMenuItem>();
        foreach (InspectorContextMenuItem action in availableActions)
        {
            Action actionDelegate = null;
            switch (action.action)
            {
                case AvailableActions.StartWork:
                    actionDelegate = () =>
                    {
                        Debug.Log("Start Work");
                        ClickableItemsController.Instance.OnDeselect();
                    };
                    break;
                case AvailableActions.MoveHere:
                    actionDelegate = () =>
                    {
                        Debug.Log("Move Here");
                        ClickableItemsController.Instance.OnDeselect();
                    };
                    break;
            }
            items.Add(new ContextMenuItem { text = action.text, action = actionDelegate });
        }
        return items;
    }

    public override Transform GetTransform() => transform;
    public override SelectableType GetSelectableType() => SelectableType.Neutral;
}