using UnityEngine;

[System.Serializable]
public class NamedFormula
{
    public string name;

    public FormulaField formula;

    public NamedFormula()
    {
        formula = new FormulaField();
    }

    public NamedFormula(ParameteredScriptableObject context, string name)
    {
        formula = new FormulaField();
        this.name = name;
        SetContext(context);
    }

    public bool IsAvailable()
    {
#if UNITY_EDITOR
        if (!formula.GetCompiled() && !string.IsNullOrEmpty(formula.formula))
        {
            formula.CompileFormula();
        }
#endif
        return IsContextSet() && formula.GetCompiled() == true;
    }

    public bool IsContextSet()
    {
        return formula.dataAssets.Count > 0;
    }

    public void SetContext(ParameteredScriptableObject context)
    {
        if (context == null)
        {
            Debug.LogError("Context or name is null, aborting...");
            return;
        }
        if (formula == null)
        {
            // ToDo: this is crazy, the contructor of this class is not called due to the SerializedReference of some of the fathers objects, hate C#.
            formula = new FormulaField();
        }
        formula.dataAssets.Clear();
        formula.dataAssets.Add(context);
        formula.names.Clear();
        formula.names.Add("Parameters");
    }

    public NamedFormula(NamedFormula other, ParameteredScriptableObject context)
    {
        this.name = other.name;
        this.formula = new FormulaField();
        this.formula.formula = other.formula.formula;
        SetContext(context);
    }
}