using UnityEngine;

public class MeleeState : IPawnState
{
    public override void HandleDoingSth(Vector3 worldPoint, ISelectable selectable) { }
    public override void HandleUIDrawing(ISelectable selectable, Vector3 worldPoint, Vector2 screenPoint, ScreenCastHitResult hit) { }
    public override bool IsErrorChance(IAttackableSelectable attackableSelectable) => true;
}
