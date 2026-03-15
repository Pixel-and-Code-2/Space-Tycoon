using UnityEngine;

public abstract class IAttackableSelectable : ISelectable
{
    public virtual bool IsAttackable => true;
    public abstract void OnGetHit(float damage);
    public abstract IFormulaData GetFormulaData();
    public abstract float GetDynamicParameterValue(string parameterName);
    public abstract void SetDynamicParameterValue(string parameterName, float value);
    public abstract void FillFormulaData(FormulaDataMonoBase formulaData, string prefix);
}