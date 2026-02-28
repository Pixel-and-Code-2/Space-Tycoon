using UnityEngine;

public enum ScreenCastHitResult { NoHit, FloorHit, ZeroPlaneHit, SelectableHit };
public abstract class ISelectorBrain : MonoBehaviour
{
    public abstract IControlableSelectable PollSelectPawn(IControlableSelectable defaultPawn);
    public abstract IPawnState PollChangeState();
    public abstract (ISelectable selectable, Vector3 worldPoint) PollSelectPosForState();
    public virtual ISelectable PollSelectClickableItem(ISelectable defaultSelectable)
    {
        return defaultSelectable;
    }
}