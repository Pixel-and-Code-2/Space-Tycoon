using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Parameters", menuName = "Parameters", order = 1)]
public class ParameteredScriptableObject : ScriptableObject, IFormulaData
{

    [SerializeField]
    private List<NamedFloat> parameters = new List<NamedFloat>();

    [SerializeField]
    private bool isContext = false;

    [SerializeField]
    private List<ParameteredScriptableObject> mustHaveParameters = new List<ParameteredScriptableObject>();
    public List<NamedFloat> GetParameters()
    {
        return parameters;
    }

    public Dictionary<string, float> parametersDict { get; private set; } = new Dictionary<string, float>();

    public Dictionary<string, float> GetParametersDict()
    {
        RebuildParametersDict();
        return parametersDict;
    }

    public string GetParametersDictState()
    {
        RebuildParametersDict();
        if (parametersDict.Count == 0)
            return "Parameters dictionary is empty";
        var sb = new System.Text.StringBuilder();
        foreach (var kv in parametersDict)
            sb.AppendLine(kv.Key + " = " + kv.Value.ToString("F2") + " (" + kv.Value.ToString("F2") + ")");
        return sb.ToString();
    }

    public void RebuildParametersDict()
    {
        parametersDict.Clear();
        AddBuiltInParameters();
        foreach (var parameter in parameters)
        {
            parameter.name = processParameterName(parameter.name);
            if (parameter.name != null && parameter.name != "")
                parametersDict[parameter.name] = parameter.value;
        }
    }

    protected virtual void AddBuiltInParameters()
    {
        if (isContext) return;
        foreach (var mustHaveParameter in mustHaveParameters)
        {
            if (mustHaveParameter == null) continue;
            if (mustHaveParameter.isContext == false)
            {
                Debug.LogWarning("Must have parameter is not a context (just skipping): " + mustHaveParameter.name);
                continue;
            }
            mustHaveParameter.RebuildParametersDict();
            foreach (var kv in mustHaveParameter.parametersDict)
            {
                parametersDict[kv.Key] = kv.Value;
            }
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
}