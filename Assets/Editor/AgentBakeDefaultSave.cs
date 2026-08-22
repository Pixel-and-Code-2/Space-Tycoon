using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class AgentBakeDefaultSave
{
    const string PendingKey = "SpaceTycoon_BakeDefaultSave_Pending";
    const string StartLayerKey = "SpaceTycoon_BakeDefaultSave_StartLayer";

    const string UiControllerPath = "MainComponent/PawnUIController/CanvasUI";
    const string WallClickablePath = "MainComponent/M_ALL_RELEASE/WallWithWires_LVL0_CLICKABLE";
    const string WallObjectName = "WallWithWires_LVL0_CLICKABLE";
    const string SaveHubPath = "MainComponent";

    [MenuItem("Space Tycoon/Bake Default Save (Play Mode) %&b")]
    public static void RunFromMenu()
    {
        string result = Run();
        if (result == "play mode started, save runs automatically")
            Debug.Log("AgentBakeDefaultSave: started — save через 0.5 c после Play");
        else
            Debug.LogError("AgentBakeDefaultSave: " + result);
    }

    public static string Run()
    {
        Debug.Log("AgentBakeDefaultSave: Run()");

        if (EditorApplication.isPlaying)
            return "already in play mode";

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return "Unity still compiling — подожди окончания и запусти снова";

        if (!PrepareScene())
            return "prepare failed — см. Console (красные ошибки AgentBakeDefaultSave)";

        EditorPrefs.SetBool(PendingKey, true);
        EditorApplication.EnterPlaymode();
        return "play mode started, save runs automatically";
    }

    static bool BumpSaveVersion()
    {
        var hubGo = GameObject.Find(SaveHubPath);
        if (hubGo == null)
        {
            Debug.LogError("AgentBakeDefaultSave: not found " + SaveHubPath);
            return false;
        }

        var hub = hubGo.GetComponent<SaveHub>();
        if (hub == null)
        {
            Debug.LogError("AgentBakeDefaultSave: SaveHub missing on MainComponent");
            return false;
        }

        var so = new SerializedObject(hub);
        var versionProp = so.FindProperty("currSavingsVersion");
        int oldVersion = versionProp.intValue;
        versionProp.intValue = oldVersion + 1;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(hub);
        Debug.Log("AgentBakeDefaultSave: curr savings version " + oldVersion + " -> " + (oldVersion + 1));
        return true;
    }

    static GameObject FindByHierarchyPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        string[] parts = path.Split('/');
        var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        Transform current = null;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name != parts[0]) continue;
            current = roots[i].transform;
            break;
        }
        if (current == null)
        {
            var rootGo = GameObject.Find(parts[0]);
            if (rootGo == null) return null;
            current = rootGo.transform;
        }
        for (int i = 1; i < parts.Length; i++)
        {
            current = current.Find(parts[i]);
            if (current == null) return null;
        }
        return current.gameObject;
    }

    static bool EnableWallBoxColliderInScene()
    {
        var wallGo = FindByHierarchyPath(WallClickablePath);
        if (wallGo == null)
        {
            Debug.LogError("AgentBakeDefaultSave: not found " + WallClickablePath);
            return false;
        }

        var box = wallGo.GetComponent<BoxCollider>();
        if (box == null)
        {
            Debug.LogError("AgentBakeDefaultSave: BoxCollider missing on " + WallClickablePath);
            return false;
        }

        var so = new SerializedObject(box);
        so.FindProperty("m_Enabled").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(wallGo);
        Debug.Log("AgentBakeDefaultSave: scene BoxCollider ON (WallWithWires_LVL0_CLICKABLE)");
        return true;
    }

    static void EnableWallBoxColliderAtRuntime()
    {
        var wallGo = GameObject.Find(WallObjectName);
        if (wallGo == null)
        {
            Debug.LogError("AgentBakeDefaultSave: runtime not found " + WallObjectName);
            return;
        }

        var box = wallGo.GetComponent<BoxCollider>();
        if (box == null)
        {
            Debug.LogError("AgentBakeDefaultSave: runtime BoxCollider missing");
            return;
        }

        box.enabled = true;
        var clickable = wallGo.GetComponent<ClickableItem>();
        if (clickable != null)
        {
            var col = wallGo.GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }
        Debug.Log("AgentBakeDefaultSave: runtime BoxCollider ON before MakeSave");
    }

    static bool PrepareScene()
    {
        if (!BumpSaveVersion())
            return false;

        var uiGo = FindByHierarchyPath(UiControllerPath);
        if (uiGo == null)
        {
            Debug.LogError("AgentBakeDefaultSave: not found " + UiControllerPath);
            return false;
        }

        var layers = uiGo.GetComponent<UILayersController>();
        if (layers == null)
        {
            Debug.LogError("AgentBakeDefaultSave: UILayersController missing on CanvasUI");
            return false;
        }

        var so = new SerializedObject(layers);
        var startLayerProp = so.FindProperty("startLayer");
        EditorPrefs.SetInt(StartLayerKey, startLayerProp.intValue);
        startLayerProp.intValue = (int)UILayersController.UILayer.GameUI;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!EnableWallBoxColliderInScene())
            return false;

        EditorSceneManager.MarkSceneDirty(uiGo.scene);
        return true;
    }

    [InitializeOnLoadMethod]
    static void HookPlayMode()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static double bakeReadyAt = 0;
    const double BakeDelaySeconds = 0.5;

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode && EditorPrefs.GetBool(PendingKey, false))
        {
            bakeReadyAt = EditorApplication.timeSinceStartup + BakeDelaySeconds;
            EditorApplication.update -= BakeAfterDelay;
            EditorApplication.update += BakeAfterDelay;
            Debug.Log("AgentBakeDefaultSave: Play Mode — жду " + BakeDelaySeconds + " c");
        }

        if (change == PlayModeStateChange.EnteredEditMode)
            RestoreSceneAfterBake();
    }

    static void BakeAfterDelay()
    {
        if (!EditorApplication.isPlaying || !EditorPrefs.GetBool(PendingKey, false))
        {
            EditorApplication.update -= BakeAfterDelay;
            return;
        }

        if (SaveHub.Instance == null)
            return;

        if (EditorApplication.timeSinceStartup < bakeReadyAt)
            return;

        EditorApplication.update -= BakeAfterDelay;
        EditorPrefs.SetBool(PendingKey, false);

        EnableWallBoxColliderAtRuntime();
        SaveHub.Instance.MakeSave();
        SaveHub.Instance.ShowLastSavedData();
        Debug.Log("AgentBakeDefaultSave: defaultSave.dat written to StreamingAssets");

        EditorApplication.ExitPlaymode();
    }

    static void RestoreSceneAfterBake()
    {
        bakeReadyAt = 0;

        var uiGo = FindByHierarchyPath(UiControllerPath);
        if (uiGo != null)
        {
            var layers = uiGo.GetComponent<UILayersController>();
            if (layers != null && EditorPrefs.HasKey(StartLayerKey))
            {
                var so = new SerializedObject(layers);
                so.FindProperty("startLayer").intValue = EditorPrefs.GetInt(StartLayerKey);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EnableWallBoxColliderInScene();

        EditorPrefs.DeleteKey(StartLayerKey);
        Debug.Log("AgentBakeDefaultSave: done — сохрани сцену Main.unity (Ctrl+S)");
    }
}
