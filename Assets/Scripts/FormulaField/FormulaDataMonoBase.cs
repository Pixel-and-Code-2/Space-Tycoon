using System.Collections.Generic;
using UnityEngine;

public class FormulaDataMonoBase : MonoBehaviour, IFormulaData
{
    public Dictionary<string, float> parametersDict { get; private set; } = new Dictionary<string, float>();

    public List<string> GetParameterNames()
    {
        var lst = new List<string>();
        foreach (var kv in parametersDict)
        {
            lst.Add(kv.Key);
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