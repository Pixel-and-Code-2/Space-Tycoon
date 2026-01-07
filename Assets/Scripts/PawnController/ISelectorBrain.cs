using UnityEngine;

public enum FloorHitResult { NoHit, FloorHit, ZeroPlaneHit };

public interface ISelectorBrain
{
    ISelectable GetSelectionClickValue();
    bool GetDeselectionClickState();
    bool GetSelectionClickState();
    (Vector3 point, FloorHitResult hit) GetMoveSelectionValue();
}