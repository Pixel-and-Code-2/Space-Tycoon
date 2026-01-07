using UnityEngine;

public interface ISelectable
{
    void OnSelect();
    void OnDeselect();
    Transform GetTransform();
    void OnMove(Vector3 position);
}