using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ClickableItemsController : MonoBehaviour
{
    [System.Serializable]
    private class TaskItem
    {
        public enum TaskItemStatus
        {
            Unavailable, // Player can't click to start job, but soon it will be available
            ReadyToStart, // Means player can click and start doing sth with this object
            InProgress,
            Done,
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
            public TextShowTime showTime;
            public string text;
            [HideInInspector]
            public bool shown = false;
            public bool showOnce = true;
            public bool showOnlyOnStepByStep = false;
        }
        public ISelectable selectable;
        // [HideInInspector]
        public TaskItemStatus status = TaskItemStatus.Unavailable;
        public int readyWhenItem = -1;
        public TaskItemStatus readyWhenStatus;
        public bool readyWhenIsMain = true;
        public List<TextToShow> textToShow = new List<TextToShow>();
    }
    public static ClickableItemsController Instance { get; private set; }
    public ISelectable currentSelectedItem { get; private set; }
    [SerializeField]
    private List<TaskItem> mainTaskScenario;
    [SerializeField]
    private List<TaskItem> sideTaskScenario;
    private int currentTaskScenarioIndex = 0;
    private const string UNIQUE_ID = "ClickableItemsController";
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
    }
    void Update()
    {
        foreach (TaskItem item in mainTaskScenario)
        {
            if (item.selectable.IsWorking() && item.status != TaskItem.TaskItemStatus.InProgress)
            {
                item.status = TaskItem.TaskItemStatus.InProgress;
                UI3DManager.Instance.UnregisterSelectable(item.selectable);
                CheckActionBox();
            }
        }
        foreach (TaskItem item in sideTaskScenario)
        {
            if (item.selectable.IsWorking() && item.status != TaskItem.TaskItemStatus.InProgress)
            {
                item.status = TaskItem.TaskItemStatus.InProgress;
                UI3DManager.Instance.UnregisterSelectable(item.selectable);
                CheckActionBox();
            }
        }
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
    }
    private void CheckActionBox(List<TaskItem> scenario, string label)
    {
        for (int i = 0; i < scenario.Count; i++)
        {
            if (scenario[i].status == TaskItem.TaskItemStatus.Unavailable)
            {
                if (CheckReadyWhen(scenario[i]))
                {
                    scenario[i].status = TaskItem.TaskItemStatus.ReadyToStart;
                }
            }
            if (scenario[i].selectable.IsWorking())
            {
                if (scenario[i].status != TaskItem.TaskItemStatus.InProgress)
                {
                    scenario[i].status = TaskItem.TaskItemStatus.InProgress;
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
    }
    private void CheckActionBox()
    {
        CheckActionBox(mainTaskScenario, "!");
        CheckActionBox(sideTaskScenario, "?");
    }

    private bool CheckReadyWhen(TaskItem item)
    {
        if (item.readyWhenItem < 0)
        {
            return true;
        }
        if (item.readyWhenIsMain)
        {
            return mainTaskScenario[item.readyWhenItem].status == item.readyWhenStatus;
        }
        return sideTaskScenario[item.readyWhenItem].status == item.readyWhenStatus;
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
        if (currentSelectedItem == null) return;
        if (CheckScenarioForText(mainTaskScenario, TaskItem.TextShowTime.BeforeContextMenu)) return;
        if (CheckScenarioForText(sideTaskScenario, TaskItem.TextShowTime.BeforeContextMenu)) return;
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
                        UILayersController.Instance.SetLayer(UILayersController.UILayer.NarrativeText, text.text);
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
        foreach (TaskItem item in mainTaskScenario)
        {
            if (item.selectable == selectable)
            {
                item.status = TaskItem.TaskItemStatus.Done;
                UI3DManager.Instance.UnregisterSelectable(item.selectable);
                CheckActionBox();
            }
        }
        foreach (TaskItem item in sideTaskScenario)
        {
            if (item.selectable == selectable)
            {
                item.status = TaskItem.TaskItemStatus.Done;
                UI3DManager.Instance.UnregisterSelectable(item.selectable);
                CheckActionBox();
            }
        }
        if (CheckScenarioForText(mainTaskScenario, TaskItem.TextShowTime.AfterComplete, selectable)) return;
        if (CheckScenarioForText(sideTaskScenario, TaskItem.TextShowTime.AfterComplete, selectable)) return;
    }
    public void OnStartTask(ISelectable selectable)
    {
        if (CheckScenarioForText(mainTaskScenario, TaskItem.TextShowTime.BeforeStart)) return;
        if (CheckScenarioForText(sideTaskScenario, TaskItem.TextShowTime.BeforeStart)) return;
    }

}