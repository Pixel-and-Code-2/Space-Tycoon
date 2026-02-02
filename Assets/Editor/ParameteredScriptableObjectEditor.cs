using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParameteredScriptableObject), true)]
public class ParameteredScriptableObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.name == "m_Script") continue;

            bool isContext = serializedObject.FindProperty("isContext").boolValue;
            if (isContext && iterator.name == "mustHaveParameters") continue;

            EditorGUILayout.PropertyField(iterator, true);
        }

        serializedObject.ApplyModifiedProperties();

        ParameteredScriptableObject data = (ParameteredScriptableObject)target;

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