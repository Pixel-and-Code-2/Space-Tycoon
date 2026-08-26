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
        public float chanceToLaunch = 1f;
        public List<TaskExitCode> exitCodes;
        public float progressPerRound = 10f;
    }
    public IControlableSelectable taskExecutor { get; private set; } = null;
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
        ChangeScenarioStatus(ClickableItemsController.TaskItem.TaskItemStatus.Unavailable);
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
        InvalidateWork();
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
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnLoad -= OnLoadData;
            SaveHub.Instance.OnSave -= OnSaveData;
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
        InvalidateWork();
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
                taskExecutor?.SetOnTask(false);
                taskExecutor = null;
                activeTaskInfo = ClickableTaskInfo.None;
            }
        }
        if ((actionCached == null || taskExecutor == null) && progressBarCached != null)
        {
            CancelAction();
        }
    }

    void StopBoostRoutine()
    {
        if (boostRoutine != null)
        {
            StopCoroutine(boostRoutine);
            boostRoutine = null;
        }
    }

    void InvalidateWork()
    {
        workEpoch++;
        StopBoostRoutine();
    }

    public override void OnSelect()
    {
        ClickableItemsController.Instance.OnContextMenu();
        StartCoroutine(OnSelectDelayed());
    }
    private IEnumerator OnSelectDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        ApplyTaskInfoToScript();
        scriptForClickable?.OnSelect();
    }

    private SliderController progressBarCached = null;
    private InspectorContextMenuItem actionCached = null;
    private ClickableTaskInfo activeTaskInfo = ClickableTaskInfo.None;
    private Coroutine boostRoutine;
    private int workEpoch;

    private void ApplyTaskInfoToScript()
    {
        if (scriptForClickable == null) return;
        ClickableTaskInfo info = activeTaskInfo.isTask
            ? activeTaskInfo
            : ClickableItemsController.Instance.GetTaskInfo(this);
        scriptForClickable.ApplyTaskInfo(info);
    }
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
        activeTaskInfo = ClickableItemsController.Instance.GetTaskInfo(this);
        ApplyTaskInfoToScript();
        scriptForClickable?.OnStart();
        ClickableItemsController.Instance.OnStartTask(this);
        if (ClickableItemsController.Instance.IsOnlyShowTextTask(this))
            progressBarCached.SetValue(100f);
    }
    private void StartWorkAction(InspectorContextMenuItem action)
    {
        actionCached = action;
        StartWork();
        ClickableItemsController.Instance.OnDeselect();
        BoostProgressBar();
        scriptForClickable?.OnDeselect();
    }

    bool TryGetStartBlockReason(IControlableSelectable executor, out string reason)
    {
        reason = null;
        if (executor == null)
        {
            reason = "Нет исполнителя";
            return true;
        }
        if (executor.GetSelectableType() == SelectableType.Dead)
        {
            reason = "Исполнитель недоступен";
            return true;
        }
        float dist = Vector3.Distance(executor.GetTransform().position, transform.position);
        if (dist > 4f)
        {
            reason = "Слишком далеко";
            return true;
        }
        return false;
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
        if (SaveHub.Instance != null && SaveHub.Instance.IsLoading) yield break;
        if (HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] < 0.5f)
        {
            yield return new WaitForSeconds(waitTime);
            if (SaveHub.Instance != null && SaveHub.Instance.IsLoading) yield break;
            BoostProgressBar();
        }
        boostRoutine = null;
    }

    private void BoostProgressBar()
    {
        if (SaveHub.Instance != null && SaveHub.Instance.IsLoading) return;
        if (progressBarCached != null)
        {
            if (taskExecutor == null || taskExecutor.GetSelectableType() == SelectableType.Dead)
            {
                CancelAction("Исполнитель недоступен");
                return;
            }
            float dist = Vector3.Distance(taskExecutor.GetTransform().position, transform.position);
            if (dist > 4f)
            {
                CancelAction("Слишком далеко");
                return;
            }
            float progress = progressBarCached.GetValue();
            float boost = actionCached != null ? actionCached.progressPerRound : 10f;
            progress += boost;
            StopBoostRoutine();
            boostRoutine = StartCoroutine(BoostProgressBarInTime(1f));
            if (progress < -0.00001f)
            {
                progress = 0f;
                CancelAction("Сбой");
                return;
            }
            progressBarCached.SetValue(progress);
            if (progress >= 100f)
            {
                ApplyTaskInfoToScript();
                progress = scriptForClickable?.OnProgress(100f) ?? 100f;
            }
            if (progress >= 100f)
            {
                StopBoostRoutine();
                UI3DManager.Instance.UnregisterSlider(transform);
                progressBarCached = null;
                actionCached = null;
                if (gameObject.layer != LayerMask.NameToLayer("DeadPawn"))
                {
                    col.enabled = false;
                }
                taskExecutor?.OnCompleteTask();
                ClickableItemsController.Instance.OnCompleteTask(this);
                int epoch = workEpoch;
                StartCoroutine(OnCompleteDelayed(epoch));
                return;
            }
            ApplyTaskInfoToScript();
            progressBarCached.SetValue(scriptForClickable?.OnProgress(progress) ?? progress);
        }
    }
    private IEnumerator OnCompleteDelayed(int epoch)
    {
        yield return new WaitForSeconds(0.1f);
        if (epoch != workEpoch) yield break;
        if (SaveHub.Instance != null && SaveHub.Instance.IsLoading) yield break;
        ApplyTaskInfoToScript();
        scriptForClickable?.OnComplete();
        activeTaskInfo = ClickableTaskInfo.None;
    }
    private void CancelAction(string reason = null)
    {
        InvalidateWork();
        UI3DManager.Instance.UnregisterSlider(transform);
        progressBarCached = null;
        actionCached = null;
        if (!string.IsNullOrEmpty(reason))
            UI3DManager.Instance.ShowMessage(reason, transform.position, Color.red);
        taskExecutor?.SetOnTask(false);
        taskExecutor = null;
        ApplyTaskInfoToScript();
        scriptForClickable?.OnCancel();
        ClickableItemsController.Instance.OnCancelTask(this);
        activeTaskInfo = ClickableTaskInfo.None;
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
            InspectorContextMenuItem captured = action;
            Action actionDelegate = null;
            switch (captured.action)
            {
                case AvailableActions.StartWork:
                    actionDelegate = () => TryLaunchStartWork(captured);
                    break;
            }
            items.Add(new ContextMenuItem { text = action.text, action = actionDelegate });
        }
        return items;
    }

    public void TryStartWorkImmediately()
    {
        if (IsWorking() || availableActions == null || availableActions.Count == 0) return;
        for (int i = 0; i < availableActions.Count; i++)
        {
            if (availableActions[i].action == AvailableActions.StartWork)
            {
                TryLaunchStartWork(availableActions[i]);
                return;
            }
        }
        TryLaunchStartWork(availableActions[0]);
    }

    void TryLaunchStartWork(InspectorContextMenuItem action)
    {
        UI3DManager.Instance.HideContextMenu();
        taskExecutor = PawnController.Instance.currentSelectedPawn;
        if (taskExecutor == null)
        {
            ClickableItemsController.Instance.OnDeselect();
            return;
        }
        if (prey != null)
            PawnController.SetCalculatableParamsForTwoPawns(taskExecutor, prey);
        else
            PawnController.SetCalculatableParamsForTwoPawns(taskExecutor, transform.position);
        float chance = action.chanceToLaunch;
        if (action.exitCodes != null)
        {
            foreach (TaskExitCode exitCode in action.exitCodes)
            {
                if (exitCode.IsEqual(chance))
                    UI3DManager.Instance.ShowMessage(exitCode.message, transform.position, exitCode.color);
            }
        }
        if (chance >= UnityEngine.Random.Range(0f, 1f))
        {
            PawnBrain deadBrain = GetComponent<PawnBrain>();
            if (deadBrain != null
                && deadBrain.GetSelectableType() == SelectableType.Dead
                && GetComponent<PawnHealing>() != null
                && !ClickableItemsController.Instance.IsOnlyShowTextTask(this))
            {
                if (!PawnHealing.CanRevive(deadBrain))
                {
                    UI3DManager.Instance.ShowMessage("Нет подъёмов", transform.position, Color.red);
                    ClickableItemsController.Instance.OnDeselect();
                    return;
                }
                if (!PawnHealing.TryPayRevive(taskExecutor.PawnData))
                {
                    UI3DManager.Instance.ShowMessage("Нет стамины", transform.position, Color.red);
                    ClickableItemsController.Instance.OnDeselect();
                    return;
                }
            }
            if (TryGetStartBlockReason(taskExecutor, out string blockReason))
            {
                UI3DManager.Instance.ShowMessage(blockReason, transform.position, Color.red);
                ClickableItemsController.Instance.OnDeselect();
                return;
            }
            taskExecutor.SetOnTask(true);
            StartWorkAction(action);
        }
        else
        {
            if (chance <= 1f && chance >= 0f)
                UI3DManager.Instance.ShowMessage("Не начато", transform.position, Color.red);
        }
        ClickableItemsController.Instance.OnDeselect();
    }

    public override Transform GetTransform() => transform;
    public override SelectableType GetSelectableType() => SelectableType.Neutral;

    public override void ChangeScenarioStatus(ClickableItemsController.TaskItem.TaskItemStatus status)
    {
        if (col == null) col = GetComponent<Collider>();
        switch (status)
        {
            case ClickableItemsController.TaskItem.TaskItemStatus.ReadyToStart:
                col.enabled = true;
                break;
            case ClickableItemsController.TaskItem.TaskItemStatus.InProgress:
                break;
            case ClickableItemsController.TaskItem.TaskItemStatus.Done:
            case ClickableItemsController.TaskItem.TaskItemStatus.Unavailable:
                if (((1 << gameObject.layer) & LayerMask.GetMask("ClickableItem", "Default")) != 0
                    && !ClickableItemsController.Instance.HasReadyOrProgressTask(this))
                {
                    col.enabled = false;
                }
                break;
        }
    }
}