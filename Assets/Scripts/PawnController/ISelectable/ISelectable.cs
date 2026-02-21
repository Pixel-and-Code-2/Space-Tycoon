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
    public abstract IFormulaData GetFormulaData();
}