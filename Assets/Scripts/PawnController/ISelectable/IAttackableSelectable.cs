using UnityEngine;

public interface IAttackableSelectable : ISelectable
{
    bool IsAttackable => true;
    void OnGetHit(float damage);
}