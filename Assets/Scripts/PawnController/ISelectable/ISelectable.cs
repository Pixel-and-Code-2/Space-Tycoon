using System.Collections.Generic;
using UnityEngine;
public enum SelectableType
{
    Player,
    Enemy,
    Neutral,
    Dead
}
public abstract class ISelectable : ILookTarget
{
    public abstract SelectableType GetSelectableType();
    public override bool isMovable => false;
    public virtual List<ContextMenuItem> OnContextMenu() { return null; }
    public virtual void OnSelect() { }
    public virtual void OnDeselect() { }
    public virtual bool IsWorking() { return false; }
}