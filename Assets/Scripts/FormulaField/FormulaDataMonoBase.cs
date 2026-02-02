using System.Collections.Generic;
using UnityEngine;

public class FormulaDataMonoBase : MonoBehaviour, IFormulaData
{
    public Dictionary<string, float> parametersDict { get; private set; } = new Dictionary<string, float>();

    public List<NamedFloat> GetParameters()
    {
        var lst = new List<NamedFloat>();
        foreach (var kv in parametersDict)
        {
            lst.Add(new NamedFloat { name = kv.Key, value = kv.Value });
        }
        return lst;
    }

    public void AddParameter(string name)
    {
        parametersDict[name] = 0f;
    }

    public void FullFillWithParameters(string[] names)
    {
        parametersDict.Clear();
        foreach (var name in names)
        {
            AddParameter(name);
        }
    }
}