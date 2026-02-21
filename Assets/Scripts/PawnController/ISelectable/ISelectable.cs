using UnityEngine;
public enum SelectableType
{
    Player,
    Enemy,
    Neutral,
    Dead
}
public interface ISelectable
{
    SelectableType GetSelectableType();
    Transform GetTransform();
    IFormulaData GetFormulaData();
}