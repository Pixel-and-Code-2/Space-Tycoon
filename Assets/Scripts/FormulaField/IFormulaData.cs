using System.Collections.Generic;
using UnityEngine;

public interface IFormulaData
{
    public List<string> GetParameterNames();
    public Dictionary<string, float> parametersDict { get; }
}