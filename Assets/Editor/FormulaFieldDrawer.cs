/// DO NOT EVEN TRY TO UNDERSTAND THIS CODE
/// 
/// !!!ATTENTION!!! --- FULLY VIBE CODED --- !!!ATTENTION!!!
/// 
/// This script is used to draw the one single FormulaField in the inspector, using given in FormulaField instance Data Assets. Additionaly in the end it adds global params asset. 


using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(FormulaField))]
public class FormulaFieldDrawer : PropertyDrawer
{
    private const float MinFormulaHeight = 80f;
    private const float MinInstructionHeight = 40f;
    private const float ButtonHeight = 20f;
    private const int ButtonsPerRow = 3;
    private const float DividerHeight = 1f;

    private static readonly Dictionary<string, bool> FoldoutStates = new();
    private static GUIStyle textAreaStyle;

    private static GUIStyle TextAreaStyle => textAreaStyle ??= new GUIStyle(EditorStyles.textArea) { wordWrap = true };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var baseExpanded = GetFoldoutState(property.propertyPath, property.isExpanded);
        var newExpanded = EditorGUI.Foldout(foldoutRect, baseExpanded, label, true);
        if (newExpanded != baseExpanded)
            SetFoldoutState(property.propertyPath, newExpanded);
        property.isExpanded = newExpanded;

        if (!newExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        float width = position.width;

        var instructionProp = property.FindPropertyRelative("instruction");
        y = DrawTextAreaSection(position.x, width, y, instructionProp, MinInstructionHeight);

        var formulaProp = property.FindPropertyRelative("formula");
        y = DrawTextAreaSection(position.x, width, y, formulaProp, MinFormulaHeight);


        EditorGUI.DrawRect(new Rect(position.x, y, width, DividerHeight), Color.grey);
        y += DividerHeight + EditorGUIUtility.standardVerticalSpacing;

        var dataAssetsProp = property.FindPropertyRelative("dataAssets");
        var dataAssetsNamesProp = property.FindPropertyRelative("names");
        if (dataAssetsProp != null && dataAssetsNamesProp != null)
        {
            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < dataAssetsProp.arraySize; i++)
            {
                y = DrawDataAssetSection(
                    position.x,
                    width,
                    y,
                    dataAssetsProp.GetArrayElementAtIndex(i),
                    formulaProp,
                    $"{property.propertyPath}.dataAssets[{i}]",
                    dataAssetsNamesProp.GetArrayElementAtIndex(i).stringValue);
            }
        }

        if (GlobalParameters.Instance != null)
        {
            y = DrawDataAssetSectionWithData(position.x, width, y, "GlobalParameters", GlobalParameters.Instance, formulaProp, $"{property.propertyPath}.globalParameters");
        }


