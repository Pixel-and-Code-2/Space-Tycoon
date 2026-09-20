using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(UnitSpotBinder))]
public class UnitSpotBinderEditor : Editor
{
    static readonly HashSet<int> Pending = new HashSet<int>();
    static readonly Regex EnemiesLvlRx = new Regex(@"^Enemies_lvl\d+$", RegexOptions.Compiled);
    static readonly Regex RoomRx = new Regex(@"^Room\d+$", RegexOptions.Compiled);
    static readonly Regex DTrigRx = new Regex(@"^DTrig\d+$", RegexOptions.Compiled);
    static readonly Regex PosRx = new Regex(@"^Pos(\d+)$", RegexOptions.Compiled);
    static readonly Regex LvlInPathRx = new Regex(@"Lvl(\d+)", RegexOptions.Compiled);
    static readonly Regex DelayedLvlRx = new Regex(@"[/\\]lvl(\d+)[/\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        UnitSpotBinder binder = (UnitSpotBinder)target;
        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Bind / Refresh Now"))
            Bind(binder, force: true);
        if (binder.Bound && binder.SpawnedInstance != null)
            EditorGUILayout.HelpBox("Bound instance: " + binder.SpawnedInstance.name, MessageType.Info);
        else if (binder.SpotMode == UnitSpotBinder.Mode.Quarantine && binder.Bound)
            EditorGUILayout.HelpBox("Quarantine spawn point registered (no scene instance).", MessageType.Info);
    }

    void OnEnable()
    {
        EditorApplication.delayCall += TryValidateTarget;
    }

    void TryValidateTarget()
    {
        if (target == null) return;
        UnitSpotBinder binder = target as UnitSpotBinder;
        if (binder == null) return;
        Bind(binder, force: false);
    }

    [InitializeOnLoadMethod]
    static void HookValidate()
    {
        UnitSpotBinder.EditorValidateHook = QueueBind;
        ObjectFactory.componentWasAdded -= OnComponentAdded;
        ObjectFactory.componentWasAdded += OnComponentAdded;
    }

    static void OnComponentAdded(Component c)
    {
        UnitSpotBinder binder = c as UnitSpotBinder;
        if (binder == null) return;
        QueueBind(binder);
    }

    static void QueueBind(UnitSpotBinder binder)
    {
        if (binder == null) return;
        int id = binder.GetInstanceID();
        if (!Pending.Add(id)) return;
        EditorApplication.delayCall += () =>
        {
            Pending.Remove(id);
            if (binder == null) return;
            Bind(binder, force: false);
        };
    }

    public static void Bind(UnitSpotBinder binder, bool force)
    {
        if (binder == null) return;
        if (Application.isPlaying) return;
        if (PrefabUtility.IsPartOfPrefabAsset(binder.gameObject)) return;
        if (binder.EnemyPrefab == null)
        {
            if (force)
                Debug.LogWarning("[UnitSpotBinder] enemyPrefab is null on " + binder.name);
            return;
        }
        if (binder.Bound && !force && binder.SpotMode == UnitSpotBinder.Mode.SceneEnemy && binder.SpawnedInstance != null)
            return;
        if (binder.Bound && !force && binder.SpotMode == UnitSpotBinder.Mode.Quarantine)
            return;

        TurnManager turnManager = Object.FindFirstObjectByType<TurnManager>();
        if (turnManager == null)
        {
            Debug.LogWarning("[UnitSpotBinder] TurnManager not found");
            return;
        }

        SnapHeight(binder);

        if (binder.SpotMode == UnitSpotBinder.Mode.SceneEnemy)
            BindSceneEnemy(binder, turnManager);
        else
            BindQuarantine(binder, turnManager);

        EditorUtility.SetDirty(binder);
        EditorUtility.SetDirty(turnManager);
        EditorSceneManager.MarkSceneDirty(binder.gameObject.scene);
    }

