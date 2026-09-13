using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DiceLabMenu
{
    const string ScenePath = "Assets/Scenes/DiceFaceLab.unity";
    const string ConfigPath = "Assets/Resources/DiceShapeConfig_D10.asset";

    [MenuItem("Space-Tycoon/Dice Lab/Create Or Open Debug Scene")]
    public static void CreateOrOpen()
    {
        DiceShapeConfig config = AssetDatabase.LoadAssetAtPath<DiceShapeConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<DiceShapeConfig>();
            if (config.sticks == null || config.sticks.Count == 0)
            {
                config.sticks = new System.Collections.Generic.List<DiceShapeConfig.StickEntry>();
                for (int i = 0; i < 10; i++)
                {
                    float yaw = i * 36f;
                    config.sticks.Add(new DiceShapeConfig.StickEntry
                    {
                        localNormal = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward,
                        displayValue = i == 9 ? 0 : i + 1,
                        color = Color.HSVToRGB(i / 10f, 0.8f, 1f)
                    });
                }
            }
            DiceFaceMap prefab = AssetDatabase.LoadAssetAtPath<DiceFaceMap>("Assets/Prefabs/DiceD10_Test.prefab");
            if (prefab != null) config.diePrefab = prefab;
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        GameObject root = new GameObject("DiceFaceLab");
        DiceFaceDebugLab lab = root.AddComponent<DiceFaceDebugLab>();
        SerializedObject so = new SerializedObject(lab);
        so.FindProperty("config").objectReferenceValue = config;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(2f, 1f, 2f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
        Debug.Log(
            "Dice lab ready. Open Assets/Scenes/DiceFaceLab.unity, select "
            + ConfigPath
            + ", tune sticks (normals/euler/values), Play, then copy faces to DiceD10_Test.");
    }
}
