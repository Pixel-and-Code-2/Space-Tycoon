using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
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
        public List<ExitCode> exitCodes;
        public FormulaFieldWithMemo progressPerRound = new FormulaFieldWithMemo();
    }

    [SerializeField]
    private List<InspectorContextMenuItem> availableActions = new List<InspectorContextMenuItem>();
    [SerializeField]
    private IScriptForClickable scriptForClickable;
    private string UNIQUE_ID => "ClickableItem_" + gameObject.name;


    void OnValidate()
    {
        foreach (InspectorContextMenuItem action in availableActions)
        {
            UpdateFormula(action.chanceToLaunch);
            UpdateFormula(action.progressPerRound);
        }
    }

    void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnEnd += OnPlayerTurnEnd;
            TurnManager.Instance.OnTriggerZoneExit += OnTriggerZoneExit;
        }
        OnValidate();
        this.gameObject.layer = LayerMask.NameToLayer("ClickableItem");
        SaveHub.Instance.OnLoad += OnLoadData;
        SaveHub.Instance.OnSave += OnSaveData;
    }
    void OnEnable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnEnd += OnPlayerTurnEnd;
        }
        OnValidate();
    }
    void OnDestroy()
    {
        if (progressBarCached != null)
        {
            UI3DManager.Instance.UnregisterSlider(transform);
            progressBarCached = null;
            actionCached = null;
        }
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerTurnEnd;
        }
    }

    private void OnSaveData(Action<SaveRecord[], string> addSaveData)
    {
        if (progressBarCached != null)
        {
            addSaveData(new SaveRecord[] {
                new SaveRecord()
                {
                    recordName = "Progress",
                    recordType = SaveRecordType.floatNumber,
                    floatValue = progressBarCached.GetValue()
                },
                new SaveRecord()
                {
                    recordName = "ActionCached",
                    recordType = SaveRecordType.integerNumber,
                    intValue = actionCached != null ? availableActions.IndexOf(actionCached) : -1
                }
            }, UNIQUE_ID);
        }
    }
    private void OnLoadData(LoadedData data)
    {
        float progress = data.GetData("Progress", UNIQUE_ID, -1f);
        if (progress != -1f)
        {
            if (progressBarCached != null)
            {
                progressBarCached.SetValue(progress);
            }
            else
            {
                StartWork();
                progressBarCached.SetValue(progress);
            }
            int actionIndex = data.GetData("ActionCached", UNIQUE_ID, -1);
            if (actionIndex != -1)
            {
                actionCached = availableActions[actionIndex];
            }
            else
            {
                actionCached = null;
            }
        }
        else
        {
            if (progressBarCached != null)
            {
                UI3DManager.Instance.UnregisterSlider(transform);
                progressBarCached = null;
                actionCached = null;
            }
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
        scriptForClickable?.OnSelect();
    }

    private SliderController progressBarCached = null;
    private InspectorContextMenuItem actionCached = null;
    public override bool IsWorking() => progressBarCached != null;
    private IControlableSelectable panwProgressing = null;
    private void StartWork()
    {
        progressBarCached = UI3DManager.Instance.RegisterSlider(transform);
        panwProgressing = PawnController.Instance.currentSelectedPawn;
        // if (progressBarCached == null)
        // {
        //     Debug.LogError("StartWorkAction: progressBarCached is null");
        //     actionCached = null;
        //     progressBarCached = null;
        //     return;
        // }
        TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerTurnEnd;
        TurnManager.Instance.OnPlayerTurnEnd += OnPlayerTurnEnd;
        progressBarCached.SetBounds(0f, 100f);
        progressBarCached.SetValue(0f);
        progressBarCached.SetClass(SelectableType.Neutral);
        scriptForClickable?.OnStart();
        ClickableItemsController.Instance.OnStartTask(this);
    }
    private void StartWorkAction(InspectorContextMenuItem action)
    {
        actionCached = action;
        StartWork();
        ClickableItemsController.Instance.OnDeselect();
        BoostProgressBar();
        // UI3DManager.Instance.ShowMessage("Started", transform.position, Color.yellow);
        scriptForClickable?.OnDeselect();
    }

    private void OnPlayerTurnEnd()
    {
        BoostProgressBar();
    }
    private void OnTriggerZoneExit()
    {
        BoostProgressBar();
    }

    private void BoostProgressBar()
    {
        if (progressBarCached != null)
        {
            float progress = progressBarCached.GetValue();
            progress += actionCached.progressPerRound.EvaluateFormula();
            if (progress < 0f)
            {
                progress = 0f;
                UI3DManager.Instance.UnregisterSlider(transform);
                progressBarCached = null;
                actionCached = null;
                UI3DManager.Instance.ShowMessage("Cancelled", transform.position, Color.green);
                panwProgressing = null;
                scriptForClickable?.OnCancel();
                return;
            }
            progressBarCached.SetValue(progress);
            if (progress >= 100f)
            {
                scriptForClickable?.OnProgress(100f);
                UI3DManager.Instance.UnregisterSlider(transform);
                progressBarCached = null;
                actionCached = null;
                UI3DManager.Instance.ShowMessage("Completed", transform.position, Color.green);
                panwProgressing?.OnCompleteTask();
                ClickableItemsController.Instance.OnCompleteTask(this);
                scriptForClickable?.OnComplete();
                return;
            }
            scriptForClickable?.OnProgress(progress);
        }
    }

    public override List<ContextMenuItem> OnContextMenu()
    {
        if (IsWorking())
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
                        float chance = action.chanceToLaunch.EvaluateFormula();
                        foreach (ExitCode exitCode in action.exitCodes)
                        {
                            if (exitCode.IsEqual(chance))
                            {
                                UI3DManager.Instance.ShowMessage(exitCode.message, transform.position, exitCode.color);
                            }
                        }
                        if (chance >= UnityEngine.Random.Range(0f, 1f))
                        {
                            StartWorkAction(action);
                        }
                        else
                        {
                            if (chance <= 1f && chance >= 0f)
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