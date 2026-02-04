using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Parameters", menuName = "Parameters", order = 1)]
public class ParameteredScriptableObject : ScriptableObject, IFormulaData
{

    [SerializeField]
    private List<NamedFloat> parameters = new List<NamedFloat>();
    [SerializeField]
    private List<NamedFormula> calculatedParameters = new List<NamedFormula>();

    [SerializeField, Tooltip("Parameters which must be added to the parameters dictionary of the current object BEFORE ANY OF YOURS, you can override them! ")]
    private List<ParameteredScriptableObject> mustHaveParameters = new List<ParameteredScriptableObject>();

    [SerializeField, HideInInspector]
    private bool isDirty = true; // Cache. If nothing is changed, we don't need to rebuild the dictionary
    [SerializeField, HideInInspector]
    private string parametersDictStateCache = string.Empty;

    public List<string> GetParameterNames()
    {
        var lst = new List<string>();
        foreach (var calculatedParameter in calculatedParameters)
        {
            lst.Add(calculatedParameter.name);
        }
        foreach (var parameter in parameters)
        {
            lst.Add(parameter.name);
        }
        return lst;
    }

    public Dictionary<string, float> parametersDict { get; private set; } = new Dictionary<string, float>();

    public Dictionary<string, float> GetParametersDict()
    {
        RebuildParametersDict();
        return parametersDict;
    }

    public string GetParametersDictState()
    {
        if (!isDirty && !string.IsNullOrEmpty(parametersDictStateCache))
        {
            return parametersDictStateCache;
        }
        RebuildParametersDict();
        if (parametersDict.Count == 0)
        {
            parametersDictStateCache = "Parameters dictionary is empty";
            return parametersDictStateCache;
        }
        var sb = new System.Text.StringBuilder();
        foreach (var kv in parametersDict)
            sb.AppendLine(kv.Key + " = " + kv.Value.ToString("F2") + " (" + kv.Value.ToString("F2") + ")");

        parametersDictStateCache = sb.ToString();
        // Debug.Log("Cache updated for: " + name);
        return parametersDictStateCache;
    }

    public new void SetDirty()
    {
        isDirty = true;
    }

    public void RebuildParametersDict()
    {
        if (!isDirty && parametersDict.Count > 0) return;
        RebuildParametersDict(new HashSet<ParameteredScriptableObject>());
        isDirty = false;
    }

    public void RebuildParametersDict(HashSet<ParameteredScriptableObject> visited)
    {
        if (visited.Contains(this)) return;
        visited.Add(this);
        // Debug.Log("Rebuilding parameters dict for: " + name);
        parametersDict.Clear();
        CheckCalculatedParameters();
        AddBuiltInParameters(visited);
        AddParametersAsConsts(parameters);
        AddParametersAsCalculatables(calculatedParameters);
    }

    private void AddParametersAsConsts(List<NamedFloat> parameters)
    {
        foreach (var parameter in parameters)
        {
            parameter.name = processParameterName(parameter.name);
            if (parameter.name != null && parameter.name != "")
                parametersDict[parameter.name] = parameter.value;
        }
    }
    private void AddParametersAsCalculatables(List<NamedFormula> calculatedParameters)
    {
        if (this.calculatedParameters != calculatedParameters)
        {
            foreach (var calculatedParameter in calculatedParameters)
            {
                if (this.calculatedParameters.Find(x => x.name == calculatedParameter.name) == null)
                {
                    NamedFormula newCalculatedParameter = new NamedFormula();
                    newCalculatedParameter.name = calculatedParameter.name;
                    newCalculatedParameter.formula = calculatedParameter.formula;
                    newCalculatedParameter.SetContext(this);
                    this.calculatedParameters.Add(newCalculatedParameter);
                }
            }
        }
        foreach (var calculatedParameter in calculatedParameters)
        {
            if (calculatedParameter.IsAvailable())
            {
                parametersDict[calculatedParameter.name] = calculatedParameter.formula.EvaluateFormula(new Dictionary<string, float>[] { parametersDict });
            }
            else
            {
                Debug.LogWarning("You are trying to use calculatable parameter which is not available (formula must be compiled): " + calculatedParameter.name);
            }
        }
    }

    private void AddBuiltInParameters(HashSet<ParameteredScriptableObject> visited)
    {
        for (int i = mustHaveParameters.Count - 1; i >= 0; i--)
        {
            var mustHaveParameter = mustHaveParameters[i];
            if (mustHaveParameter == null || mustHaveParameter == this) continue;
            mustHaveParameter.RebuildParametersDict(visited);
            AddParametersAsConsts(mustHaveParameter.parameters);
            AddParametersAsCalculatables(mustHaveParameter.calculatedParameters);
        }
    }


    public static string processParameterName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;
        var filtered = new System.Text.StringBuilder(name.Length);
        foreach (var c in name)
        {
            if ((c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z'))
            {
                filtered.Append(c);
            }
        }
        return filtered.ToString();
    }

    void CheckCalculatedParameters()
    {
        foreach (var calculatedParameter in calculatedParameters)
        {
            if (calculatedParameter.IsContextSet() == false)
            {
                calculatedParameter.SetContext(this);
            }
            // Debug.Log("Calculated parameter: " + calculatedParameter.name + " is available: " + calculatedParameter.IsAvailable());
        }
    }
}