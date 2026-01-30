using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GlobalParameters))]
public class GlobalParametersEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GlobalParameters data = (GlobalParameters)target;

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Update dictionary"))
        {
            data.RebuildParametersDict();
            EditorUtility.SetDirty(data);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Current parameters state (there are some predefined parameters, used in code directly, so if you see duplicates RENAME YOUR PROPERTY to the duplicates name):", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(data.GetParametersDictState(), MessageType.None);
    }
}