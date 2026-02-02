using UnityEngine;

public interface ISelectable
{
    void OnSelect() { }
    void OnDeselect() { }
    Transform GetTransform();
}