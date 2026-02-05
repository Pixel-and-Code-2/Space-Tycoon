using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParameteredScriptableObject), true)]
public class ParameteredScriptableObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();

        ParameteredScriptableObject data = (ParameteredScriptableObject)target;
        if (changed)
        {
            data.SetDirty();
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Update dictionary"))
        {
            data.SetDirty();
            data.RebuildParametersDict();
            EditorUtility.SetDirty(data);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Current parameters state (there are some predefined parameters, used in code directly, so if you see duplicates RENAME YOUR PROPERTY to the duplicates name):", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(data.GetParametersDictState(), MessageType.None);
    }
}