using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClickableItem : ISelectable
{
    private enum AvailableActions
    {
        StartWork
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
    private IControlableSelectable taskExecutor = null;
    [SerializeField]
    private List<InspectorContextMenuItem> availableActions = new List<InspectorContextMenuItem>();
    [SerializeField]
    private IScriptForClickable scriptForClickable;
    private string UNIQUE_ID => "ClickableItem_" + gameObject.name;
    private IControlableSelectable prey = null;
    private Collider col = null;


    void OnValidate()
    {
        prey = GetComponent<IControlableSelectable>();
        foreach (InspectorContextMenuItem action in availableActions)
        {
            UpdateFormula(action.chanceToLaunch);
            UpdateFormula(action.progressPerRound);
        }
        if (col == null) col = GetComponent<Collider>();
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
            TurnManager.Instance.OnTriggerZoneExit -= OnTriggerZoneExit;
        }
    }

    private void OnSaveData(Action<SaveRecord[], string> addSaveData)
    {
        List<SaveRecord> saveRecords = new List<SaveRecord>
        {
            new()
            {
                recordName = "ColliderEnabled",
                recordType = SaveRecordType.boolean,
                boolValue = col.enabled
            }
        };
        if (progressBarCached != null)
        {
            saveRecords.Add(new SaveRecord()
            {
                recordName = "Progress",
                recordType = SaveRecordType.floatNumber,
                floatValue = progressBarCached.GetValue()
            });
            saveRecords.Add(new SaveRecord()
            {
                recordName = "ActionCached",
                recordType = SaveRecordType.integerNumber,
                intValue = actionCached != null ? availableActions.IndexOf(actionCached) : -1
            });
            saveRecords.Add(new SaveRecord()
            {
                recordName = "TaskExecutor",
                recordType = SaveRecordType.stringValue,
                stringValue = taskExecutor != null ? "Pawn_" + taskExecutor.gameObject.name : ""
            });
        }
        addSaveData(saveRecords.ToArray(), UNIQUE_ID);
    }
    private void OnLoadData(LoadedData data)
    {
        data.GetData("ColliderEnabled", UNIQUE_ID, true);
        col.enabled = data.GetData("ColliderEnabled", UNIQUE_ID, true);
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
            string taskExecutorName = data.GetData("TaskExecutor", UNIQUE_ID, "");
            if (taskExecutorName.StartsWith("Pawn_"))
            {
                taskExecutor = GameObject.Find(taskExecutorName.Substring(5)).GetComponent<IControlableSelectable>();
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
        if ((actionCached == null || taskExecutor == null) && progressBarCached != null)
        {
            CancelAction();
        }
        if (progressBarCached != null && HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] < 0.5f)
        {
            BoostProgressBar();
        }
    }

    void UpdateFormula(FormulaFieldWithMemo formula)
    {
        if (formula.memorySize != 2)
        {
            formula.ClearMemorizedDatasets();
            formula.AddMemorizedDataset(() => (HandleInittingGlobalVars.mainCalculatedFormulaData, "Calculated"));
            formula.AddMemorizedDataset(() => (taskExecutor == null ? HandleInittingGlobalVars.pawnMustHaveParams : taskExecutor.GetFormulaData(), "Player"));
            formula.AddMemorizedDataset(() => (prey == null || prey.GetFormulaData() == null ? HandleInittingGlobalVars.pawnMustHaveParams : prey.GetFormulaData(), "Prey"));
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
    private void StartWork()
    {
        progressBarCached = UI3DManager.Instance.RegisterSlider(transform);
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
    private IEnumerator BoostProgressBarInTime(float waitTime)
    {
        yield return null;
        if (HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] < 0.5f)
        {
            yield return new WaitForSeconds(waitTime);
            BoostProgressBar();
        }
    }

    private void BoostProgressBar()
    {
        if (progressBarCached != null)
        {
            if (prey != null)
            {
                PawnController.SetCalculatableParamsForTwoPawns(taskExecutor, prey);
            }
            else
            {
                PawnController.SetCalculatableParamsForTwoPawns(taskExecutor, transform.position);
            }
            float progress = progressBarCached.GetValue();
            float boost = actionCached.progressPerRound.EvaluateFormula();
            progress += boost;
            if (Math.Abs(boost) <= 0.001f && HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] < 0.5f)
            {
                // Debug.LogWarning("BoostProgressBar: boost is too small, boosting more");
                // LogBoostProgressFormulaDiagnostics(boost);
                progress += 5f;
            }
            StartCoroutine(BoostProgressBarInTime(1f));
            if (progress < -0.00001f)
            {
                progress = 0f;
                CancelAction();
                return;
            }
            progressBarCached.SetValue(progress);
            if (progress >= 100f)
            {
                scriptForClickable?.OnProgress(100f);
                UI3DManager.Instance.UnregisterSlider(transform);
                progressBarCached = null;
                actionCached = null;
                UI3DManager.Instance.ShowMessage("Завершено", transform.position, Color.black);
                if (gameObject.layer != LayerMask.NameToLayer("DeadPawn"))
                {
                    col.enabled = false;
                }
                taskExecutor?.OnCompleteTask();
                ClickableItemsController.Instance.OnCompleteTask(this);
                scriptForClickable?.OnComplete();
                return;
            }
            scriptForClickable?.OnProgress(progress);
        }
    }
    void LogBoostProgressFormulaDiagnostics(float boostEvaluated)
    {
        var calc = HandleInittingGlobalVars.mainCalculatedFormulaData?.parametersDict;
        var globals = HandleInittingGlobalVars.globalParameters?.GetParametersDict();
        var playerDict = taskExecutor?.GetFormulaData()?.parametersDict;
        string Fc(string k) => calc != null && calc.TryGetValue(k, out float v) ? v.ToString() : "—";
        string Fg(string k) => globals != null && globals.TryGetValue(k, out float v) ? v.ToString() : "—";
        string Fp(string k) => playerDict != null && playerDict.TryGetValue(k, out float v) ? v.ToString() : "—";
        string atkW = PawnController.ATTACKER_PREFIX + PawnDataController.WALKED_KEY;
        string atkS = PawnController.ATTACKER_PREFIX + PawnDataController.SHOOTED_AMOUNT_KEY;
        string atkM = PawnController.ATTACKER_PREFIX + PawnDataController.MELEE_AMOUNT_KEY;
        Debug.Log(
            "BoostProgressBar context [" + gameObject.name + "] " +
            "c_pawnDistance=" + Fc(PawnController.PAWN_DISTANCE_LABEL) +
            " g_IsStepByStep=" + Fg(HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY) +
            " c_AttackerWalkedDistance=" + Fc(atkW) +
            " c_AttackerShotAmount=" + Fc(atkS) +
            " c_AttackerMeleeAmount=" + Fc(atkM) +
            " p_IQ=" + Fp("IQ") +
            " prey=" + (prey != null ? prey.gameObject.name : "null") +
            " boost=" + boostEvaluated
        );
    }

    private void CancelAction()
    {
        UI3DManager.Instance.UnregisterSlider(transform);
        progressBarCached = null;
        actionCached = null;
        UI3DManager.Instance.ShowMessage("Отменено", transform.position, Color.red);
        taskExecutor = null;
        scriptForClickable?.OnCancel();
        ClickableItemsController.Instance.OnCancelTask(this);
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
                        taskExecutor = PawnController.Instance.currentSelectedPawn;
                        if (prey != null)
                        {
                            PawnController.SetCalculatableParamsForTwoPawns(taskExecutor, prey);
                        }
                        else
                        {
                            PawnController.SetCalculatableParamsForTwoPawns(taskExecutor, transform.position);
                        }
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
                                UI3DManager.Instance.ShowMessage("Не начато", transform.position, Color.red);
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