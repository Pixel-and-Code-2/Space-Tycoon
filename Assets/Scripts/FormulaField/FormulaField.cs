using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FormulaField
{
    public FormulaField()
    {
        instruction = "Type a mathematical expression using common '+', '-', '*', '/', '^', '(', ')'.\nYou also can provide variables from Data Assets available in this object, using words like 'g_Statname' - the full list of available vars is listed below.\nEverything starting with '//' till the end of the line will be ignored as well as between those two: '/*', '*/'.\nYou also can use Math C# library, for example: Math.Abs(-g_someglobalstat).\nList of math formuls: Abs, Ceiling, Floor, Round, Pow, Sqrt, Sin, Cos, Tan (which use radians), Min, Max, Log. Also you can use: 'Math.PI' and 'Math.E'.\nClick the available variable below to add its name to the end oа the formula (but be sure to the formula to be blured when clicked).";
        // dataAssets.Add(GlobalParameters.Instance);
    }
    public string formula;
    public string instruction;
    public List<Object> dataAssets = new List<Object>();
    public List<string> names = new List<string>();



    // Делегат принимает массив локальных словарей и глобальный словарь
    private System.Func<Dictionary<string, float>[], Dictionary<string, float>, float> compiledFormulaAction;

    public float EvaluateFormula(Dictionary<string, float>[] locals)
    {
        if (compiledFormulaAction == null)
        {
            Debug.LogWarning("Program is trying to use formula which is not compiled?!\nFormula: " + formula);
            CompileFormula();
            if (compiledFormulaAction == null)
            {
                Debug.LogError("Program is trying to use formula which is cannot be compiled?!\nFormula: " + formula);
                return 0f;
            }
        }
        // Debug.Log("GlobalParameters.Instance: " + GlobalParameters.Instance.GetParametersDictState() + "locals: " + locals[0].Count);
        return compiledFormulaAction(locals, GlobalParameters.Instance.parametersDict);
    }


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
        try
        {
            string formulaString = GetFormulaString();
            if (string.IsNullOrEmpty(formulaString)) return;

            // 1. Препроцессинг: заменяем p_Stat на прямой доступ к словарю
            // Паттерн ищет "X_Word", где X - одна буква
            string processedFormula = System.Text.RegularExpressions.Regex.Replace(
                formulaString,
                @"\b([a-zA-Z])_(\w+)\b",
                match =>
                {
                    string prefix = match.Groups[1].Value;
                    string paramName = match.Groups[2].Value;

                    // Глобальные параметры (g_ или G_)
                    if (string.Equals(prefix, "g", System.StringComparison.OrdinalIgnoreCase))
                    {
                        return $"globals[\"{paramName}\"]";
                    }

                    // Локальные параметры по первой букве имени из списка names
                    for (int i = 0; i < names.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(names[i]) &&
                            names[i].StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                        {
                            return $"locals[{i}][\"{paramName}\"]";
                        }
                    }

                    // Если не нашли соответствие, оставляем как есть (вызовет ошибку или будет переменной)
                    return match.Value;
                }
            );

            // 2. Компиляция с параметрами
            var interpreter = new DynamicExpresso.Interpreter();

            var localParam = new DynamicExpresso.Parameter("locals", typeof(Dictionary<string, float>[]));
            var globalParam = new DynamicExpresso.Parameter("globals", typeof(Dictionary<string, float>));

            compiledFormulaAction = interpreter.Parse(processedFormula, localParam, globalParam)
                .Compile<System.Func<Dictionary<string, float>[], Dictionary<string, float>, float>>();

            Debug.Log($"Compiled formula: {formulaString} -> {processedFormula}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error compiling formula '{formula}': {ex.Message}");
            compiledFormulaAction = null;
        }
    }
}