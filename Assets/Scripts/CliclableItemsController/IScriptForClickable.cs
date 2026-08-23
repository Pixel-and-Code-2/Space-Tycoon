using UnityEngine;

public struct ClickableTaskInfo
{
    public bool isTask;
    public bool isSide;
    public int taskId;

    public static ClickableTaskInfo None => new ClickableTaskInfo { isTask = false, isSide = false, taskId = -1 };
}

public class IScriptForClickable : MonoBehaviour
{
    public int selectTriggerIndex = -1;
    public int startTriggerIndex = -1;
    public int completeTriggerIndex = -1;
    public int cancelTriggerIndex = -1;

    public bool IsTask { get; private set; }
    public bool IsSide { get; private set; }
    public int TaskId { get; private set; }

    public void ApplyTaskInfo(ClickableTaskInfo info)
    {
        IsTask = info.isTask;
        IsSide = info.isSide;
        TaskId = info.taskId;
    }

    public virtual void OnSelect()
    {
        if (selectTriggerIndex != -1) TurnManager.Instance.StartDelayedEncounterByIndex(selectTriggerIndex);
    }
    public virtual void OnStart()
    {
        if (startTriggerIndex != -1) TurnManager.Instance.StartDelayedEncounterByIndex(startTriggerIndex);
    }
    public virtual void OnDeselect() { }
    public virtual void OnComplete()
    {
        if (completeTriggerIndex != -1) TurnManager.Instance.StartDelayedEncounterByIndex(completeTriggerIndex);
    }
    public virtual void OnCancel()
    {
        if (cancelTriggerIndex != -1) TurnManager.Instance.StartDelayedEncounterByIndex(cancelTriggerIndex);
    }
    public virtual float OnProgress(float newProgress) { return newProgress; }
}