    public static int BindAllSceneEnemiesByPosition()
    {
        if (Application.isPlaying) return 0;
        TurnManager turnManager = Object.FindFirstObjectByType<TurnManager>();
        if (turnManager == null)
        {
            Debug.LogWarning("[UnitSpotBinder] TurnManager not found");
            return 0;
        }

        int bound = 0;
        foreach (PawnBrain brain in Object.FindObjectsByType<PawnBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (brain == null) continue;
            if (!IsUnderEnemiesFolder(brain.transform)) continue;
            if (IsPlayerPawn(brain)) continue;
            if (BindExistingSceneEnemy(brain.gameObject, turnManager, null, null))
                bound++;
        }

        EditorUtility.SetDirty(turnManager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return bound;
    }

    public static bool BindExistingSceneEnemy(
        GameObject instance,
        TurnManager turnManager,
        WarFog warFogOverride,
        GameObject triggerObjectOverride)
    {
        if (instance == null || turnManager == null) return false;

        Vector3 pos = instance.transform.position;
        if (!Mathf.Approximately(pos.y, 0f))
        {
            Undo.RecordObject(instance.transform, "Snap enemy height to 0");
            instance.transform.position = new Vector3(pos.x, 0f, pos.z);
            pos = instance.transform.position;
        }

        TriggerData trigger = FindTriggerContaining(turnManager, pos, triggerObjectOverride);
        if (trigger == null)
            Debug.LogWarning("[UnitSpotBinder] no TriggerData covers " + instance.name);
        else
            RegisterInTrigger(trigger, instance, turnManager);

        WarFog fog = warFogOverride != null ? warFogOverride : FindWarFogContaining(pos);
        if (fog == null)
            Debug.LogWarning("[UnitSpotBinder] no WarFog covers " + instance.name + " @ " + pos);
        else
            RegisterInWarFog(fog, instance);

        EditorUtility.SetDirty(instance);
        return trigger != null || fog != null;
    }

    public static void RemoveSelected()
    {
        if (Application.isPlaying) return;
        GameObject[] selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("[UnitSpotBinder] nothing selected");
            return;
        }

        TurnManager turnManager = Object.FindFirstObjectByType<TurnManager>();
        var roots = new List<GameObject>();
        foreach (GameObject go in selected)
            CollectRemovable(go, roots);

        if (roots.Count == 0)
        {
            Debug.LogWarning("[UnitSpotBinder] nothing removable in selection");
            return;
        }

        Undo.SetCurrentGroupName("Remove Unit Spot Selected");
        int group = Undo.GetCurrentGroup();
        foreach (GameObject root in roots)
        {
            if (root == null) continue;
            UnregisterDependencies(root, turnManager);
            Undo.DestroyObjectImmediate(root);
        }
        Undo.CollapseUndoOperations(group);
        if (turnManager != null)
            EditorUtility.SetDirty(turnManager);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[UnitSpotBinder] removed " + roots.Count + " object(s)");
    }

    static void CollectRemovable(GameObject go, List<GameObject> roots)
    {
        if (go == null) return;

        UnitSpotBinder binder = go.GetComponentInParent<UnitSpotBinder>();
        if (binder != null)
        {
            AddUnique(roots, binder.SpawnedInstance);
            AddUnique(roots, binder.gameObject);
            return;
        }

        Transform root = ResolveRemovableRoot(go.transform);
        if (root == null) return;
        AddUnique(roots, root.gameObject);

        foreach (var other in Object.FindObjectsByType<UnitSpotBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (other == null || other.SpawnedInstance != root.gameObject) continue;
            AddUnique(roots, other.gameObject);
        }
    }

    static void AddUnique(List<GameObject> list, GameObject go)
    {
        if (go == null) return;
        if (!list.Contains(go)) list.Add(go);
    }

    static Transform ResolveRemovableRoot(Transform selection)
    {
        if (selection == null) return null;
        if (IsManagedContainer(selection.name)) return null;

        Transform child = selection;
        Transform parent = selection.parent;
        while (parent != null)
        {
            if (IsManagedContainer(parent.name))
                return child;
            child = parent;
            parent = parent.parent;
        }
        return null;
    }

