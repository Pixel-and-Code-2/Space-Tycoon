using UnityEngine;

public abstract class IAttackableSelectable : ISelectable
{
    public virtual bool IsAttackable => true;
    public abstract void OnGetHit(float damage);
}