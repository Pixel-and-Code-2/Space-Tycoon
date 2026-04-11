using UnityEngine;

public class IScriptForClickable : MonoBehaviour
{
    public int selectTriggerIndex = -1;
    public int startTriggerIndex = -1;
    public int completeTriggerIndex = -1;
    public int cancelTriggerIndex = -1;
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
    public virtual void OnProgress(float newProgress) { }
}