using UnityEngine;

public abstract class IAttackableSelectable : ISelectable
{
    public virtual bool IsAttackable => true;
    public abstract bool OnGetHit(float damage);
    public abstract void OnGetDefendedHit(Vector3 hitDirection, bool isMelee);
    public abstract float GetDynamicParameterValue(string parameterName);
    public abstract void SetDynamicParameterValue(string parameterName, float value);
}