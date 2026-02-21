using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FormulaFieldWithMemo : FormulaField
{
    // list of getters to actual datasets
    private List<System.Func<(IFormulaData, string)>> memorizedDatasets;
    public int memorySize => memorizedDatasets.Count;
    public FormulaFieldWithMemo() : base()
    {
        memorizedDatasets = new List<System.Func<(IFormulaData, string)>>();
        HandleInittingGlobalVars.onParamsUpdated += OnParamsUpdated;
    }

    public void ClearMemorizedDatasets()
    {
        memorizedDatasets.Clear();
        OnParamsUpdated();
    }

    public void AddMemorizedDataset(System.Func<(IFormulaData, string)> getter)
    {
        memorizedDatasets.Add(getter);
        OnParamsUpdated();
    }

    public void OnParamsUpdated()
    {
        if (this == null)
        {
            HandleInittingGlobalVars.onParamsUpdated -= OnParamsUpdated;
            return;
        }
        UpdateLists();
    }

    public float EvaluateFormula()
    {
        Dictionary<string, float>[] locals = UpdateLists();
        return base.EvaluateFormula(locals);
    }
    private Dictionary<string, float>[] UpdateLists()
    {
        if (memorizedDatasets.Count != dataAssets.Count)
        {
            dataAssets.Clear();
            names.Clear();
            for (int i = 0; i < memorizedDatasets.Count; i++)
            {
                dataAssets.Add(null);
                names.Add(null);
            }
        }
        Dictionary<string, float>[] locals = new Dictionary<string, float>[memorizedDatasets.Count];
        for (int i = 0; i < memorizedDatasets.Count; i++)
        {
            (IFormulaData dataset, string name) = memorizedDatasets[i]();
            if (dataset == null)
            {
                // Debug.LogError("Dataset is null");
                continue;
            }
            locals[i] = dataset.parametersDict;
            if (names[i] == name)
            {
                if (dataset != dataAssets[i] as IFormulaData)
                {
                    dataAssets[i] = dataset as Object;
                }
            }
            else
            {
                names[i] = name;
                dataAssets[i] = dataset as Object;
            }
        }
        return locals;
    }

    ~FormulaFieldWithMemo()
    {
        HandleInittingGlobalVars.onParamsUpdated -= OnParamsUpdated;
    }
}