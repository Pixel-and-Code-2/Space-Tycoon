using System.Collections.Generic;
using UnityEngine;

public interface IFormulaData
{
    public List<NamedFloat> GetParameters();
    public Dictionary<string, float> parametersDict { get; }
}