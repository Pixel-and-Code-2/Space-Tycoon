using UnityEngine;

public abstract class ISelectorBrainWithUI : ISelectorBrain
{
    public abstract (ISelectable selectable, Vector3 worldPoint, Vector2 screenPoint, ScreenCastHitResult hit) PollForIntermidiateAiming();
}