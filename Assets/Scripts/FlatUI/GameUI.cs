using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GameUI : IUILayer
{
    [SerializeField]
    private InputActionReference togglePause;
    [SerializeField]
    private TaskTextStyleChanger mainTaskTextStyleChanger;
    [SerializeField]
    private List<TaskTextStyleChanger> sideTaskTextStyleChangers;
    void Start()
    {
        ClickableItemsController.Instance.OnTaskUpdated += OnTaskUpdated;
    }
    void OnDestroy()
    {
        ClickableItemsController.Instance.OnTaskUpdated -= OnTaskUpdated;
    }
    void OnEnable()
    {
        gameObject.SetActive(true);
        togglePause.action.Enable();
    }
    void OnDisable()
    {
        gameObject.SetActive(false);
        togglePause.action.Disable();
    }
    void Update()
    {
        if (togglePause.action.triggered)
        {
            OnPause();
        }
    }
    public void OnPause()
    {
        UILayersController.Instance.SetLayer(UILayersController.UILayer.PauseMenu);
    }
    public void OnHelp()
    {
        // UILayersController.Instance.SetLayer(UILayersController.UILayer.HelpMenu);
    }
    private void OnTaskUpdated()
    {
        ClickableItemsController.TaskItem mainTask = null;
        var scenario = ClickableItemsController.Instance.mainTaskScenario;
        for (int i = scenario.Count - 1; i >= 0; i--)
        {
            if (scenario[i].status != ClickableItemsController.TaskItem.TaskItemStatus.Done)
            {
                mainTask = scenario[i];
                break;
            }
        }

        if (mainTask != null)
        {
            UpdateButton(mainTaskTextStyleChanger, mainTask);
        }
        else
        {
            mainTaskTextStyleChanger.ClearText();
            UILayersController.Instance.SetLayer(UILayersController.UILayer.AttentionText, "Победа!_persistent");
        }
        int subTaskIndex = 0;
        for (int i = 0; i < scenario.Count && subTaskIndex < sideTaskTextStyleChangers.Count; i++)
        {
            var item = scenario[i];
            if (item == mainTask || item.status == ClickableItemsController.TaskItem.TaskItemStatus.Done)
                continue;

            UpdateButton(sideTaskTextStyleChangers[subTaskIndex], item);
            subTaskIndex++;
        }

        for (int i = subTaskIndex; i < sideTaskTextStyleChangers.Count; i++)
        {
            sideTaskTextStyleChangers[i].ClearText();
        }
    }
    private void UpdateButton(TaskTextStyleChanger button, ClickableItemsController.TaskItem item)
    {
        int isAvailable = -1;
        if (item.status == ClickableItemsController.TaskItem.TaskItemStatus.Unavailable) isAvailable = 0;
        else if (item.status == ClickableItemsController.TaskItem.TaskItemStatus.ReadyToStart) isAvailable = 1;
        button.ChangeText(
            item.shortLevelName,
            isAvailable,
            item.status == ClickableItemsController.TaskItem.TaskItemStatus.Done ? 1 : -1,
            item.status == ClickableItemsController.TaskItem.TaskItemStatus.InProgress ? 1 : -1
        );
    }
}