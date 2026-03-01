using System.Collections.Generic;
using UnityEngine;
public enum SelectableType
{
    Player,
    Enemy,
    Neutral,
    Dead
}
public abstract class ISelectable : MonoBehaviour
{
    public abstract SelectableType GetSelectableType();
    public abstract Transform GetTransform();
    public virtual List<ContextMenuItem> OnContextMenu() { return null; }
    public virtual void OnSelect() { }
    public virtual void OnDeselect() { }
    public virtual bool IsWorking() { return false; }
}