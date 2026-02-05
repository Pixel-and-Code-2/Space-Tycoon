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
    bool IsShootable => false;
    SelectableType GetSelectableType();
    void OnDealDamage(float damage) { }
    void OnSelect() { }
    void OnDeselect() { }
    Transform GetTransform();
    string GetHPText() { return ""; }
    IFormulaData GetFormulaData();
}