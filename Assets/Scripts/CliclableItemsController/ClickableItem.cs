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
        public FormulaFieldWithMemo chanceToLaunch = new FormulaFieldWithMemo();
        public FormulaFieldWithMemo progressPerRound = new FormulaFieldWithMemo();
    }

    [SerializeField]
    private List<InspectorContextMenuItem> availableActions = new List<InspectorContextMenuItem>();

    void OnValidate()
    {
        foreach (InspectorContextMenuItem action in availableActions)
        {
            UpdateFormula(action.chanceToLaunch);
            UpdateFormula(action.progressPerRound);
        }
    }

    void UpdateFormula(FormulaFieldWithMemo formula)
    {
        if (formula.memorySize != 2)
        {
            formula.ClearMemorizedDatasets();
            formula.AddMemorizedDataset(() => (HandleInittingGlobalVars.mainCalculatedFormulaData, "Calculated"));
            formula.AddMemorizedDataset(() => (PawnController.Instance.currentSelectedPawn == null ? HandleInittingGlobalVars.pawnMustHaveParams : PawnController.Instance.currentSelectedPawn.GetFormulaData(), "Player"));
        }
        formula.OnParamsUpdated();
    }

    public override void OnSelect()
    {
        ClickableItemsController.Instance.OnContextMenu();
    }

    private void StartWorkAction()
    {
        Debug.Log("Start Work");
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
                        if (action.chanceToLaunch.EvaluateFormula() >= UnityEngine.Random.Range(0f, 1f))
                        {
                            StartWorkAction();
                        }
                        else
                        {
                            UI3DManager.Instance.ShowMessage("Not started", transform.position, Color.red);
                        }
                        ClickableItemsController.Instance.OnDeselect();
                    };
                    break;
                case AvailableActions.MoveHere:
                    actionDelegate = () =>
                    {
                        if (action.chanceToLaunch.EvaluateFormula() >= UnityEngine.Random.Range(0f, 1f))
                        {
                            // Debug.Log("Move Here");
                        }
                        else
                        {
                            UI3DManager.Instance.ShowMessage("Not moved", transform.position, Color.red);
                        }
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