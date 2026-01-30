using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FormulaField
{
    public FormulaField()
    {
        instruction = "Instructions...\nmultiline\nyay";
        // dataAssets.Add(GlobalParameters.Instance);
    }
    public string formula;
    public string instruction;
    public List<Object> dataAssets = new List<Object>();
    public List<string> names = new List<string>();

    public string GetFormulaString()
    {
        if (string.IsNullOrEmpty(formula))
            return string.Empty;

        string noSingleLine = System.Text.RegularExpressions.Regex.Replace(
            formula, @"//[^\n/][^\n]*\n", "\n");

        string noComments = System.Text.RegularExpressions.Regex.Replace(
            noSingleLine, @"/\*.*?\*/", "", System.Text.RegularExpressions.RegexOptions.Singleline);

        string noWhitespace = System.Text.RegularExpressions.Regex.Replace(
            noComments, @"\s+", "");

        return noWhitespace;
    }

    public void CompileFormula()
    {
        string formulaString = GetFormulaString();
    }
}