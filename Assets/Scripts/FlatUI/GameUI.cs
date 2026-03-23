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
        int i = 0;
        int amountOfButtons = 0;
        if (ClickableItemsController.Instance.mainTaskScenario.Count > 0)
        {
            UpdateButton(mainTaskTextStyleChanger, ClickableItemsController.Instance.mainTaskScenario[ClickableItemsController.Instance.mainTaskScenario.Count - 1]);
            amountOfButtons += 1;
        }
        while (ClickableItemsController.Instance.mainTaskScenario.Count > i && ClickableItemsController.Instance.mainTaskScenario[i].status == ClickableItemsController.TaskItem.TaskItemStatus.Done)
        {
            i += 1;
        }
        for (int j = 0; j < sideTaskTextStyleChangers.Count && ClickableItemsController.Instance.mainTaskScenario.Count > i; j++, i++, amountOfButtons++)
        {
            UpdateButton(sideTaskTextStyleChangers[j], ClickableItemsController.Instance.mainTaskScenario[i]);
        }
        if (amountOfButtons < 1 + sideTaskTextStyleChangers.Count)
        {
            if (amountOfButtons < 1)
            {
                mainTaskTextStyleChanger.ClearText();
            }
            for (int j = amountOfButtons; j < 1 + sideTaskTextStyleChangers.Count; j++)
            {
                sideTaskTextStyleChangers[j - 1].ClearText();
            }
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