        EditorGUI.EndProperty();
    }

    private IFormulaData GetObjectFromProperty(SerializedProperty property)
    {
        if (property == null)
        {
            Debug.LogError("Property is null");
            return null;
        }
        if (property.objectReferenceValue == null)
        {
            Debug.LogError("Object reference is null");
            return null;
        }
        if (property.objectReferenceValue is not IFormulaData formulaData)
        {
            Debug.LogError("Object is not IFormulaData");
            return null;
        }
        return formulaData;
    }

    private float DrawDataAssetSection(float x, float width, float y, SerializedProperty dataReference, SerializedProperty formulaProp, string sectionKey, string dataName)
    {
        var formulaData = GetObjectFromProperty(dataReference);
        return DrawDataAssetSectionWithData(x, width, y, dataName, formulaData, formulaProp, sectionKey);
    }

    private float DrawDataAssetSectionWithData(float x, float width, float y, string title, IFormulaData dataReference, SerializedProperty formulaProp, string sectionKey)
    {
        var headerRect = new Rect(x, y, width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(headerRect, title, EditorStyles.boldLabel);
        y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        if (dataReference == null)
        {
            var message = "Объект не реализует IFormulaData или отсутствует.";
            var helpHeight = GetHelpBoxHeight(message, width);
            EditorGUI.HelpBox(new Rect(x, y, width, helpHeight), message, MessageType.Warning);
            y += helpHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        var dropdownKey = sectionKey + ".parameters";
        var expanded = GetFoldoutState(dropdownKey, false);
        var foldoutRect = new Rect(x, y, width, EditorGUIUtility.singleLineHeight);
        var newExpanded = EditorGUI.Foldout(foldoutRect, expanded, "Параметры", true);
        if (newExpanded != expanded)
            SetFoldoutState(dropdownKey, newExpanded);
        y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        if (newExpanded && dataReference != null)
        {
            var parameters = dataReference.GetParameters();
            if (parameters != null && parameters.Count > 0)
            {
                var buttonBaseY = y;
                var buttonWidth = (width - (ButtonsPerRow - 1) * EditorGUIUtility.standardVerticalSpacing) / ButtonsPerRow;
                int validButtons = 0;
                for (int index = 0; index < parameters.Count; index++)
                {
                    var parameter = parameters[index];
                    var itemName = GetParameterName(title, parameter.name);
                    if (string.IsNullOrEmpty(itemName) || itemName == "")
                        continue;
                    var row = validButtons / ButtonsPerRow;
                    var column = validButtons % ButtonsPerRow;
                    validButtons++;
                    var buttonRect = new Rect(x + column * (buttonWidth + EditorGUIUtility.standardVerticalSpacing),
                                               buttonBaseY + row * (ButtonHeight + EditorGUIUtility.standardVerticalSpacing),
                                               buttonWidth,
                                               ButtonHeight);
                    if (GUI.Button(buttonRect, itemName))
                    {
                        if (formulaProp != null)
                            formulaProp.stringValue = AppendParameterName(formulaProp.stringValue, itemName);
                    }
                }

                var rows = Mathf.CeilToInt(validButtons / (float)ButtonsPerRow);
                y = buttonBaseY + rows * (ButtonHeight + EditorGUIUtility.standardVerticalSpacing);
            }
        }

        EditorGUI.DrawRect(new Rect(x, y, width, DividerHeight), Color.gray);
        y += DividerHeight + EditorGUIUtility.standardVerticalSpacing;
        return y;
    }




    // ------------------------------------------------------------------------




    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = EditorGUIUtility.singleLineHeight;
        var expanded = GetFoldoutState(property.propertyPath, property.isExpanded);
        property.isExpanded = expanded;
        if (!expanded)
            return height;

        var width = GetInspectorWidth();

        var formulaProp = property.FindPropertyRelative("formula");
        height += EditorGUIUtility.standardVerticalSpacing;
        height += GetTextAreaHeight(formulaProp, width, MinFormulaHeight);

        var instructionProp = property.FindPropertyRelative("instruction");
        height += EditorGUIUtility.standardVerticalSpacing;
        height += GetTextAreaHeight(instructionProp, width, MinInstructionHeight);

        height += EditorGUIUtility.standardVerticalSpacing;
        height += DividerHeight + EditorGUIUtility.standardVerticalSpacing;

        var dataAssetsProp = property.FindPropertyRelative("dataAssets");
        if (dataAssetsProp != null)
        {
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            for (int i = 0; i < dataAssetsProp.arraySize; i++)
            {
                height += GetDataAssetHeight(width, dataAssetsProp.GetArrayElementAtIndex(i), $"{property.propertyPath}.dataAssets[{i}]");
            }
        }

        if (GlobalParameters.Instance != null)
        {
            height += GetDataAssetHeightWithData(width, GlobalParameters.Instance, $"{property.propertyPath}.globalParameters");
        }

        return height;
    }

    private float GetDataAssetHeight(float width, SerializedProperty dataAssetProp, string sectionKey)
    {
        var formulaData = GetObjectFromProperty(dataAssetProp);
        return GetDataAssetHeightWithData(width, formulaData, sectionKey);
    }

    private float GetDataAssetHeightWithData(float width, IFormulaData dataReference, string sectionKey)
    {
        var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        var dropdownKey = sectionKey + ".parameters";
        if (GetFoldoutState(dropdownKey, false) && dataReference != null)
        {
            var parameters = dataReference.GetParameters();
            if (parameters != null && parameters.Count > 0)
            {
                var rows = Mathf.CeilToInt(parameters.Count / (float)ButtonsPerRow);
                height += rows * (ButtonHeight + EditorGUIUtility.standardVerticalSpacing);
            }
        }
        height += DividerHeight + EditorGUIUtility.standardVerticalSpacing;
        return height;
    }
    private float DrawTextAreaSection(float x, float width, float y, SerializedProperty property, float minHeight)
    {
        if (property == null)
            return y;

        var contentHeight = Mathf.Max(minHeight, CalcTextHeight(property.stringValue, width));
        var rect = new Rect(x, y, width, contentHeight);
        property.stringValue = EditorGUI.TextArea(rect, property.stringValue, TextAreaStyle);
        y += contentHeight + EditorGUIUtility.standardVerticalSpacing;
        return y;
    }
    private static float GetTextAreaHeight(SerializedProperty property, float width, float minHeight)
    {
        if (property == null)
            return minHeight;
        return Mathf.Max(minHeight, CalcTextHeight(property.stringValue, width));
    }

    private static float CalcTextHeight(string text, float width)
    {
        return TextAreaStyle.CalcHeight(new GUIContent(text ?? string.Empty), width);
    }

    private static float GetInspectorWidth()
    {
        var width = EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - 24f;
        return Mathf.Max(220f, width);
    }

    private static float GetHelpBoxHeight(string message, float width)
    {
        return EditorStyles.helpBox.CalcHeight(new GUIContent(message), width);
    }

    private static bool GetFoldoutState(string key, bool defaultValue)
    {
        if (FoldoutStates.TryGetValue(key, out var state))
            return state;
        FoldoutStates[key] = defaultValue;
        return defaultValue;
    }

    private static void SetFoldoutState(string key, bool value)
    {
        FoldoutStates[key] = value;
    }

    private static string AppendParameterName(string current, string addition)
    {
        if (string.IsNullOrWhiteSpace(current))
            return addition;
        return $"{current} {addition}";
    }

    private static string GetParameterName(string title, string name)
    {
        if (string.IsNullOrEmpty(name) || name == "")
            return "";
        return title.ToLower()[0] + "_" + name;
    }
}