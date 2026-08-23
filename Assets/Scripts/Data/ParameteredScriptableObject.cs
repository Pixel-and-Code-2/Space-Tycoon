using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "Parameters", menuName = "Parameters", order = 1)]
public class ParameteredScriptableObject : ScriptableObject
{
    [SerializeField]
    private List<NamedFloat> parameters = new List<NamedFloat>();

    [SerializeField, Tooltip("Parameters merged before local ones; local values override.")]
    private List<ParameteredScriptableObject> mustHaveParameters = new List<ParameteredScriptableObject>();

    [SerializeField, HideInInspector]
    private bool isDirty = true;
    [SerializeField, HideInInspector]
    private string parametersDictStateCache = string.Empty;
    public Dictionary<string, float> parametersDict { get; private set; } = new Dictionary<string, float>();
    [SerializeField, HideInInspector]
    public static event Action<ParameteredScriptableObject> OnUpdateParams;

    public List<string> GetParameterNames()
    {
        var lst = new List<string>();
        RebuildParametersDict();
        foreach (var kv in parametersDict)
            lst.Add(kv.Key);
        return lst;
    }

    public void AddParameter(string name)
    {
        if (parameters.Find(x => x.name == name) != null) return;
        parameters.Add(new NamedFloat(name, 0f));
        SetDirty();
    }

    public Dictionary<string, float> GetParametersDict()
    {
        RebuildParametersDict();
        return parametersDict;
    }

    public string GetParametersDictState()
    {
        RebuildParametersDict();
        if (parametersDict.Count == 0)
        {
            parametersDictStateCache = "Parameters dictionary is empty";
            return parametersDictStateCache;
        }
        var sb = new System.Text.StringBuilder();
        foreach (var kv in parametersDict)
            sb.AppendLine(kv.Key + " = " + kv.Value.ToString("F2"));
        parametersDictStateCache = sb.ToString();
        return parametersDictStateCache;
    }

    public new void SetDirty()
    {
        isDirty = true;
    }

    public void RebuildParametersDict()
    {
        OnUpdateParams?.Invoke(this);
        if (!isDirty && parametersDict.Count > 0) return;
        RebuildParametersDict(new HashSet<ParameteredScriptableObject>());
        isDirty = false;
    }

    public void RebuildParametersDict(HashSet<ParameteredScriptableObject> visited)
    {
        if (visited.Count > 15)
        {
            Debug.LogError("Recursive call limit reached for: " + name);
            return;
        }
        if (visited.Contains(this)) return;
        visited.Add(this);
        parametersDict.Clear();
        for (int i = mustHaveParameters.Count - 1; i >= 0; i--)
        {
            var dep = mustHaveParameters[i];
            if (dep == null || dep == this) continue;
            var depConst = dep.GetRecursiveConstParametersDict(new HashSet<ParameteredScriptableObject>());
            foreach (var kv in depConst)
                parametersDict[kv.Key] = kv.Value;
        }
        AddParametersAsConsts(parameters);
    }

    public Dictionary<string, float> GetRecursiveConstParametersDict(HashSet<ParameteredScriptableObject> visited)
    {
        if (visited.Contains(this)) return new Dictionary<string, float>();
        visited.Add(this);
        var result = new Dictionary<string, float>();
        for (int i = mustHaveParameters.Count - 1; i >= 0; i--)
        {
            var dep = mustHaveParameters[i];
            if (dep == null || dep == this) continue;
            var depConst = dep.GetRecursiveConstParametersDict(visited);
            foreach (var kv in depConst)
                result[kv.Key] = kv.Value;
        }
        foreach (var p in parameters)
        {
            var key = processParameterName(p.name);
            if (!string.IsNullOrEmpty(key))
                result[key] = p.value;
        }
        return result;
    }

    void AddParametersAsConsts(List<NamedFloat> parameters)
    {
        foreach (var parameter in parameters)
        {
            parameter.name = processParameterName(parameter.name);
            if (parameter.name != null && parameter.name != "")
                parametersDict[parameter.name] = parameter.value;
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
