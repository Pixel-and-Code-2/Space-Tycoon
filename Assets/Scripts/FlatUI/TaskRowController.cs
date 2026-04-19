using UnityEngine;
using System.Collections.Generic;

public class TaskRowController : MonoBehaviour
{
    [SerializeField]
    private TaskTextStyleChanger taskTextStyleChanger;
    [System.Serializable]
    private class StateChanger
    {
        public ClickableItemsController.TaskItem.TaskItemStatus type;
        public List<GameObject> objectsToTurnOn;
        public List<GameObject> objectsToTurnOff;
    }
    [SerializeField]
    private List<StateChanger> stateChangers;
    public void UpdateTask(ClickableItemsController.TaskItem item)
    {
        int isAvailable = -1;
        if (item.status == ClickableItemsController.TaskItem.TaskItemStatus.Unavailable) isAvailable = 0;
        else if (item.status == ClickableItemsController.TaskItem.TaskItemStatus.ReadyToStart) isAvailable = 1;
        taskTextStyleChanger.ChangeText(
            item.shortLevelName,
            isAvailable,
            item.status == ClickableItemsController.TaskItem.TaskItemStatus.Done ? 1 : -1,
            item.status == ClickableItemsController.TaskItem.TaskItemStatus.InProgress ? 1 : -1
        );
        UpdateState(item);
    }
    public void ClearText()
    {
        taskTextStyleChanger.ClearText();
        // UpdateState(ClickableItemsController.TaskItem.TaskItemStatus.Unavailable);
    }
    private void UpdateState(ClickableItemsController.TaskItem item)
    {
        foreach (StateChanger stateChanger in stateChangers)
        {
            if (stateChanger.type == item.status)
            {
                foreach (GameObject obj in stateChanger.objectsToTurnOn)
                {
                    obj.SetActive(true);
                }
                foreach (GameObject obj in stateChanger.objectsToTurnOff)
                {
                    obj.SetActive(false);
                }
            }
        }

    }
}