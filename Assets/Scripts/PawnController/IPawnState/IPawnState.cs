using UnityEngine;

public abstract class IPawnState : MonoBehaviour
{
    public abstract void HandleDoingSth(Vector3 worldPoint, ISelectable selectable);
    public abstract void HandleUIDrawing(ISelectable selectable, Vector3 worldPoint, Vector2 screenPoint, ScreenCastHitResult hit);

}