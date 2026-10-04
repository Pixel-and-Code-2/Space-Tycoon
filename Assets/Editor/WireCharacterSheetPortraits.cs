using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WireCharacterSheetPortraits
{
    const string PortraitDir = "Assets/Media/Portraits";

    [MenuItem("Space-Tycoon/Wire Character Sheet Portraits")]
    public static void Run()
    {
        ImportSprites();
        Sprite pistol = LoadSprite("PISTOL");
        Sprite rabbit = LoadSprite("RABBIT");
        Sprite sniper = LoadSprite("SNIPER");

        CharacterSheetUI sheet = Object.FindFirstObjectByType<CharacterSheetUI>(FindObjectsInactive.Include);
        if (sheet == null)
        {
            Debug.LogError("[WirePortraits] CharacterSheetUI not found");
            return;
        }

        Transform left = sheet.transform.Find("Body/LeftPage");
        if (left == null)
        {
            Debug.LogError("[WirePortraits] LeftPage not found");
            return;
        }

        Image portrait = EnsurePortraitImage(left);
        SerializedObject so = new SerializedObject(sheet);
        so.FindProperty("portrait").objectReferenceValue = portrait;

        SerializedProperty pages = so.FindProperty("pages");
        for (int i = 0; i < pages.arraySize; i++)
        {
            SerializedProperty page = pages.GetArrayElementAtIndex(i);
            string name = page.FindPropertyRelative("displayName").stringValue;
            if (string.IsNullOrEmpty(name))
            {
                Object pawn = page.FindPropertyRelative("pawn").objectReferenceValue;
                if (pawn != null) name = pawn.name;
            }
            Sprite sprite = PickSprite(i, name, pistol, rabbit, sniper);
            page.FindPropertyRelative("portraitSprite").objectReferenceValue = sprite;
            Debug.Log("[WirePortraits] page " + i + " '" + name + "' -> " + (sprite != null ? sprite.name : "null"));
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(sheet);
        PrefabUtility.RecordPrefabInstancePropertyModifications(sheet);

        string prefabPath = "Assets/Prefabs/PawnUIController.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            CharacterSheetUI prefabSheet = prefabRoot.GetComponentInChildren<CharacterSheetUI>(true);
            if (prefabSheet != null)
            {
                Transform prefabLeft = prefabSheet.transform.Find("Body/LeftPage");
                Image prefabPortrait = EnsurePortraitImage(prefabLeft);
                SerializedObject pso = new SerializedObject(prefabSheet);
                pso.FindProperty("portrait").objectReferenceValue = prefabPortrait;
                SerializedProperty ppages = pso.FindProperty("pages");
                for (int i = 0; i < ppages.arraySize; i++)
                {
                    SerializedProperty page = ppages.GetArrayElementAtIndex(i);
                    string name = page.FindPropertyRelative("displayName").stringValue;
                    if (string.IsNullOrEmpty(name))
                    {
                        Object pawn = page.FindPropertyRelative("pawn").objectReferenceValue;
                        if (pawn != null) name = pawn.name;
                    }
                    page.FindPropertyRelative("portraitSprite").objectReferenceValue =
                        PickSprite(i, name, pistol, rabbit, sniper);
                }
                pso.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[WirePortraits] done");
    }

    static void ImportSprites()
    {
        string[] names = { "PISTOL", "RABBIT", "SNIPER" };
        for (int i = 0; i < names.Length; i++)
        {
            string path = PortraitDir + "/" + names[i] + ".png";
            AssetDatabase.ImportAsset(path);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
    }

    static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(PortraitDir + "/" + name + ".png");
    }

    static Sprite PickSprite(int pageIndex, string name, Sprite pistol, Sprite rabbit, Sprite sniper)
    {
        string n = (name ?? "").ToLowerInvariant();
        // Pages: 0 З.А.Я=RABBIT, 1 Гусев=SNIPER, 2 Зелёный=PISTOL
        if (n.Contains("zaya") || n.Contains("rabbit") || n.Contains("зая") || n.Contains("з.а.я") || pageIndex == 0)
            return rabbit;
        if (n.Contains("gusev") || n.Contains("гусев") || n.Contains("sniper") || n.Contains("rifle") || pageIndex == 1)
            return sniper;
        if (n.Contains("zelen") || n.Contains("зелён") || n.Contains("зелен") || n.Contains("pistol") || pageIndex == 2)
            return pistol;
        return pistol;
    }

    static Image EnsurePortraitImage(Transform left)
    {
        if (left == null) return null;
        Image keep = null;
        for (int i = left.childCount - 1; i >= 0; i--)
        {
            Transform ch = left.GetChild(i);
            if (ch.name != "Portrait") continue;
            Image img = ch.GetComponent<Image>();
            if (img == null) continue;
            if (keep == null) keep = img;
            else Object.DestroyImmediate(ch.gameObject);
        }
        GameObject go;
        if (keep != null)
            go = keep.gameObject;
        else
        {
            go = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(left, false);
            go.transform.SetSiblingIndex(0);
        }
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(433f, 433f);
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 433f;
        le.preferredHeight = 433f;
        le.minWidth = 433f;
        le.minHeight = 433f;
        Image image = go.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        EditorUtility.SetDirty(go);
        return image;
    }
}
