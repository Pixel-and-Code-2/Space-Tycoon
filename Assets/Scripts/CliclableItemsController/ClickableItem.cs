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

    void OnEnable()
    {
        TurnManager.Instance.OnPlayerTurnEnd += OnPlayerTurnEnd;
    }
    void OnDisable()
    {
        if (progressBarCached != null)
        {
            UI3DManager.Instance.UnregisterSlider(transform);
            progressBarCached = null;
            actionCached = null;
        }
        TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerTurnEnd;
    }
    void OnDestroy()
    {
        if (progressBarCached != null)
        {
            UI3DManager.Instance.UnregisterSlider(transform);
            progressBarCached = null;
            actionCached = null;
        }
        TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerTurnEnd;
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

    private SliderController progressBarCached = null;
    private InspectorContextMenuItem actionCached = null;
    private void StartWorkAction(InspectorContextMenuItem action)
    {
        Debug.Log("Start Work");
        actionCached = action;
        progressBarCached = UI3DManager.Instance.RegisterSlider(transform);
        if (progressBarCached == null)
        {
            Debug.LogError("StartWorkAction: progressBarCached is null");
            actionCached = null;
            progressBarCached = null;
            return;
        }
        progressBarCached.SetBounds(0f, 100f);
        progressBarCached.SetValue(0f);
        progressBarCached.SetClass(SelectableType.Neutral);
        ClickableItemsController.Instance.OnDeselect();
        UI3DManager.Instance.ShowMessage("Started", transform.position, Color.yellow);
    }

    private void OnPlayerTurnEnd()
    {
        if (progressBarCached != null)
        {
            float progress = progressBarCached.GetValue();
            progress += actionCached.progressPerRound.EvaluateFormula();
            progressBarCached.SetValue(progress);
            if (progress >= 100f)
            {
                UI3DManager.Instance.UnregisterSlider(transform);
                progressBarCached = null;
                actionCached = null;
                UI3DManager.Instance.ShowMessage("Completed", transform.position, Color.green);
            }
        }
    }

    public override List<ContextMenuItem> OnContextMenu()
    {
        if (progressBarCached != null)
        {
            return null;
        }
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
                            StartWorkAction(action);
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