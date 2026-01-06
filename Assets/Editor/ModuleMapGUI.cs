using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ModuleMap))]
public class ModuleMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ModuleMap modulemap = (ModuleMap)target;

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Send Pawns to target", GUILayout.Height(30)))
        {
            modulemap.SendPawnsToTarget();
        }
    }
}