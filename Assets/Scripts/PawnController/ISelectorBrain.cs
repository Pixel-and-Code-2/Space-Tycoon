using UnityEngine;

public enum FloorHitResult { NoHit, FloorHit, ZeroPlaneHit };
public enum PawnHitResult { NoHit, PawnHit, FloorHit };
public interface ISelectorBrain
{
    ISelectable GetSelectionClickValue();
    ISelectable GetSelectionValue();
    bool GetDeselectionClickState();
    bool GetSelectionClickState();
    (Vector3 worldPoint, Vector2 screenPoint, FloorHitResult hit) GetMoveSelectionValue();
    (Vector3 worldPoint, Vector2 screenPoint, PawnHitResult hit) GetShootSelectionValue();
    event System.Action<ControlType> OnControlTypeChange;
}