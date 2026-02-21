using System.Collections.Generic;
using UnityEngine;

public class FormulaDataMonoBase : MonoBehaviour, IFormulaData
{
    public Dictionary<string, float> parametersDict { get; private set; } = new Dictionary<string, float>();

    public Object owner = null;
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

    public static FormulaDataMonoBase GetValidFormulaData(MonoBehaviour owner)
    {
        FormulaDataMonoBase[] formulaDataMonoBases = owner.GetComponents<FormulaDataMonoBase>();
        int index_of_free = -1;
        for (int index = 0; index < formulaDataMonoBases.Length; index++)
        {
            if (formulaDataMonoBases[index].owner == owner)
            {
                return formulaDataMonoBases[index];
            }
            if (index_of_free == -1 && formulaDataMonoBases[index].owner == null) index_of_free = index;
        }

        if (index_of_free != -1)
        {
            formulaDataMonoBases[index_of_free].owner = owner;
            return formulaDataMonoBases[index_of_free];
        }
        else
        {
            Debug.LogError("No free formula data mono base found. Try adding another formula data mono base near" + owner.gameObject.name);
            return null;
        }
    }
}