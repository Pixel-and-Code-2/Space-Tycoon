using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WireEnemyStatsAndStaminaPreview
{
    const string PrefabStatus = "Assets/Prefabs/PawnUIStatus.prefab";
    const string PrefabController = "Assets/Prefabs/PawnUIController.prefab";

    [MenuItem("Space-Tycoon/Wire Enemy Stats + Stamina Preview")]
    public static void Run()
    {
        int wiredPreview = WireStaminaPreviewInPrefab();
        string statsMsg = WireStatsPanelInControllerPrefab();
        WireSceneInstances();
        AssetDatabase.SaveAssets();
        Debug.Log("[Wire] staminaPreview=" + wiredPreview + " | " + statsMsg);
    }

    static int WireStaminaPreviewInPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabStatus);
        int n = 0;
        try
        {
            SliderToPawnConnector[] connectors = root.GetComponentsInChildren<SliderToPawnConnector>(true);
            for (int i = 0; i < connectors.Length; i++)
            {
                if (WireConnector(connectors[i]))
                {
                    n++;
                    EditorUtility.SetDirty(connectors[i]);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabStatus);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return n;
    }

    static bool WireConnector(SliderToPawnConnector connector)
    {
        SerializedObject so = new SerializedObject(connector);
        SerializedProperty previewProp = so.FindProperty("allyStaminaPreviewSlider");
        if (previewProp == null) return false;

        SliderController preview = null;
        Transform t = connector.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            Transform child = t.GetChild(i);
            if (child.name != "Stamina Cost preview") continue;
            preview = child.GetComponent<SliderController>();
            break;
        }
        if (preview == null) return false;

        Slider slider = preview.GetComponent<Slider>();
        if (slider != null)
        {
            slider.interactable = false;
            slider.direction = Slider.Direction.RightToLeft;
            EditorUtility.SetDirty(slider);
        }

        SerializedObject pso = new SerializedObject(preview);
        SerializedProperty bgProp = pso.FindProperty("backgroundImage");
        if (bgProp != null && bgProp.objectReferenceValue is Image bg)
        {
            Color c = bg.color;
            c.a = 0f;
            bg.color = c;
            EditorUtility.SetDirty(bg);
        }

        preview.gameObject.SetActive(false);
        previewProp.objectReferenceValue = preview;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    static string WireStatsPanelInControllerPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabController);
        try
        {
            Transform stats = FindDeep(root.transform, "Stats panel");
            if (stats == null) return "Stats panel NOT FOUND in controller prefab";

            TextMeshProUGUI title = EnsureTmpChild(stats, "Title", 18f, FontStyles.Bold);
            TextMeshProUGUI body = EnsureTmpChild(stats, "Stats", 14f, FontStyles.Normal);

            VerticalLayoutGroup vlg = stats.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.padding = new RectOffset(8, 8, 8, 8);
                vlg.spacing = 4f;
                vlg.childForceExpandHeight = false;
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;
                EditorUtility.SetDirty(vlg);
            }

            Transform gameUi = FindDeep(root.transform, "GameUI");
            if (gameUi == null) return "GameUI NOT FOUND";

            EnemyInfoPanel panel = gameUi.GetComponent<EnemyInfoPanel>();
            if (panel == null)
                panel = gameUi.gameObject.AddComponent<EnemyInfoPanel>();

            SerializedObject so = new SerializedObject(panel);
            so.FindProperty("rootSlot").objectReferenceValue = stats.gameObject;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("statsText").objectReferenceValue = body;
            so.ApplyModifiedPropertiesWithoutUndo();
            stats.gameObject.SetActive(false);
            EditorUtility.SetDirty(panel);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabController);
            return "EnemyInfoPanel wired on GameUI";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void WireSceneInstances()
    {
        SliderToPawnConnector[] connectors = Object.FindObjectsByType<SliderToPawnConnector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < connectors.Length; i++)
        {
            if (WireConnector(connectors[i]))
                EditorUtility.SetDirty(connectors[i]);
        }

        GameUI gameUi = Object.FindFirstObjectByType<GameUI>();
        if (gameUi == null) return;
        Transform stats = FindDeep(gameUi.transform, "Stats panel");
        if (stats == null) return;

        TextMeshProUGUI title = EnsureTmpChild(stats, "Title", 18f, FontStyles.Bold);
        TextMeshProUGUI body = EnsureTmpChild(stats, "Stats", 14f, FontStyles.Normal);
        VerticalLayoutGroup vlg = stats.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 4f;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            EditorUtility.SetDirty(vlg);
        }

        EnemyInfoPanel panel = gameUi.GetComponent<EnemyInfoPanel>();
        if (panel == null)
            panel = gameUi.gameObject.AddComponent<EnemyInfoPanel>();
        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("rootSlot").objectReferenceValue = stats.gameObject;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("statsText").objectReferenceValue = body;
        so.ApplyModifiedPropertiesWithoutUndo();
        stats.gameObject.SetActive(false);
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(gameUi.gameObject);
    }

    static TextMeshProUGUI EnsureTmpChild(Transform parent, string name, float fontSize, FontStyles style)
    {
        Transform existing = parent.Find(name);
        GameObject go;
        if (existing != null)
            go = existing.gameObject;
        else
        {
            go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
        }

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            tmp = go.AddComponent<TextMeshProUGUI>();

        ContentSizeFitter csf = go.GetComponent<ContentSizeFitter>();
        if (csf == null)
            csf = go.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, fontSize + 8f);

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font != null)
            tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        tmp.text = name == "Title" ? "Enemy" : "stats";
        EditorUtility.SetDirty(tmp);
        EditorUtility.SetDirty(go);
        return tmp;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
