using UnityEngine;

public abstract class IControlableSelectable : IAttackableSelectable
{
    public abstract void OnMove(Vector3 position);
    public abstract void OnShoot(Vector3 position);
    public abstract void OnMelee(Vector3 position);

    public abstract (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) GetPathPointsTo(Vector3 position);

    public abstract bool IsMoving();
}