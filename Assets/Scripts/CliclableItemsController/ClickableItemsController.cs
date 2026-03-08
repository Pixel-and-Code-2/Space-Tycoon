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
        public ISelectable selectable;
        // [HideInInspector]
        public TaskItemStatus status = TaskItemStatus.Unavailable;
        public int readyWhenItem = -1;
        public TaskItemStatus readyWhenStatus;
        public bool readyWhenIsMain = true;
    }
    public static ClickableItemsController Instance { get; private set; }
    public ISelectable currentSelectedItem { get; private set; }
    [SerializeField]
    private List<TaskItem> mainTaskScenario;
    [SerializeField]
    private List<TaskItem> sideTaskScenario;
    private int currentTaskScenarioIndex = 0;
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
        List<ContextMenuItem> items = currentSelectedItem.OnContextMenu();
        if (items != null)
        {
            UI3DManager.Instance.ShowContextMenu(currentSelectedItem.GetTransform().position, items);
        }
        UI3DManager.Instance.UnregisterSelectable(currentSelectedItem);
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
    }

}