    static bool IsManagedContainer(string name)
    {
        return EnemiesLvlRx.IsMatch(name) || RoomRx.IsMatch(name) || DTrigRx.IsMatch(name);
    }

    static bool IsUnderEnemiesFolder(Transform t)
    {
        while (t != null)
        {
            if (EnemiesLvlRx.IsMatch(t.name) || RoomRx.IsMatch(t.name))
                return true;
            t = t.parent;
        }
        return false;
    }

    public static bool IsPlayerPawn(PawnBrain brain)
    {
        if (brain == null) return true;
        SelectableType st = brain.GetSelectableType();
        if (st == SelectableType.Player) return true;
        string n = brain.gameObject.name;
        return n == "Zaya" || n == "Gusev" || n == "Zeleniy" || n == "Max" || n == "GAAD";
    }

    static void UnregisterDependencies(GameObject root, TurnManager turnManager)
    {
        if (root == null) return;

        foreach (var binder in Object.FindObjectsByType<UnitSpotBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (binder == null) continue;
            if (binder.gameObject == root)
                continue;
            if (binder.SpawnedInstance == root)
            {
                Undo.RecordObject(binder, "Clear UnitSpotBinder instance");
                binder.EditorSetBound(null, false);
                EditorUtility.SetDirty(binder);
            }
        }

        if (turnManager != null)
        {
            Undo.RecordObject(turnManager, "Unregister UnitSpot deps");
            RemoveFromTriggerLists(turnManager, root);
            RemoveFromDelayedSpawnPoints(turnManager, root);
        }

        foreach (var fog in Object.FindObjectsByType<WarFog>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            RemoveFromWarFog(fog, root);
    }

    static void RemoveFromTriggerLists(TurnManager turnManager, GameObject root)
    {
        SerializedObject so = new SerializedObject(turnManager);
        SerializedProperty list = so.FindProperty("listOfTriggers");
        PawnBrain[] brains = root.GetComponentsInChildren<PawnBrain>(true);
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty enemies = list.GetArrayElementAtIndex(i).FindPropertyRelative("enemies");
            for (int e = enemies.arraySize - 1; e >= 0; e--)
            {
                Object existing = enemies.GetArrayElementAtIndex(e).objectReferenceValue;
                if (existing == null)
                {
                    enemies.DeleteArrayElementAtIndex(e);
                    continue;
                }
                foreach (PawnBrain brain in brains)
                {
                    if (existing != brain) continue;
                    enemies.GetArrayElementAtIndex(e).objectReferenceValue = null;
                    enemies.DeleteArrayElementAtIndex(e);
                    break;
                }
            }
        }
        so.ApplyModifiedProperties();
    }

