using UnityEngine;
using System.Collections.Generic;

public class ClickableItemsController : MonoBehaviour
{
    [System.Serializable]
    public class TaskItem
    {
        public enum TaskItemStatus
        {
            Unavailable = 0,
            ReadyToStart = 1,
            InProgress = 2,
            Done = 3,
        }
        public enum TextShowTime
        {
            BeforeContextMenu,
            BeforeStart,
            AfterComplete
        }
        [System.Serializable]
        public class TextToShow
        {
            [System.Serializable]
            public class CompleteSound
            {
                public AudioClip clip;
                public int authorIndex;
            }
            public TextShowTime showTime;
            public string text;
            [HideInInspector]
            public bool shown = false;
            public bool showOnce = true;
            public bool showOnlyOnStepByStep = false;
            public List<CompleteSound> completeSounds = new List<CompleteSound>();
        }
        [System.Serializable]
        public class TaskCondition
        {
            public int itemIndex = -1;
            public TaskItemStatus requiredStatus = TaskItemStatus.Done;
            public bool isMain = true;
            public bool atLeast;
        }
        public ISelectable selectable;
        // [HideInInspector]
        public TaskItemStatus status = TaskItemStatus.Unavailable;
        public List<TaskCondition> readyWhen = new List<TaskCondition>();
        public List<TaskCondition> doneWhen = new List<TaskCondition>();
        public List<TextToShow> textToShow = new List<TextToShow>();
        public string shortLevelName = string.Empty;
        public string completeText = string.Empty;
        public Color completeTextColor = Color.yellow;
    }
    public static ClickableItemsController Instance { get; private set; }
    public System.Action OnTaskUpdated;
    public ISelectable currentSelectedItem { get; private set; }
    [SerializeField]
    public List<TaskItem> mainTaskScenario;
    [SerializeField]
    public List<TaskItem> sideTaskScenario;
    private int currentTaskScenarioIndex = 0;
    private const string UNIQUE_ID = "ClickableItemsController";
    [SerializeField]
    private GameObject[] authorsObjects;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Debug.LogError("ClickableItemsController instance already exists");
    }

    void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
            TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerTurnEnd;
            TurnManager.Instance.OnPlayerTurnEnd += OnPlayerTurnEnd;
        }
        SaveHub.Instance.OnLoad += OnLoadData;
        SaveHub.Instance.OnSave += OnSaveData;
        UILayersController.Instance.OnGameResumed += OnGameResumed;
    }

    void OnEnable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnPlayerTurnStart += OnPlayerTurnStart;
            TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerTurnEnd;
            TurnManager.Instance.OnPlayerTurnEnd += OnPlayerTurnEnd;
        }
    }
    void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerTurnEnd;
        }
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= OnPlayerTurnStart;
            TurnManager.Instance.OnPlayerTurnEnd -= OnPlayerTurnEnd;
        }
        if (UILayersController.Instance != null)
            UILayersController.Instance.OnGameResumed -= OnGameResumed;
        if (SaveHub.Instance != null)
        {
            SaveHub.Instance.OnLoad -= OnLoadData;
            SaveHub.Instance.OnSave -= OnSaveData;
        }
    }
    void Update()
    {
        bool updated = false;
        foreach (TaskItem item in mainTaskScenario)
        {
            if (item.selectable.IsWorking() && item.status != TaskItem.TaskItemStatus.InProgress)
            {
                item.status = TaskItem.TaskItemStatus.InProgress;
                item.selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.InProgress);
                UI3DManager.Instance.UnregisterSelectable(item.selectable);
                CheckActionBox();
                updated = true;
            }
        }
        foreach (TaskItem item in sideTaskScenario)
        {
            if (item.selectable.IsWorking() && item.status != TaskItem.TaskItemStatus.InProgress)
            {
                item.status = TaskItem.TaskItemStatus.InProgress;
                item.selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.InProgress);
                UI3DManager.Instance.UnregisterSelectable(item.selectable);
                CheckActionBox();
                updated = true;
            }
        }
        if (updated) OnTaskUpdated?.Invoke();
    }
    private void OnSaveData(System.Action<SaveRecord[], string> addSaveData)
    {
        List<SaveRecord> records = new List<SaveRecord>();
        for (int i = 0; i < mainTaskScenario.Count; i++)
        {
            records.Add(new SaveRecord()
            {
                recordName = "MainTaskScenario_" + i,
                recordType = SaveRecordType.integerNumber,
                intValue = (int)mainTaskScenario[i].status
            });
            for (int j = 0; j < mainTaskScenario[i].textToShow.Count; j++)
            {
                records.Add(new SaveRecord()
                {
                    recordName = "MainTaskScenarioTextShown_" + i + "_" + j,
                    recordType = SaveRecordType.boolean,
                    boolValue = mainTaskScenario[i].textToShow[j].shown
                });
            }
        }


        for (int i = 0; i < sideTaskScenario.Count; i++)
        {
            records.Add(new SaveRecord()
            {
                recordName = "SideTaskScenario_" + i,
                recordType = SaveRecordType.integerNumber,
                intValue = (int)sideTaskScenario[i].status
            });
            for (int j = 0; j < sideTaskScenario[i].textToShow.Count; j++)
            {
                records.Add(new SaveRecord()
                {
                    recordName = "SideTaskScenarioTextShown_" + i + "_" + j,
                    recordType = SaveRecordType.boolean,
                    boolValue = sideTaskScenario[i].textToShow[j].shown
                });
            }
        }
        addSaveData(records.ToArray(), UNIQUE_ID);
    }
    private void OnLoadData(LoadedData data)
    {
        for (int i = 0; i < mainTaskScenario.Count; i++)
        {
            mainTaskScenario[i].status = (TaskItem.TaskItemStatus)data.GetData("MainTaskScenario_" + i, UNIQUE_ID, (int)TaskItem.TaskItemStatus.Unavailable);
            for (int j = 0; j < mainTaskScenario[i].textToShow.Count; j++)
            {
                mainTaskScenario[i].textToShow[j].shown = data.GetData("MainTaskScenarioTextShown_" + i + "_" + j, UNIQUE_ID, false);
            }
        }
        for (int i = 0; i < sideTaskScenario.Count; i++)
        {
            sideTaskScenario[i].status = (TaskItem.TaskItemStatus)data.GetData("SideTaskScenario_" + i, UNIQUE_ID, (int)TaskItem.TaskItemStatus.Unavailable);
            for (int j = 0; j < sideTaskScenario[i].textToShow.Count; j++)
            {
                sideTaskScenario[i].textToShow[j].shown = data.GetData("SideTaskScenarioTextShown_" + i + "_" + j, UNIQUE_ID, false);
            }
        }
        CheckActionBox();
        OnTaskUpdated?.Invoke();
    }
    private bool CheckActionBoxInternal(List<TaskItem> scenario, string label)
    {
        bool updated = false;
        for (int i = 0; i < scenario.Count; i++)
        {
            if (scenario[i].status == TaskItem.TaskItemStatus.Unavailable)
            {
                if (CheckReadyWhen(scenario[i]))
                {
                    scenario[i].status = TaskItem.TaskItemStatus.ReadyToStart;
                    scenario[i].selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.ReadyToStart);
                    updated = true;
                }
            }
            if (scenario[i].status != TaskItem.TaskItemStatus.Done
                && scenario[i].status != TaskItem.TaskItemStatus.Unavailable
                && scenario[i].doneWhen.Count > 0
                && CheckDoneWhen(scenario[i]))
            {
                scenario[i].status = TaskItem.TaskItemStatus.Done;
                scenario[i].selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.Done);
                UI3DManager.Instance.UnregisterSelectable(scenario[i].selectable);
                updated = true;
            }
            if (scenario[i].selectable.IsWorking())
            {
                if (scenario[i].status != TaskItem.TaskItemStatus.InProgress)
                {
                    scenario[i].status = TaskItem.TaskItemStatus.InProgress;
                    updated = true;
                }
            }
            if (scenario[i].status != TaskItem.TaskItemStatus.ReadyToStart)
            {
                UI3DManager.Instance.UnregisterSelectable(scenario[i].selectable);
            }
            else
            {
                UI3DManager.Instance.RegisterSelectable(scenario[i].selectable, label);
            }
        }
        return updated;
    }
    private void CheckActionBox()
    {
        bool anyUpdated = false;
        bool updated;
        do
        {
            updated = false;
            updated |= CheckActionBoxInternal(mainTaskScenario, "!");
            updated |= CheckActionBoxInternal(sideTaskScenario, "?");
            anyUpdated |= updated;
        } while (updated);
        if (anyUpdated) OnTaskUpdated?.Invoke();
    }

    private static bool StatusMatchesCondition(TaskItem.TaskItemStatus actual, TaskItem.TaskCondition cond)
    {
        int a = (int)actual;
        int r = (int)cond.requiredStatus;
        return cond.atLeast ? a >= r : a == r;
    }

    private bool CheckReadyWhen(TaskItem item)
    {
        if (item.readyWhen.Count == 0) return true;
        foreach (var cond in item.readyWhen)
        {
            var scenario = cond.isMain ? mainTaskScenario : sideTaskScenario;
            if (!StatusMatchesCondition(scenario[cond.itemIndex].status, cond))
                return false;
        }
        return true;
    }
    private bool CheckDoneWhen(TaskItem item)
    {
        foreach (var cond in item.doneWhen)
        {
            var scenario = cond.isMain ? mainTaskScenario : sideTaskScenario;
            if (StatusMatchesCondition(scenario[cond.itemIndex].status, cond))
                return true;
        }
        return false;
    }
    public bool OnSelect(ISelectable selectable)
    {
        bool selecting = false;
        foreach (TaskItem item in mainTaskScenario)
        {
            if (item.selectable == selectable && item.status == TaskItem.TaskItemStatus.ReadyToStart)
            {
                selecting = true;
                break;
            }
        }
        foreach (TaskItem item in sideTaskScenario)
        {
            if (item.selectable == selectable && item.status == TaskItem.TaskItemStatus.ReadyToStart)
            {
                selecting = true;
                break;
            }
        }

        ClickableItem clickableItem = selectable.GetClickableItem();
        if (clickableItem != null && clickableItem.gameObject.layer != LayerMask.NameToLayer("ClickableItem"))
        {
            selecting = true;
        }
        if (selecting)
        {
            UI3DManager.Instance.UnregisterSelectable(selectable);
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
        return selecting;
    }
    public void OnDeselect()
    {
        if (currentSelectedItem == null) return;
        currentSelectedItem.OnDeselect();
        CheckActionBox();
        currentSelectedItem = null;
    }
    public void OnContextMenu()
    {
        if (currentSelectedItem == null)
        {
            return;
        }
        bool blocked = CheckScenarioForText(mainTaskScenario, TaskItem.TextShowTime.BeforeContextMenu);
        if (blocked) return;
        blocked = CheckScenarioForText(sideTaskScenario, TaskItem.TextShowTime.BeforeContextMenu);
        if (blocked) return;
        List<ContextMenuItem> items = currentSelectedItem.OnContextMenu();
        if (items != null)
        {
            UI3DManager.Instance.ShowContextMenu(currentSelectedItem.GetTransform().position, items);
        }
        UI3DManager.Instance.UnregisterSelectable(currentSelectedItem);
    }
    bool CheckScenarioForText(List<TaskItem> taskScenario, TaskItem.TextShowTime showTime, ISelectable target = null)
    {
        if (target == null) target = currentSelectedItem;
        foreach (TaskItem item in taskScenario)
        {
            if (item.selectable == target)
            {
                foreach (TaskItem.TextToShow text in item.textToShow)
                {
                    if (text.showOnlyOnStepByStep && HandleInittingGlobalVars.globalParameters.parametersDict[HandleInittingGlobalVars.IS_STEP_BY_STEP_KEY] < 0.5f) continue;
                    if (text.showTime == showTime && !text.shown)
                    {
                        text.shown = true;
                        int authorIndex = -1;
                        var checker = target.GetClickableItem().taskExecutor == null ? PawnController.Instance.currentSelectedPawn.gameObject : target.GetClickableItem().taskExecutor.gameObject;
                        AudioClip clip = null;
                        for (int i = 0; i < authorsObjects.Length; i++)
                        {
                            if (authorsObjects[i] == checker)
                            {
                                authorIndex = i;
                                break;
                            }
                        }
                        foreach (var sound in text.completeSounds)
                        {
                            if (sound.authorIndex == authorIndex)
                            {
                                clip = sound.clip;
                                break;
                            }
                        }
                        AudioController.Instance.Play(clip);
                        UILayersController.Instance.ShowOverlay(UILayersController.UILayer.NarrativeText, text.text + "_" + authorIndex);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void OnGameResumed()
    {
        OnDeselect();
    }
    public void OnPlayerTurnStart()
    {
        // if (taskScenario.Count > currentTaskScenarioIndex && !taskScenario[currentTaskScenarioIndex].IsWorking())
        //     UI3DManager.Instance.ShowMessage("Task " + (currentTaskScenarioIndex + 1), taskScenario[currentTaskScenarioIndex].GetTransform().position, Color.purple);
        CheckActionBox();
    }

    private void OnPlayerTurnEnd()
    {
        if (currentTaskScenarioIndex < mainTaskScenario.Count)
        {
            foreach (TaskItem item in mainTaskScenario)
            {
                if (item.status == TaskItem.TaskItemStatus.ReadyToStart)
                {
                    UI3DManager.Instance.UnregisterSelectable(item.selectable);
                }
            }
            foreach (TaskItem item in sideTaskScenario)
            {
                if (item.status == TaskItem.TaskItemStatus.ReadyToStart)
                {
                    UI3DManager.Instance.UnregisterSelectable(item.selectable);
                }
            }
        }
    }

    public void OnCompleteTask(ISelectable selectable)
    {
        bool updated = false;
        TaskItem completedItem = null;
        foreach (TaskItem item in mainTaskScenario)
        {
            if (item.selectable == selectable)
            {
                item.status = TaskItem.TaskItemStatus.Done;
                item.selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.Done);
                UI3DManager.Instance.UnregisterSelectable(item.selectable);
                CheckActionBox();
                updated = true;
                completedItem = item;
            }
        }
        foreach (TaskItem item in sideTaskScenario)
        {
            if (item.selectable == selectable)
            {
                item.status = TaskItem.TaskItemStatus.Done;
                item.selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.Done);
                UI3DManager.Instance.UnregisterSelectable(item.selectable);
                CheckActionBox();
                updated = true;
                completedItem = item;
            }
        }
        if (completedItem != null && !string.IsNullOrEmpty(completedItem.completeText))
        {
            UI3DManager.Instance.ShowMessage(completedItem.completeText, selectable.GetTransform().position, completedItem.completeTextColor);
        }
        if (updated) OnTaskUpdated?.Invoke();
        if (CheckScenarioForText(mainTaskScenario, TaskItem.TextShowTime.AfterComplete, selectable)) return;
        if (CheckScenarioForText(sideTaskScenario, TaskItem.TextShowTime.AfterComplete, selectable)) return;
    }
    public void OnStartTask(ISelectable selectable)
    {
        if (CheckScenarioForText(mainTaskScenario, TaskItem.TextShowTime.BeforeStart)) return;
        if (CheckScenarioForText(sideTaskScenario, TaskItem.TextShowTime.BeforeStart)) return;
    }
    public void OnCancelTask(ClickableItem clickableItem)
    {
        foreach (TaskItem item in mainTaskScenario)
        {
            if (item.selectable == clickableItem)
            {
                item.status = TaskItem.TaskItemStatus.ReadyToStart;
                item.selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.ReadyToStart);
                CheckActionBox();
                return;
            }
        }
        foreach (TaskItem item in sideTaskScenario)
        {
            if (item.selectable == clickableItem)
            {
                item.status = TaskItem.TaskItemStatus.ReadyToStart;
                item.selectable.ChangeScenarioStatus(TaskItem.TaskItemStatus.ReadyToStart);
                CheckActionBox();
                return;
            }
        }
    }

}