using UnityEngine;

[System.Serializable]
public class NamedFormula
{
    public string name;

    // ToDo: try to make it private
    [SerializeReference]
    public FormulaField formula;
    private bool formulaAvailable = false;

    public NamedFormula()
    {
        formulaAvailable = false;
        formula = new FormulaField();
    }

    public NamedFormula(ParameteredScriptableObject context, string name)
    {
        formulaAvailable = false;
        formula = new FormulaField();
        this.name = name;
        SetContext(context);
    }

    public bool IsAvailable()
    {
        return formulaAvailable && formula.GetCompiled() == true;
    }

    public bool IsContextSet()
    {
        return formulaAvailable;
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
        formulaAvailable = true;
    }
}