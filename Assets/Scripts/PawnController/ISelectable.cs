using UnityEngine;

public interface ISelectable
{
    bool IsShootable => false;
    void OnGetShot(float damage) { }
    void OnSelect() { }
    void OnDeselect() { }
    Transform GetTransform();
    string GetHPText() { return ""; }
}