    static void RemoveFromDelayedSpawnPoints(TurnManager turnManager, GameObject root)
    {
        SerializedObject so = new SerializedObject(turnManager);
        SerializedProperty list = so.FindProperty("listOfDelayedTriggers");
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty points = list.GetArrayElementAtIndex(i).FindPropertyRelative("enemySpawnPoints");
            for (int p = points.arraySize - 1; p >= 0; p--)
            {
                Transform where = points.GetArrayElementAtIndex(p).FindPropertyRelative("where").objectReferenceValue as Transform;
                if (where == null)
                {
                    points.DeleteArrayElementAtIndex(p);
                    continue;
                }
                if (where != root.transform && !where.IsChildOf(root.transform))
                    continue;
                points.GetArrayElementAtIndex(p).FindPropertyRelative("where").objectReferenceValue = null;
                points.GetArrayElementAtIndex(p).FindPropertyRelative("enemy").objectReferenceValue = null;
                points.DeleteArrayElementAtIndex(p);
            }
        }
        so.ApplyModifiedProperties();
    }

    static void RemoveFromWarFog(WarFog fog, GameObject root)
    {
        if (fog == null) return;
        SerializedObject so = new SerializedObject(fog);
        SerializedProperty list = so.FindProperty("othersToInclude");
        bool changed = false;
        for (int i = list.arraySize - 1; i >= 0; i--)
        {
            GameObject go = list.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (go == null || go == root || go.transform.IsChildOf(root.transform))
            {
                list.DeleteArrayElementAtIndex(i);
                changed = true;
            }
        }
        if (!changed) return;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(fog);
    }

    static void SnapHeight(UnitSpotBinder binder)
    {
        if (!binder.SnapHeightToNavMesh) return;
        Vector3 p = binder.Anchor.position;
        if (Mathf.Approximately(p.y, 0f)) return;
        Undo.RecordObject(binder.transform, "Snap UnitSpot height to 0");
        binder.transform.position = new Vector3(p.x, 0f, p.z);
    }

    static void BindSceneEnemy(UnitSpotBinder binder, TurnManager turnManager)
    {
        TriggerData trigger = FindTriggerContaining(turnManager, binder.Anchor.position, binder.TriggerObjectOverride);
        Transform folder = ResolveSceneEnemyParent(turnManager, trigger);

        if (folder != null && binder.transform.parent != folder)
            Undo.SetTransformParent(binder.transform, folder, "Parent UnitSpot binder");

        GameObject instance = binder.SpawnedInstance;
        if (instance == null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(binder.EnemyPrefab);
            Undo.RegisterCreatedObjectUndo(instance, "Spawn UnitSpot enemy");
            instance.name = binder.EnemyPrefab.name + "_" + binder.name;
        }

        Undo.RecordObject(instance.transform, "Place UnitSpot enemy");
        if (folder != null && instance.transform.parent != folder)
            Undo.SetTransformParent(instance.transform, folder, "Parent UnitSpot enemy");
        Vector3 p = binder.Anchor.position;
        instance.transform.SetPositionAndRotation(new Vector3(p.x, 0f, p.z), binder.Anchor.rotation);

        ApplyCombatantStats(instance, binder.CombatantStats);
        ApplyAiOverwrite(instance, binder.AiOverwrite);
        BindExistingSceneEnemy(instance, turnManager, binder.WarFogOverride, binder.TriggerObjectOverride);

        binder.EditorSetBound(instance, true);
        EditorUtility.SetDirty(instance);
    }

    static Transform ResolveSceneEnemyParent(TurnManager turnManager, TriggerData trigger)
    {
        if (trigger == null) return null;

        Transform common = null;
        bool hasAny = false;
        if (trigger.enemies != null)
        {
            foreach (PawnBrain brain in trigger.enemies)
            {
                if (brain == null) continue;
                Transform parent = brain.transform.parent;
                if (!hasAny)
                {
                    common = parent;
                    hasAny = true;
                }
                else if (common != parent)
                {
                    common = null;
                    break;
                }
            }
        }
        if (common != null) return common;

        if (trigger.triggerObject == null) return null;
        Match m = LvlInPathRx.Match(GetHierarchyPath(trigger.triggerObject.transform));
        if (!m.Success) return null;
        GameObject folder = GameObject.Find("Enemies_lvl" + m.Groups[1].Value);
        return folder != null ? folder.transform : null;
    }

    static string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    static void BindQuarantine(UnitSpotBinder binder, TurnManager turnManager)
    {
        DelayedTriggerData delayed = FindDelayedTriggerContaining(turnManager, binder.Anchor.position, binder.TriggerObjectOverride);
        if (delayed == null)
            delayed = FindDelayedTriggerByHierarchy(turnManager, binder.Anchor);
        if (delayed == null)
        {
            Debug.LogWarning("[UnitSpotBinder] no DelayedTriggerData covers " + binder.name);
            return;
        }

        if (delayed.triggerObject != null)
        {
            Transform dtrig = delayed.triggerObject.transform;
            if (binder.transform.parent != dtrig)
                Undo.SetTransformParent(binder.transform, dtrig, "Parent UnitSpot to DTrig");
            if (!PosRx.IsMatch(binder.name))
            {
                Undo.RecordObject(binder.gameObject, "Rename UnitSpot Pos");
                binder.gameObject.name = NextPosName(dtrig);
            }
        }

        Vector3 p = binder.Anchor.position;
        if (!Mathf.Approximately(p.y, 0f))
        {
            Undo.RecordObject(binder.transform, "Snap quarantine Pos height");
            binder.transform.position = new Vector3(p.x, 0f, p.z);
        }

        UpsertDelayedSpawnPoint(turnManager, delayed, binder.Anchor, binder.EnemyPrefab, binder.AiOverwrite);
        binder.EditorSetBound(null, true);
    }

    public static void UpsertDelayedSpawnPoint(
        TurnManager turnManager,
        DelayedTriggerData delayed,
        Transform where,
        GameObject prefab,
        EnemyAiProfile aiOverwrite)
    {
        SerializedObject so = new SerializedObject(turnManager);
        SerializedProperty list = so.FindProperty("listOfDelayedTriggers");
        int delayedIndex = IndexOfDelayed(turnManager, delayed);
        if (delayedIndex < 0) return;
        SerializedProperty entry = list.GetArrayElementAtIndex(delayedIndex);
        SerializedProperty points = entry.FindPropertyRelative("enemySpawnPoints");

        int existing = FindSpawnPointIndex(points, where);
        SerializedProperty spot;
        if (existing >= 0)
            spot = points.GetArrayElementAtIndex(existing);
        else
        {
            points.InsertArrayElementAtIndex(points.arraySize);
            spot = points.GetArrayElementAtIndex(points.arraySize - 1);
        }
        spot.FindPropertyRelative("enemy").objectReferenceValue = prefab;
        spot.FindPropertyRelative("where").objectReferenceValue = where;
        SerializedProperty aiProp = spot.FindPropertyRelative("aiOverwrite");
        if (aiProp != null)
            aiProp.objectReferenceValue = aiOverwrite;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(turnManager);
    }

    public static GameObject ConvertSceneEnemyToDelayedPose(
        GameObject instance,
        TurnManager turnManager,
        DelayedTriggerData delayed,
        EnemyAiProfile aiOverwrite)
    {
        if (instance == null || turnManager == null || delayed == null || delayed.triggerObject == null)
            return null;

        GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(instance);
        if (prefab == null)
        {
            Debug.LogWarning("[UnitSpotBinder] no prefab source for " + instance.name);
            return null;
        }

        Transform dtrig = delayed.triggerObject.transform;
        GameObject posGo = new GameObject(NextPosName(dtrig));
        Undo.RegisterCreatedObjectUndo(posGo, "Create delayed Pos");
        Undo.SetTransformParent(posGo.transform, dtrig, "Parent delayed Pos");
        Vector3 p = instance.transform.position;
        posGo.transform.SetPositionAndRotation(new Vector3(p.x, 0f, p.z), instance.transform.rotation);

        UpsertDelayedSpawnPoint(turnManager, delayed, posGo.transform, prefab, aiOverwrite);
        UnregisterDependencies(instance, turnManager);
        Undo.DestroyObjectImmediate(instance);
        return posGo;
    }

    static string NextPosName(Transform dtrig)
    {
        int max = 0;
        for (int i = 0; i < dtrig.childCount; i++)
        {
            Match m = PosRx.Match(dtrig.GetChild(i).name);
            if (!m.Success) continue;
            int n;
            if (int.TryParse(m.Groups[1].Value, out n) && n > max)
                max = n;
        }
        return "Pos" + (max + 1);
    }

    static int IndexOfDelayed(TurnManager tm, DelayedTriggerData delayed)
    {
        var so = new SerializedObject(tm);
        var list = so.FindProperty("listOfDelayedTriggers");
        for (int i = 0; i < list.arraySize; i++)
        {
            var e = list.GetArrayElementAtIndex(i);
            var triggerObj = e.FindPropertyRelative("triggerObject").objectReferenceValue as GameObject;
            if (delayed.triggerObject == triggerObj) return i;
        }
        return -1;
    }

    static int FindSpawnPointIndex(SerializedProperty points, Transform where)
    {
        for (int i = 0; i < points.arraySize; i++)
        {
            var spot = points.GetArrayElementAtIndex(i);
            var w = spot.FindPropertyRelative("where").objectReferenceValue as Transform;
            if (w == where) return i;
        }
        return -1;
    }

    static void ApplyCombatantStats(GameObject instance, CombatantStats stats)
    {
        if (stats == null || instance == null) return;
        PawnDataController data = instance.GetComponent<PawnDataController>();
        if (data == null) data = instance.GetComponentInChildren<PawnDataController>(true);
        if (data == null) return;
        SerializedObject so = new SerializedObject(data);
        SerializedProperty prop = so.FindProperty("combatantStats");
        if (prop == null) return;
        prop.objectReferenceValue = stats;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
    }

    static void ApplyAiOverwrite(GameObject instance, EnemyAiProfile profile)
    {
        if (profile == null || instance == null) return;
        PawnDataController data = instance.GetComponent<PawnDataController>();
        if (data == null) data = instance.GetComponentInChildren<PawnDataController>(true);
        if (data == null) return;
        Undo.RecordObject(data, "Apply AI overwrite");
        data.SetAiProfileOverride(profile);
        EditorUtility.SetDirty(data);
    }

    static void RegisterInTrigger(TriggerData trigger, GameObject instance, TurnManager turnManager)
    {
        PawnBrain brain = instance.GetComponent<PawnBrain>();
        if (brain == null) brain = instance.GetComponentInChildren<PawnBrain>(true);
        if (brain == null) return;

        SerializedObject so = new SerializedObject(turnManager);
        SerializedProperty list = so.FindProperty("listOfTriggers");
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty entry = list.GetArrayElementAtIndex(i);
            GameObject triggerObj = entry.FindPropertyRelative("triggerObject").objectReferenceValue as GameObject;
            if (triggerObj != trigger.triggerObject) continue;
            SerializedProperty enemies = entry.FindPropertyRelative("enemies");
            for (int e = 0; e < enemies.arraySize; e++)
            {
                Object existing = enemies.GetArrayElementAtIndex(e).objectReferenceValue;
                if (existing == brain) return;
            }
            enemies.InsertArrayElementAtIndex(enemies.arraySize);
            enemies.GetArrayElementAtIndex(enemies.arraySize - 1).objectReferenceValue = brain;
            so.ApplyModifiedPropertiesWithoutUndo();
            return;
        }
    }

    static void RegisterInWarFog(WarFog fog, GameObject instance)
    {
        SerializedObject so = new SerializedObject(fog);
        SerializedProperty list = so.FindProperty("othersToInclude");
        for (int i = 0; i < list.arraySize; i++)
        {
            GameObject go = list.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (go == instance) return;
        }
        list.InsertArrayElementAtIndex(list.arraySize);
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = instance;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(fog);
    }

    static TriggerData FindTriggerContaining(TurnManager tm, Vector3 pos, GameObject overrideTrigger)
    {
        if (overrideTrigger != null)
        {
            var so = new SerializedObject(tm);
            var list = so.FindProperty("listOfTriggers");
            for (int i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                var triggerObj = entry.FindPropertyRelative("triggerObject").objectReferenceValue as GameObject;
                if (triggerObj == overrideTrigger)
                    return GetTriggerAt(tm, i);
            }
        }

        var soAll = new SerializedObject(tm);
        var triggers = soAll.FindProperty("listOfTriggers");
        TriggerData best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < triggers.arraySize; i++)
        {
            var entry = triggers.GetArrayElementAtIndex(i);
            var triggerObj = entry.FindPropertyRelative("triggerObject").objectReferenceValue as GameObject;
            if (ContainsPointXZ(triggerObj, pos))
                return GetTriggerAt(tm, i);
            Collider col = triggerObj != null ? triggerObj.GetComponent<Collider>() : null;
            if (col == null) continue;
            float dist = HorizontalDistance(col.bounds.center, pos);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = GetTriggerAt(tm, i);
            }
        }
        if (best != null && bestDist <= 12f)
            return best;
        return null;
    }

    static DelayedTriggerData FindDelayedTriggerContaining(TurnManager tm, Vector3 pos, GameObject overrideTrigger)
    {
        var so = new SerializedObject(tm);
        var list = so.FindProperty("listOfDelayedTriggers");
        for (int i = 0; i < list.arraySize; i++)
        {
            var entry = list.GetArrayElementAtIndex(i);
            var triggerObj = entry.FindPropertyRelative("triggerObject").objectReferenceValue as GameObject;
            if (overrideTrigger != null)
            {
                if (triggerObj == overrideTrigger)
                    return GetDelayedAt(tm, i);
                continue;
            }
            if (ContainsPointXZ(triggerObj, pos))
                return GetDelayedAt(tm, i);
        }
        return FindClosestDelayedByPosChildren(tm, pos);
    }

    static DelayedTriggerData FindDelayedTriggerByHierarchy(TurnManager tm, Transform t)
    {
        while (t != null)
        {
            if (DTrigRx.IsMatch(t.name))
            {
                var so = new SerializedObject(tm);
                var list = so.FindProperty("listOfDelayedTriggers");
                for (int i = 0; i < list.arraySize; i++)
                {
                    var triggerObj = list.GetArrayElementAtIndex(i).FindPropertyRelative("triggerObject").objectReferenceValue as GameObject;
                    if (triggerObj == t.gameObject)
                        return GetDelayedAt(tm, i);
                }
            }
            t = t.parent;
        }
        return null;
    }

    static DelayedTriggerData FindClosestDelayedByPosChildren(TurnManager tm, Vector3 pos)
    {
        DelayedTriggerData best = null;
        float bestDist = float.MaxValue;
        var field = typeof(TurnManager).GetField("listOfDelayedTriggers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = field.GetValue(tm) as List<DelayedTriggerData>;
        if (list == null) return null;
        for (int i = 0; i < list.Count; i++)
        {
            DelayedTriggerData delayed = list[i];
            if (delayed == null || delayed.triggerObject == null) continue;
            Transform dtrig = delayed.triggerObject.transform;
            if (dtrig.childCount == 0) continue;
            Vector3 sum = Vector3.zero;
            int n = 0;
            for (int c = 0; c < dtrig.childCount; c++)
            {
                Transform child = dtrig.GetChild(c);
                if (!PosRx.IsMatch(child.name)) continue;
                sum += child.position;
                n++;
            }
            if (n == 0) continue;
            Vector3 center = sum / n;
            float dist = HorizontalDistance(center, pos);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = delayed;
            }
        }
        if (best != null && bestDist <= 25f)
            return best;
        return null;
    }

    static TriggerData GetTriggerAt(TurnManager tm, int index)
    {
        var field = typeof(TurnManager).GetField("listOfTriggers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = field.GetValue(tm) as List<TriggerData>;
        if (list == null || index < 0 || index >= list.Count) return null;
        return list[index];
    }

    static DelayedTriggerData GetDelayedAt(TurnManager tm, int index)
    {
        var field = typeof(TurnManager).GetField("listOfDelayedTriggers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = field.GetValue(tm) as List<DelayedTriggerData>;
        if (list == null || index < 0 || index >= list.Count) return null;
        return list[index];
    }

    public static DelayedTriggerData GetDelayedTrigger(TurnManager tm, int index) => GetDelayedAt(tm, index);

    static bool ContainsPointXZ(GameObject triggerObject, Vector3 pos)
    {
        if (triggerObject == null) return false;
        Collider col = triggerObject.GetComponent<Collider>();
        if (col == null) col = triggerObject.GetComponentInChildren<Collider>(true);
        if (col == null) return false;
        Bounds b = col.bounds;
        return pos.x >= b.min.x && pos.x <= b.max.x && pos.z >= b.min.z && pos.z <= b.max.z;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    static WarFog FindWarFogContaining(Vector3 pos)
    {
        WarFog best = null;
        float bestDist = float.MaxValue;
        foreach (var fog in Object.FindObjectsByType<WarFog>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Collider col = fog.GetComponent<Collider>();
            if (col == null) continue;
            Bounds b = col.bounds;
            bool inside = pos.x >= b.min.x && pos.x <= b.max.x && pos.z >= b.min.z && pos.z <= b.max.z;
            float dist = HorizontalDistance(b.center, pos);
            if (inside)
                return fog;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = fog;
            }
        }
        if (best != null && bestDist <= 8f)
            return best;
        return null;
    }
}

public static class UnitSpotBinderMenu
{
    [MenuItem("Space-Tycoon/Unit Spot/Create Binder At Selection")]
    public static void CreateBinderAtSelection()
    {
        Transform parent = Selection.activeTransform;
        GameObject go = new GameObject("UnitSpot");
        Undo.RegisterCreatedObjectUndo(go, "Create UnitSpot");
        if (parent != null)
            go.transform.SetParent(parent, false);
        go.transform.position = parent != null
            ? new Vector3(parent.position.x, 0f, parent.position.z)
            : Vector3.zero;
        go.AddComponent<UnitSpotBinder>();
        Selection.activeGameObject = go;
    }

    [MenuItem("Space-Tycoon/Unit Spot/Bind All In Scene")]
    public static void BindAll()
    {
        int binders = 0;
        foreach (var binder in Object.FindObjectsByType<UnitSpotBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            UnitSpotBinderEditor.Bind(binder, force: true);
            binders++;
        }
        int enemies = UnitSpotBinderEditor.BindAllSceneEnemiesByPosition();
        Debug.Log("[UnitSpotBinder] Bind All done: binders=" + binders + " sceneEnemies=" + enemies);
    }

    [MenuItem("Space-Tycoon/Unit Spot/Remove Selected")]
    public static void RemoveSelected()
    {
        UnitSpotBinderEditor.RemoveSelected();
    }

    [MenuItem("Space-Tycoon/Unit Spot/Convert Selected To Delayed Pose")]
    public static void ConvertSelectedToDelayed()
    {
        if (Application.isPlaying) return;
        TurnManager tm = Object.FindFirstObjectByType<TurnManager>();
        if (tm == null)
        {
            Debug.LogWarning("[UnitSpotBinder] TurnManager not found");
            return;
        }

        int converted = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            PawnBrain brain = go.GetComponentInParent<PawnBrain>();
            if (brain == null || UnitSpotBinderEditor.IsPlayerPawn(brain)) continue;
            DelayedTriggerData delayed = UnitSpotBinderEditor.GetDelayedTrigger(tm, GuessDelayedIndex(brain.transform.position, tm));
            if (delayed == null)
            {
                Debug.LogWarning("[UnitSpotBinder] no delayed trigger for " + brain.name);
                continue;
            }
            if (UnitSpotBinderEditor.ConvertSceneEnemyToDelayedPose(brain.gameObject, tm, delayed, null) != null)
                converted++;
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[UnitSpotBinder] converted to delayed: " + converted);
    }

    static int GuessDelayedIndex(Vector3 pos, TurnManager tm)
    {
        DelayedTriggerData best = null;
        int bestIndex = -1;
        float bestDist = float.MaxValue;
        var field = typeof(TurnManager).GetField("listOfDelayedTriggers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = field.GetValue(tm) as List<DelayedTriggerData>;
        if (list == null) return -1;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null || list[i].triggerObject == null) continue;
            Transform dtrig = list[i].triggerObject.transform;
            Vector3 sum = Vector3.zero;
            int n = 0;
            for (int c = 0; c < dtrig.childCount; c++)
            {
                sum += dtrig.GetChild(c).position;
                n++;
            }
            if (n == 0) continue;
            float dist = Vector3.Distance(new Vector3(sum.x / n, 0f, sum.z / n), new Vector3(pos.x, 0f, pos.z));
            if (dist < bestDist)
            {
                bestDist = dist;
                best = list[i];
                bestIndex = i;
            }
        }
        return bestIndex;
    }
}
