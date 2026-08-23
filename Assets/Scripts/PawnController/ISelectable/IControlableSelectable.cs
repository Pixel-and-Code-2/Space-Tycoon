using UnityEngine;

public abstract class IControlableSelectable : IAttackableSelectable
{
    public abstract void OnMove(Vector3 position);
    public virtual void OnMoveFree(Vector3 position) { OnMove(position); }
    public abstract void OnShoot(Vector3 position, bool isAlive);
    public virtual void OnNoAmmoShoot() { }
    public abstract void OnMelee(Vector3 position);

    public abstract (Vector3[] pointsAvailable, Vector3[] pointsOutOfRange) GetPathPointsTo(Vector3 position);

    public abstract bool IsMoving();

    public abstract bool IsInActiveTriggerZone();
    public abstract void MakeReload();
    public virtual void OnCompleteTask() { }

    public virtual PawnDataController PawnData => null;
    public virtual bool IsAlive => GetSelectableType() != SelectableType.Dead;
    public virtual bool IsPlayer => GetSelectableType() == SelectableType.Player;
    public virtual bool IsOnTask => false;
    public virtual bool IsBusy => IsOnTask;
    public virtual bool HasMovedThisTurn => PawnData != null && PawnData.HasMovedThisTurn;
    public virtual float CurrentHp => PawnData != null ? PawnData.CurrentHp : 0f;
    public virtual float Stamina => PawnData != null ? PawnData.Stamina : 0f;
    public virtual bool CanWalkNow => IsAlive && !IsMoving() && !IsBusy && (PawnData == null || PawnData.MaxMoveMetersFromStamina > 0.01f);
    public virtual void SetOnTask(bool onTask) { }
    public virtual void MarkCtrlSoloMove() { }
    public virtual void MarkBusyFromNow() { }
    public virtual void ClearMoveHold() { }
    public virtual bool IsAutoFollowHold => false;
    public virtual void StopMove() { }
}
